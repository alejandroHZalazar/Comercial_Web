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

    public virtual DbSet<ConceptoCaja> ConceptosCaja { get; set; }

    public virtual DbSet<MedioPago> MediosPago { get; set; }

    public virtual DbSet<PlanPago> PlanesPago { get; set; }

    public virtual DbSet<DocumentoTipo> DocumentosTipo { get; set; }
    public virtual DbSet<VentasFormasPago> VentasFormasPago { get; set; }
    public virtual DbSet<MovimientoCC>    MovimientosCC    { get; set; }
    public virtual DbSet<Documento>       Documentos       { get; set; }
    public virtual DbSet<Cobro>           Cobros           { get; set; }
    public virtual DbSet<CobroDetalle>    CobrosDetalle    { get; set; }
    public virtual DbSet<Caja>            Cajas            { get; set; }
    public virtual DbSet<Imputacion>      Imputaciones     { get; set; }
    public virtual DbSet<CajaMovimiento>  CajaMovimientos  { get; set; }
    public virtual DbSet<TipoGasto>       TiposGasto       { get; set; }
    public virtual DbSet<Gasto>           Gastos           { get; set; }
    public virtual DbSet<Pago>            Pagos            { get; set; }
    public virtual DbSet<ComprobanteFiscal> ComprobantesFiscales { get; set; }
    public virtual DbSet<ErrorFe>           ErroresFE            { get; set; }
    public virtual DbSet<NotaDebito>        NotasDebito          { get; set; }

    // ── Módulo Promociones ───────────────────────────────────────────────────
    public virtual DbSet<Promocion>              Promociones              { get; set; }
    public virtual DbSet<PromocionSlot>          PromocionSlots           { get; set; }
    public virtual DbSet<PromocionSlotProducto>  PromocionSlotProductos   { get; set; }
    public virtual DbSet<VentaPromoComponente>   VentaPromoComponentes    { get; set; }

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
            entity.Property(e => e.AbrevFE)
                .HasMaxLength(5)
                .HasDefaultValue(null)
                .HasColumnName("abrevFE");
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
            entity.Property(e => e.Impuesto)
                .HasPrecision(18, 4)
                .HasColumnName("impuesto");
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
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 4)
                .HasDefaultValue(null)
                .HasColumnName("descuento");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 4)
                .HasDefaultValue(null)
                .HasColumnName("recargo");
            entity.Property(e => e.SubtotalSinIva)
                .HasPrecision(18, 4)
                .HasDefaultValue(null)
                .HasColumnName("subtotalSinIVA");
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
            entity.Property(e => e.Descuento)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("descuento");
            entity.Property(e => e.Recargo)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("recargo");
            entity.Property(e => e.SubtotalSinIva)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("subtotalSinIva");
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
            entity.Property(e => e.Fraccionado).HasColumnName("fraccionado");
            entity.Property(e => e.Dolarizado).HasColumnName("dolarizado");
            entity.Property(e => e.EsPromocion)
                .HasDefaultValueSql("'0'")
                .HasColumnName("esPromocion");
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
            entity.Property(e => e.Descuento)
                .HasPrecision(18, 4)
                .HasDefaultValue(null)
                .HasColumnName("descuento");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 4)
                .HasDefaultValue(null)
                .HasColumnName("recargo");
            entity.Property(e => e.SubtotalSinIva)
                .HasPrecision(18, 4)
                .HasDefaultValue(null)
                .HasColumnName("subtotalSinIVA");
        });

        modelBuilder.Entity<ConceptoCaja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("conceptos_caja");

            entity.Property(e => e.Id)
                .HasColumnType("int")
                .HasColumnName("concepto_caja_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(1)
                .HasColumnName("tipo_movimiento");
            entity.Property(e => e.AfectaEfectivo)
                .HasColumnType("tinyint(1)")
                .HasColumnName("afecta_efectivo");
            entity.Property(e => e.FkMedioPago)
                .HasColumnType("int")
                .HasDefaultValue(null)
                .HasColumnName("fk_medio_pago");
            entity.Property(e => e.Operacion)
                .HasMaxLength(20)
                .HasDefaultValue(null)
                .HasColumnName("Operacion");
        });

        modelBuilder.Entity<MedioPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("medios_pago");

            entity.Property(e => e.Id)
                .HasColumnType("int")
                .HasColumnName("medio_pago_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 2)
                .HasDefaultValue(null)
                .HasColumnName("recargo");
            entity.Property(e => e.ConDatos)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(null)
                .HasColumnName("conDatos");
        });

        modelBuilder.Entity<PlanPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("planes_pago");

            entity.Property(e => e.Id)
                .HasColumnType("int")
                .HasColumnName("id");
            entity.Property(e => e.FkMedioPago)
                .HasColumnType("int")
                .HasDefaultValue(null)
                .HasColumnName("fk_medioPago");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
            entity.Property(e => e.Recargo)
                .HasPrecision(18, 2)
                .HasDefaultValue(null)
                .HasColumnName("recargo");
        });

        modelBuilder.Entity<DocumentoTipo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Documentos_Tipo");

            entity.Property(e => e.Id)
                .HasColumnType("int")
                .HasColumnName("id");
            entity.Property(e => e.Abreviatura)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("abreviatura");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .HasDefaultValue(null)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<VentasFormasPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ventas_formasPago");

            entity.Property(e => e.Id)
                .HasColumnType("int")
                .HasColumnName("id");
            entity.Property(e => e.FkVenta)
                .HasColumnType("int")
                .HasColumnName("fk_venta");
            entity.Property(e => e.FkMedioPago)
                .HasColumnType("int")
                .HasColumnName("fk_medioPago");
            entity.Property(e => e.FkPlanPago)
                .HasColumnType("int")
                .HasColumnName("fk_planPago");
            entity.Property(e => e.Importe)
                .HasPrecision(18, 2)
                .HasColumnName("importe");
            entity.Property(e => e.Referencia1)
                .HasMaxLength(45)
                .HasDefaultValue(null)
                .HasColumnName("referencia1");
            entity.Property(e => e.Referencia2)
                .HasMaxLength(45)
                .HasDefaultValue(null)
                .HasColumnName("referencia2");
            entity.Property(e => e.Referencia3)
                .HasMaxLength(45)
                .HasDefaultValue(null)
                .HasColumnName("referencia3");
        });

        modelBuilder.Entity<MovimientoCC>(entity =>
        {
            entity.HasKey(e => e.MovimientoId).HasName("PRIMARY");

            entity.ToTable("MovimientosCC");

            entity.Property(e => e.MovimientoId)
                .HasColumnType("int")
                .HasColumnName("MovimientoId");
            entity.Property(e => e.ClienteId)
                .HasColumnType("int")
                .HasColumnName("ClienteId");
            entity.Property(e => e.DocumentoId)
                .HasColumnType("int")
                .HasDefaultValue(null)
                .HasColumnName("DocumentoId");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("Fecha");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("TipoMovimiento");
            entity.Property(e => e.Importe)
                .HasPrecision(18, 2)
                .HasColumnName("Importe");
            entity.Property(e => e.SaldoPendiente)
                .HasPrecision(18, 2)
                .HasColumnName("SaldoPendiente");
        });

        modelBuilder.Entity<Cobro>(entity =>
        {
            entity.HasKey(e => e.CobroId).HasName("PRIMARY");
            entity.ToTable("Cobros");
            entity.Property(e => e.CobroId).HasColumnType("int").HasColumnName("CobroId");
            entity.Property(e => e.ClienteId).HasColumnType("int").HasColumnName("ClienteId");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("Fecha");
            entity.Property(e => e.ImporteTotal).HasPrecision(18, 2).HasColumnName("ImporteTotal");
            entity.Property(e => e.EstadoId).HasColumnType("int").HasDefaultValue(null).HasColumnName("Estado_Id");
            entity.Property(e => e.DocumentoId).HasColumnType("int").HasColumnName("DocumentoId");
            entity.Property(e => e.TipoCobro).HasColumnType("int").HasDefaultValue(null).HasColumnName("tipoCobro");
            entity.Property(e => e.Observaciones).HasMaxLength(100).HasDefaultValue(null).HasColumnName("Observaciones");
        });

        modelBuilder.Entity<CobroDetalle>(entity =>
        {
            entity.HasKey(e => e.CobroDetalleId).HasName("PRIMARY");
            entity.ToTable("CobrosDetalle");
            entity.Property(e => e.CobroDetalleId).HasColumnType("int").HasColumnName("CobroDetalleId");
            entity.Property(e => e.CobroId).HasColumnType("int").HasColumnName("CobroId");
            entity.Property(e => e.MedioPagoId).HasColumnType("int").HasColumnName("MedioPagoId");
            entity.Property(e => e.Importe).HasPrecision(18, 2).HasColumnName("Importe");
            entity.Property(e => e.Referencia1).HasMaxLength(50).HasDefaultValue(null).HasColumnName("Referencia1");
            entity.Property(e => e.Referencia2).HasMaxLength(50).HasDefaultValue(null).HasColumnName("Referencia2");
            entity.Property(e => e.Referencia3).HasMaxLength(50).HasDefaultValue(null).HasColumnName("Referencia3");
        });

        modelBuilder.Entity<Caja>(entity =>
        {
            entity.HasKey(e => e.CajaId).HasName("PRIMARY");
            entity.ToTable("caja");
            entity.Property(e => e.CajaId).HasColumnType("int").HasColumnName("caja_id");
            entity.Property(e => e.UsuarioId).HasColumnType("int").HasColumnName("usuario_id");
            entity.Property(e => e.FechaApertura).HasColumnType("datetime").HasColumnName("fecha_apertura");
            entity.Property(e => e.FechaCierre).HasColumnType("datetime").HasDefaultValue(null).HasColumnName("fecha_cierre");
            entity.Property(e => e.SaldoInicial).HasPrecision(18, 2).HasColumnName("saldo_inicial");
            entity.Property(e => e.SaldoCierre).HasPrecision(18, 2).HasDefaultValue(null).HasColumnName("saldo_cierre");
            entity.Property(e => e.Estado).HasColumnType("enum('ABIERTA','CERRADA')").HasColumnName("estado");
            entity.Property(e => e.Observaciones).HasMaxLength(500).HasDefaultValue(null).HasColumnName("observaciones");
        });

        modelBuilder.Entity<Imputacion>(entity =>
        {
            entity.HasKey(e => e.ImputacionId).HasName("PRIMARY");
            entity.ToTable("Imputaciones");
            entity.Property(e => e.ImputacionId).HasColumnType("int").HasColumnName("ImputacionId");
            entity.Property(e => e.MovimientoDebitoId).HasColumnType("int").HasColumnName("MovimientoDebitoId");
            entity.Property(e => e.MovimientoCreditoId).HasColumnType("int").HasColumnName("MovimientoCreditoId");
            entity.Property(e => e.Importe).HasPrecision(18, 2).HasColumnName("Importe");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("Fecha")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<CajaMovimiento>(entity =>
        {
            entity.HasKey(e => e.MovimientoCajaId).HasName("PRIMARY");
            entity.ToTable("movimientos_caja");
            entity.Property(e => e.MovimientoCajaId).HasColumnType("int").HasColumnName("movimiento_caja_id");
            entity.Property(e => e.CajaId).HasColumnType("int").HasColumnName("caja_id");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("fecha");
            entity.Property(e => e.ConceptoCajaId).HasColumnType("int").HasColumnName("concepto_caja_id");
            entity.Property(e => e.MedioPagoId).HasColumnType("int").HasColumnName("medio_pago_id");
            entity.Property(e => e.Importe).HasPrecision(18, 2).HasColumnName("importe");
            entity.Property(e => e.Observaciones).HasMaxLength(500).HasDefaultValue(null).HasColumnName("observaciones");
        });

        modelBuilder.Entity<TipoGasto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("tipo_gastos");
            entity.Property(e => e.Id).HasColumnType("int").HasColumnName("id");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
        });

        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("gastos");
            entity.Property(e => e.Id).HasColumnType("int").HasColumnName("id");
            entity.Property(e => e.IdMovimientoCaja).HasColumnType("int").HasColumnName("id_movimiento_caja");
            entity.Property(e => e.IdTipoGasto).HasColumnType("int").HasColumnName("id_tipo_gasto");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.PagoId).HasName("PRIMARY");
            entity.ToTable("Pagos");
            entity.Property(e => e.PagoId)
                .HasColumnType("int")
                .HasColumnName("PagoId")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.ProveedorId)
                .HasColumnType("int")
                .HasColumnName("ProveedorId");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("Fecha");
            entity.Property(e => e.ImporteTotal)
                .HasPrecision(18, 2)
                .HasColumnName("ImporteTotal");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(100)
                .HasColumnName("Observaciones")
                .HasDefaultValue(null);
        });

        modelBuilder.Entity<Documento>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PRIMARY");

            entity.ToTable("Documentos");

            entity.Property(e => e.ID)
                .HasColumnType("int")
                .HasColumnName("ID");
            entity.Property(e => e.ClienteId)
                .HasColumnType("int")
                .HasColumnName("ClienteId");
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("TipoDocumento");
            entity.Property(e => e.Numero)
                .HasMaxLength(30)
                .HasColumnName("Numero");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("Fecha");
            entity.Property(e => e.Total)
                .HasPrecision(18, 2)
                .HasColumnName("Total");
        });

        modelBuilder.Entity<ComprobanteFiscal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("comprobantes_fiscales");
            entity.Property(e => e.Id).HasColumnType("bigint").HasColumnName("id");
            entity.Property(e => e.TipoComprobante).HasMaxLength(20).HasColumnName("tipo_comprobante");
            entity.Property(e => e.Letra).HasMaxLength(1).IsFixedLength().HasColumnName("letra");
            entity.Property(e => e.PuntoVenta).HasColumnType("int").HasColumnName("punto_venta");
            entity.Property(e => e.Numero).HasColumnType("int").HasColumnName("numero");
            entity.Property(e => e.FechaEmision).HasColumnType("datetime").HasColumnName("fecha_emision");
            entity.Property(e => e.NroReferencia).HasColumnType("int").HasDefaultValue(null).HasColumnName("nroReferencia");
            entity.Property(e => e.FkCliente).HasColumnType("int").HasDefaultValue(null).HasColumnName("fk_cliente");
            entity.Property(e => e.RazonSocial).HasMaxLength(150).HasDefaultValue(null).HasColumnName("razon_social");
            entity.Property(e => e.Cuit).HasMaxLength(20).HasDefaultValue(null).HasColumnName("cuit");
            entity.Property(e => e.ImporteTotal).HasPrecision(18, 2).HasColumnName("importe_total");
            entity.Property(e => e.Cae).HasMaxLength(20).HasDefaultValue(null).HasColumnName("cae");
            entity.Property(e => e.FechaVencimientoCae).HasColumnType("datetime").HasDefaultValue(null).HasColumnName("fecha_vencimiento_cae");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue(null).HasColumnName("estado");
            entity.Property(e => e.FiscalStatus).HasColumnType("int").HasDefaultValue(null).HasColumnName("fiscal_status");
            entity.Property(e => e.NumeroJornada).HasColumnType("int").HasDefaultValue(null).HasColumnName("numero_jornada");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasColumnName("created_at");
            entity.Property(e => e.AfipQr).HasMaxLength(500).HasDefaultValue(null).HasColumnName("afip_qr");
            entity.Property(e => e.LinkPdf).HasMaxLength(500).HasDefaultValue(null).HasColumnName("linkPDF");
        });

        modelBuilder.Entity<ErrorFe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("erroresFE");
            entity.Property(e => e.Id).HasColumnType("int").HasColumnName("id");
            entity.Property(e => e.FkVenta).HasColumnType("int").HasDefaultValue(null).HasColumnName("fk_venta");
            entity.Property(e => e.Error).HasMaxLength(2000).HasDefaultValue(null).HasColumnName("error");
        });

        modelBuilder.Entity<NotaDebito>(e =>
        {
            e.ToTable("NotasDebito");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("Id");
            e.Property(x => x.ClienteId).HasColumnName("ClienteId");
            e.Property(x => x.Fecha).HasColumnName("Fecha");
            e.Property(x => x.ImporteTotal).HasColumnName("ImporteTotal").HasColumnType("decimal(18,2)");
            e.Property(x => x.EstadoId).HasColumnName("Estado_Id");
            e.Property(x => x.Observaciones).HasColumnName("Observaciones").HasMaxLength(100);
        });

        // ── Promociones ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Promocion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("promociones");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.FkProducto).HasColumnType("int(11)").HasColumnName("fk_producto");
            entity.Property(e => e.Activa).HasColumnName("activa").HasDefaultValueSql("'1'");
            entity.Property(e => e.FechaDesde).HasColumnName("fechaDesde");
            entity.Property(e => e.FechaHasta).HasColumnName("fechaHasta");
        });

        modelBuilder.Entity<PromocionSlot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("promociones_slots");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.FkPromocion).HasColumnType("int(11)").HasColumnName("fk_promocion");
            entity.Property(e => e.Numero).HasColumnType("int(11)").HasColumnName("numero");
            entity.Property(e => e.CantidadRequerida).HasPrecision(18, 4).HasColumnName("cantidadRequerida");
            entity.Property(e => e.Descripcion).HasMaxLength(100).HasColumnName("descripcion");
        });

        modelBuilder.Entity<PromocionSlotProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("promociones_slot_productos");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.FkSlot).HasColumnType("int(11)").HasColumnName("fk_slot");
            entity.Property(e => e.FkProducto).HasColumnType("int(11)").HasColumnName("fk_producto");
        });

        modelBuilder.Entity<VentaPromoComponente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("ventas_promo_componentes");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.FkVentaDetalle).HasColumnType("bigint(20)").HasColumnName("fk_ventaDetalle");
            entity.Property(e => e.FkSlot).HasColumnType("int(11)").HasColumnName("fk_slot");
            entity.Property(e => e.FkProducto).HasColumnType("int(11)").HasColumnName("fk_producto");
            entity.Property(e => e.Cantidad).HasPrecision(18, 4).HasColumnName("cantidad");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
