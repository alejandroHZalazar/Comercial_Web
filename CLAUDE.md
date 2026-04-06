# Gestión Comercial — Guía de Proyecto

## Descripción

Aplicación web de gestión comercial para empresas. Maneja clientes, productos, proveedores, ventas, órdenes de compra, inventario, facturación IVA y estadísticas. Construida en .NET 8 con Razor Pages y MySQL.

---

## Estructura de la Solución

```
Comercial Web.sln
├── Domain/                  # Núcleo de negocio (sin dependencias)
│   ├── Entities/            # 32 entidades
│   ├── Contracts/           # 17 interfaces de servicios
│   ├── DTO/                 # 10+ Data Transfer Objects
│   └── ViewModel/           # View models
│
├── Application/             # Capa de aplicación (depende de Domain)
│   ├── Auth/                # ILoginService, LoginResult
│   └── Security/            # IPasswordService
│
├── Infrastructure/          # Acceso a datos (depende de Domain, Application)
│   ├── Data/
│   │   └── ComercialDbContext.cs   # EF Core DbContext (28 DbSets)
│   └── Services/            # 21 implementaciones de servicios
│
└── Comercial Web/           # Presentación (depende de Application, Infrastructure)
    ├── Pages/               # 29 Razor Pages
    ├── wwwroot/             # Archivos estáticos (css, js, images)
    └── Program.cs           # DI, middleware, configuración
```

**Flujo de dependencias:** `Domain` <-- `Application` <-- `Infrastructure` <-- `Comercial Web`

---

## Stack Tecnológico

| Componente | Tecnología | Versión |
|------------|-----------|---------|
| Runtime | .NET | 8.0 |
| Web | ASP.NET Core Razor Pages | 8.0 |
| ORM | Entity Framework Core | 9.0 |
| BD | MySQL (Pomelo driver) | 5.5 / Pomelo 9.0 |
| Auth | Cookie Authentication | ASP.NET Identity Hasher |
| Frontend | Bootstrap + jQuery | 5.x / 3.x |
| Reportes | ReportViewerCore | 15.1.29 |
| Cultura | es-AR | Argentina |

---

## Base de Datos

- **Motor:** MySQL 5.5.62
- **Host:** 72.61.47.240:3306
- **Database:** `ale`
- **Charset:** latin1_swedish_ci
- **Connection string:** en `appsettings.json` clave `"Default"`

### Tablas principales (~35)

**Maestros:** clientes, productos, proveedores, rubros, localidades, provincias, colores, clientesZonas
**Transacciones:** ventas, ventasDetalle, pedidos, pedidoDetalle, ordenCompra, ordenCompraDetalle, devoluciones, devolucionesDetalles
**Inventario:** stockProductos, preciosProductos, costosProductos, preciosProveedores, productosMovimientos, productosLog, ProductosACrear
**Configuración:** condIVA, ivaPorcentajes, impuestos, tipoPrecios, tipoValoresPrecios, tipoUsuarios, parametros
**Seguridad:** usuarios, menuPermisos, tipoDeUsuariosPermisos, permisos

---

## Mapa de Páginas

```
Pages/
├── Login / Logout                      Autenticación (Cookie, 8hs sliding)
├── Index                               Home protegido
├── Configuracion/
│   ├── CondicionIva/                   ABM condiciones IVA
│   ├── PorcentajeIva/                  ABM porcentajes IVA
│   ├── Rubros/                         ABM categorías de producto
│   ├── TipoPrecios/                    ABM tipos de precio + cotización USD
│   ├── TipoUsuarios/                   ABM roles + asignación de permisos
│   ├── Localidades/                    ABM localidades con provincias
│   ├── ZonasCliente/                   ABM zonas comerciales
│   └── Logo/                           Upload logo del sistema
├── Usuarios/                           ABM usuarios del sistema
├── Clientes/ABM/                       ABM clientes
├── Productos/
│   ├── ABM/                            ABM productos
│   └── IngresoProductos/               Recepción de mercadería
├── Proveedores/
│   ├── ABM/                            ABM proveedores
│   └── OrdenesCompra/                  Gestión de órdenes de compra
├── Estadistica/
│   ├── Ventas/                         Estadísticas de ventas con filtros
│   └── Ranking/                        Rankings de productos y clientes
└── Shared/
    ├── _Layout.cshtml                  Layout master (sidebar responsive)
    ├── _FormularioConfiguracion.cshtml  Partial genérico para formularios ABM
    ├── _GrillaConfiguracion.cshtml      Partial genérico para grillas ABM
    └── Components/MainMenu/            ViewComponent menú dinámico por permisos
```

---

## Patrones y Convenciones

### Arquitectura

- **Layered Architecture**: Domain -> Application -> Infrastructure -> Web
- **Service Pattern**: toda lógica de negocio en servicios con interface en Domain/Contracts e implementación en Infrastructure/Services
- **DI**: todos los servicios registrados como Scoped en Program.cs
- **Async/Await**: todas las operaciones de BD son async

### Entidades

- El campo `Baja` (bool) se usa para **soft delete** en entidades principales (clientes, productos, proveedores, localidades, provincias, zonas)
- Las propiedades se mapean a columnas MySQL en el DbContext con Fluent API
- Los nombres de tabla en BD son camelCase (`condIVA`, `stockProductos`, `menuPermisos`)
- Los nombres de columnas en BD son camelCase (`nombreComercial`, `codBarras`)

### Menú y Permisos

- **Tabla `menuPermisos`**: cada registro es un ítem de menú
  - **Menús padre**: `Id == 1` o `Id % 100 == 0` (ej: 100, 200, 300)
  - **Menús hijo**: los que están en el rango del padre (ej: 201-299 son hijos de 200)
- **Campo `Url`**: ruta Razor Page (ej: `/Configuracion/CondicionIva`)
- **Tabla `tipoDeUsuariosPermisos`**: relaciona TipoUsuario con MenuPermiso (clave compuesta)
- **MainMenuViewComponent**: lee permisos del usuario logueado y renderiza el menú accordion

### ABMs de Configuración

- Usan dos partials genéricos reutilizables:
  - `_FormularioConfiguracion.cshtml`: formulario con tipos de campo (Texto, Select, Textarea, CheckboxList, Password)
  - `_GrillaConfiguracion.cshtml`: tabla con columnas dinámicas + botones Editar/Eliminar
- Los campos se definen mediante `CampoVm` y las filas mediante `FilaVm`
- Se pasan al partial como `FormularioVm` y `GrillaVm`

### Servicios — Patrón de implementación

```csharp
// Interface en Domain/Contracts/
public interface IEntidadService
{
    Task<List<Entidad>> GetAllAsync();
    Task<Entidad?> GetByIdAsync(int id);
    Task<Entidad> CreateAsync(/* params */);
    Task UpdateAsync(int id, /* params */);
    Task DeleteAsync(int id);
}

// Implementación en Infrastructure/Services/
public class EntidadService : IEntidadService
{
    private readonly ComercialDbContext _db;
    public EntidadService(ComercialDbContext db) => _db = db;
    // CRUD con validación, Upper(), y SaveChangesAsync()
}
```

### Razor Pages — Patrón de PageModel

```csharp
[Authorize]
public class IndexModel : PageModel
{
    private readonly IEntidadService _service;

    public List<Entidad> Items { get; private set; } = new();

    [BindProperty] public int? Id { get; set; }
    [BindProperty] public string Campo { get; set; } = string.Empty;
    [BindProperty] public bool MostrarFormulario { get; set; }

    // Handlers: OnGetAsync, OnPostNuevoAsync, OnPostAgregarAsync,
    //           OnPostEditarAsync, OnGetEditarAsync(id), OnPostEliminarAsync(id)
}
```

### Tabla de Parámetros del Sistema

- **Tabla `parametros`**: almacena configuraciones clave-valor por módulo
- Columnas: `modulo`, `parametro` (Parametro1 en EF), `valor`, `imagen`
- Acceso via `IParametroService.ObtenerValorAsync(modulo, parametro)`
- Actualización via `IParametroService.ActualizarValorAsync(modulo, parametro, valor)`
- Ejemplos de parámetros:
  - `productos/dolarizaProductos` — flag para dolarización de precios
  - `productos/cotizacionDolar` — valor actual del dólar
  - `productos/decimales` — cantidad de decimales en precios
  - `productos/decimalesCant` — cantidad de decimales en stock
  - `notaPedido/indiceBusqueda` — índice de búsqueda en notas de pedido
  - `login/imagen` — logo del sistema

---

## Autenticación

- Cookie Authentication con 8hs de expiración (sliding)
- Login path: `/Login`, Logout path: `/Logout`
- Claims: `ClaimTypes.Name` (nombre usuario) + `TipoUsuario` (rol)
- Migración automática de passwords planos a hash (BCrypt via Identity Hasher)
- Páginas protegidas con atributo `[Authorize]`

---

## Registro de Servicios (Program.cs)

Todos registrados como **Scoped**:

```
IPasswordService, ILoginService, ICondicionIvaService, IPorcentajeIvaService,
IRubroService, ITipoPrecioService, ITipoUsuarioService, IMenuPermisoService,
ILocalidadService, IZonaClienteService, IParametroService, IUsuarioService,
IClienteService, IProveedorService, IVentasEstadisticasService,
IVentasRankingService, IProductoService, IOrdenCompraService,
IIngresoProductosService
```

---

## Frontend

- **Layout**: sidebar fijo 220px en desktop, menú hamburguesa deslizable en mobile (<768px)
- **Menú**: accordion Bootstrap generado por MainMenuViewComponent
- **Estilos**: esquema gris/neutral (degradado #f9f9f9 a #e0e0e0)
- **Background**: imagen de fondo en el área de contenido (`login_background.jpg`)
- **Validación**: jQuery Validation + Unobtrusive

---

## Comandos de Build

```bash
# Compilar solución
cd "C:\Desarrollos\Comercial-master\Comercial Web"
dotnet build

# Ejecutar
dotnet run --project "Comercial Web"
```

---

## Convenciones de Código

- Entidades: PascalCase (C#), tablas: camelCase (MySQL)
- Servicios: siempre async, sufijo `Async` en métodos
- Validaciones en servicios, no en PageModels
- Textos se guardan en UPPER (`.Trim().ToUpperInvariant()`)
- Campos nullable con `?` para columnas DEFAULT NULL de la BD
- Razor escape: `@@` para `@` literal en CSS dentro de .cshtml (ej: `@@media`)
