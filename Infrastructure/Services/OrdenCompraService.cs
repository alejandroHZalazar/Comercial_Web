using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using static Application.Services.OrdenCompraService;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Application.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly ComercialDbContext _context;

        public OrdenCompraService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrdenCompra>> GetOrdenesAsync()
        {
            return await _context.OrdenCompras
                .Where(o => o.Procesado != true)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();
        }

        public async Task<OrdenCompra?> GetOrdenByIdAsync(long id)
        {
            return await _context.OrdenCompras
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<OrdenCompraDetalle>> GetDetallesByOrdenIdAsync(long ordenId)
        {
            return await _context.OrdenCompraDetalles
                .Where(d => d.FkOrdenCompra == ordenId)
                .OrderBy(d => d.Linea)
                .ToListAsync();
        }

        public async Task<long> CrearOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles)
        {
            orden.Fecha ??= DateTime.Today;
            orden.Procesado = false;
            orden.Total = detalles.Sum(d => d.Subtotal ?? 0m);

            _context.OrdenCompras.Add(orden);
            await _context.SaveChangesAsync();

            long nuevoId = orden.Id;

            long linea = 1;
            foreach (var d in detalles)
            {
                d.FkOrdenCompra = nuevoId;
                d.Linea = linea++;
                d.Subtotal = (d.Cantidad ?? 0m) * (d.PrecioProveedor ?? 0m);
                d.Procesado = false;
                _context.OrdenCompraDetalles.Add(d);
            }

            await _context.SaveChangesAsync();
            return nuevoId;
        }

        public async Task ActualizarOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles)
        {
            var existente = await _context.OrdenCompras.FirstOrDefaultAsync(o => o.Id == orden.Id);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró la orden Id={orden.Id}");

            existente.Fecha = orden.Fecha;
            existente.FkProveedor = orden.FkProveedor;
            existente.Iva = orden.Iva;
            existente.Recargo = orden.Recargo;
            existente.Descuento = orden.Descuento;
            existente.Total = detalles.Sum(d => d.Subtotal ?? 0m);

            var existentesDetalles = await _context.OrdenCompraDetalles
                .Where(d => d.FkOrdenCompra == orden.Id)
                .ToListAsync();

            _context.OrdenCompraDetalles.RemoveRange(existentesDetalles);

            long linea = 1;
            foreach (var d in detalles)
            {
                d.FkOrdenCompra = orden.Id;
                d.Linea = linea++;
                d.Subtotal = (d.Cantidad ?? 0m) * (d.PrecioProveedor ?? 0m);
                d.Procesado = false;
                _context.OrdenCompraDetalles.Add(d);
            }

            await _context.SaveChangesAsync();
        }

        public async Task EliminarOrdenAsync(long id)
        {
            var orden = await _context.OrdenCompras.FirstOrDefaultAsync(o => o.Id == id);
            if (orden == null) return;

            var detalles = await _context.OrdenCompraDetalles
                .Where(d => d.FkOrdenCompra == id)
                .ToListAsync();

            _context.OrdenCompraDetalles.RemoveRange(detalles);
            _context.OrdenCompras.Remove(orden);

            await _context.SaveChangesAsync();
        }


        public async Task<List<ProductoAPedirOCDto>> TraerListaProdAPedirAsync(int proveedorId, DateTime desde, DateTime hasta)
        {
            var ingresoId = await _context.TipoProductosMovimientos
                .Where(t => t.Descripcion == "INGRESO DE MERCADERIA")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var ventaId = await _context.TipoProductosMovimientos
                .Where(t => t.Descripcion == "VENTAS")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var devolucionId = await _context.TipoProductosMovimientos
                .Where(t => t.Descripcion == "DEVOLUCION")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var query = await _context.Productos
                .Where(p => p.FkProveedor == proveedorId && p.Baja == false)
                .Select(p => new ProductoAPedirOCDto
                {
                    CodBarras = p.CodBarras,
                    CodProveedor = p.CodProveedor,
                    Descripcion = p.Descripcion,

                    Stock = _context.StockProductos
                        .Where(s => s.FkProducto == p.Id)
                        .Select(s => s.Cantidad ?? 0)
                        .FirstOrDefault(),

                    CantidadMinima = _context.StockProductos
                        .Where(s => s.FkProducto == p.Id)
                        .Select(s => s.CantidadMinima ?? 0)
                        .FirstOrDefault(),

                    Ingreso = _context.ProductosMovimientos
                        .Where(m => m.FkProducto == p.Id &&
                                    m.FechaMov >= desde &&
                                    m.FechaMov <= hasta &&
                                    m.TipoMovimiento == ingresoId)
                        .Sum(m => (decimal?)m.Cantidad) ?? 0,

                    Ventas = (
                        (_context.ProductosMovimientos
                            .Where(m => m.FkProducto == p.Id &&
                                        m.FechaMov >= desde &&
                                        m.FechaMov <= hasta &&
                                        m.TipoMovimiento == ventaId)
                            .Sum(m => (decimal?)m.Cantidad) ?? 0)
                        -
                        (_context.ProductosMovimientos
                            .Where(m => m.FkProducto == p.Id &&
                                        m.FechaMov >= desde &&
                                        m.FechaMov <= hasta &&
                                        m.TipoMovimiento == devolucionId)
                            .Sum(m => (decimal?)m.Cantidad) ?? 0)
                    ),

                    PrecioProveedor = _context.PreciosProveedores
                        .Where(pp => pp.FkProducto == p.Id)
                        .Select(pp => pp.Precio ?? 0)
                        .FirstOrDefault(),

                    PrecioLista = _context.PreciosProductos
                        .Where(prp => prp.FkProducto == p.Id)
                        .Select(prp => prp.Precio ?? 0)
                        .FirstOrDefault(),

                    Costo = _context.CostosProductos
                        .Where(c => c.FkProducto == p.Id)
                        .Select(c => c.Costo ?? 0)
                        .FirstOrDefault(),

                    ProductoId = p.Id,
                    Cantidad = 0
                })
                .ToListAsync();

            return query;
        }

        public async Task<List<ProductoAPedirImprimirOCDto>> TraerListaProdAPedirImprimirAsync(int proveedorId, DateTime desde, DateTime hasta)
        {
            var ingresoId = await _context.TipoProductosMovimientos
                .Where(t => t.Descripcion == "INGRESO DE MERCADERIA")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var ventaId = await _context.TipoProductosMovimientos
                .Where(t => t.Descripcion == "VENTAS")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var devolucionId = await _context.TipoProductosMovimientos
                .Where(t => t.Descripcion == "DEVOLUCION")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var query = await _context.Productos
                .Where(p => p.FkProveedor == proveedorId && p.Baja == false)
                .Select(p => new ProductoAPedirImprimirOCDto
                {
                    CodBarras = p.CodBarras,
                    Cod_Prov = p.CodProveedor,
                    Descripcion = p.Descripcion,

                    Stock = _context.StockProductos
                        .Where(s => s.FkProducto == p.Id)
                        .Select(s => s.Cantidad ?? 0)
                        .FirstOrDefault(),

                    C_Min = _context.StockProductos
                        .Where(s => s.FkProducto == p.Id)
                        .Select(s => s.CantidadMinima ?? 0)
                        .FirstOrDefault(),

                    Ingreso = _context.ProductosMovimientos
                        .Where(m => m.FkProducto == p.Id &&
                                    m.FechaMov >= desde &&
                                    m.FechaMov <= hasta &&
                                    m.TipoMovimiento == ingresoId)
                        .Sum(m => (decimal?)m.Cantidad) ?? 0,

                    Ventas = (
                        (_context.ProductosMovimientos
                            .Where(m => m.FkProducto == p.Id &&
                                        m.FechaMov >= desde &&
                                        m.FechaMov <= hasta &&
                                        m.TipoMovimiento == ventaId)
                            .Sum(m => (decimal?)m.Cantidad) ?? 0)
                        -
                        (_context.ProductosMovimientos
                            .Where(m => m.FkProducto == p.Id &&
                                        m.FechaMov >= desde &&
                                        m.FechaMov <= hasta &&
                                        m.TipoMovimiento == devolucionId)
                            .Sum(m => (decimal?)m.Cantidad) ?? 0)
                    ),

                    P_Prov = _context.PreciosProveedores
                        .Where(pp => pp.FkProducto == p.Id)
                        .Select(pp => pp.Precio ?? 0)
                        .FirstOrDefault(),

                    P_Lista = _context.PreciosProductos
                        .Where(prp => prp.FkProducto == p.Id)
                        .Select(prp => prp.Precio ?? 0)
                        .FirstOrDefault(),

                    Costo = _context.CostosProductos
                        .Where(c => c.FkProducto == p.Id)
                        .Select(c => c.Costo ?? 0)
                        .FirstOrDefault(),

                    Prod = p.Id,
                    Cant = 0
                })
                .ToListAsync();

            return query;
        }

        public async Task<long> GrabarOrdenCompraAsync(
            int proveedorId,
            decimal total,
            decimal iva,
            decimal recargo,
            decimal descuento,
            List<OrdenCompraDetalle> detalles,
            IProgress<int> progreso,
            CancellationToken ct)
            {
            if (detalles == null || !detalles.Any(d => d.Cantidad > 0))
                throw new InvalidOperationException("La grilla está vacía o sin pedidos válidos.");

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                // 1. Insertar cabecera
                var cabecera = new OrdenCompra
                {
                    Fecha = DateTime.Now,
                    FkProveedor = proveedorId,
                    Total = total,
                    Procesado = false,
                    Iva = iva,
                    Recargo = recargo,
                    Descuento = descuento
                };

                _context.OrdenCompras.Add(cabecera);
                await _context.SaveChangesAsync(ct);

                long salida = cabecera.Id;

                // 2. Insertar detalles válidos
                var detallesValidos = detalles.Where(d => d.Cantidad > 0).ToList();
                int totalFilas = detallesValidos.Count;
                int procesadas = 0;

                foreach (var d in detallesValidos)
                {
                    d.FkOrdenCompra = salida;
                    d.Procesado = false;

                    _context.OrdenCompraDetalles.Add(d);
                    await _context.SaveChangesAsync(ct);

                    procesadas++;
                    progreso?.Report((int)((procesadas / (double)totalFilas) * 100));
                }

                // 3. Commit
                await transaction.CommitAsync(ct);
                return salida;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<List<OrdenCompraPrint>> OrdenCompraImprimirAsync(int id)
        {
            var query = await (
            from o in _context.OrdenCompras
            join d in _context.OrdenCompraDetalles on o.Id equals d.FkOrdenCompra
            join pr in _context.Proveedores on o.FkProveedor equals pr.Id
            join p in _context.Parametros
                .Where(x => x.Modulo == "login" && x.Parametro1 == "imagen")
                on 1 equals 1 into pg
            from p in pg.DefaultIfEmpty()
            where o.Id == id
            select new OrdenCompraPrint
            {
                id = o.Id.ToString().PadLeft(8, '0'),

                fecha = o.Fecha,
                fk_proveedor = o.FkProveedor,
                total = o.Total,
                procesado = o.Procesado,
                iva = o.Iva,
                recargo = o.Recargo,
                descuento = o.Descuento,

                fk_producto = d.FkProducto,
                codBarras = d.CodBarras,
                codProveedor = d.CodProveedor,
                descripcion = d.Descripcion,
                precioProveedor = d.PrecioProveedor,
                cantidad = d.Cantidad,
                subtotal = d.Subtotal,

                imagen = p != null ? p.Imagen : null,
                nombreComercial = pr.NombreComercial,
                direccion = pr.Direccion
            }).ToListAsync();

            return query;
        }
    }
}