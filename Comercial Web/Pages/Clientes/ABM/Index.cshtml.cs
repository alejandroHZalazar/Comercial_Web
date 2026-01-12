using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace Comercial_Web.Pages.Clientes.ABM
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IParametroService _parametroService;
        private readonly IClienteService _clienteService;
        private readonly ICondicionIvaService _condicionIvaService;
        private readonly IUsuarioService _usuarioService;
        private readonly ILocalidadService _localidadService;
        private readonly IZonaClienteService _zonaClienteService;

        public IndexModel (IParametroService parametroService, IClienteService clienteService, ICondicionIvaService condicionIvaService, IUsuarioService usuarioService, ILocalidadService localidadService, IZonaClienteService zonaClienteService)
        {
            _parametroService = parametroService;
            _clienteService = clienteService;
            _condicionIvaService = condicionIvaService;
            _usuarioService = usuarioService;
            _localidadService = localidadService;
            _zonaClienteService = zonaClienteService;
        }

        public List<Cliente> Clientes { get; set; } = new();
        public List<SelectListItem> CondicionIvas { get; set; } = new();
        public List<SelectListItem> Vendedores { get; set; } = new();
        public List<SelectListItem> Localidades { get; set; } = new();
        public List<SelectListItem> Zonas { get; set; } = new();

        [BindProperty]
        public Cliente Cliente { get; set; } = new();


        public async Task OnGetAsync()
        {
            var filtroDefecto = await ObtenerIndiceBusquedaDefectoAsync();
            ViewData["FiltroDefecto"] = filtroDefecto;
            await CargarCombosAsync();
            Clientes = await _clienteService.GetAllAsync();
            
        }

        private async Task CargarCombosAsync()
        {
            CondicionIvas = (await _condicionIvaService.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Descripcion }).ToList();

            Vendedores = (await _usuarioService.GetAllAsync())
                .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Nombre }).ToList();

            Localidades = (await _localidadService.GetAllAsync())
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Nombre }).ToList();

            Zonas = (await _zonaClienteService.GetAllAsync())
                .Select(z => new SelectListItem { Value = z.Id.ToString(), Text = z.Nombre }).ToList();
        }

        private async Task<int> ObtenerIndiceBusquedaDefectoAsync()
        {
            var parametro = await _parametroService.ObtenerValorAsync("clientes", "indiceBusqueda");
            return int.TryParse(parametro, out var indice) ? indice : 0;
        }

        public async Task<IActionResult> OnGetBuscarClientesAsync(int tipo, string valor)
        {
            valor = valor.ToLower();

            IEnumerable<Cliente> clientes;

            switch (tipo)
            {
                case 0: // Nombre Comercial
                    clientes = await _clienteService.BuscarAsync(tipo,valor);
                    break;

                case 1: // Razón Social
                    clientes = await _clienteService.BuscarAsync(tipo, valor);
                    break;

                case 2: // CUIL
                    clientes = await _clienteService.BuscarAsync(tipo, valor);
                    break;

                case 3: // Localidad (consulta física)
                    clientes = await _clienteService.BuscarPorLocalidadAsync(valor);
                    break;

                case 4: // Zona (consulta física)
                    clientes = await _clienteService.BuscarPorZonaAsync(valor);
                    break;

                default:
                    clientes = Enumerable.Empty<Cliente>();
                    break;
            }

            var html = new StringBuilder();
            foreach (var c in clientes)
            {
                        html.AppendLine($@"
                <tr onclick=""mostrarDetalle({c.Id})"">
                    <td>{c.Id}</td>
                    <td>{c.NombreComercial}</td>
                    <td>{c.RazonSocial}</td>
                    <td>
                       <button class='btn btn-outline-danger btn-sm' onclick='eliminarCliente(event, {c.Id})'>Eliminar</button>
                    </td>
                </tr>");
            }

            return Content(html.ToString(), "text/html");
        }

        public async Task<IActionResult> OnGetDetalleAsync(int id)
        {
            var cliente = await _clienteService.traerDetalleAsync(id);
            if (cliente is null)
                return Content("<p class='text-danger'>Cliente no encontrado.</p>", "text/html");

            // Armamos el HTML que se inyectará en el panel de detalle
            var html = $@"
            <div class='card shadow-sm border-0'>
                <div class='card-header bg-info text-white'>
                    <h5 class='mb-0'>Detalle del Cliente</h5>
                </div>
                <div class='card-body p-0'>
                    <ul class='list-group list-group-flush'>
                        <li class='list-group-item'><strong>Id:</strong> {cliente.Id}</li>
                        <li class='list-group-item'><strong>Nombre Comercial:</strong> {cliente.NombreComercial}</li>
                        <li class='list-group-item'><strong>Razón Social:</strong> {cliente.RazonSocial}</li>
                        <li class='list-group-item'><strong>CUIL:</strong> {cliente.Cuil}</li>
                        <li class='list-group-item'><strong>Dirección:</strong> {cliente.Direccion}</li>
                        <li class='list-group-item'><strong>Localidad:</strong> {cliente.LocalidadDescripcion}</li>
                        <li class='list-group-item'><strong>Provincia:</strong> {cliente.ProvinciaDescripcion}</li>
                        <li class='list-group-item'><strong>Zona:</strong> {cliente.ZonaDescripcion}</li>
                        <li class='list-group-item'><strong>Email:</strong> {cliente.Email}</li>
                        <li class='list-group-item'><strong>Teléfono:</strong> {cliente.Telefono}</li>
                        <li class='list-group-item'><strong>Celular:</strong> {cliente.Celular}</li>
                        <li class='list-group-item'><strong>Contacto:</strong> {cliente.Contacto}</li>
                        <li class='list-group-item'><strong>Cond. IVA:</strong> {cliente.CondicionIva}</li>
                        <li class='list-group-item'><strong>Vendedor:</strong> {cliente.Vendedor}</li>
                    </ul>
                </div>
            </div>";

            return Content(html, "text/html");

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

                // Si hay errores de validación, volvemos a mostrar la página con la grilla y combos
                await CargarCombosAsync();
                //await CargarGrillaAsync();
                return Page();
            }

            if (Cliente.Id == 0)
            {
                // Alta
                await _clienteService.CreateAsync(Cliente);
            }
            else
            {
                // Edición
                await _clienteService.UpdateAsync(Cliente);
            }

            // Recargamos la grilla y volvemos a la página principal
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostEliminarAsync([FromForm] int id)
        {
            await _clienteService.DeleteAsync(id);
            return new JsonResult(new { success = true });
        }
        public async Task<IActionResult> OnGetClienteAsync(int id)
        {
            var cliente = await _clienteService.traerDetalleAsync(id);
            if (cliente is null)
                return NotFound();

            return new JsonResult(cliente);
        }


    }
}
