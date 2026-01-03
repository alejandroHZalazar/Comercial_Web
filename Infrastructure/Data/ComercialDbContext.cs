using Microsoft.EntityFrameworkCore;
using Domain.Entities;
namespace Infrastructure.Data;

public partial class ComercialDbContext : DbContext
{
    public ComercialDbContext()
    {
    }

    public ComercialDbContext(DbContextOptions<ComercialDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ClientesZona> ClientesZonas { get; set; }

    public virtual DbSet<Colore> Colores { get; set; }

    public virtual DbSet<CondicionIva> CondIvas { get; set; }

    public virtual DbSet<CostosProducto> CostosProductos { get; set; }

    public virtual DbSet<Devolucione> Devoluciones { get; set; }

    public virtual DbSet<DevolucionesDetalle> DevolucionesDetalles { get; set; }

    public virtual DbSet<Impuesto> Impuestos { get; set; }

    public virtual DbSet<IvaPorcentaje> IvaPorcentajes { get; set; }

    public virtual DbSet<Localidade> Localidades { get; set; }

    public virtual DbSet<MenuPermiso> MenuPermisos { get; set; }

    public virtual DbSet<OrdenCompra> OrdenCompras { get; set; }

    public virtual DbSet<OrdenCompraDetalle> OrdenCompraDetalles { get; set; }

    public virtual DbSet<Parametro> Parametros { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<PedidoDetalle> PedidoDetalles { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<PreciosProducto> PreciosProductos { get; set; }

    public virtual DbSet<PreciosProveedore> PreciosProveedores { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductosAcrear> ProductosAcrears { get; set; }

    public virtual DbSet<ProductosLog> ProductosLogs { get; set; }

    public virtual DbSet<ProductosMovimiento> ProductosMovimientos { get; set; }

    public virtual DbSet<Proveedore> Proveedores { get; set; }

    public virtual DbSet<Provincia> Provincias { get; set; }

    public virtual DbSet<Rubro> Rubros { get; set; }

    public virtual DbSet<StockProducto> StockProductos { get; set; }

    public virtual DbSet<TipoDeUsuariosPermiso> TipoDeUsuariosPermisos { get; set; }

    public virtual DbSet<TipoPrecio> TipoPrecios { get; set; }

    public virtual DbSet<TipoProductosMovimiento> TipoProductosMovimientos { get; set; }

    public virtual DbSet<TipoUsuario> TipoUsuarios { get; set; }

    public virtual DbSet<TipoValoresPrecio> TipoValoresPrecios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VentasDetalle> VentasDetalles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=72.61.47.240;database=ale;user=remoto;password=0315061", ServerVersion.Parse("5.5.62-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("latin1_swedish_ci")
            .HasCharSet("latin1");

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.NombreComercial, "nomComercialIndex");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja)
                .HasDefaultValueSql("'0'")
                .HasColumnName("baja");
            entity.Property(e => e.Celular)
                .HasMaxLength(150)
                .HasColumnName("celular");
            entity.Property(e => e.Contacto)
                .HasMaxLength(150)
                .HasColumnName("contacto");
            entity.Property(e => e.Cuil)
                .HasMaxLength(11)
                .HasColumnName("cuil");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .HasColumnName("direccion");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FkCondIva)
                .HasColumnType("int(11)")
                .HasColumnName("fk_condIva");
            entity.Property(e => e.FkLocalidad)
                .HasColumnType("int(11)")
                .HasColumnName("fk_localidad");
            entity.Property(e => e.FkVendedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_Vendedor");
            entity.Property(e => e.FkZona)
                .HasColumnType("int(11)")
                .HasColumnName("fk_zona");
            entity.Property(e => e.NombreComercial)
                .HasMaxLength(150)
                .HasColumnName("nombreComercial");
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(150)
                .HasColumnName("razonSocial");
            entity.Property(e => e.Telefono)
                .HasMaxLength(150)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<ClientesZona>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja).HasColumnName("baja");
            entity.Property(e => e.Nombre)
                .HasMaxLength(45)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Colore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("colores");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .HasColumnName("color");
            entity.Property(e => e.Nombre)
                .HasMaxLength(45)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CondicionIva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("condIVA");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Abrev)
                .HasMaxLength(2)
                .HasColumnName("abrev");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .HasColumnName("descripcion");
            entity.Property(e => e.Letra)
                .HasMaxLength(1)
                .HasColumnName("letra");
        });

        modelBuilder.Entity<CostosProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("costosProductos");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Costo)
                .HasPrecision(18, 4)
                .HasColumnName("costo");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
        });

        modelBuilder.Entity<Devolucione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Comision)
                .HasPrecision(18, 4)
                .HasColumnName("comision");
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 2)
                .HasColumnName("descuento");
            entity.Property(e => e.FFactura).HasColumnName("fFactura");
            entity.Property(e => e.Factura)
                .HasMaxLength(45)
                .HasColumnName("factura");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FkCajero)
                .HasColumnType("int(11)")
                .HasColumnName("fk_cajero");
            entity.Property(e => e.FkCliente)
                .HasColumnType("int(11)")
                .HasColumnName("fk_cliente");
            entity.Property(e => e.FkCondIva)
                .HasColumnType("int(11)")
                .HasColumnName("fk_condIVA");
            entity.Property(e => e.FkVendedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_vendedor");
            entity.Property(e => e.Iva)
                .HasPrecision(18, 4)
                .HasColumnName("IVA");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 2)
                .HasColumnName("recargo");
            entity.Property(e => e.TotalCosto)
                .HasPrecision(18, 4)
                .HasColumnName("totalCosto");
            entity.Property(e => e.TotalDevolucion)
                .HasPrecision(18, 4)
                .HasColumnName("totalDevolucion");
        });

        modelBuilder.Entity<DevolucionesDetalle>(entity =>
        {
            entity.HasKey(e => e.Linea).HasName("PRIMARY");

            entity.ToTable("devolucionesDetalles");

            entity.Property(e => e.Linea)
                .HasColumnType("int(11)")
                .HasColumnName("linea");
            entity.Property(e => e.Cantidad)
                .HasPrecision(18, 4)
                .HasColumnName("cantidad");
            entity.Property(e => e.CodBarras)
                .HasMaxLength(45)
                .HasColumnName("codBarras");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(45)
                .HasColumnName("codProveedor");
            entity.Property(e => e.Costo)
                .HasPrecision(18, 4)
                .HasColumnName("costo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkDevolucion)
                .HasColumnType("int(11)")
                .HasColumnName("fk_devolucion");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.PrecioConIva)
                .HasPrecision(18, 4)
                .HasColumnName("precioConIva");
            entity.Property(e => e.PrecioSinIva)
                .HasPrecision(18, 4)
                .HasColumnName("precioSinIva");
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 4)
                .HasColumnName("subtotal");
        });

        modelBuilder.Entity<Impuesto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("impuestos");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Valor)
                .HasPrecision(18, 4)
                .HasColumnName("valor");
        });

        modelBuilder.Entity<IvaPorcentaje>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ivaPorcentajes");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Valor)
                .HasPrecision(18, 4)
                .HasColumnName("valor");
        });

        modelBuilder.Entity<Localidade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja).HasColumnName("baja");
            entity.Property(e => e.FkProvincia)
                .HasColumnType("int(11)")
                .HasColumnName("fk_Provincia");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<MenuPermiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("menuPermisos");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Funcion)
                .HasMaxLength(100)
                .HasColumnName("funcion");
            entity.Property(e => e.NombreControl)
                .HasMaxLength(100)
                .HasColumnName("nombreControl");
            entity.Property(e => e.Url)
                .HasMaxLength(100)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");
        });

        modelBuilder.Entity<OrdenCompra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ordenCompra");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 4)
                .HasColumnName("descuento");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FkProveedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_proveedor");
            entity.Property(e => e.Iva)
                .HasPrecision(18, 4)
                .HasColumnName("iva");
            entity.Property(e => e.Procesado).HasColumnName("procesado");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 4)
                .HasColumnName("recargo");
            entity.Property(e => e.Total)
                .HasPrecision(18, 4)
                .HasColumnName("total");
        });

        modelBuilder.Entity<OrdenCompraDetalle>(entity =>
        {
            entity.HasKey(e => e.Linea).HasName("PRIMARY");

            entity.ToTable("ordenCompraDetalle");

            entity.Property(e => e.Linea)
                .HasColumnType("bigint(20)")
                .HasColumnName("linea");
            entity.Property(e => e.CantRecibida)
                .HasPrecision(18, 4)
                .HasColumnName("cantRecibida");
            entity.Property(e => e.Cantidad)
                .HasPrecision(18, 4)
                .HasColumnName("cantidad");
            entity.Property(e => e.CodBarras)
                .HasMaxLength(45)
                .HasColumnName("codBarras");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(45)
                .HasColumnName("codProveedor");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkColor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_color");
            entity.Property(e => e.FkOrdenCompra)
                .HasColumnType("bigint(20)")
                .HasColumnName("fk_ordenCompra");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.PrecioProveedor)
                .HasPrecision(18, 4)
                .HasColumnName("precioProveedor");
            entity.Property(e => e.Procesado).HasColumnName("procesado");
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 4)
                .HasColumnName("subtotal");
        });

        modelBuilder.Entity<Parametro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("parametros");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Imagen).HasColumnName("imagen");
            entity.Property(e => e.Modulo)
                .HasMaxLength(45)
                .HasColumnName("modulo");
            entity.Property(e => e.Parametro1)
                .HasMaxLength(45)
                .HasColumnName("parametro");
            entity.Property(e => e.Valor)
                .HasMaxLength(150)
                .HasColumnName("valor");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pedidos");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 4)
                .HasColumnName("descuento");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FkCliente)
                .HasColumnType("int(11)")
                .HasColumnName("fk_cliente");
            entity.Property(e => e.FkVendedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_vendedor");
            entity.Property(e => e.Impreso).HasColumnName("impreso");
            entity.Property(e => e.Iva)
                .HasPrecision(18, 4)
                .HasColumnName("iva");
            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .HasColumnName("observacion");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 4)
                .HasColumnName("recargo");
            entity.Property(e => e.Total)
                .HasPrecision(18, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vendido).HasColumnName("vendido");
        });

        modelBuilder.Entity<PedidoDetalle>(entity =>
        {
            entity.HasKey(e => e.Linea).HasName("PRIMARY");

            entity.ToTable("pedidoDetalle");

            entity.HasIndex(e => e.FkPedido, "fk_pedidoIndex");

            entity.Property(e => e.Linea)
                .HasColumnType("bigint(20)")
                .HasColumnName("linea");
            entity.Property(e => e.CantEntregada)
                .HasPrecision(18, 4)
                .HasColumnName("cantEntregada");
            entity.Property(e => e.Cantidad)
                .HasPrecision(18, 4)
                .HasColumnName("cantidad");
            entity.Property(e => e.CodBarras)
                .HasMaxLength(45)
                .HasColumnName("codBarras");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(45)
                .HasColumnName("codProveedor");
            entity.Property(e => e.Costo)
                .HasPrecision(18, 4)
                .HasColumnName("costo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkColor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_color");
            entity.Property(e => e.FkPedido)
                .HasColumnType("int(11)")
                .HasColumnName("fk_pedido");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.Observ)
                .HasMaxLength(50)
                .HasColumnName("observ");
            entity.Property(e => e.PrecioConIva)
                .HasPrecision(18, 4)
                .HasColumnName("precioConIva");
            entity.Property(e => e.PrecioOrig)
                .HasPrecision(18, 4)
                .HasColumnName("precioOrig");
            entity.Property(e => e.PrecioSinIva)
                .HasPrecision(18, 4)
                .HasColumnName("precioSinIva");
            entity.Property(e => e.Procesado).HasColumnName("procesado");
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 4)
                .HasColumnName("subtotal");
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Contenedor)
                .HasMaxLength(100)
                .HasColumnName("contenedor");
            entity.Property(e => e.Control)
                .HasMaxLength(100)
                .HasColumnName("control");
            entity.Property(e => e.FkTipoUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("fk_tipoUsuario");
            entity.Property(e => e.Permiso1).HasColumnName("permiso");
        });

        modelBuilder.Entity<PreciosProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("preciosProductos");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.Precio)
                .HasPrecision(18, 4)
                .HasColumnName("precio");
        });

        modelBuilder.Entity<PreciosProveedore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("preciosProveedores");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.Precio)
                .HasPrecision(18, 2)
                .HasColumnName("precio");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.CodProveedor, "codProvIndex");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja).HasColumnName("baja");
            entity.Property(e => e.CodBarras)
                .HasMaxLength(45)
                .HasColumnName("codBarras");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(45)
                .HasColumnName("codProveedor");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkProveedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_proveedor");
            entity.Property(e => e.FkRubro)
                .HasColumnType("int(11)")
                .HasColumnName("fk_Rubro");
            entity.Property(e => e.Iva)
                .HasColumnType("int(11)")
                .HasColumnName("iva");
        });

        modelBuilder.Entity<ProductosAcrear>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ProductosACrear");

            entity.HasIndex(e => e.CodProveedor, "codProvIndex");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CodBarras)
                .HasMaxLength(45)
                .HasColumnName("codBarras");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(45)
                .HasColumnName("codProveedor");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkProveedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_proveedor");
            entity.Property(e => e.FkRubro)
                .HasColumnType("int(11)")
                .HasColumnName("fk_Rubro");
            entity.Property(e => e.Iva)
                .HasColumnType("int(11)")
                .HasColumnName("iva");
            entity.Property(e => e.PrecioProv)
                .HasPrecision(18, 4)
                .HasColumnName("precioProv");
        });

        modelBuilder.Entity<ProductosLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("productosLog");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.ModifDate).HasColumnName("modifDate");
            entity.Property(e => e.PrecioCosto)
                .HasPrecision(18, 4)
                .HasColumnName("precioCosto");
            entity.Property(e => e.PrecioLista)
                .HasPrecision(18, 4)
                .HasColumnName("precioLista");
            entity.Property(e => e.PrecioProv)
                .HasPrecision(18, 4)
                .HasColumnName("precioProv");
        });

        modelBuilder.Entity<ProductosMovimiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("productosMovimientos");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(18, 4)
                .HasColumnName("cantidad");
            entity.Property(e => e.Costo)
                .HasPrecision(18, 4)
                .HasColumnName("costo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaEntrega).HasColumnName("fechaEntrega");
            entity.Property(e => e.FechaMov).HasColumnName("fechaMov");
            entity.Property(e => e.FkColor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_color");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.NroComprobante)
                .HasMaxLength(45)
                .HasColumnName("nroComprobante");
            entity.Property(e => e.PrecioProveedor)
                .HasPrecision(18, 4)
                .HasColumnName("precio_Proveedor");
            entity.Property(e => e.StockAct)
                .HasPrecision(18, 4)
                .HasColumnName("stockAct");
            entity.Property(e => e.StockAnt)
                .HasPrecision(18, 4)
                .HasColumnName("stockAnt");
            entity.Property(e => e.TipoMovimiento)
                .HasColumnType("int(11)")
                .HasColumnName("tipoMovimiento");
            entity.Property(e => e.Venta)
                .HasPrecision(18, 4)
                .HasColumnName("venta");
        });

        modelBuilder.Entity<Proveedore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja).HasColumnName("baja");
            entity.Property(e => e.Celular)
                .HasMaxLength(45)
                .HasColumnName("celular");
            entity.Property(e => e.Cuil)
                .HasMaxLength(11)
                .HasColumnName("cuil");
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 4)
                .HasColumnName("descuento");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .HasColumnName("direccion");
            entity.Property(e => e.Email)
                .HasMaxLength(45)
                .HasColumnName("email");
            entity.Property(e => e.Ganancia)
                .HasPrecision(18, 4)
                .HasColumnName("ganancia");
            entity.Property(e => e.NombreComercial)
                .HasMaxLength(150)
                .HasColumnName("nombreComercial");
            entity.Property(e => e.Telefono)
                .HasMaxLength(45)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Provincia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja).HasColumnName("baja");
            entity.Property(e => e.Nombre)
                .HasMaxLength(45)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Rubro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<StockProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("stockProductos");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(18, 4)
                .HasColumnName("cantidad");
            entity.Property(e => e.CantidadMinima)
                .HasPrecision(18, 4)
                .HasColumnName("cantidadMinima");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
        });

        modelBuilder.Entity<TipoDeUsuariosPermiso>(entity =>
        {
            entity
                .ToTable("tipoDeUsuariosPermisos");

            // Definir clave compuesta
            entity.HasKey(e => new { e.FkTipoUsuario, e.FkMenuPermiso });

            entity.Property(e => e.FkMenuPermiso)
                .HasColumnType("int(11)")
                .HasColumnName("fk_menuPermiso");

            entity.Property(e => e.FkTipoUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("fk_tipoUsuario");
        });


        modelBuilder.Entity<TipoPrecio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tipoPrecios");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(45)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkTipoValor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_tipoValor");
            entity.Property(e => e.Valor)
                .HasPrecision(18, 4)
                .HasColumnName("valor");
        });

        modelBuilder.Entity<TipoProductosMovimiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tipoProductosMovimientos");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(45)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<TipoUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tipoUsuarios");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<TipoValoresPrecio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tipoValoresPrecios");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Abrev)
                .HasMaxLength(1)
                .HasColumnName("abrev");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(45)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Baja)
                .HasDefaultValueSql("'0'")
                .HasColumnName("baja");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PasswordMigrated)
                .HasDefaultValueSql("'0'")
                .HasColumnName("password_migrated");
            entity.Property(e => e.Tipo)
                .HasColumnType("int(11)")
                .HasColumnName("tipo");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ventas");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Comision)
                .HasPrecision(18, 4)
                .HasColumnName("comision");
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 4)
                .HasColumnName("descuento");
            entity.Property(e => e.FFactura).HasColumnName("fFactura");
            entity.Property(e => e.Factura)
                .HasMaxLength(45)
                .HasColumnName("factura");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FkCajero)
                .HasColumnType("int(11)")
                .HasColumnName("fk_cajero");
            entity.Property(e => e.FkCliente)
                .HasColumnType("int(11)")
                .HasColumnName("fk_cliente");
            entity.Property(e => e.FkCondIva)
                .HasColumnType("int(11)")
                .HasColumnName("fk_condIva");
            entity.Property(e => e.FkVendedor)
                .HasColumnType("int(11)")
                .HasColumnName("fk_vendedor");
            entity.Property(e => e.Impuesto)
                .HasPrecision(18, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("impuesto");
            entity.Property(e => e.Iva)
                .HasPrecision(18, 4)
                .HasColumnName("IVA");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 4)
                .HasColumnName("recargo");
            entity.Property(e => e.TotalCosto)
                .HasPrecision(18, 4)
                .HasColumnName("totalCosto");
            entity.Property(e => e.TotalVenta)
                .HasPrecision(18, 4)
                .HasColumnName("totalVenta");
        });

        modelBuilder.Entity<VentasDetalle>(entity =>
        {
            entity.HasKey(e => e.Linea).HasName("PRIMARY");

            entity.ToTable("ventasDetalle");

            entity.HasIndex(e => e.FkVenta, "fk_ventaIndex");

            entity.Property(e => e.Linea)
                .HasColumnType("bigint(20)")
                .HasColumnName("linea");
            entity.Property(e => e.Cantidad)
                .HasPrecision(18, 4)
                .HasColumnName("cantidad");
            entity.Property(e => e.CodBarras)
                .HasMaxLength(45)
                .HasColumnName("codBarras");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(45)
                .HasColumnName("codProveedor");
            entity.Property(e => e.Costo)
                .HasPrecision(18, 4)
                .HasColumnName("costo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.FkProducto)
                .HasColumnType("int(11)")
                .HasColumnName("fk_producto");
            entity.Property(e => e.FkVenta)
                .HasColumnType("bigint(20)")
                .HasColumnName("fk_venta");
            entity.Property(e => e.PrecioConIva)
                .HasPrecision(18, 4)
                .HasColumnName("precioConIva");
            entity.Property(e => e.PrecioSinIva)
                .HasPrecision(18, 4)
                .HasColumnName("precioSinIva");
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 4)
                .HasColumnName("subtotal");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
