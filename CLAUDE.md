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

### Promociones por cantidad (NxM) y porcentaje

Módulo independiente de los combos por slots (`IPromocionService`). Un único motor en el servidor que usan Ventas, Venta minorista y Pedidos.

- **Activación:** parámetro `promociones/descuentos = '1'` **y** tablas creadas con `scripts/promociones_reglas.sql` (lo ejecuta el usuario). Con el parámetro apagado o sin tablas el comportamiento es idéntico al anterior (no se consulta ninguna tabla nueva). Estado cacheado 60 s en `PromocionDescuentosService`.
- **Tablas:** `promociones_reglas` (PORC|NXM, vigencia DATETIME hora Argentina), `promociones_reglas_productos`, y auditoría `ventas_promo_detalle` / `pedidos_promo_detalle`. `ventasDetalle` y `pedidoDetalle` NO se modifican.
- **Motor puro:** `Domain/Promociones/MotorPromociones.cs`. Evalúa por producto con la cantidad total; elige la regla de **menor importe final** (no se acumulan); desempate = menor `Id`. NxM: `grupos = floor(q/N)`, bonificadas = `grupos × (N−M)`.
- **Servidor manda:** `IPromocionDescuentosService.EvaluarAsync` (handler `?handler=EvaluarPromos` en Ventas/Minorista/Pedidos) y `ResolverParaGuardarAsync` (llamado desde `VentaService.GrabarVentaAsync` y `PedidoService.GuardarPedidoAsync`): valida lo que envía el navegador (lanza `InvalidOperationException` si no coincide) y **expande NxM en dos líneas** (cobradas a precio normal + bonificadas con 100 % de descuento) con su registro de auditoría.
- **Navegador:** `wwwroot/js/promo-descuentos.js` solo envía líneas y pinta el resultado; no tiene reglas de cálculo. Un Desc/Rec manual (≠ 0 sin promoción) gana sobre la promoción; las líneas cargadas desde un pedido quedan bloqueadas (precio pactado) y los productos fraccionados no participan.
- **ABM:** `/Productos/PromocionesDescuentos` (menú 213). Búsqueda de productos reutiliza `IImpresionEtiquetasService.BuscarAsync` (respeta la regla de variantes: sin fichas).
- **Acumular variantes (NxM):** columna `promociones_reglas.agrupaVariantes` (TINYINT(1) NOT NULL DEFAULT 0; solo NxM). Con la opción activa, las cantidades de las variantes de una misma ficha (`productosVariantes.fk_principal`) que estén en la lista de la regla se suman para completar el grupo; no se mezclan fichas ni variantes fuera de la lista. El motor recibe la ficha en `LineaPromoEntrada.FichaId` (la carga `PromocionDescuentosService.AsignarFichasAsync`, solo si hay una regla vigente que acumula y el módulo de variantes está habilitado). Una regla que acumula solo se usa si su ahorro supera al de las ofertas individuales de esas líneas. Sin reglas que acumulen el cálculo es idéntico al anterior (prueba de equivalencia). Si falta la columna el módulo queda inerte (el estado la verifica en `information_schema`).
- **Aviso de proximidad:** `MotorPromociones.Proximas` (misma carga de reglas que `Evaluar`, devuelto en `EvaluarAsync` → `proximas`). Avisa solo cuando falta **exactamente 1 unidad** para completar un grupo NxM y esa NxM ganaría al agregarla (sin avisar si ya se alcanzó el beneficio, si otra promoción mejor seguiría ganando o con cantidades decimales). Es solo informativo: etiqueta ámbar en la fila + aviso flotante (`PromoDesc.toast`) una vez por combinación producto/promoción/cantidad; no modifica importes.
- **Hora:** vigencia siempre en hora argentina (`HoraArgentina.Ahora()`), el servidor Linux corre en UTC.
- **Pruebas:** `Tests/Promociones.Tests` (xUnit, base EF en memoria): `dotnet test "Tests/Promociones.Tests"`.

### Escaneo de productos (Ventas y Venta minorista)

- **Un solo circuito de alta por código:** `procesarCodigoBarras(raw, desdeEscaner)` en cada página (balanza, prefijo `cantidad*código`, búsqueda por `?handler=BuscarProductos` → `PedidoService.BuscarProductosAsync`, `_agregarOSumar` → promociones). Lo usan el Enter de `txtBusqCB` (teclado o lector Bluetooth/USB tipo teclado) y la cámara. Desde la cámara siempre agrega 1 unidad (o suma).
- **Códigos repetidos:** si un código corresponde a varios productos se muestra `ElegirProducto.mostrar(lista, codigo)` (nunca se toma el primero).
- **Cámara:** `wwwroot/js/escaner-productos.js` (`Escaner.abrir({ onCodigo })`): `getUserMedia` (cámara trasera, requiere HTTPS), `BarcodeDetector` nativo o polyfill `barcode-detector` 3.2.3 + `zxing_reader.wasm` (zxing-wasm 3.1.6) alojados en `wwwroot/lib/barcode-detector/` (sin CDN). Anti-duplicados (mismo código 1,5 s), feedback sonoro/vibración, linterna/cambio de cámara si existen; libera la cámara al cerrar, al ocultar la pestaña y en `pagehide`. No envía imágenes.
- **Modo lector:** botón 🔫 pone `inputmode="none"` en `txtBusqCB` (sin teclado virtual del celular).

### Código de barras automático

- Parámetros `productos/generaCodigoBarras` ('1' activo) y `productos/proximoCodigoBarras` (contador). Script: `scripts/codigo_barras_automatico.sql`.
- Solo en altas del ABM (producto simple y variantes nuevas) y **solo si código de barras y de proveedor vienen vacíos**; el mismo valor va a ambos (`001`…`999`, `1000`…). La ficha principal no lleva código. NO aplica a importaciones masivas (alta masiva / cambio de precios), a la edición, ni a LitoralMarket o la aplicación de escritorio.
- `IGeneradorCodigoBarrasService.ReservarAsync` debe ser la primera lectura de la transacción: bloquea la fila del contador (`SELECT … FOR UPDATE`) y verifica que el código no exista como `codBarras` ni `codProveedor` en ningún producto.
- En varias bases `parametros.id` **no es AUTO_INCREMENT**: los scripts deben calcular el id.

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
