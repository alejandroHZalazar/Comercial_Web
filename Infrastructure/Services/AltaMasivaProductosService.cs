using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class AltaMasivaProductosService : IAltaMasivaProductosService
    {
        private readonly ComercialDbContext _db;

        public AltaMasivaProductosService(ComercialDbContext db) => _db = db;

        // ---------------------------------------------------------------
        public async Task<bool> ExisteProductoAsync(string codProveedor, int fkProveedor)
        {
            return await _db.Productos.AnyAsync(p =>
                p.CodProveedor == codProveedor.Trim().ToUpperInvariant() &&
                p.FkProveedor  == fkProveedor &&
                p.Baja         != true);
        }

        // ---------------------------------------------------------------
        public async Task<AltaMasivaResultDto> CrearProductoAsync(AltaMasivaFilaDto fila)
        {
            // Obtener IVA por defecto desde parámetros (equivale a SELECT valor FROM parametros WHERE modulo='productos' AND parametro='sinIVA')
            var sinIvaRaw = await _db.Parametros
                .Where(p => p.Modulo == "productos" && p.Parametro1 == "sinIVA")
                .Select(p => p.Valor)
                .FirstOrDefaultAsync();

            int ivaDefecto = int.TryParse(sinIvaRaw, out var iv) ? iv : 0;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Producto
                var producto = new Producto
                {
                    CodProveedor = fila.CodProveedor.Trim().ToUpperInvariant(),
                    CodBarras    = fila.CodBarras.Trim(),
                    FkRubro      = fila.FkRubro,
                    Iva          = ivaDefecto,
                    Descripcion  = fila.Descripcion.Trim().ToUpperInvariant(),
                    FkProveedor  = fila.FkProveedor,
                    Baja         = false,
                    Fraccionado  = false,
                    Dolarizado   = fila.EsDolarizado
                };
                _db.Productos.Add(producto);
                await _db.SaveChangesAsync();   // necesario para obtener el Id generado

                int prodId = producto.Id;

                // 2. Costo
                _db.CostosProductos.Add(new CostosProducto
                {
                    FkProducto = prodId,
                    Costo      = fila.Costo
                });

                // 3. Precio de lista
                _db.PreciosProductos.Add(new PreciosProducto
                {
                    FkProducto = prodId,
                    Precio     = fila.PrecioLista
                });

                // 4. Stock
                _db.StockProductos.Add(new StockProducto
                {
                    FkProducto       = prodId,
                    Cantidad         = fila.Stock,
                    CantidadMinima   = fila.CantMinima
                });

                // 5. Precio proveedor
                _db.PreciosProveedores.Add(new PreciosProveedore
                {
                    FkProducto = prodId,
                    Precio     = fila.PrecioProveedor
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new AltaMasivaResultDto
                {
                    Success    = true,
                    ProductoId = prodId,
                    Mensaje    = $"Producto #{prodId} creado correctamente."
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new AltaMasivaResultDto
                {
                    Success = false,
                    Mensaje = $"Error al insertar: {ex.Message}"
                };
            }
        }
    }
}
