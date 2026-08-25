using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Http;

namespace Comercial_Web.Pages.Productos.ABM
{
    [Authorize]
    [RequestSizeLimit(60 * 1024 * 1024)]   // 60 MB — cubre varias imágenes en base64 cargadas antes de guardar un alta
    [RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
    public class IndexModel : PageModel
    {
        private readonly IParametroService _parametroService;
        private readonly IRubroService _rubroService;
        private readonly IProveedorService _proveedorService;
        private readonly IProductoService _productoService;
        public List<Producto> Productos { get; set; } = new();
        public List<SelectListItem> Rubros { get; set; } = new();
        public List<ProovedoresSelectProductoDTO> Proveedores { get; set; } = new();
        public IndexModel(IParametroService parametroService, IRubroService rubroService, IProveedorService proveedorService, IProductoService productoService)
        {
            _parametroService = parametroService;
            _rubroService = rubroService;
            _proveedorService = proveedorService;
            _productoService = productoService;
        }

        [BindProperty]
        public ProductoDetallesDTO Producto { get; set; } = new();

        private async Task<int> ObtenerIndiceBusquedaDefectoAsync()
        {
            var parametro = await _parametroService.ObtenerValorAsync("productos", "indiceBusqueda");
            return int.TryParse(parametro, out var indice) ? indice : 0;
        }

        public async Task OnGetAsync()
        {
            var filtroDefecto = await ObtenerIndiceBusquedaDefectoAsync();
            ViewData["FiltroDefecto"] = filtroDefecto;
            await CargarCombosAsync();
        }

        private async Task CargarCombosAsync()
        {
            Rubros = (await _rubroService.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Descripcion }).ToList();

            Proveedores = (await _proveedorService.GetAllAsync())
                            .Select(v => new ProovedoresSelectProductoDTO
                            {
                                Id = v.Id,
                                NombreComercial = v.NombreComercial ?? "",
                                Ganancia = v.Ganancia ?? 0,
                                Descuento = v.Descuento ?? 0,
                                PreciosPorProducto = v.PreciosPorProducto
                            }).ToList();
        }

        public async Task<IActionResult> OnGetBuscarProductosAsync(int tipo, string valor)
        {
            valor = valor.ToLower();

            IEnumerable<Producto> productos;

            switch (tipo)
            {
                case 0: productos = await _productoService.GetByCodProveedorAsync(valor); break;
                case 1: productos = await _productoService.GetByCodBarrasAsync(valor);    break;
                case 2: productos = await _productoService.BuscarPorDescripcionAsync(valor); break;
                default: productos = Enumerable.Empty<Producto>(); break;
            }

            const string editSvg = "<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 16 16'>" +
                "<path d='M12.854.146a.5.5 0 0 0-.707 0L10.5 1.793 14.207 5.5l1.647-1.646a.5.5 0 0 0 0-.708l-3-3zm.646 6.061L9.793 2.5 3.293 9H3.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.207l6.5-6.5zm-7.468 7.468A.5.5 0 0 1 6 13.5V13h-.5a.5.5 0 0 1-.5-.5V12h-.5a.5.5 0 0 1-.5-.5V11h-.5a.5.5 0 0 1-.5-.5V10h-.5a.499.499 0 0 1-.175-.032l-.179.178a.5.5 0 0 0-.11.168l-2 5a.5.5 0 0 0 .65.65l5-2a.5.5 0 0 0 .168-.11l.178-.178z'/></svg>";

            const string delSvg = "<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 16 16'>" +
                "<path d='M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z'/>" +
                "<path fill-rule='evenodd' d='M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z'/></svg>";

            var html = new StringBuilder();
            foreach (var p in productos)
            {
                var desc    = System.Net.WebUtility.HtmlEncode(p.Descripcion  ?? "");
                var codProv = System.Net.WebUtility.HtmlEncode(p.CodProveedor ?? "");
                var codBar  = System.Net.WebUtility.HtmlEncode(p.CodBarras    ?? "");
                var descJs  = (p.Descripcion ?? "").Replace("\\", "\\\\").Replace("'", "\\'");

                html.AppendLine($@"
<tr onclick=""mostrarDetalle({p.Id}, this)"" data-id=""{p.Id}"">
    <td>{codProv}</td>
    <td>{codBar}</td>
    <td>{desc}</td>
    <td onclick=""event.stopPropagation()"">
        <div class=""d-flex gap-1 justify-content-center"">
            <button class=""btn-accion btn-edit"" title=""Editar"" onclick=""abrirModalEditar({p.Id})"">{editSvg}</button>
            <button class=""btn-accion btn-delete"" title=""Dar de baja"" onclick=""confirmarEliminar({p.Id}, '{descJs}')"">{delSvg}</button>
        </div>
    </td>
</tr>");
            }

            return Content(html.ToString(), "text/html");
        }

        public async Task<IActionResult> OnGetDetalleAsync(int id)
        {
            var cantDec   = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();
            var producto  = await _productoService.traerDetalleAsync(id, cantDec, cantStock);
            if (producto is null)
                return Content("<p class='text-danger p-3'>Producto no encontrado.</p>", "text/html");

            // Helpers
            static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "—");

            var stockBajo   = producto.CantidadMinima.HasValue && producto.Cantidad < producto.CantidadMinima;
            var stockBadge  = stockBajo
                ? $"<span class='badge-stock-bajo'>Stock bajo ({producto.Cantidad})</span>"
                : $"<span class='badge-stock-ok'>En stock ({producto.Cantidad})</span>";

            // Galería de imágenes (imagenesProductos, servidas desde BD vía handler ImagenItem).
            // Fallback: si el producto todavía no tiene ninguna imagen en la galería nueva pero sí
            // tiene la imagen legacy (Productos.imagen), se sigue mostrando para no perderla de vista
            // durante la transición (la columna legacy no se toca, ver ObtenerImagenAsync/handler=Imagen).
            var imagenHtml = producto.Imagenes.Count > 0
                ? $@"<div class='detail-section text-center'>
                        <div style='display:flex;gap:6px;flex-wrap:wrap;justify-content:center;'>
                            {string.Join("", producto.Imagenes.Select(im => $@"
                            <div style='position:relative;'>
                                <img src='/Productos/ABM?handler=ImagenItem&amp;id={im.Id}'
                                     alt='Imagen producto'
                                     style='width:72px;height:72px;border-radius:8px;object-fit:cover;border:2px solid {(im.EsPrincipal ? "#4e73df" : "#e3e6f0")};padding:2px;' />
                                {(im.EsPrincipal ? "<span style='position:absolute;top:-4px;right:-4px;background:#4e73df;color:#fff;border-radius:50%;width:16px;height:16px;font-size:10px;display:flex;align-items:center;justify-content:center;' title='Principal'>★</span>" : "")}
                            </div>"))}
                        </div>
                     </div>"
                : !producto.TieneImagen
                    ? ""
                    : $@"<div class='detail-section text-center'>
                            <img src='/Productos/ABM?handler=Imagen&amp;id={producto.Id}'
                                 alt='Imagen producto'
                                 style='max-width:100%;max-height:180px;border-radius:8px;object-fit:contain;border:1px solid #e3e6f0;padding:4px;' />
                         </div>";

            // Descripción larga
            var descLargaHtml = string.IsNullOrWhiteSpace(producto.DescripcionLarga)
                ? ""
                : $@"<div class='detail-section'>
                        <div class='detail-section-title'>Descripción detallada</div>
                        <p style='font-size:.83rem;color:#4a5568;margin:0;white-space:pre-wrap;'>{H(producto.DescripcionLarga)}</p>
                     </div>";

            var html = $@"
<div class='detail-card shadow-sm border' style='border-color:#e3e6f0!important;'>

    <div class='detail-card-header'>
        <strong>{H(producto.Descripcion)}</strong>
        {stockBadge}
    </div>

    {imagenHtml}

    <div class='detail-section'>
        <div class='detail-section-title'>
            <svg xmlns='http://www.w3.org/2000/svg' width='11' height='11' fill='currentColor' viewBox='0 0 16 16' class='me-1'>
                <path d='M5.5 2a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5zM3 3.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0 2a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5z'/>
                <path d='M1 1a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V1zm1 0v14h12V1H2z'/>
            </svg>
            Identificación
        </div>
        <div class='detail-item'><label>Cód. Producto</label><span>#{producto.Id}</span></div>
        <div class='detail-item'><label>Cód. Proveedor</label><span>{H(producto.CodProveedor)}</span></div>
        <div class='detail-item'><label>Cód. Barras</label><span>{H(producto.CodBarras)}</span></div>
    </div>

    <div class='detail-section'>
        <div class='detail-section-title'>
            <svg xmlns='http://www.w3.org/2000/svg' width='11' height='11' fill='currentColor' viewBox='0 0 16 16' class='me-1'>
                <path d='M4 10.781c.148 1.667 1.513 2.85 3.591 3.003V15h1.043v-1.216c2.27-.179 3.678-1.438 3.678-3.3 0-1.59-.947-2.51-2.956-3.028l-.722-.187V3.467c1.122.11 1.879.714 2.07 1.616h1.47c-.166-1.6-1.54-2.748-3.54-2.875V1H7.591v1.233c-1.939.23-3.27 1.472-3.27 3.156 0 1.454.966 2.483 2.661 2.917l.61.162v4.031c-1.149-.17-1.95-.8-2.161-1.718H4zm6.137-3.115c0 1.23-.627 1.955-1.96 2.164V7.677c1.116.248 1.96.905 1.96 1.989zm-5.05-1.9c0-1.064.603-1.796 1.719-2.019V8.21c-1.112-.244-1.719-.895-1.719-2.024z'/>
            </svg>
            Precios
        </div>
        <div class='detail-item'><label>P. Lista</label><span>{producto.Precio?.ToString("N2") ?? "—"}</span></div>
        <div class='detail-item'><label>P. Costo</label><span>{producto.Costo?.ToString("N2") ?? "—"}</span></div>
        <div class='detail-item'><label>P. Proveedor</label><span>{producto.PrecioProveedor?.ToString("N2") ?? "—"}</span></div>
    </div>

    <div class='detail-section'>
        <div class='detail-section-title'>
            <svg xmlns='http://www.w3.org/2000/svg' width='11' height='11' fill='currentColor' viewBox='0 0 16 16' class='me-1'>
                <path d='M0 1.5A.5.5 0 0 1 .5 1H2a.5.5 0 0 1 .485.379L2.89 3H14.5a.5.5 0 0 1 .491.592l-1.5 8A.5.5 0 0 1 13 12H4a.5.5 0 0 1-.491-.408L2.01 3.607 1.61 2H.5a.5.5 0 0 1-.5-.5z'/>
            </svg>
            Stock &amp; Clasificación
        </div>
        <div class='detail-item'><label>Stock actual</label><span>{producto.Cantidad}</span></div>
        <div class='detail-item'><label>Stock mínimo</label><span>{producto.CantidadMinima}</span></div>
        <div class='detail-item'><label>Rubro</label><span>{H(producto.Rubro)}</span></div>
        <div class='detail-item'><label>Proveedor</label><span>{H(producto.Proveedor)}</span></div>
    </div>

    {descLargaHtml}

</div>";

            return Content(html, "text/html");
        }

        // ── Servir imagen de un producto desde BD ─────────────────────────────
        public async Task<IActionResult> OnGetImagenAsync(int id)
        {
            // Cargamos solo la columna imagen para no traer todo el producto
            var bytes = await _productoService.ObtenerImagenAsync(id);
            if (bytes == null || bytes.Length == 0)
                return NotFound();

            var contentType = _DetectarContentType(bytes);
            return File(bytes, contentType);
        }

        private static string _DetectarContentType(byte[] data)
        {
            if (data.Length >= 2)
            {
                if (data[0] == 0xFF && data[1] == 0xD8) return "image/jpeg";
                if (data[0] == 0x89 && data[1] == 0x50) return "image/png";
                if (data[0] == 0x47 && data[1] == 0x49) return "image/gif";
                // WEBP: "RIFF....WEBP"
                if (data.Length >= 12 &&
                    data[0] == 0x52 && data[1] == 0x49 &&
                    data[8] == 0x57 && data[9] == 0x45)  return "image/webp";
            }
            return "image/jpeg";
        }

        // ── Subir imagen → convierte a base64 y lo devuelve al cliente ────────
        // El base64 viaja en el hidden field del formulario y se guarda en BD al hacer POST Guardar.
        public async Task<IActionResult> OnPostSubirImagenAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return new JsonResult(new { ok = false, msg = "No se recibió archivo." });

            // Validar extensión
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var extPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!extPermitidas.Contains(ext))
                return new JsonResult(new { ok = false, msg = "Extensión no permitida. Use JPG, PNG, GIF o WEBP." });

            // Validar tamaño (máx 5 MB)
            if (archivo.Length > 5 * 1024 * 1024)
                return new JsonResult(new { ok = false, msg = "El archivo supera el límite de 5 MB." });

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Devolvemos base64 con prefijo para usar directamente como src de <img>
            var mime   = _DetectarContentType(bytes);
            var b64    = Convert.ToBase64String(bytes);
            var dataUrl = $"data:{mime};base64,{b64}";

            return new JsonResult(new { ok = true, dataUrl });
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                .Where(ms => ms.Value.Errors.Count > 0)
                .Select(ms => new { Campo = ms.Key, Errores = ms.Value.Errors.Select(e => e.ErrorMessage) })
                .ToList();

                // Loguear o inspeccionar errores
                foreach (var e in errores)
                {
                    Console.WriteLine($"Campo: {e.Campo}, Errores: {string.Join(",", e.Errores)}");
                }

                // Si hay errores de validaci�n, volvemos a mostrar la p�gina con la grilla y combos
                await CargarCombosAsync();
                //await CargarGrillaAsync();
                return Page();
            }

            // Regla de porcentajes por producto: solo se persisten si el proveedor
            // tiene preciosPorProducto = 1. En cualquier otro caso se fuerzan a NULL
            // (no se confía en el cliente; se valida contra la BD).
            var proveedorSel = Producto.FkProveedor.HasValue
                ? await _proveedorService.GetByIdAsync(Producto.FkProveedor.Value)
                : null;
            if (proveedorSel?.PreciosPorProducto != true)
            {
                Producto.Ganancia  = null;
                Producto.Descuento = null;
            }

            if (Producto.Id == 0)
            {
                Producto.PrecioProveedor = decimal.Parse(Request.Form["Producto.PrecioProveedor"], CultureInfo.InvariantCulture);
                Producto.PrecioProveedor = decimal.Parse(Request.Form["Producto.PrecioProveedor"], new CultureInfo("es-AR"));



                // Alta
                await _productoService.CrearAsync(Producto);
            }
            else
            {
                // Edici�n
                await _productoService.ActualizarAsync(Producto);
            }

            // Recargamos la grilla y volvemos a la p�gina principal
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetProductoAsync(int id)
        {
            var cantDec   = await _parametroService.ObtenerCantidadDecimalesProductosAsync();
            var cantStock = await _parametroService.ObtenerCantidadDecimalesStockAsync();
            var producto  = await _productoService.traerDetalleAsync(id, cantDec, cantStock);
            if (producto is null)
                return NotFound();

            // No enviamos el blob al cliente; la UI muestra la imagen vía /handler=Imagen
            return new JsonResult(producto, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        // ══════════════════════════════════════════════════════════════════
        //  Múltiples imágenes (imagenesProductos)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Sirve el binario de una imagen puntual de imagenesProductos (por su Id de fila).</summary>
        public async Task<IActionResult> OnGetImagenItemAsync(int id)
        {
            var item = await _productoService.ObtenerImagenItemAsync(id);
            if (item == null || item.Value.Bytes.Length == 0)
                return NotFound();

            var contentType = item.Value.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = _DetectarContentType(item.Value.Bytes);

            return File(item.Value.Bytes, contentType);
        }

        /// <summary>
        /// Sube una imagen para la galería de un producto.
        /// - productoId > 0 (edición): se persiste de inmediato en imagenesProductos y se devuelve su Id.
        /// - productoId == 0 (alta): el producto todavía no existe; se devuelve el dataUrl para que el
        ///   cliente lo mantenga en una lista pendiente y se envíe con el resto del formulario al Guardar.
        /// </summary>
        public async Task<IActionResult> OnPostSubirImagenProductoAsync(IFormFile archivo, int productoId, bool esPrincipal)
        {
            if (archivo == null || archivo.Length == 0)
                return new JsonResult(new { ok = false, msg = "No se recibió archivo." });

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var extPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!extPermitidas.Contains(ext))
                return new JsonResult(new { ok = false, msg = "Extensión no permitida. Use JPG, PNG, GIF o WEBP." });

            if (archivo.Length > 5 * 1024 * 1024)
                return new JsonResult(new { ok = false, msg = "El archivo supera el límite de 5 MB." });

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var mime  = _DetectarContentType(bytes);

            if (productoId > 0)
            {
                var img = await _productoService.AgregarImagenAsync(productoId, bytes, mime, esPrincipal);
                return new JsonResult(new
                {
                    ok = true,
                    id = img.Id,
                    esPrincipal = img.EsPrincipal,
                    orden = img.Orden,
                    url = $"/Productos/ABM?handler=ImagenItem&id={img.Id}"
                });
            }

            // Alta: todavía no hay producto; devolvemos el dataUrl para la lista pendiente del cliente.
            var b64     = Convert.ToBase64String(bytes);
            var dataUrl = $"data:{mime};base64,{b64}";
            return new JsonResult(new { ok = true, dataUrl });
        }

        /// <summary>Baja lógica de una imagen (edición de producto existente).</summary>
        public async Task<IActionResult> OnPostEliminarImagenAsync(int id)
        {
            await _productoService.EliminarImagenAsync(id);
            return new JsonResult(new { ok = true });
        }

        /// <summary>Marca una imagen como principal, garantizando que sea la única para ese producto.</summary>
        public async Task<IActionResult> OnPostMarcarPrincipalAsync(int idProducto, int id)
        {
            await _productoService.MarcarPrincipalAsync(idProducto, id);
            return new JsonResult(new { ok = true });
        }

        /// <summary>Mueve una imagen un lugar hacia arriba o abajo dentro del orden de la galería.</summary>
        public async Task<IActionResult> OnPostMoverImagenAsync(int idProducto, int id, string direccion)
        {
            await _productoService.MoverImagenAsync(idProducto, id, direccion == "arriba");
            return new JsonResult(new { ok = true });
        }
    }
}
