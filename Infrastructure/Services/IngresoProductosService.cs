using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain.DTO.IngresoProductosDTO;

namespace Infrastructure.Services
{
    public class IngresoProductosService: IIngresoProductosService
    {
        private readonly ComercialDbContext _context;

        public IngresoProductosService(ComercialDbContext context)
        {
            _context = context;
        }
        public async Task<List<ProductosPorProveedorDTO>> TraerProductosProveedorAsync(int proveedorId, int decCant, int decStock)
        {
            var producto = await (
            from p in _context.Productos
            join s in _context.StockProductos on p.Id equals s.FkProducto
            join pp in _context.PreciosProductos on p.Id equals pp.FkProducto
            join cp in _context.CostosProductos on p.Id equals cp.FkProducto
            join prpr in _context.PreciosProveedores on p.Id equals prpr.FkProducto
            where p.FkProveedor == proveedorId && p.Baja == false
            select new ProductosPorProveedorDTO
            {
                CodBarras = p.CodBarras,
                CodProv = p.CodProveedor,
                Descripcion = p.Descripcion,
                Stock = Math.Round(s.Cantidad ?? 0, decStock),
                Cmin = Math.Round(s.CantidadMinima ?? 0, decStock),
                Costo = Math.Round(cp.Costo ?? 0, decCant),
                Precio = Math.Round(pp.Precio ?? 0, decCant),
                PrecProveedor = Math.Round(prpr.Precio ?? 0, decCant),
                Id = p.Id
            }).ToListAsync();           

            return producto;
           
        }

        public async Task<List<OrdenCompraBuscarDTO>> TraerOrdenesCompraSinProcesarPorProveedor(int unProveedorint, int decCant)
        {
            var notas = await (
                from o in _context.OrdenCompras
                join p in _context.Proveedores on o.FkProveedor equals p.Id                
                where o.FkProveedor == unProveedorint && o.Procesado == false
                orderby o.Fecha descending
                select new OrdenCompraBuscarDTO
                {
                    Id = o.Id,
                    Fecha = o.Fecha,
                    Proveedor = p.NombreComercial,
                    Recargo = Math.Round(o.Recargo??0,decCant),
                    Descuento = Math.Round(o.Descuento??0,decCant),
                    Iva = Math.Round(o.Iva ?? 0, decCant),
                    Total = Math.Round(o.Total ?? 0, decCant)
                }
                
                ).Take(10).ToListAsync();

            return notas;
        }

        public async Task<List<OrdenCompraBuscarDTO>> TraerOrdenesCompraSinProcesarPorProveedorFecha(int unProveedorint, DateTime desde, DateTime hasta, int decCant)
        {
            var notas = await (
                from o in _context.OrdenCompras
                join p in _context.Proveedores on o.FkProveedor equals p.Id
                where o.FkProveedor == unProveedorint && o.Procesado == false && o.Fecha >= desde && o.Fecha <= hasta.Date.AddDays(1)
                orderby o.Fecha descending
                select new OrdenCompraBuscarDTO
                {
                    Id = o.Id,
                    Fecha = o.Fecha,
                    Proveedor = p.NombreComercial,
                    Recargo = Math.Round(o.Recargo ?? 0, decCant),
                    Descuento = Math.Round(o.Descuento ?? 0, decCant),
                    Iva = Math.Round(o.Iva ?? 0, decCant),
                    Total = Math.Round(o.Total ?? 0, decCant)
                }

                ).Take(10).ToListAsync();

            return notas;
        }

        public async Task<List<OrdenCompraBuscarDetalleDTO>> TraerOrdenCompraDetalle(int unaOrden, int decCant, int decStock)
        {
             var detalle = await (
             from d in _context.OrdenCompraDetalles
             join s in _context.StockProductos on d.FkProducto equals s.FkProducto
             join c in _context.CostosProductos on d.FkProducto equals c.FkProducto
             join pp in _context.PreciosProductos on d.FkProducto equals pp.FkProducto
             join prp in _context.PreciosProveedores on d.FkProducto equals prp.FkProducto
             where d.FkOrdenCompra == unaOrden
             select new OrdenCompraBuscarDetalleDTO
             {
                 Linea = d.Linea,
                 CodBarras = d.CodBarras,
                 CodProveedor = d.CodProveedor,
                 Descripcion = d.Descripcion,
                 Stock = Math.Round(s.Cantidad??0,decStock),
                 CMin = Math.Round(s.CantidadMinima ?? 0, decStock),
                 Costo = Math.Round(c.Costo ?? 0, decCant),
                 PLista = Math.Round(pp.Precio ?? 0, decCant),
                 PProveedorSinIva = Math.Round(prp.Precio ?? 0, decCant),
                 PProveedorConIva = Math.Round(prp.Precio ?? 0, decCant),
                 Cantidad = Math.Round(d.Cantidad ?? 0, decStock),
                 Subtotal = Math.Round(d.Subtotal ?? 0, decCant),
                 IdOrdenCompra = d.FkOrdenCompra,
                 Id = d.FkProducto
             }
         ).ToListAsync();

            return detalle;
        }

        public async Task ProcesarIngresoAsync(List<IngresoProductoDetalleDto> items, IProgress<int> progress)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                int total = items.Count;
                int procesados = 0;

                foreach (var item in items)
                {
                    // 1️⃣ Obtener producto
                    var producto = await _context.StockProductos
                        .FirstAsync(p => p.FkProducto == item.ProductoId);

                    var stockAnterior = producto.Cantidad;

                    // 2️⃣ UPDATE stockProductos
                    producto.Cantidad += item.Cantidad;

                    // 3️⃣ INSERT productosMovimientos
                    _context.ProductosMovimientos.Add(new ProductosMovimiento
                    {
                        FkProducto = item.ProductoId,
                        TipoMovimiento = 2,
                        Descripcion = item.Descripcion,
                        StockAnt = stockAnterior,
                        StockAct = producto.Cantidad,
                        Costo = item.Costo,
                        Venta = item.Venta,
                        Cantidad = item.Cantidad,
                        FechaEntrega = item.FechaEntrega,
                        NroComprobante = item.NroComprobante,
                        PrecioProveedor = item.PrecioProveedor,
                        FechaMov = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    // 4️⃣ Progreso
                    procesados++;
                    progress.Report(procesados * 100 / total);
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
