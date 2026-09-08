using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FacturacionElectronicaService : IFacturacionElectronicaService
{
    private readonly ComercialDbContext _db;

    // La API de TusFacturas rechaza comprobantes con más de 130 líneas de detalle.
    // Para Facturas se reserva 1 lugar (129 productos máx. por parte) porque, si la venta
    // tiene recargo, se agrega una línea sintética "Recargo" adicional por comprobante.
    private const int MaxItemsPorParteFactura = 129;
    // Las Notas de Crédito (rama con detalle real) nunca agregan línea sintética de recargo,
    // así que pueden usar el límite completo de la API.
    private const int MaxItemsPorParteNC = 130;

    public FacturacionElectronicaService(ComercialDbContext db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  EMITIR FACTURA DE VENTA
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<FacturaElectronicaResultDto> EmitirFacturaVentaAsync(long ventaId)
    {
        var result = new FacturaElectronicaResultDto();
        try
        {
            // ── Parámetros ───────────────────────────────────────────────────
            var prm = await CargarParametrosFEAsync();

            if (string.IsNullOrEmpty(prm.UserToken) || string.IsNullOrEmpty(prm.ApiKey) || string.IsNullOrEmpty(prm.ApiToken))
            { result.Errores.Add("Falta configurar credenciales de factura electrónica (fe_userToken, fe_apiKey, fe_apiToken)."); return result; }
            if (prm.PuntoVenta == 0)
            { result.Errores.Add("Debe configurar el punto de venta para este equipo."); return result; }

            // ── Datos de la venta ────────────────────────────────────────────
            var ventaData = await GetVentaParaFEAsync(ventaId);
            if (ventaData == null) { result.Errores.Add("Venta no encontrada."); return result; }
            if (ventaData.Detalle.Count == 0) { result.Errores.Add("La venta no tiene detalle."); return result; }

            bool esConsumFinal = ventaData.FkCliente.HasValue && ventaData.FkCliente.Value == prm.ClienteCFId;

            string tipoCuenta = esConsumFinal ? "DNI" : "CUIT";
            string nroCuenta  = esConsumFinal ? "99999999" : (ventaData.Cuil ?? "0");
            string letra      = (ventaData.CondIvaLetra ?? "B").ToUpper();
            string fecha      = DateTime.Now.ToString("dd/MM/yyyy");

            // ── Dividir en partes de ≤130 ítems si corresponde ────────────────
            // Si la venta tiene ≤130 ítems, esto produce una única "parte" con todo el
            // detalle: el resto del método ejecuta exactamente el mismo camino que antes
            // de soportar la división, una sola vez, sin lógica de reintento/salteo.
            var partes = ChunkList(ventaData.Detalle, MaxItemsPorParteFactura);
            int totalPartes = partes.Count;

            if (totalPartes == 1)
            {
                var item = await EmitirComprobanteFacturaChunkAsync(
                    ventaId, ventaData, partes[0], 1, 1, prm, nroCuenta, letra, fecha);

                result.Comprobantes.Add(item);
                AplicarUltimoComprobante(result, item);

                if (!item.Ok)
                {
                    result.Errores.Add(item.Error ?? "Error desconocido.");
                    await GuardarErrorFEAsync((int)ventaId, item.Error ?? "Error desconocido.");
                }
                return result;
            }

            // ── Más de 130 ítems: emitir una parte por vez, en orden ──────────
            bool huboFallo = false;
            for (int i = 0; i < totalPartes; i++)
            {
                int parte = i + 1;

                // Reintento seguro: si esta parte ya fue emitida en un intento anterior
                // (ej. la venta se facturó parcialmente y falló a mitad de camino), no
                // volver a emitirla — evita duplicar/doble-facturar los mismos productos.
                var existente = await _db.ComprobantesFiscales.AsNoTracking().FirstOrDefaultAsync(cf =>
                    cf.TipoComprobante == "Factura" &&
                    cf.NroReferencia   == (int)ventaId &&
                    cf.NroParte        == parte);

                var item = existente != null
                    ? ComprobanteExistenteAItem(existente, parte, totalPartes, partes[i].Count)
                    : await EmitirComprobanteFacturaChunkAsync(
                        ventaId, ventaData, partes[i], parte, totalPartes, prm, nroCuenta, letra, fecha);

                result.Comprobantes.Add(item);

                if (!item.Ok)
                {
                    huboFallo = true;
                    await GuardarErrorFEAsync((int)ventaId, $"Parte {parte}/{totalPartes}: {item.Error}");
                    break; // no continuar con las partes siguientes
                }
            }

            var ultimoOk = result.Comprobantes.LastOrDefault(c => c.Ok);
            if (ultimoOk != null) AplicarUltimoComprobante(result, ultimoOk);

            result.Ok = !huboFallo && result.Comprobantes.Count == totalPartes;
            if (!result.Ok)
            {
                var cantOk = result.Comprobantes.Count(c => c.Ok);
                var fallo  = result.Comprobantes.LastOrDefault(c => !c.Ok);
                result.Errores.Add(
                    $"Se emitieron {cantOk} de {totalPartes} comprobantes. " +
                    (fallo != null ? $"Falló la parte {fallo.Parte}: {fallo.Error}" : ""));
            }
        }
        catch (Exception ex)
        {
            result.Errores.Add($"Error técnico: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Arma y emite UN comprobante de Factura para el subconjunto <paramref name="chunkDetalle"/>
    /// del detalle de la venta. Contiene exactamente la lógica de cálculo/armado/envío/persistencia
    /// que existía antes de soportar la división en partes — se reutiliza sin cambios, una vez por
    /// cada parte. No lanza excepciones: cualquier error queda reflejado en el resultado devuelto.
    /// </summary>
    private async Task<ComprobanteEmitidoResultItemDto> EmitirComprobanteFacturaChunkAsync(
        long ventaId, VentaImpresionDto ventaData, List<VentaDetalleImpresionItemDto> chunkDetalle,
        int parte, int totalPartes, ParametrosFE prm, string nroCuenta, string letra, string fecha)
    {
        var item = new ComprobanteEmitidoResultItemDto { Parte = parte, TotalPartes = totalPartes, CantidadItems = chunkDetalle.Count };
        try
        {
            bool esConsumFinal = ventaData.FkCliente.HasValue && ventaData.FkCliente.Value == prm.ClienteCFId;
            string tipoCuenta  = esConsumFinal ? "DNI" : "CUIT";

            // ── Recorrer detalle — acumular neto, IVA y recargo ──────────────
            decimal neto = 0m, ivaTotal = 0m, recargoTotal = 0m;
            var detalleFE = new List<TfDetalle>();

            foreach (var d in chunkDetalle)
            {
                decimal cantidad       = Math.Round(d.Cantidad, 2);
                decimal precioUnitario = Math.Round(ventaData.Iva == 0 ? d.PrecioSinIva / 1.21m : d.PrecioSinIva, 3);
                decimal bonif          = Math.Round(d.Descuento ?? 0m, 2);
                decimal alicuota       = ventaData.Iva == 0 ? 21m : ventaData.Iva;

                decimal subtotal = precioUnitario * cantidad;
                if (bonif > 0) subtotal -= subtotal * (bonif / 100m);

                neto     += subtotal;
                ivaTotal += Math.Round(subtotal * (alicuota / 100m), 2);

                if ((d.Recargo ?? 0m) > 0)
                    recargoTotal += subtotal * (d.Recargo!.Value / 100m);

                string codigoProd = prm.CodigoDetalle switch
                {
                    "CodProveedor" => d.CodProveedor ?? d.FkProducto.ToString(),
                    "CodBarras"    => d.CodBarras    ?? d.FkProducto.ToString(),
                    _              => d.FkProducto.ToString()
                };

                detalleFE.Add(new TfDetalle
                {
                    cantidad                = cantidad,
                    afecta_stock            = "S",
                    bonificacion_porcentaje = bonif,
                    producto = new TfProducto
                    {
                        descripcion             = d.Descripcion ?? "",
                        unidad_bulto            = 1,
                        lista_precios           = "Lista de Precios",
                        codigo                  = codigoProd,
                        precio_unitario_sin_iva = precioUnitario,
                        alicuota                = alicuota,
                        unidad_medida           = 7,
                        actualiza_precio        = "N",
                        rg5329                  = "N"
                    }
                });
            }

            // ── Recargo global como línea separada ───────────────────────────
            if (recargoTotal > 0)
            {
                decimal recargoRedondeado = Math.Round(recargoTotal, 3);
                neto += recargoTotal;
                decimal alicuotaRec = ventaData.Iva == 0 ? 21m : ventaData.Iva;

                detalleFE.Add(new TfDetalle
                {
                    cantidad                = 1,
                    afecta_stock            = "S",
                    bonificacion_porcentaje = 0m,
                    producto = new TfProducto
                    {
                        descripcion             = "Recargo",
                        unidad_bulto            = 1,
                        lista_precios           = "Lista de Precios",
                        codigo                  = "1",
                        precio_unitario_sin_iva = recargoRedondeado,
                        alicuota                = alicuotaRec,
                        unidad_medida           = 7,
                        actualiza_precio        = "N",
                        rg5329                  = "N"
                    }
                });
                ivaTotal += Math.Round(recargoRedondeado * (alicuotaRec / 100m), 2);
            }

            // ── Bonificación general (descuento general sobre Total S/IVA) ────
            // Solo en modo por línea: el descuento general es aditivo (no está en las líneas).
            // Se aplica sobre el neto de ESTA parte (por linealidad, la suma de las partes da
            // el mismo resultado que aplicarlo una vez sobre el total de la venta).
            decimal bonifGeneral = 0m;
            if (prm.BonificacionPorLinea && (ventaData.Descuento ?? 0m) > 0m)
            {
                decimal alicIva = ventaData.Iva == 0 ? 21m : ventaData.Iva;
                bonifGeneral = Math.Round(neto * (ventaData.Descuento!.Value / 100m), 2);
                neto    -= bonifGeneral;
                ivaTotal = Math.Round(neto * (alicIva / 100m), 2);
            }

            // ── Tributos (Ingresos Brutos) ───────────────────────────────────
            decimal tributosTotal = 0m;
            List<TfTributo>? tributos = null;

            if (ventaData.Impuesto > 0 && !string.IsNullOrEmpty(prm.TributoIIBB))
            {
                decimal alicuotaTributo = Math.Round(ventaData.Impuesto, 2);
                decimal totalTributo    = Math.Round(neto * (alicuotaTributo / 100m), 2);
                tributosTotal           = totalTributo;
                tributos = new List<TfTributo>
                {
                    new() { tipo = int.TryParse(prm.TributoIIBB, out var t) ? t : 0,
                            regimen = int.TryParse(prm.RegimenIIBB, out var r) ? r : 0,
                            base_imponible = neto, alicuota = alicuotaTributo, total = totalTributo }
                };
            }

            decimal totalFinal = Math.Round(neto + ivaTotal + tributosTotal, 2);

            // ── Armar request ────────────────────────────────────────────────
            var req = new TfFacturaRequest
            {
                usertoken = prm.UserToken,
                apikey    = prm.ApiKey,
                apitoken  = prm.ApiToken,
                cliente = new TfCliente
                {
                    documento_tipo = tipoCuenta,
                    documento_nro  = nroCuenta,
                    razon_social   = ventaData.RazonSocial ?? ventaData.NombreCliente ?? "",
                    domicilio      = ventaData.DireccionCliente ?? "",
                    provincia      = MapProvincia(ventaData.Provincia ?? ""),
                    codigo         = $"Clie{nroCuenta}",
                    envia_por_mail      = prm.EnviarMail ? "S" : "N",
                    email               = prm.EnviarMail ? ventaData.Email : null,
                    condicion_pago      = "214",
                    condicion_pago_otra = ventaData.FormaPago,
                    condicion_iva       = ventaData.CondIvaAbrevFE ?? ventaData.CondIvaAbrev ?? "CF",
                    rg5329              = "N"
                },
                comprobante = new TfComprobante
                {
                    fecha                   = fecha,
                    tipo                    = $"FACTURA {letra}",
                    operacion               = "V",
                    idioma                  = "1",
                    punto_venta             = prm.PuntoVenta.ToString(),
                    moneda                  = "PES",
                    cotizacion              = "1",
                    vencimiento             = fecha,
                    periodo_facturado_desde = fecha,
                    periodo_facturado_hasta = fecha,
                    rubro                   = prm.RubroFE,
                    rubro_grupo_contable    = $"{DateTime.Now.Month}/{DateTime.Now.Year}",
                    total                   = totalFinal,
                    bonificacion            = bonifGeneral,
                    detalle                 = detalleFE,
                    tributos                = tributos
                }
            };

            // ── Enviar con reintentos ────────────────────────────────────────
            var (ok, respuesta, erroresStr) = await EnviarConReintentoAsync(req);

            if (ok && respuesta != null)
            {
                item.Ok                = true;
                item.Cae               = respuesta.cae?.Trim();
                item.VencimientoCae    = respuesta.vencimiento_cae;
                item.NumeroComprobante = respuesta.comprobante_nro;
                item.PdfUrl            = respuesta.comprobante_pdf_url;
                item.QrAfip            = respuesta.afip_qr;
                item.Importe           = totalFinal;

                if (!string.IsNullOrEmpty(item.Cae))
                {
                    _db.ComprobantesFiscales.Add(new ComprobanteFiscal
                    {
                        TipoComprobante     = "Factura",
                        Letra               = letra,
                        PuntoVenta          = prm.PuntoVenta,
                        Numero              = ParseNumeroComprobante(item.NumeroComprobante),
                        FechaEmision        = DateTime.Now,
                        NroReferencia       = (int)ventaId,
                        NroParte            = totalPartes > 1 ? parte : (int?)null,
                        FkCliente           = ventaData.FkCliente,
                        RazonSocial         = ventaData.RazonSocial ?? ventaData.NombreCliente,
                        Cuit                = nroCuenta,
                        ImporteTotal        = totalFinal,
                        Estado              = "Emitido",
                        Cae                 = item.Cae,
                        FechaVencimientoCae = ParseFechaFE(item.VencimientoCae),
                        LinkPdf             = item.PdfUrl,
                        AfipQr              = item.QrAfip,
                        CreatedAt           = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                }

                // Descarga automática del PDF al directorio Descargas del usuario
                string sufijo = totalPartes > 1 ? $"_Parte{parte}de{totalPartes}" : "";
                await _DescargarPdfAsync(
                    item.PdfUrl,
                    $"Factura_{letra}_{ParseNumeroComprobante(item.NumeroComprobante)}{sufijo}_{DateTime.Now:yyyyMMdd}");
            }
            else
            {
                item.Ok    = false;
                item.Error = erroresStr ?? "Error desconocido.";
            }
        }
        catch (Exception ex)
        {
            item.Ok    = false;
            item.Error = $"Error técnico: {ex.Message}";
        }
        return item;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  EMITIR NOTA DE CRÉDITO MANUAL (unaDevolucion == 0 en escritorio)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<NotaCreditoElectronicaResultDto> EmitirNotaCreditoManualAsync(
        NotaCreditoRequestDto dto, int puntoVenta)
    {
        var result = new NotaCreditoElectronicaResultDto();
        try
        {
            var prm = await CargarParametrosFEAsync();
            if (string.IsNullOrEmpty(prm.UserToken) || string.IsNullOrEmpty(prm.ApiKey) || string.IsNullOrEmpty(prm.ApiToken))
            { result.Error = "Parámetros de facturación electrónica incompletos."; return result; }

            // ── Datos fiscales del cliente ────────────────────────────────────
            var datosCliente = await (
                from c  in _db.Clientes
                join ci in _db.CondIvas    on c.FkCondIva   equals ci.Id  into cij from ci in cij.DefaultIfEmpty()
                join l  in _db.Localidades on c.FkLocalidad equals l.Id   into lj  from l  in lj.DefaultIfEmpty()
                join p  in _db.Provincias  on (int?)l.FkProvincia equals p.Id into pj from p in pj.DefaultIfEmpty()
                where c.Id == dto.ClienteId
                select new
                {
                    ClienteId   = c.Id,
                    c.RazonSocial, c.Cuil, c.Direccion, c.Email,
                    Localidad   = l  != null ? l.Nombre  : null,
                    Provincia   = p  != null ? p.Nombre  : null,
                    Letra       = ci != null ? ci.Letra   : null,
                    AbrevFE     = ci != null ? (ci.AbrevFE ?? ci.Abrev) : null
                }
            ).FirstOrDefaultAsync();

            if (datosCliente == null)
            { result.Error = "No se encontraron datos fiscales del cliente."; return result; }

            bool esCF = datosCliente.ClienteId == prm.ClienteCFId;

            string letra        = (datosCliente.Letra ?? "B").ToUpper();
            string tipoNC       = $"NOTA DE CREDITO {letra}";
            string tipoFactAsoc = $"FACTURA {letra}";
            string nroCuenta    = esCF ? "99999999" : (datosCliente.Cuil ?? "0");

            long cuit = 0;
            if (!esCF && !string.IsNullOrEmpty(datosCliente.Cuil))
                long.TryParse(datosCliente.Cuil.Replace("-", ""), out cuit);

            decimal alicuotaIva = dto.IvaPorcentaje == 0 ? 21m : dto.IvaPorcentaje;
            string fecha = DateTime.Now.ToString("dd/MM/yyyy");

            if (dto.IdDevolucion > 0)
            {
                // ── Detalle real obtenido de DevolucionesDetalles, orden estable ──
                // (indispensable para que "saltear partes ya emitidas" sea consistente
                // entre el intento original y un reintento posterior).
                var devDetalleTodo = await _db.DevolucionesDetalles
                    .Where(d => d.FkDevolucion == dto.IdDevolucion)
                    .OrderBy(d => d.Linea)
                    .ToListAsync();

                decimal? descGeneralDevolucion = null;
                if (prm.BonificacionPorLinea)
                {
                    descGeneralDevolucion = await _db.Devoluciones
                        .Where(x => x.Id == dto.IdDevolucion)
                        .Select(x => x.Descuento)
                        .FirstOrDefaultAsync();
                }

                var partes = ChunkList(devDetalleTodo, MaxItemsPorParteNC);
                int totalPartes = partes.Count;

                bool huboFallo = false;
                for (int i = 0; i < totalPartes; i++)
                {
                    int parte = i + 1;
                    ComprobanteEmitidoResultItemDto item;

                    if (totalPartes > 1)
                    {
                        var existente = await _db.ComprobantesFiscales.AsNoTracking().FirstOrDefaultAsync(cf =>
                            cf.TipoComprobante == "Nota de Crédito" &&
                            cf.NroReferencia   == dto.IdDevolucion &&
                            cf.NroParte        == parte);

                        if (existente != null)
                        {
                            item = ComprobanteExistenteAItem(existente, parte, totalPartes, partes[i].Count);
                            result.Comprobantes.Add(item);
                            continue;
                        }
                    }

                    item = await EmitirComprobanteNCChunkAsync(
                        dto, puntoVenta, partes[i], parte, totalPartes, prm,
                        letra, tipoNC, tipoFactAsoc, nroCuenta, esCF, cuit, alicuotaIva, fecha,
                        datosCliente.ClienteId, datosCliente.RazonSocial, datosCliente.Direccion,
                        datosCliente.Email, datosCliente.Provincia, datosCliente.AbrevFE,
                        descGeneralDevolucion);
                    result.Comprobantes.Add(item);

                    if (!item.Ok)
                    {
                        huboFallo = true;
                        var prefijo = totalPartes > 1 ? $"Parte {parte}/{totalPartes}: " : "";
                        await GuardarErrorFEAsync(0, $"{prefijo}{item.Error}");
                        break;
                    }
                }

                var ultimoOk = result.Comprobantes.LastOrDefault(c => c.Ok);
                result.PdfUrl = ultimoOk?.PdfUrl;
                result.Ok     = !huboFallo && result.Comprobantes.Count == totalPartes;

                if (!result.Ok)
                {
                    var cantOk = result.Comprobantes.Count(c => c.Ok);
                    var fallo  = result.Comprobantes.LastOrDefault(c => !c.Ok);
                    result.Error = totalPartes > 1
                        ? $"Se emitieron {cantOk} de {totalPartes} notas de crédito. " +
                          (fallo != null ? $"Falló la parte {fallo.Parte}: {fallo.Error}" : "")
                        : fallo?.Error;
                }
                return result;
            }
            else
            {
                // ── Comportamiento original, sin cambios: línea única "Productos Varios" ──
                // (siempre 1 sola línea; nunca puede superar el límite de 130, no se divide)
                decimal neto = Math.Round(
                    dto.Importe / (1m + (dto.IvaPorcentaje / 100m) + (dto.IIBBPorcentaje / 100m)), 2);
                decimal ivaTotal = Math.Round(neto * alicuotaIva / 100m, 2);

                var detalleFE = new List<TfDetalle>
                {
                    new()
                    {
                        cantidad                = 1,
                        afecta_stock            = "S",
                        bonificacion_porcentaje = 0,
                        producto = new TfProducto
                        {
                            descripcion             = "Productos Varios",
                            unidad_bulto            = 1,
                            lista_precios           = "Lista de Precios",
                            codigo                  = "1",
                            precio_unitario_sin_iva = neto,
                            alicuota                = alicuotaIva,
                            unidad_medida           = 7,
                            actualiza_precio        = "N",
                            rg5329                  = "N"
                        }
                    }
                };

                var req = new TfFacturaRequest
                {
                    usertoken = prm.UserToken,
                    apikey    = prm.ApiKey,
                    apitoken  = prm.ApiToken,
                    cliente = new TfCliente
                    {
                        documento_tipo = esCF ? "DNI" : "CUIT",
                        documento_nro  = nroCuenta,
                        razon_social   = datosCliente.RazonSocial ?? "",
                        domicilio      = datosCliente.Direccion ?? "",
                        provincia      = MapProvincia(datosCliente.Provincia ?? ""),
                        codigo         = $"Clie{nroCuenta}",
                        envia_por_mail = prm.EnviarMail ? "S" : "N",
                        email          = prm.EnviarMail ? datosCliente.Email : null,
                        condicion_pago = "201",
                        condicion_iva  = datosCliente.AbrevFE ?? "CF",
                        rg5329         = "N"
                    },
                    comprobante = new TfComprobante
                    {
                        fecha                   = fecha,
                        tipo                    = tipoNC,
                        operacion               = "V",
                        idioma                  = "1",
                        punto_venta             = puntoVenta.ToString("D4"),
                        moneda                  = "PES",
                        cotizacion              = "1",
                        vencimiento             = fecha,
                        periodo_facturado_desde = fecha,
                        periodo_facturado_hasta = fecha,
                        rubro                   = prm.RubroFE,
                        rubro_grupo_contable    = $"{DateTime.Now.Month}/{DateTime.Now.Year}",
                        total                   = dto.Importe,   // se recalcula abajo
                        comprobantes_asociados = new List<TfComprobanteAsociado>
                        {
                            new()
                            {
                                tipo_comprobante  = tipoFactAsoc,
                                punto_venta       = puntoVenta,
                                numero            = dto.FacturaAsociada ?? 0,
                                comprobante_fecha = dto.FechaFacturaAsoc ?? fecha,
                                cuit              = cuit
                            }
                        },
                        detalle = detalleFE
                    }
                };

                // ── Tributo IIBB ─────────────────────────────────────────────────
                if (dto.IIBBPorcentaje > 0 && !string.IsNullOrEmpty(prm.TributoIIBB))
                {
                    var totalTributo = Math.Round(neto * (dto.IIBBPorcentaje / 100m), 2);
                    req.comprobante.tributos = new List<TfTributo>
                    {
                        new()
                        {
                            tipo           = int.TryParse(prm.TributoIIBB, out var t) ? t : 0,
                            regimen        = int.TryParse(prm.RegimenIIBB, out var r) ? r : 0,
                            base_imponible = neto,
                            alicuota       = dto.IIBBPorcentaje,
                            total          = totalTributo
                        }
                    };
                }

                // ── Total del comprobante ─────────────────────────────────────────
                decimal iibbTotal = dto.IIBBPorcentaje > 0 ? Math.Round(neto * dto.IIBBPorcentaje / 100m, 2) : 0m;
                req.comprobante.total = Math.Round(neto + ivaTotal + iibbTotal, 2);

                // ── Enviar con reintentos ─────────────────────────────────────────
                var (ok, respuesta, erroresStr) = await EnviarConReintentoAsync(req);

                var item = new ComprobanteEmitidoResultItemDto { Parte = 1, TotalPartes = 1, CantidadItems = 1 };

                if (ok && respuesta != null)
                {
                    item.Ok                = true;
                    item.Cae               = respuesta.cae?.Trim();
                    item.VencimientoCae    = respuesta.vencimiento_cae;
                    item.NumeroComprobante = respuesta.comprobante_nro;
                    item.PdfUrl            = respuesta.comprobante_pdf_url;
                    item.QrAfip            = respuesta.afip_qr;
                    item.Importe           = req.comprobante.total;

                    _db.ComprobantesFiscales.Add(new ComprobanteFiscal
                    {
                        TipoComprobante     = "Nota de Crédito",
                        Letra               = letra,
                        PuntoVenta          = puntoVenta,
                        Numero              = ParseNumeroComprobante(respuesta.comprobante_nro),
                        FechaEmision        = DateTime.Now,
                        NroReferencia       = 0, // NC manual sin devolución asociada: sin referencia
                        FkCliente           = datosCliente.ClienteId,
                        RazonSocial         = datosCliente.RazonSocial,
                        Cuit                = nroCuenta,
                        ImporteTotal        = dto.Importe,
                        Estado              = "Emitido",
                        Cae                 = respuesta.cae?.Trim(),
                        FechaVencimientoCae = ParseFechaFE(respuesta.vencimiento_cae),
                        AfipQr              = respuesta.afip_qr,
                        LinkPdf             = respuesta.comprobante_pdf_url,
                        CreatedAt           = DateTime.Now
                    });
                    await _db.SaveChangesAsync();

                    // Descarga automática del PDF al directorio Descargas del usuario
                    await _DescargarPdfAsync(
                        respuesta.comprobante_pdf_url,
                        $"NC_{letra}_{ParseNumeroComprobante(respuesta.comprobante_nro)}_{DateTime.Now:yyyyMMdd}");

                    result.Ok     = true;
                    result.PdfUrl = respuesta.comprobante_pdf_url;
                }
                else
                {
                    item.Ok    = false;
                    item.Error = erroresStr ?? "Error desconocido.";
                    await GuardarErrorFEAsync(0, erroresStr ?? "Error desconocido.");

                    result.Ok    = false;
                    result.Error = $"Error FE: {erroresStr}";
                }

                result.Comprobantes.Add(item);
                return result;
            }
        }
        catch (Exception ex)
        {
            result.Error = $"Error inesperado: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Arma y emite UNA Nota de Crédito para el subconjunto <paramref name="chunkDetalle"/> del
    /// detalle de la devolución. Misma lógica de cálculo que existía antes de soportar la
    /// división en partes, reutilizada sin cambios una vez por cada parte.
    /// </summary>
    private async Task<ComprobanteEmitidoResultItemDto> EmitirComprobanteNCChunkAsync(
        NotaCreditoRequestDto dto, int puntoVenta,
        List<DevolucionesDetalle> chunkDetalle, int parte, int totalPartes,
        ParametrosFE prm, string letra, string tipoNC, string tipoFactAsoc, string nroCuenta,
        bool esCF, long cuit, decimal alicuotaIva, string fecha,
        int clienteId, string? razonSocial, string? direccion, string? email, string? provincia, string? abrevFE,
        decimal? descGeneralDevolucion)
    {
        var item = new ComprobanteEmitidoResultItemDto { Parte = parte, TotalPartes = totalPartes, CantidadItems = chunkDetalle.Count };
        try
        {
            decimal neto = 0m, ivaTotal = 0m;
            var detalleFE = new List<TfDetalle>();

            foreach (var d in chunkDetalle)
            {
                decimal cantidad       = Math.Round(d.Cantidad ?? 0m, 2);
                decimal precioUnitario = Math.Round(
                    alicuotaIva == 0
                        ? (d.PrecioSinIva ?? 0m) / 1.21m
                        : (d.PrecioSinIva ?? 0m), 3);
                decimal bonif    = Math.Round(d.Descuento ?? 0m, 2);

                decimal subtotal = precioUnitario * cantidad;
                if (bonif > 0) subtotal -= subtotal * (bonif / 100m);

                neto     += subtotal;
                ivaTotal += Math.Round(subtotal * (alicuotaIva / 100m), 2);

                string codigoProd = prm.CodigoDetalle switch
                {
                    "CodProveedor" => d.CodProveedor ?? d.FkProducto?.ToString() ?? "1",
                    "CodBarras"    => d.CodBarras    ?? d.FkProducto?.ToString() ?? "1",
                    _              => d.FkProducto?.ToString() ?? "1"
                };

                detalleFE.Add(new TfDetalle
                {
                    cantidad                = cantidad,
                    afecta_stock            = "S",
                    bonificacion_porcentaje = bonif,
                    producto = new TfProducto
                    {
                        descripcion             = d.Descripcion ?? "",
                        unidad_bulto            = 1,
                        lista_precios           = "Lista de Precios",
                        codigo                  = codigoProd,
                        precio_unitario_sin_iva = precioUnitario,
                        alicuota                = alicuotaIva,
                        unidad_medida           = 7,
                        actualiza_precio        = "N",
                        rg5329                  = "N"
                    }
                });
            }

            // ── Bonificación general (descuento general de la devolución) ──
            // Solo en modo por línea: se aplica sobre el neto de ESTA parte y sobre ese
            // nuevo neto se recalcula el IVA. Mismo % que quedó en la venta original.
            decimal bonifGeneral = 0m;
            if (descGeneralDevolucion.HasValue && descGeneralDevolucion.Value > 0m)
            {
                bonifGeneral = Math.Round(neto * (descGeneralDevolucion.Value / 100m), 2);
                neto    -= bonifGeneral;
                ivaTotal = Math.Round(neto * (alicuotaIva / 100m), 2);
            }

            var req = new TfFacturaRequest
            {
                usertoken = prm.UserToken,
                apikey    = prm.ApiKey,
                apitoken  = prm.ApiToken,
                cliente = new TfCliente
                {
                    documento_tipo = esCF ? "DNI" : "CUIT",
                    documento_nro  = nroCuenta,
                    razon_social   = razonSocial ?? "",
                    domicilio      = direccion ?? "",
                    provincia      = MapProvincia(provincia ?? ""),
                    codigo         = $"Clie{nroCuenta}",
                    envia_por_mail = prm.EnviarMail ? "S" : "N",
                    email          = prm.EnviarMail ? email : null,
                    condicion_pago = "201",
                    condicion_iva  = abrevFE ?? "CF",
                    rg5329         = "N"
                },
                comprobante = new TfComprobante
                {
                    fecha                   = fecha,
                    tipo                    = tipoNC,
                    operacion               = "V",
                    idioma                  = "1",
                    punto_venta             = puntoVenta.ToString("D4"),
                    moneda                  = "PES",
                    cotizacion              = "1",
                    vencimiento             = fecha,
                    periodo_facturado_desde = fecha,
                    periodo_facturado_hasta = fecha,
                    rubro                   = prm.RubroFE,
                    rubro_grupo_contable    = $"{DateTime.Now.Month}/{DateTime.Now.Year}",
                    total                   = 0m, // se recalcula abajo
                    bonificacion            = bonifGeneral,
                    comprobantes_asociados = new List<TfComprobanteAsociado>
                    {
                        new()
                        {
                            tipo_comprobante  = tipoFactAsoc,
                            punto_venta       = puntoVenta,
                            numero            = dto.FacturaAsociada ?? 0,
                            comprobante_fecha = dto.FechaFacturaAsoc ?? fecha,
                            cuit              = cuit
                        }
                    },
                    detalle = detalleFE
                }
            };

            // ── Tributo IIBB (sobre el neto de esta parte) ────────────────────
            if (dto.IIBBPorcentaje > 0 && !string.IsNullOrEmpty(prm.TributoIIBB))
            {
                var totalTributo = Math.Round(neto * (dto.IIBBPorcentaje / 100m), 2);
                req.comprobante.tributos = new List<TfTributo>
                {
                    new()
                    {
                        tipo           = int.TryParse(prm.TributoIIBB, out var t) ? t : 0,
                        regimen        = int.TryParse(prm.RegimenIIBB, out var r) ? r : 0,
                        base_imponible = neto,
                        alicuota       = dto.IIBBPorcentaje,
                        total          = totalTributo
                    }
                };
            }

            decimal iibbTotal = dto.IIBBPorcentaje > 0 ? Math.Round(neto * dto.IIBBPorcentaje / 100m, 2) : 0m;
            decimal totalFinal = Math.Round(neto + ivaTotal + iibbTotal, 2);
            req.comprobante.total = totalFinal;

            var (ok, respuesta, erroresStr) = await EnviarConReintentoAsync(req);

            if (ok && respuesta != null)
            {
                item.Ok                = true;
                item.Cae               = respuesta.cae?.Trim();
                item.VencimientoCae    = respuesta.vencimiento_cae;
                item.NumeroComprobante = respuesta.comprobante_nro;
                item.PdfUrl            = respuesta.comprobante_pdf_url;
                item.QrAfip            = respuesta.afip_qr;
                item.Importe           = totalFinal;

                _db.ComprobantesFiscales.Add(new ComprobanteFiscal
                {
                    TipoComprobante     = "Nota de Crédito",
                    Letra               = letra,
                    PuntoVenta          = puntoVenta,
                    Numero              = ParseNumeroComprobante(item.NumeroComprobante),
                    FechaEmision        = DateTime.Now,
                    NroReferencia       = dto.IdDevolucion,
                    NroParte            = totalPartes > 1 ? parte : (int?)null,
                    FkCliente           = clienteId,
                    RazonSocial         = razonSocial,
                    Cuit                = nroCuenta,
                    ImporteTotal        = totalFinal,
                    Estado              = "Emitido",
                    Cae                 = item.Cae,
                    FechaVencimientoCae = ParseFechaFE(item.VencimientoCae),
                    AfipQr              = item.QrAfip,
                    LinkPdf             = item.PdfUrl,
                    CreatedAt           = DateTime.Now
                });
                await _db.SaveChangesAsync();

                string sufijo = totalPartes > 1 ? $"_Parte{parte}de{totalPartes}" : "";
                await _DescargarPdfAsync(
                    item.PdfUrl,
                    $"NC_{letra}_{ParseNumeroComprobante(item.NumeroComprobante)}{sufijo}_{DateTime.Now:yyyyMMdd}");
            }
            else
            {
                item.Ok    = false;
                item.Error = erroresStr ?? "Error desconocido.";
            }
        }
        catch (Exception ex)
        {
            item.Ok    = false;
            item.Error = $"Error técnico: {ex.Message}";
        }
        return item;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FUNCIONES COMPARTIDAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Divide una lista en trozos contiguos de tamaño máximo <paramref name="tamano"/>.</summary>
    private static List<List<T>> ChunkList<T>(List<T> origen, int tamano)
    {
        var resultado = new List<List<T>>();
        for (int i = 0; i < origen.Count; i += tamano)
            resultado.Add(origen.GetRange(i, Math.Min(tamano, origen.Count - i)));
        if (resultado.Count == 0) resultado.Add(new List<T>());
        return resultado;
    }

    /// <summary>Copia los datos del último comprobante emitido con éxito a los campos "legacy" del resultado.</summary>
    private static void AplicarUltimoComprobante(FacturaElectronicaResultDto result, ComprobanteEmitidoResultItemDto item)
    {
        result.Ok                = item.Ok;
        result.Cae                = item.Cae;
        result.VencimientoCae     = item.VencimientoCae;
        result.NumeroComprobante  = item.NumeroComprobante;
        result.PdfUrl             = item.PdfUrl;
        result.QrAfip             = item.QrAfip;
    }

    /// <summary>Reconstruye el resultado de una parte a partir de un ComprobanteFiscal ya persistido (reintento).</summary>
    private static ComprobanteEmitidoResultItemDto ComprobanteExistenteAItem(
        ComprobanteFiscal cf, int parte, int totalPartes, int cantidadItems) => new()
    {
        Ok                = true,
        Parte             = parte,
        TotalPartes       = totalPartes,
        CantidadItems     = cantidadItems,
        Importe           = cf.ImporteTotal,
        Cae               = cf.Cae,
        VencimientoCae    = cf.FechaVencimientoCae?.ToString("dd/MM/yyyy"),
        NumeroComprobante = $"{cf.PuntoVenta:D4}-{cf.Numero:D8}",
        PdfUrl            = cf.LinkPdf,
        QrAfip            = cf.AfipQr
    };

    /// <summary>
    /// Envía el request a TusFacturas con un máximo de 2 intentos.
    /// Si el primer intento falla por "sumatorias finales", extrae el total
    /// correcto del error y reintenta una sola vez.
    /// </summary>
    private static async Task<(bool ok, TfFacturaResponse? respuesta, string? errores)>
        EnviarConReintentoAsync(TfFacturaRequest req)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        bool reintento = false;

        for (int intento = 0; intento < 2; intento++)
        {
            var json    = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp    = await http.PostAsync("https://www.tusfacturas.app/app/api/v2/facturacion/nuevo", content);
            var body    = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, null, $"Error HTTP {(int)resp.StatusCode}: {body[..Math.Min(300, body.Length)]}");

            var respuesta = JsonSerializer.Deserialize<TfFacturaResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (respuesta?.error == "N")
                return (true, respuesta, null);

            // Error
            string erroresStr = string.Join(" | ", respuesta?.errores ?? new List<string>());

            // Detectar diferencia de totales → reintento con total corregido
            if (!reintento && erroresStr.Contains("sumatorias finales"))
            {
                decimal? totalCorrecto = ObtenerTotalDesdeError(erroresStr);
                if (totalCorrecto.HasValue && req.comprobante != null)
                {
                    req.comprobante.total = totalCorrecto.Value;
                    reintento = true;
                    continue;
                }
            }

            // Error definitivo
            return (false, null, erroresStr);
        }

        return (false, null, "Se agotaron los reintentos.");
    }

    /// <summary>Carga los parámetros comunes de facturación electrónica.</summary>
    private async Task<ParametrosFE> CargarParametrosFEAsync()
    {
        var parametros = await _db.Parametros.ToListAsync();
        string? Get(string m, string p) =>
            parametros.FirstOrDefault(x => x.Modulo == m && x.Parametro1 == p)?.Valor;

        return new ParametrosFE
        {
            UserToken     = Get("facturacionElectronica", "userToken")             ?? "",
            ApiKey        = Get("facturacionElectronica", "apiKey")                ?? "",
            ApiToken      = Get("facturacionElectronica", "apiToken")              ?? "",
            PuntoVenta    = int.TryParse(
                            parametros.FirstOrDefault(x => x.Modulo == "PuntoVenta")?.Valor,
                            out var pv) ? pv : 0,
            RubroFE       = Get("facturacionElectronica", "rubro")                 ?? "",
            RegimenIIBB   = Get("facturacionElectronica", "regimenIIBB")           ?? "",
            TributoIIBB   = Get("facturacionElectronica", "tributoIIBB")           ?? "",
            EnviarMail    = Get("facturacionElectronica", "enviarFacturaPorMail") == "S",
            CodigoDetalle = Get("facturacionElectronica", "CodigoDetalle")         ?? "Descripcion",
            ClienteCFId   = int.TryParse(Get("ventas", "clienteConsumidorFinal"), out var cf) ? cf : 0,
            BonificacionPorLinea = Get("ventas", "bonificacionesPorDetalle") == "1"
        };
    }

    /// <summary>
    /// Extrae el total correcto desde el mensaje de error de TusFacturas
    /// cuando indica diferencia en "sumatorias finales (NNNN.NN)".
    /// </summary>
    private static decimal? ObtenerTotalDesdeError(string error)
    {
        try
        {
            if (string.IsNullOrEmpty(error)) return null;

            string clave = "sumatorias finales (";
            int index = error.IndexOf(clave);
            if (index == -1) return null;

            int inicioNumero = index + clave.Length;
            int finNumero = error.IndexOf(")", inicioNumero);
            if (finNumero == -1) return null;

            string numeroStr = error.Substring(inicioNumero, finNumero - inicioNumero).Trim();

            if (decimal.TryParse(numeroStr, System.Globalization.NumberStyles.Any,
                                 CultureInfo.InvariantCulture, out decimal total))
                return total;

            return null;
        }
        catch { return null; }
    }

    /// <summary>Parsea "0001-00001234" → 1234.</summary>
    private static int ParseNumeroComprobante(string? nro)
    {
        if (string.IsNullOrEmpty(nro)) return 0;
        var partes = nro.Split('-');
        if (partes.Length > 1 && int.TryParse(partes[1].TrimStart('0'), out var n)) return n;
        return 0;
    }

    /// <summary>Parsea "dd/MM/yyyy" → DateTime.</summary>
    private static DateTime? ParseFechaFE(string? fecha)
    {
        if (string.IsNullOrEmpty(fecha)) return null;
        return DateTime.TryParseExact(fecha, "dd/MM/yyyy",
            CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)
            ? dt : null;
    }

    /// <summary>Guarda un error de facturación electrónica en la tabla erroresFE.</summary>
    private async Task GuardarErrorFEAsync(int fkVenta, string error)
    {
        _db.ErroresFE.Add(new ErrorFe { FkVenta = fkVenta, Error = error });
        await _db.SaveChangesAsync();
    }

    /// <summary>Mapeo de nombre de provincia → código TusFacturas.</summary>
    internal static string MapProvincia(string nombre)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Buenos Aires"]        = "1",  ["Capital Federal"]     = "2",  ["CABA"]          = "2",
            ["Catamarca"]           = "3",  ["Chaco"]               = "4",  ["Chubut"]        = "5",
            ["Córdoba"]             = "6",  ["Cordoba"]             = "6",  ["Corrientes"]    = "7",
            ["Entre Ríos"]          = "8",  ["Entre Rios"]          = "8",  ["Formosa"]       = "9",
            ["Jujuy"]               = "10", ["La Pampa"]            = "11", ["La Rioja"]      = "12",
            ["Mendoza"]             = "13", ["Misiones"]            = "14", ["Neuquén"]       = "15",
            ["Neuquen"]             = "15", ["Río Negro"]           = "16", ["Rio Negro"]     = "16",
            ["Salta"]               = "17", ["San Juan"]            = "18", ["San Luis"]      = "19",
            ["Santa Cruz"]          = "20", ["Santa Fe"]            = "21",
            ["Santiago del Estero"] = "22", ["Tierra del Fuego"]    = "23",
            ["Tucumán"]             = "24", ["Tucuman"]             = "24",
        };
        return mapa.TryGetValue(nombre.Trim(), out var cod) ? cod : "1";
    }

    // ── DTO interno para parámetros FE ───────────────────────────────────────
    private class ParametrosFE
    {
        public string UserToken     { get; set; } = "";
        public string ApiKey        { get; set; } = "";
        public string ApiToken      { get; set; } = "";
        public int    PuntoVenta    { get; set; }
        public string RubroFE       { get; set; } = "";
        public string RegimenIIBB   { get; set; } = "";
        public string TributoIIBB   { get; set; } = "";
        public bool   EnviarMail    { get; set; }
        public string CodigoDetalle { get; set; } = "";
        public int    ClienteCFId   { get; set; }
        public bool   BonificacionPorLinea { get; set; }
    }

    // ── Descarga automática de PDF ───────────────────────────────────────────────
    /// <summary>
    /// Descarga el PDF del comprobante fiscal desde <paramref name="pdfUrl"/> y lo guarda
    /// en la carpeta Descargas del usuario con el nombre <paramref name="nombreBase"/>.pdf.
    /// Si la URL es vacía o la descarga falla, no lanza excepción ni afecta el flujo de emisión.
    /// </summary>
    private static async Task _DescargarPdfAsync(string? pdfUrl, string nombreBase)
    {
        if (string.IsNullOrWhiteSpace(pdfUrl)) return;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var bytes = await http.GetByteArrayAsync(pdfUrl);

            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            Directory.CreateDirectory(carpeta);   // no-op si ya existe

            // Nombre único para evitar colisiones
            var nombreArchivo = $"{nombreBase}.pdf";
            var rutaCompleta  = Path.Combine(carpeta, nombreArchivo);
            int sufijo = 1;
            while (File.Exists(rutaCompleta))
            {
                nombreArchivo = $"{nombreBase}_{sufijo}.pdf";
                rutaCompleta  = Path.Combine(carpeta, nombreArchivo);
                sufijo++;
            }

            await File.WriteAllBytesAsync(rutaCompleta, bytes);
        }
        catch
        {
            // Descarga fallida: no interrumpe el flujo de emisión
        }
    }

    // ── Consulta de venta para FE (evita dependencia circular con VentaService) ─
    private async Task<VentaImpresionDto?> GetVentaParaFEAsync(long ventaId)
    {
        var vRow = await (
            from v    in _db.Ventas
            where v.Id == ventaId
            join c    in _db.Clientes  on v.FkCliente  equals c.Id     into cj
            from c    in cj.DefaultIfEmpty()
            join ci   in _db.CondIvas  on c.FkCondIva  equals ci.Id    into cij
            from ci   in cij.DefaultIfEmpty()
            join l    in _db.Localidades on c.FkLocalidad equals l.Id  into lj
            from l    in lj.DefaultIfEmpty()
            join prov in _db.Provincias on (int?)l.FkProvincia equals prov.Id into pj
            from prov in pj.DefaultIfEmpty()
            select new
            {
                v.Id, v.Fecha, v.TotalVenta, v.Iva, v.Descuento, v.Recargo, v.Impuesto,
                FkCliente       = v.FkCliente,
                NombreCliente   = c    != null ? c.NombreComercial : null,
                RazonSocial     = c    != null ? c.RazonSocial     : null,
                Cuil            = c    != null ? c.Cuil            : null,
                Direccion       = c    != null ? c.Direccion       : null,
                LocalidadNombre = l    != null ? l.Nombre          : null,
                ProvinciaNombre = prov != null ? prov.Nombre       : null,
                Email              = c  != null ? c.Email        : null,
                CondIvaAbrev       = ci != null ? ci.Abrev       : null,
                CondIvaAbrevFE     = ci != null ? ci.AbrevFE     : null,
                CondIvaLetra       = ci != null ? ci.Letra       : null,
                CondIvaDescripcion = ci != null ? ci.Descripcion : null
            }
        ).FirstOrDefaultAsync();

        if (vRow == null) return null;

        // ── Formas de pago (equivale a fn_Ventas_FormasPagoFactura) ─────────
        var formasPagoNombres = await (
            from vfp in _db.VentasFormasPago
            join mp  in _db.MediosPago on vfp.FkMedioPago equals mp.Id
            where vfp.FkVenta == (int)ventaId
            select mp.Nombre
        ).ToListAsync();
        string formaPagoStr = formasPagoNombres.Count > 0
            ? string.Join(" | ", formasPagoNombres)
            : "Otra Condición de Pago";

        // Orden estable (por Linea, la PK de inserción) — imprescindible para que los límites
        // de cada "parte" sean consistentes entre el intento original y un reintento posterior.
        var detalle = await (
            from vd in _db.VentasDetalles
            where vd.FkVenta == ventaId
            orderby vd.Linea
            select new VentaDetalleImpresionItemDto
            {
                FkProducto     = vd.FkProducto ?? 0,
                Descripcion    = vd.Descripcion,
                CodBarras      = vd.CodBarras,
                CodProveedor   = vd.CodProveedor,
                Cantidad       = vd.Cantidad      ?? 0m,
                PrecioSinIva   = vd.PrecioSinIva  ?? 0m,
                PrecioConIva   = vd.PrecioConIva  ?? 0m,
                Descuento      = vd.Descuento,
                Recargo        = vd.Recargo,
                SubtotalSinIva = vd.SubtotalSinIva ?? 0m
            }
        ).ToListAsync();

        return new VentaImpresionDto
        {
            VentaId          = vRow.Id,
            FkCliente        = vRow.FkCliente,
            Fecha            = vRow.Fecha,
            NombreCliente    = vRow.NombreCliente,
            RazonSocial      = vRow.RazonSocial,
            Cuil             = vRow.Cuil,
            Email            = vRow.Email,
            DireccionCliente = string.Join(", ",
                new[] { vRow.Direccion, vRow.LocalidadNombre, vRow.ProvinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            Provincia          = vRow.ProvinciaNombre,
            CondIvaAbrev       = vRow.CondIvaAbrev,
            CondIvaAbrevFE     = vRow.CondIvaAbrevFE,
            CondIvaLetra       = vRow.CondIvaLetra,
            CondIvaDescripcion = vRow.CondIvaDescripcion,
            Iva              = vRow.Iva       ?? 0m,
            Descuento        = vRow.Descuento,
            Recargo          = vRow.Recargo,
            Impuesto         = vRow.Impuesto  ?? 0m,
            TotalVenta       = vRow.TotalVenta ?? 0m,
            FormaPago        = formaPagoStr,
            Detalle          = detalle
        };
    }
}
