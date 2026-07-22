using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CambiosPreciosMasivoService : ICambiosPreciosMasivoService
{
    private readonly ComercialDbContext _db;

    public CambiosPreciosMasivoService(ComercialDbContext db) => _db = db;

    // ================================================================
    // Emulación LINQ del sp_Productos_CambiarPreciosMasivos
    // (sin la rama INSERT en ProductosACrear — eso queda en el cliente)
    // ================================================================
    public async Task<CambioPrecioResultDto> CambiarPrecioAsync(FilaCsvPrecioDto fila)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Resolver CodProveedor: si viene vacío buscar por CodBarras
            var codProv = fila.Codigo?.Trim();
            if (string.IsNullOrEmpty(codProv) && !string.IsNullOrEmpty(fila.CodBarras))
            {
                codProv = await _db.Productos
                    .Where(p => p.CodBarras == fila.CodBarras.Trim())
                    .Select(p => p.CodProveedor)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrEmpty(codProv))
                return new CambioPrecioResultDto
                {
                    Success    = false,
                    EsNuevo    = true,
                    Mensaje    = "No se encontró el producto.",
                    CodProveedor = fila.Codigo,
                    Descripcion  = fila.Descripcion
                };

            // 2. ¿Existe el producto?
            var existe = await _db.Productos
                .AnyAsync(p => p.CodProveedor == codProv);

            if (!existe)
                return new CambioPrecioResultDto
                {
                    Success      = false,
                    EsNuevo      = true,
                    Mensaje      = "Producto no encontrado en la base de datos.",
                    CodProveedor = codProv,
                    Descripcion  = fila.Descripcion
                };

            // 3. Obtener el producto específico del proveedor
            // Nota: FkProveedor es int? → hay que comparar con int? para que EF Core
            // pueda construir el árbol de expresión sin excepción.
            int? idProvNullable = fila.IdProveedor;
            var producto = await _db.Productos
                .FirstOrDefaultAsync(p =>
                    p.CodProveedor == codProv &&
                    p.FkProveedor  == idProvNullable);

            if (producto == null)
            {
                // Existe con otro proveedor → lo tomamos sin filtrar por proveedor
                producto = await _db.Productos
                    .FirstOrDefaultAsync(p => p.CodProveedor == codProv);
            }

            if (producto == null)
                return new CambioPrecioResultDto
                {
                    Success      = false,
                    EsNuevo      = true,
                    Mensaje      = "Producto no encontrado.",
                    CodProveedor = codProv,
                    Descripcion  = fila.Descripcion
                };

            int prodId = producto.Id;

            // 4. Obtener ganancia y descuento
            //    - preciosPorProducto = 1 → se toman de Productos (ganancia/descuento del producto; NULL = 0)
            //    - NULL o 0             → comportamiento actual: se toman del proveedor
            int idProvFinal = producto.FkProveedor ?? fila.IdProveedor;
            var proveedor = await _db.Proveedores
                .Where(p => p.Id == idProvFinal)
                .Select(p => new { p.Ganancia, p.Descuento, p.PreciosPorProducto })
                .FirstOrDefaultAsync();

            decimal ganancia;
            decimal descuento;
            if (proveedor?.PreciosPorProducto == true)
            {
                ganancia  = producto.Ganancia  ?? 0m;
                descuento = producto.Descuento ?? 0m;
            }
            else
            {
                ganancia  = proveedor?.Ganancia  ?? 0m;
                descuento = proveedor?.Descuento ?? 0m;
            }

            // 5. Resguardar precios anteriores en productosLog
            //    DELETE previo + INSERT (igual que el SP)
            var logExistente = await _db.ProductosLogs
                .Where(l => l.FkProducto == prodId)
                .ToListAsync();
            _db.ProductosLogs.RemoveRange(logExistente);

            var precioPrevProv  = await _db.PreciosProveedores
                .Where(p => p.FkProducto == prodId).Select(p => p.Precio).FirstOrDefaultAsync();
            var precioPrevLista = await _db.PreciosProductos
                .Where(p => p.FkProducto == prodId).Select(p => p.Precio).FirstOrDefaultAsync();
            var precioPrevCosto = await _db.CostosProductos
                .Where(p => p.FkProducto == prodId).Select(p => p.Costo).FirstOrDefaultAsync();

            _db.ProductosLogs.Add(new ProductosLog
            {
                FkProducto  = prodId,
                PrecioProv  = precioPrevProv,
                PrecioLista = precioPrevLista,
                PrecioCosto = precioPrevCosto,
                ModifDate   = DateOnly.FromDateTime(DateTime.Now)
            });

            // 6. Calcular nuevos precios
            decimal nuevoCosto = fila.Precio * (1m - descuento / 100m);
            decimal nuevoLista = fila.Precio * (1m + ganancia  / 100m);

            // 7. Actualizar costosProductos
            var costo = await _db.CostosProductos
                .FirstOrDefaultAsync(c => c.FkProducto == prodId);
            if (costo != null)
                costo.Costo = nuevoCosto;
            else
                _db.CostosProductos.Add(new CostosProducto { FkProducto = prodId, Costo = nuevoCosto });

            // 8. Actualizar preciosProductos
            var lista = await _db.PreciosProductos
                .FirstOrDefaultAsync(p => p.FkProducto == prodId);
            if (lista != null)
                lista.Precio = nuevoLista;
            else
                _db.PreciosProductos.Add(new PreciosProducto { FkProducto = prodId, Precio = nuevoLista });

            // 9. Actualizar preciosProveedores
            var pprov = await _db.PreciosProveedores
                .FirstOrDefaultAsync(p => p.FkProducto == prodId);
            if (pprov != null)
                pprov.Precio = fila.Precio;
            else
                _db.PreciosProveedores.Add(new PreciosProveedore { FkProducto = prodId, Precio = fila.Precio });

            // 10. Dar de alta si estaba de baja
            producto.Baja = false;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new CambioPrecioResultDto
            {
                Success      = true,
                EsNuevo      = false,
                Mensaje      = $"Precio actualizado → Costo: {nuevoCosto:N2} | Lista: {nuevoLista:N2}",
                ProductoId   = prodId,
                CodProveedor = codProv,
                Descripcion  = producto.Descripcion ?? fila.Descripcion
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new CambioPrecioResultDto
            {
                Success      = false,
                EsNuevo      = false,
                Mensaje      = $"Error: {ex.Message}",
                CodProveedor = fila.Codigo,
                Descripcion  = fila.Descripcion
            };
        }
    }

    // ================================================================
    // Insertar producto nuevo (desde la grilla de no encontrados)
    // Reutiliza la misma lógica que AltaMasivaProductosService
    // ================================================================
    public async Task<InsertarProductoNuevoResult> InsertarProductoNuevoAsync(
        InsertarProductoNuevoRequest req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Validar duplicado
            int? idProvNew = req.IdProveedor;
            var existe = await _db.Productos.AnyAsync(p =>
                p.CodProveedor == req.CodProveedor &&
                p.FkProveedor  == idProvNew        &&
                p.Baja != true);

            if (existe)
                return new InsertarProductoNuevoResult
                {
                    Success = false,
                    Mensaje = "El producto ya existe en la base de datos."
                };

            // Obtener parámetro sinIVA
            var sinIvaStr = await _db.Parametros
                .Where(p => p.Modulo == "productos" && p.Parametro1 == "dolarizaProductos")
                .Select(p => p.Valor)
                .FirstOrDefaultAsync();
            // Ganancia / descuento
            //   - preciosPorProducto = 1 → se toman del CSV (req.Ganancia/Descuento; NULL = 0)
            //                                y se persisten en Productos.ganancia/descuento
            //   - NULL o 0             → comportamiento actual: se toman del proveedor
            //                                y NO se persisten en Productos (quedan NULL)
            var prov = await _db.Proveedores
                .Where(p => p.Id == req.IdProveedor)
                .Select(p => new { p.Ganancia, p.Descuento, p.PreciosPorProducto })
                .FirstOrDefaultAsync();

            decimal ganancia, descuento;
            decimal? gananciaProducto = null;   // lo que se guarda en Productos
            decimal? descuentoProducto = null;
            if (prov?.PreciosPorProducto == true)
            {
                gananciaProducto  = req.Ganancia;
                descuentoProducto = req.Descuento;
                ganancia  = req.Ganancia  ?? 0m;
                descuento = req.Descuento ?? 0m;
            }
            else
            {
                ganancia  = prov?.Ganancia  ?? 0m;
                descuento = prov?.Descuento ?? 0m;
            }

            decimal nuevoCosto = req.PrecioProv * (1m - descuento / 100m);
            decimal nuevoLista = req.PrecioProv * (1m + ganancia  / 100m);

            // Insertar Producto
            var prod = new Producto
            {
                CodProveedor = req.CodProveedor.Trim().ToUpperInvariant(),
                CodBarras    = req.CodBarras.Trim(),
                Descripcion  = req.Descripcion.Trim().ToUpperInvariant(),
                FkProveedor  = req.IdProveedor,
                FkRubro      = 7,
                Iva          = 1,
                Baja         = false,
                Fraccionado  = false,
                Dolarizado   = false,
                Ganancia     = gananciaProducto,
                Descuento    = descuentoProducto
            };
            _db.Productos.Add(prod);
            await _db.SaveChangesAsync();   // obtiene prod.Id

            _db.CostosProductos.Add(   new CostosProducto    { FkProducto = prod.Id, Costo  = nuevoCosto        });
            _db.PreciosProductos.Add(  new PreciosProducto   { FkProducto = prod.Id, Precio = nuevoLista        });
            _db.PreciosProveedores.Add(new PreciosProveedore  { FkProducto = prod.Id, Precio = req.PrecioProv   });
            _db.StockProductos.Add(    new StockProducto      { FkProducto = prod.Id, Cantidad = req.StockInicial, CantidadMinima = req.CantMinima });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new InsertarProductoNuevoResult
            {
                Success    = true,
                Mensaje    = "Producto creado correctamente.",
                ProductoId = prod.Id
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new InsertarProductoNuevoResult
            {
                Success = false,
                Mensaje = $"Error: {ex.Message}"
            };
        }
    }
}
