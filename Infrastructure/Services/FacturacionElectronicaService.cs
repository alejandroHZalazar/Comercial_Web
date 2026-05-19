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

            // ── Recorrer detalle — acumular neto, IVA y recargo ──────────────
            decimal neto = 0m, ivaTotal = 0m, recargoTotal = 0m;
            var detalleFE = new List<TfDetalle>();

            foreach (var d in ventaData.Detalle)
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
                    detalle                 = detalleFE,
                    tributos                = tributos
                }
            };

            // ── Enviar con reintentos ────────────────────────────────────────
            var (ok, respuesta, erroresStr) = await EnviarConReintentoAsync(req);

            if (ok && respuesta != null)
            {
                result.Ok                = true;
                result.Cae               = respuesta.cae?.Trim();
                result.VencimientoCae    = respuesta.vencimiento_cae;
                result.NumeroComprobante = respuesta.comprobante_nro;
                result.PdfUrl            = respuesta.comprobante_pdf_url;
                result.QrAfip            = respuesta.afip_qr;

                if (!string.IsNullOrEmpty(result.Cae))
                {
                    _db.ComprobantesFiscales.Add(new ComprobanteFiscal
                    {
                        TipoComprobante     = "Factura",
                        Letra               = letra,
                        PuntoVenta          = prm.PuntoVenta,
                        Numero              = ParseNumeroComprobante(result.NumeroComprobante),
                        FechaEmision        = DateTime.Now,
                        NroReferencia       = (int)ventaId,
                        FkCliente           = ventaData.FkCliente,
                        RazonSocial         = ventaData.RazonSocial ?? ventaData.NombreCliente,
                        Cuit                = nroCuenta,
                        ImporteTotal        = ventaData.TotalVenta,
                        Estado              = "Emitido",
                        Cae                 = result.Cae,
                        FechaVencimientoCae = ParseFechaFE(result.VencimientoCae),
                        LinkPdf             = result.PdfUrl,
                        AfipQr              = result.QrAfip,
                        CreatedAt           = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                }

                // Descarga automática del PDF al directorio Descargas del usuario
                await _DescargarPdfAsync(
                    result.PdfUrl,
                    $"Factura_{letra}_{ParseNumeroComprobante(result.NumeroComprobante)}_{DateTime.Now:yyyyMMdd}");
            }
            else
            {
                result.Errores.Add(erroresStr ?? "Error desconocido.");
                await GuardarErrorFEAsync((int)ventaId, erroresStr ?? "Error desconocido.");
            }
        }
        catch (Exception ex)
        {
            result.Errores.Add($"Error técnico: {ex.Message}");
        }
        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  EMITIR NOTA DE CRÉDITO MANUAL (unaDevolucion == 0 en escritorio)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<(bool ok, string? error, string? pdfUrl)> EmitirNotaCreditoManualAsync(
        NotaCreditoRequestDto dto, int puntoVenta)
    {
        try
        {
            var prm = await CargarParametrosFEAsync();
            if (string.IsNullOrEmpty(prm.UserToken) || string.IsNullOrEmpty(prm.ApiKey) || string.IsNullOrEmpty(prm.ApiToken))
                return (false, "Parámetros de facturación electrónica incompletos.", null);

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
                return (false, "No se encontraron datos fiscales del cliente.", null);

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

            // ── Construir detalle según origen ───────────────────────────────
            decimal neto;
            decimal ivaTotal;
            List<TfDetalle> detalleFE;

            if (dto.IdDevolucion > 0)
            {
                // Detalle real obtenido de DevolucionesDetalles
                var devDetalle = await _db.DevolucionesDetalles
                    .Where(d => d.FkDevolucion == dto.IdDevolucion)
                    .ToListAsync();

                detalleFE = new List<TfDetalle>();
                neto      = 0m;
                ivaTotal  = 0m;

                foreach (var d in devDetalle)
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
            }
            else
            {
                // Comportamiento original: línea única "Productos Varios"
                neto     = Math.Round(
                    dto.Importe / (1m + (dto.IvaPorcentaje / 100m) + (dto.IIBBPorcentaje / 100m)), 2);
                ivaTotal = Math.Round(neto * alicuotaIva / 100m, 2);

                detalleFE = new List<TfDetalle>
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
            }

            // ── Armar request ────────────────────────────────────────────────
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

            if (ok && respuesta != null)
            {
                _db.ComprobantesFiscales.Add(new ComprobanteFiscal
                {
                    TipoComprobante     = "Nota de Crédito",
                    Letra               = letra,
                    PuntoVenta          = puntoVenta,
                    Numero              = ParseNumeroComprobante(respuesta.comprobante_nro),
                    FechaEmision        = DateTime.Now,
                    NroReferencia       = 0,
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

                return (true, null, respuesta.comprobante_pdf_url);
            }
            else
            {
                await GuardarErrorFEAsync(0, erroresStr ?? "Error desconocido.");
                return (false, $"Error FE: {erroresStr}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error inesperado: {ex.Message}", null);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FUNCIONES COMPARTIDAS
    // ═══════════════════════════════════════════════════════════════════════════

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
            ClienteCFId   = int.TryParse(Get("ventas", "clienteConsumidorFinal"), out var cf) ? cf : 0
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

        var detalle = await (
            from vd in _db.VentasDetalles
            where vd.FkVenta == ventaId
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
