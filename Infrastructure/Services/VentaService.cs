using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services;

public class VentaService : IVentaService
{
    private readonly ComercialDbContext _db;
    private readonly IFacturacionElectronicaService _feService;

    public VentaService(ComercialDbContext db, IFacturacionElectronicaService feService)
    {
        _db        = db;
        _feService = feService;
    }

    // ── Lista de impuestos ────────────────────────────────────────────────────
    public async Task<List<Impuesto>> GetImpuestosAsync()
        => await _db.Impuestos.OrderBy(i => i.Valor).ToListAsync();

    // ── Parámetros del sistema ────────────────────────────────────────────────
    public async Task<VentaParametrosDto> GetParametrosAsync(string machineName)
    {
        static int pi(string? v) => string.IsNullOrWhiteSpace(v) ? 0 : (int.TryParse(v, out var x) ? x : 0);
        static decimal pd(string? v) => string.IsNullOrWhiteSpace(v) ? 0m : (decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : 0m);

        var parametros = await _db.Parametros.ToListAsync();
        string? Get(string modulo, string param) =>
            parametros.FirstOrDefault(p => p.Modulo == modulo && p.Parametro1 == param)?.Valor;

        return new VentaParametrosDto
        {
            LlevaCC                = pi(Get("clientes",   "llevaCC")),
            Comisiona              = pi(Get("ventas",     "comisiona")),
            TieneConsumidorFinal   = pi(Get("ventas",     "tieneConsumidorFinal")),
            ClienteConsumidorFinal = pi(Get("ventas",     "clienteConsumidorFinal")),
            TieneMediosPagos       = pi(Get("ventas",     "mediosPagos")),
            ImputaEnVenta          = pi(Get("ventas",     "pagosEnVenta")),
            TieneCaja              = pi(Get("caja",       "haceCaja")),
            FacturaEnVenta         = pi(Get("ventas",     "facturaEnVenta")),
            FacturaFiscal          = pi(Get("ventas",     "facturaFiscal")),
            FacturaElectronica     = pi(Get("ventas",     "facturaElectronica")),
            MarcaFiscal            = Get("ventas",        "marcaFiscal") ?? "",
            ValorDolar             = pd(Get("productos",  "cotizacionDolar")),
            HaceNotaVentaTK        = pi(Get("ventas",     "notaVentaTK")),
            AnchoTk                = pi(Get("ventas",     "anchoTk")),
            PuntoVenta             = pi(Get("PuntoVenta", machineName)),
            TieneLectoraCB         = pi(Get("productos",  "MecanismoLectora")),
            FiltraPorProveedor     = pi(Get("ventas",     "filtraPorProveedor")),
            BonificacionPorLinea   = pi(Get("ventas",     "bonificacionesPorDetalle")),
            DolarizaProductos      = pi(Get("productos",  "dolarizaProductos")),
            Decimales              = pi(Get("productos",  "decimales")),
            DecimalesCant          = pi(Get("productos",  "decimalesCant")),
            IndiceBusqueda         = pi(Get("notaPedido", "indiceBusqueda")),
            IdPlanEfectivo         = pi(Get("Cobros",     "idPlanEfectivo"))
        };
    }

    // ── Datos del cliente (con condición IVA, localidad, provincia) ───────────
    public async Task<ClienteVentaDataDto?> GetClienteDataAsync(int clienteId)
    {
        var row = await (
            from c    in _db.Clientes
            where c.Id == clienteId
            join ci   in _db.CondIvas    on c.FkCondIva   equals ci.Id  into cij
            from ci   in cij.DefaultIfEmpty()
            join l    in _db.Localidades on c.FkLocalidad equals l.Id   into lj
            from l    in lj.DefaultIfEmpty()
            join prov in _db.Provincias  on (int?)l.FkProvincia equals prov.Id into pj
            from prov in pj.DefaultIfEmpty()
            select new
            {
                c.Id, c.NombreComercial, c.RazonSocial, c.Cuil, c.Email, c.Telefono, c.Direccion,
                LocalidadNombre  = l    != null ? l.Nombre    : null,
                ProvinciaNombre  = prov != null ? prov.Nombre : null,
                CondIvaAbrev     = ci   != null ? ci.Abrev    : null,
                CondIvaLetra     = ci   != null ? ci.Letra    : null,
                CondIvaAbrevFE   = ci   != null ? ci.AbrevFE  : null,
                CondIvaDescripcion = ci != null ? ci.Descripcion : null
            }
        ).FirstOrDefaultAsync();

        if (row == null) return null;

        return new ClienteVentaDataDto
        {
            Id             = row.Id,
            NombreComercial= row.NombreComercial,
            RazonSocial    = row.RazonSocial,
            Cuil           = row.Cuil,
            Email          = row.Email,
            Telefono       = row.Telefono,
            DireccionFull  = string.Join(", ",
                new[] { row.Direccion, row.LocalidadNombre, row.ProvinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            CondIvaAbrev      = row.CondIvaAbrev,
            CondIvaLetra      = row.CondIvaLetra,
            CondIvaAbrevFE    = row.CondIvaAbrevFE,
            CondIvaDescripcion = row.CondIvaDescripcion,
            Provincia         = row.ProvinciaNombre
        };
    }

    // ── Proveedores activos (para filtro) ─────────────────────────────────────
    public async Task<List<Proveedore>> GetProveedoresActivosAsync()
        => await _db.Proveedores
            .Where(p => p.Baja != true && p.NombreComercial != null)
            .OrderBy(p => p.NombreComercial)
            .ToListAsync();

    // ── Caja abierta del usuario ───────────────────────────────────────────────
    public async Task<(bool abierta, int cajaId)> GetCajaAbiertaAsync(int usuarioId)
    {
        var caja = await _db.Cajas
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CajaId)
            .FirstOrDefaultAsync();
        if (caja == null) return (false, 0);
        return (caja.Estado == "ABIERTA", caja.CajaId);
    }

    // ── Grabar venta (emula sp_VentasAddVenta) ────────────────────────────────
    public async Task<long> GrabarVentaAsync(GrabarVentaRequestDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ── 1. Insertar Venta ─────────────────────────────────────────────
            var venta = new Venta
            {
                Fecha      = DateTime.Now,
                TotalVenta = dto.Total,
                TotalCosto = dto.Costo,
                FkCliente  = dto.FkCliente,
                FkCajero   = dto.FkCajero,
                Iva        = dto.Iva,
                Descuento  = dto.Descuento,
                Recargo    = dto.Recargo,
                FkVendedor = dto.FkVendedor > 0 ? dto.FkVendedor : (int?)null,
                Comision   = dto.Comision,
                Impuesto   = dto.Impuesto
            };
            _db.Ventas.Add(venta);
            await _db.SaveChangesAsync();
            long ventaId = venta.Id;

            // ── 2. Detalle, stock y movimientos ───────────────────────────────
            foreach (var item in dto.Detalle)
            {
                decimal descuento = item.DescRec < 0 ? item.DescRec * -1m : 0m;
                decimal recargo   = item.DescRec > 0 ? item.DescRec        : 0m;

                decimal subtotal = item.Fraccionado
                    ? item.Subtotal
                    : item.Cantidad * item.PrecioConIva;

                _db.VentasDetalles.Add(new VentasDetalle
                {
                    FkVenta       = ventaId,
                    FkProducto    = item.FkProducto,
                    CodBarras     = await _db.Productos.Where(p => p.Id == item.FkProducto).Select(p => p.CodBarras).FirstOrDefaultAsync(),
                    CodProveedor  = await _db.Productos.Where(p => p.Id == item.FkProducto).Select(p => p.CodProveedor).FirstOrDefaultAsync(),
                    Descripcion   = await _db.Productos.Where(p => p.Id == item.FkProducto).Select(p => p.Descripcion).FirstOrDefaultAsync(),
                    PrecioSinIva  = item.PrecioSinIva,
                    PrecioConIva  = item.PrecioConIva,
                    Cantidad      = item.Cantidad,
                    Costo         = item.Cantidad * (await _db.CostosProductos.Where(c => c.FkProducto == item.FkProducto).Select(c => c.Costo ?? 0m).FirstOrDefaultAsync()),
                    Subtotal      = subtotal,
                    Descuento     = descuento > 0 ? descuento : (decimal?)null,
                    Recargo       = recargo   > 0 ? recargo   : (decimal?)null,
                    SubtotalSinIva= item.SubtotalSinIva
                });

                // Stock anterior
                var stockRec = await _db.StockProductos.FirstOrDefaultAsync(s => s.FkProducto == item.FkProducto);
                decimal stockAnt = stockRec?.Cantidad ?? 0m;

                // Bajar stock
                if (stockRec != null)
                    stockRec.Cantidad = (stockRec.Cantidad ?? 0m) - item.Cantidad;
                else
                    _db.StockProductos.Add(new StockProducto { FkProducto = item.FkProducto, Cantidad = -item.Cantidad });

                decimal costoUnit = await _db.CostosProductos
                    .Where(c => c.FkProducto == item.FkProducto)
                    .Select(c => c.Costo ?? 0m)
                    .FirstOrDefaultAsync();

                // Movimiento de stock
                _db.ProductosMovimientos.Add(new ProductosMovimiento
                {
                    FkProducto     = item.FkProducto,
                    TipoMovimiento = 3,
                    Descripcion    = await _db.Productos.Where(p => p.Id == item.FkProducto).Select(p => p.Descripcion).FirstOrDefaultAsync(),
                    StockAnt       = stockAnt,
                    StockAct       = stockAnt - item.Cantidad,
                    Costo          = item.Cantidad * costoUnit,
                    Venta          = item.Cantidad * item.PrecioConIva,
                    Cantidad       = item.Cantidad,
                    FkColor        = 0,
                    FechaMov       = DateTime.Now
                });

                // Marcar línea de pedido si vino de pedido
                if (item.FkPedido > 0)
                {
                    var linPed = await _db.PedidoDetalles
                        .FirstOrDefaultAsync(d => d.FkPedido == item.FkPedido && d.FkProducto == item.FkProducto);
                    if (linPed != null)
                    {
                        linPed.Procesado    = true;
                        linPed.CantEntregada = item.Cantidad;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // ── 3. Marcar pedido como vendido ──────────────────────────────────
            if (dto.PedidoCargado > 0)
            {
                var ped = await _db.Pedidos.FindAsync(dto.PedidoCargado);
                if (ped != null) ped.Vendido = true;
                await _db.SaveChangesAsync();
            }

            // ── 4. Cuenta corriente: Documento FA + MovimientoCC D ─────────────
            if (dto.LlevaCC)
            {
                var docFA = new Documento
                {
                    ClienteId     = dto.FkCliente,
                    TipoDocumento = "FA",
                    Numero        = ventaId.ToString(),
                    Fecha         = DateTime.Now,
                    Total         = dto.Total
                };
                _db.Documentos.Add(docFA);
                await _db.SaveChangesAsync();

                var movD = new MovimientoCC
                {
                    ClienteId      = dto.FkCliente,
                    DocumentoId    = docFA.ID,
                    Fecha          = DateTime.Now,
                    TipoMovimiento = "D",
                    Importe        = dto.Total,
                    SaldoPendiente = dto.Total
                };
                _db.MovimientosCC.Add(movD);
                await _db.SaveChangesAsync();

                // Auto-imputación con créditos existentes
                await ImputarMovimientoAsync(movD, dto.FkCliente, "D");
                await _db.SaveChangesAsync();
            }

            // ── 5. Cobro e imputación si corresponde ───────────────────────────
            if (dto.ImputaEnVenta && dto.ImporteCobro > 0 && dto.FormasPago.Count > 0)
            {
                // Insertar Cobro
                var cobro = new Cobro
                {
                    ClienteId    = dto.FkCliente,
                    Fecha        = DateTime.Now,
                    ImporteTotal = dto.ImporteCobro,
                    DocumentoId  = 0,
                    TipoCobro    = 1,
                    Observaciones = ""
                };
                _db.Cobros.Add(cobro);
                await _db.SaveChangesAsync();

                foreach (var fp in dto.FormasPago)
                {
                    _db.CobrosDetalle.Add(new CobroDetalle
                    {
                        CobroId     = cobro.CobroId,
                        MedioPagoId = fp.FkMedioPago,
                        Importe     = fp.Importe,
                        Referencia1 = fp.Referencia1,
                        Referencia2 = fp.Referencia2,
                        Referencia3 = fp.Referencia3
                    });

                    if (dto.HaceCaja && dto.CajaId > 0)
                    {
                        var conceptoCaja = await _db.ConceptosCaja
                            .FirstOrDefaultAsync(c => c.FkMedioPago == fp.FkMedioPago && c.Operacion == "Cobros");
                        if (conceptoCaja != null)
                        {
                            _db.CajaMovimientos.Add(new CajaMovimiento
                            {
                                CajaId         = dto.CajaId,
                                ConceptoCajaId = conceptoCaja.Id,
                                MedioPagoId    = fp.FkMedioPago,
                                Importe        = fp.Importe,
                                Observaciones  = $"Cobro Venta N° {ventaId}",
                                Fecha          = DateTime.Now
                            });
                        }
                    }
                }
                await _db.SaveChangesAsync();

                // Documento RE + MovimientoCC C
                var docRE = new Documento
                {
                    ClienteId     = dto.FkCliente,
                    TipoDocumento = "RE",
                    Numero        = cobro.CobroId.ToString(),
                    Fecha         = DateTime.Now,
                    Total         = dto.ImporteCobro
                };
                _db.Documentos.Add(docRE);
                await _db.SaveChangesAsync();

                var movC = new MovimientoCC
                {
                    ClienteId      = dto.FkCliente,
                    DocumentoId    = docRE.ID,
                    Fecha          = DateTime.Now,
                    TipoMovimiento = "C",
                    Importe        = dto.ImporteCobro,
                    SaldoPendiente = dto.ImporteCobro
                };
                _db.MovimientosCC.Add(movC);
                await _db.SaveChangesAsync();

                await ImputarMovimientoAsync(movC, dto.FkCliente, "C");
                await _db.SaveChangesAsync();
            }

            // ── 6. Formas de pago en ventas_formasPago ─────────────────────────
            if (dto.TieneMediosPagos && dto.FormasPago.Count > 0)
            {
                foreach (var fp in dto.FormasPago)
                {
                    _db.VentasFormasPago.Add(new VentasFormasPago
                    {
                        FkVenta    = (int)ventaId,
                        FkMedioPago= fp.FkMedioPago,
                        FkPlanPago = fp.FkPlanPago,
                        Importe    = fp.Importe,
                        Referencia1= fp.Referencia1,
                        Referencia2= fp.Referencia2,
                        Referencia3= fp.Referencia3
                    });
                }
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return ventaId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Helper: auto-imputación de movimiento CC ──────────────────────────────
    private async Task ImputarMovimientoAsync(MovimientoCC movNuevo, int clienteId, string tipoNuevo)
    {
        string tipoOpuesto = tipoNuevo == "D" ? "C" : "D";
        decimal saldoRestante = movNuevo.SaldoPendiente;

        var pendientes = await _db.MovimientosCC
            .Where(m => m.ClienteId      == clienteId &&
                        m.TipoMovimiento == tipoOpuesto &&
                        m.SaldoPendiente  > 0)
            .OrderBy(m => m.Fecha)
            .ThenBy(m => m.MovimientoId)
            .ToListAsync();

        foreach (var pend in pendientes)
        {
            if (saldoRestante <= 0m) break;

            var aImputar = Math.Min(saldoRestante, pend.SaldoPendiente);
            pend.SaldoPendiente  -= aImputar;
            movNuevo.SaldoPendiente -= aImputar;
            saldoRestante        -= aImputar;

            _db.Imputaciones.Add(new Imputacion
            {
                MovimientoDebitoId  = tipoNuevo == "D" ? movNuevo.MovimientoId : pend.MovimientoId,
                MovimientoCreditoId = tipoNuevo == "C" ? movNuevo.MovimientoId : pend.MovimientoId,
                Importe             = aImputar,
                Fecha               = DateTime.Now
            });
        }
    }

    // ── Obtener datos de venta para impresión ─────────────────────────────────
    public async Task<VentaImpresionDto?> GetVentaParaImpresionAsync(long ventaId)
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
                CondIvaAbrev       = ci != null ? ci.Abrev       : null,
                CondIvaLetra       = ci != null ? ci.Letra       : null,
                CondIvaDescripcion = ci != null ? ci.Descripcion : null
            }
        ).FirstOrDefaultAsync();

        if (vRow == null) return null;

        // Formas de pago con importe
        var formasPagoDetalle = await (
            from fp in _db.VentasFormasPago
            where fp.FkVenta == (int)ventaId
            join mp in _db.MediosPago on fp.FkMedioPago equals mp.Id into mpj
            from mp in mpj.DefaultIfEmpty()
            join pp in _db.PlanesPago  on fp.FkPlanPago  equals pp.Id into ppj
            from pp in ppj.DefaultIfEmpty()
            select new FormaPagoImpresionDto
            {
                Nombre  = mp != null ? (mp.Nombre ?? "Efectivo") : "Efectivo",
                Plan    = pp != null ? pp.Nombre : null,
                Importe = fp.Importe
            }
        ).ToListAsync();

        // Texto simple para compatibilidad (nombres únicos)
        var formasPago = formasPagoDetalle
            .Select(f => f.Nombre)
            .Distinct()
            .ToList();

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
            DireccionCliente = string.Join(", ",
                new[] { vRow.Direccion, vRow.LocalidadNombre, vRow.ProvinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            CondIvaAbrev       = vRow.CondIvaAbrev,
            CondIvaLetra       = vRow.CondIvaLetra,
            CondIvaDescripcion = vRow.CondIvaDescripcion,
            Iva              = vRow.Iva       ?? 0m,
            Descuento        = vRow.Descuento,
            Recargo          = vRow.Recargo,
            Impuesto         = vRow.Impuesto  ?? 0m,
            TotalVenta       = vRow.TotalVenta ?? 0m,
            FormaPago         = string.Join(", ", formasPago),
            Detalle           = detalle,
            FormasPagoDetalle = formasPagoDetalle
        };
    }

    // ── Precios actuales de productos ─────────────────────────────────────────
    public async Task<List<ProductoPrecioActualDto>> GetPreciosActualesAsync(
        List<int> productoIds, bool dolariza, decimal cotizDolar)
    {
        var precios = await _db.PreciosProductos
            .Where(pp => productoIds.Contains(pp.FkProducto ?? 0))
            .ToListAsync();

        var productos = await _db.Productos
            .Where(p => productoIds.Contains(p.Id))
            .ToListAsync();

        return productoIds.Select(id =>
        {
            var precio    = precios.FirstOrDefault(pp => pp.FkProducto == id)?.Precio ?? 0m;
            var dolarizado = productos.FirstOrDefault(p => p.Id == id)?.Dolarizado ?? false;
            if (dolariza && dolarizado && cotizDolar > 0) precio *= cotizDolar;
            return new ProductoPrecioActualDto
            {
                ProductoId   = id,
                PrecioSinIva = precio,
                PrecioConIva = precio,
                Dolarizado   = dolarizado
            };
        }).ToList();
    }

    // ── Stock de productos ─────────────────────────────────────────────────
    public async Task<Dictionary<int, decimal>> GetStockProductosAsync(List<int> productoIds)
    {
        var stocks = await _db.StockProductos
            .Where(s => s.FkProducto != null && productoIds.Contains(s.FkProducto.Value))
            .ToListAsync();

        return productoIds.ToDictionary(
            id => id,
            id => stocks.FirstOrDefault(s => s.FkProducto == id)?.Cantidad ?? 0m);
    }

    // ── Emitir factura electrónica (delegado a FacturacionElectronicaService) ─
    public Task<FacturaElectronicaResultDto> EmitirFacturaElectronicaAsync(long ventaId)
        => _feService.EmitirFacturaVentaAsync(ventaId);
}
