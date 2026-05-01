using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PromocionService : IPromocionService
{
    private readonly ComercialDbContext _db;

    public PromocionService(ComercialDbContext db) => _db = db;

    // ── Lista ────────────────────────────────────────────────────────────────
    public async Task<List<PromocionListDto>> GetListaAsync()
    {
        // Traer datos principales sin subconsulta correlacionada
        // (EF Core/MySQL 5.5 genera nombres de columna por convención en subqueries)
        var raw = await (
            from pr in _db.Promociones
            join p  in _db.Productos on pr.FkProducto equals p.Id into pj
            from p  in pj.DefaultIfEmpty()
            orderby p.Descripcion
            select new
            {
                pr.Id,
                pr.FkProducto,
                NombreProducto = p.Descripcion ?? "(sin nombre)",
                p.CodBarras,
                pr.Activa,
                pr.FechaDesde,
                pr.FechaHasta
            }
        ).ToListAsync();

        // Contar slots por promoción en consulta aparte
        var ids       = raw.Select(x => x.Id).ToList();
        var conteos   = await _db.PromocionSlots
            .Where(s => ids.Contains(s.FkPromocion))
            .GroupBy(s => s.FkPromocion)
            .Select(g => new { PromoId = g.Key, Total = g.Count() })
            .ToListAsync();
        var conteoMap = conteos.ToDictionary(x => x.PromoId, x => x.Total);

        return raw.Select(x => new PromocionListDto
        {
            Id             = x.Id,
            FkProducto     = x.FkProducto,
            NombreProducto = x.NombreProducto,
            CodBarras      = x.CodBarras,
            Activa         = x.Activa,
            FechaDesde     = x.FechaDesde,
            FechaHasta     = x.FechaHasta,
            CantidadSlots  = conteoMap.GetValueOrDefault(x.Id, 0)
        }).ToList();
    }

    // ── Detalle completo (para edición) ──────────────────────────────────────
    public async Task<PromocionDetalleDto?> GetDetalleAsync(int id)
    {
        var promo = await _db.Promociones.FindAsync(id);
        if (promo == null) return null;

        var prodBase = await _db.Productos
            .Where(p => p.Id == promo.FkProducto)
            .Select(p => p.Descripcion)
            .FirstOrDefaultAsync();

        var slots = await _db.PromocionSlots
            .Where(s => s.FkPromocion == id)
            .OrderBy(s => s.Numero)
            .ToListAsync();

        var slotIds = slots.Select(s => s.Id).ToList();

        var slotProds = await (
            from sp in _db.PromocionSlotProductos
            where slotIds.Contains(sp.FkSlot)
            join p  in _db.Productos on sp.FkProducto equals p.Id into pj
            from p  in pj.DefaultIfEmpty()
            select new
            {
                sp.Id,
                sp.FkSlot,
                sp.FkProducto,
                Descripcion  = p.Descripcion  ?? "",
                CodBarras    = p.CodBarras,
                CodProveedor = p.CodProveedor
            }
        ).ToListAsync();

        return new PromocionDetalleDto
        {
            Id             = promo.Id,
            FkProducto     = promo.FkProducto,
            NombreProducto = prodBase,
            Activa         = promo.Activa,
            FechaDesde = promo.FechaDesde,
            FechaHasta = promo.FechaHasta,
            Slots      = slots.Select(s => new SlotDto
            {
                Id                = s.Id,
                Numero            = s.Numero,
                CantidadRequerida = s.CantidadRequerida,
                Descripcion       = s.Descripcion,
                Productos         = slotProds
                    .Where(sp => sp.FkSlot == s.Id)
                    .Select(sp => new SlotProdDto
                    {
                        Id           = sp.Id,
                        FkProducto   = sp.FkProducto,
                        Descripcion  = sp.Descripcion,
                        CodBarras    = sp.CodBarras,
                        CodProveedor = sp.CodProveedor
                    }).ToList()
            }).ToList()
        };
    }

    // ── Guardar (insert o update) ─────────────────────────────────────────────
    public async Task<int> GuardarAsync(PromocionDetalleDto dto)
    {
        // Marcar el producto base como esPromocion = 1
        var prod = await _db.Productos.FindAsync(dto.FkProducto);
        if (prod != null)
        {
            prod.EsPromocion = true;
            _db.Productos.Update(prod);
        }

        Promocion promo;
        if (dto.Id == 0)
        {
            promo = new Promocion
            {
                FkProducto = dto.FkProducto,
                Activa     = dto.Activa,
                FechaDesde = dto.FechaDesde,
                FechaHasta = dto.FechaHasta
            };
            _db.Promociones.Add(promo);
            await _db.SaveChangesAsync();
        }
        else
        {
            promo = await _db.Promociones.FindAsync(dto.Id)
                    ?? throw new InvalidOperationException("Promocion no encontrada");
            promo.FkProducto = dto.FkProducto;
            promo.Activa     = dto.Activa;
            promo.FechaDesde = dto.FechaDesde;
            promo.FechaHasta = dto.FechaHasta;
            _db.Promociones.Update(promo);

            // Eliminar slots y slot-productos existentes
            var slotsViejos = await _db.PromocionSlots
                .Where(s => s.FkPromocion == promo.Id).ToListAsync();
            var slotIdsViejos = slotsViejos.Select(s => s.Id).ToList();
            var prodViejos = await _db.PromocionSlotProductos
                .Where(sp => slotIdsViejos.Contains(sp.FkSlot)).ToListAsync();

            _db.PromocionSlotProductos.RemoveRange(prodViejos);
            _db.PromocionSlots.RemoveRange(slotsViejos);
            await _db.SaveChangesAsync();
        }

        // Recrear slots
        int numero = 1;
        foreach (var slotDto in dto.Slots.OrderBy(s => s.Numero))
        {
            var slot = new PromocionSlot
            {
                FkPromocion       = promo.Id,
                Numero            = numero++,
                CantidadRequerida = slotDto.CantidadRequerida,
                Descripcion       = slotDto.Descripcion?.Trim()
            };
            _db.PromocionSlots.Add(slot);
            await _db.SaveChangesAsync();

            foreach (var p2 in slotDto.Productos)
            {
                _db.PromocionSlotProductos.Add(new PromocionSlotProducto
                {
                    FkSlot     = slot.Id,
                    FkProducto = p2.FkProducto
                });
            }
        }

        await _db.SaveChangesAsync();
        return promo.Id;
    }

    // ── Eliminar (lógico sobre el producto, físico sobre promo/slots) ────────
    public async Task EliminarAsync(int id)
    {
        var promo = await _db.Promociones.FindAsync(id);
        if (promo == null) return;

        // Limpiar esPromocion en el producto
        var prod = await _db.Productos.FindAsync(promo.FkProducto);
        if (prod != null) { prod.EsPromocion = false; _db.Productos.Update(prod); }

        // Eliminar slots y productos de slots
        var slots    = await _db.PromocionSlots.Where(s => s.FkPromocion == id).ToListAsync();
        var slotIds  = slots.Select(s => s.Id).ToList();
        var slotProds = await _db.PromocionSlotProductos
            .Where(sp => slotIds.Contains(sp.FkSlot)).ToListAsync();

        _db.PromocionSlotProductos.RemoveRange(slotProds);
        _db.PromocionSlots.RemoveRange(slots);
        _db.Promociones.Remove(promo);

        await _db.SaveChangesAsync();
    }

    // ── Activar / desactivar ─────────────────────────────────────────────────
    public async Task ToggleActivaAsync(int id)
    {
        var promo = await _db.Promociones.FindAsync(id);
        if (promo == null) return;
        promo.Activa = !promo.Activa;
        await _db.SaveChangesAsync();
    }

    // ── Para el modal de ventas ───────────────────────────────────────────────
    public async Task<PromocionParaVentaDto?> GetParaVentaAsync(int fkProducto)
    {
        var promo = await _db.Promociones
            .Where(p => p.FkProducto == fkProducto && p.Activa)
            .FirstOrDefaultAsync();
        if (promo == null) return null;

        var prodBase = await _db.Productos.FindAsync(fkProducto);

        var slots = await _db.PromocionSlots
            .Where(s => s.FkPromocion == promo.Id)
            .OrderBy(s => s.Numero)
            .ToListAsync();

        var slotIds  = slots.Select(s => s.Id).ToList();

        var slotProds = await (
            from sp in _db.PromocionSlotProductos
            where slotIds.Contains(sp.FkSlot)
            join p  in _db.Productos     on sp.FkProducto equals p.Id   into pj from p  in pj.DefaultIfEmpty()
            join st in _db.StockProductos on p.Id          equals st.FkProducto into stj from st in stj.DefaultIfEmpty()
            select new
            {
                sp.FkSlot,
                sp.FkProducto,
                Descripcion  = p.Descripcion  ?? "",
                CodBarras    = p.CodBarras,
                CodProveedor = p.CodProveedor,
                Stock        = st != null ? (st.Cantidad ?? 0m) : 0m
            }
        ).ToListAsync();

        return new PromocionParaVentaDto
        {
            PromocionId = promo.Id,
            FkProducto  = fkProducto,
            Nombre      = prodBase?.Descripcion ?? "",
            Slots       = slots.Select(s => new SlotVentaDto
            {
                SlotId            = s.Id,
                Numero            = s.Numero,
                CantidadRequerida = s.CantidadRequerida,
                Descripcion       = s.Descripcion,
                Productos         = slotProds
                    .Where(sp => sp.FkSlot == s.Id)
                    .Select(sp => new SlotProdVentaDto
                    {
                        FkProducto   = sp.FkProducto,
                        Descripcion  = sp.Descripcion,
                        CodBarras    = sp.CodBarras,
                        CodProveedor = sp.CodProveedor,
                        Stock        = sp.Stock
                    }).ToList()
            }).ToList()
        };
    }

    // ── Guardar componentes elegidos en la venta y descontar stock ────────────
    public async Task GuardarComponentesAsync(
        long fkVentaDetalle, IEnumerable<PromoComponenteDto> componentes)
    {
        foreach (var c in componentes)
        {
            _db.VentaPromoComponentes.Add(new VentaPromoComponente
            {
                FkVentaDetalle = fkVentaDetalle,
                FkSlot         = c.FkSlot,
                FkProducto     = c.FkProducto,
                Cantidad       = c.Cantidad
            });

            // Descontar stock del componente
            var stock = await _db.StockProductos
                .FirstOrDefaultAsync(s => s.FkProducto == c.FkProducto);
            if (stock != null)
                stock.Cantidad = (stock.Cantidad ?? 0m) - c.Cantidad;
        }

        await _db.SaveChangesAsync();
    }

    // ── Guardar por ventaId + fkProductoPromo (busca la linea automáticamente) ──
    public async Task GuardarComponentesPorVentaAsync(
        long ventaId, int fkProductoPromo, IEnumerable<PromoComponenteDto> componentes)
    {
        var detalle = await _db.VentasDetalles
            .Where(d => d.FkVenta == ventaId && d.FkProducto == fkProductoPromo)
            .FirstOrDefaultAsync();
        if (detalle == null) return;

        await GuardarComponentesAsync(detalle.Linea, componentes);
    }

    // ── Calcular máximas promociones formables desde el carrito ─────────────
    public async Task<CalcularPromocionesResultDto> CalcularPromocionesAsync(
        IEnumerable<ItemCarritoDto> carrito)
    {
        var result = new CalcularPromocionesResultDto();

        // 1. Cargar todas las promociones activas
        var promos = await _db.Promociones.Where(p => p.Activa).ToListAsync();
        if (!promos.Any()) return result;

        var promoIds = promos.Select(p => p.Id).ToList();

        var slots = await _db.PromocionSlots
            .Where(s => promoIds.Contains(s.FkPromocion))
            .OrderBy(s => s.Numero)
            .ToListAsync();

        var slotIds = slots.Select(s => s.Id).ToList();

        var slotProds = await _db.PromocionSlotProductos
            .Where(sp => slotIds.Contains(sp.FkSlot))
            .ToListAsync();

        // 2. Cargar nombres de todos los productos involucrados
        var slotProdIds    = slotProds.Select(sp => sp.FkProducto).Distinct().ToList();
        var promoProductoIds = promos.Select(p => p.FkProducto).Distinct().ToList();
        var allProdIds     = slotProdIds.Union(promoProductoIds).ToList();

        var productos = await _db.Productos
            .Where(p => allProdIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Descripcion, p.CodBarras })
            .ToListAsync();

        var prodMap = productos.ToDictionary(
            p => p.Id,
            p => (Descripcion: p.Descripcion, CodBarras: p.CodBarras));

        // 3. Disponible = copia mutable del carrito (agregando si el mismo fkProducto aparece varias veces)
        var disponible = new Dictionary<int, decimal>();
        foreach (var item in carrito)
            disponible[item.FkProducto] = disponible.GetValueOrDefault(item.FkProducto, 0m) + item.Cantidad;

        // 4. Greedy: para cada promo calcular cuántas veces se puede formar
        foreach (var promo in promos)
        {
            var slotsDePromo = slots.Where(s => s.FkPromocion == promo.Id).ToList();
            if (!slotsDePromo.Any()) continue;

            int maxVeces = int.MaxValue;
            foreach (var slot in slotsDePromo)
            {
                var eligible   = slotProds.Where(sp => sp.FkSlot == slot.Id).ToList();
                decimal totalDisp = eligible.Sum(sp => disponible.GetValueOrDefault(sp.FkProducto, 0m));
                int vecesSlot  = (int)Math.Floor(totalDisp / slot.CantidadRequerida);
                if (vecesSlot < maxVeces) maxVeces = vecesSlot;
            }

            if (maxVeces == int.MaxValue) maxVeces = 0;

            if (maxVeces == 0)
            {
                _verificarCasiCompleta(promo, slotsDePromo, slotProds, disponible, prodMap, result);
                continue;
            }

            // Registrar promo formada
            var formada = new PromocionFormadaDto
            {
                PromocionId     = promo.Id,
                FkProductoPromo = promo.FkProducto,
                NombrePromo     = prodMap.TryGetValue(promo.FkProducto, out var np) ? (np.Descripcion ?? "") : "",
                Cantidad        = maxVeces
            };

            // Descontar del disponible (slot a slot, producto con más stock primero)
            foreach (var slot in slotsDePromo)
            {
                decimal restante = slot.CantidadRequerida * maxVeces;
                var eligible = slotProds
                    .Where(sp => sp.FkSlot == slot.Id)
                    .OrderByDescending(sp => disponible.GetValueOrDefault(sp.FkProducto, 0m))
                    .ToList();

                foreach (var sp in eligible)
                {
                    if (restante <= 0m) break;
                    decimal usar = Math.Min(disponible.GetValueOrDefault(sp.FkProducto, 0m), restante);
                    if (usar <= 0m) continue;

                    disponible[sp.FkProducto] = disponible.GetValueOrDefault(sp.FkProducto, 0m) - usar;
                    restante -= usar;

                    formada.ComponentesUsados.Add(new ComponenteUsadoDto
                    {
                        FkSlot       = slot.Id,
                        FkProducto   = sp.FkProducto,
                        Descripcion  = prodMap.TryGetValue(sp.FkProducto, out var pp) ? (pp.Descripcion ?? "") : "",
                        CantidadUsada = usar
                    });
                }
            }

            result.Formadas.Add(formada);
            result.HayPromociones = true;
        }

        return result;
    }

    private void _verificarCasiCompleta(
        Promocion                                               promo,
        List<PromocionSlot>                                     slotsDePromo,
        List<PromocionSlotProducto>                             slotProds,
        Dictionary<int, decimal>                                disponible,
        Dictionary<int, (string? Descripcion, string? CodBarras)> prodMap,
        CalcularPromocionesResultDto                            result)
    {
        // Ver si formando UNA sola promo falta exactamente ≤1 unidad en exactamente 1 slot
        int     slotsFaltantes = 0;
        string  prodFaltante   = "";
        string? codFaltante    = null;
        decimal cantFaltante   = 0m;

        foreach (var slot in slotsDePromo)
        {
            var eligible    = slotProds.Where(sp => sp.FkSlot == slot.Id).ToList();
            decimal total   = eligible.Sum(sp => disponible.GetValueOrDefault(sp.FkProducto, 0m));
            decimal deficit = slot.CantidadRequerida - total;
            if (deficit > 0m)
            {
                slotsFaltantes++;
                if (deficit <= 1m)
                {
                    var primero = eligible.FirstOrDefault();
                    if (primero != null && prodMap.TryGetValue(primero.FkProducto, out var pi))
                    {
                        prodFaltante = pi.Descripcion ?? "";
                        codFaltante  = pi.CodBarras;
                        cantFaltante = deficit;
                    }
                }
            }
        }

        if (slotsFaltantes == 1 && cantFaltante > 0m)
        {
            result.CasiCompletas.Add(new PromocionCasiCompletaDto
            {
                PromocionId      = promo.Id,
                NombrePromo      = prodMap.ContainsKey(promo.FkProducto)
                                      ? (prodMap[promo.FkProducto].Descripcion ?? "") : "",
                ProductoFaltante  = prodFaltante,
                CodBarrasFaltante = codFaltante,
                CantidadFaltante  = cantFaltante
            });
        }
    }

    // ── Buscar productos disponibles para agregar a slots ────────────────────
    public async Task<List<SlotProdDto>> BuscarProductosAsync(string texto)
    {
        var t = texto.Trim().ToUpperInvariant();
        return await (
            from p in _db.Productos
            where p.Baja != true
               && (p.CodBarras   != null && p.CodBarras.Contains(t)
                || p.CodProveedor != null && p.CodProveedor.Contains(t)
                || p.Descripcion  != null && p.Descripcion.Contains(t))
            orderby p.Descripcion
            select new SlotProdDto
            {
                Id           = 0,
                FkProducto   = p.Id,
                Descripcion  = p.Descripcion ?? "",
                CodBarras    = p.CodBarras,
                CodProveedor = p.CodProveedor
            }
        )
        .Take(30)
        .ToListAsync();
    }
}
