-- ============================================================================
-- Promociones por cantidad (NxM) y por porcentaje de descuento
-- ----------------------------------------------------------------------------
-- IMPORTANTE
--   * Este script SOLO CREA estructuras nuevas (CREATE TABLE / INSERT). No modifica ni borra
--     ninguna tabla ni dato existente (ventasDetalle, pedidoDetalle y promociones* no se tocan).
--   * Es compatible con MySQL 5.5 y 8.x. En 5.5 las restricciones CHECK se aceptan pero NO se
--     aplican; las mismas validaciones se hacen igual en el servicio (PromocionDescuentosService).
--   * Es idempotente: se puede ejecutar más de una vez (CREATE TABLE IF NOT EXISTS / INSERT condicionado).
--   * Ejecutar sobre la base correcta (ale, pañalera, ...). Hacer backup antes.
--   * Los nombres de tabla distinguen mayúsculas en Linux: aquí se usa `Productos` (como en la base ale).
--     Si en otra base la tabla se llama `productos`, ajustar la referencia de la FK en el punto 2.
--   * Hasta que no se ejecute, la aplicación no usa nada de esto (el módulo nuevo queda inerte).
--   * Para ACTIVAR el motor en ventas/pedidos: UPDATE parametros SET valor='1'
--       WHERE modulo='promociones' AND parametro='descuentos';
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1) Reglas de promoción (cabecera)
--    tipo 'PORC' = porcentaje de descuento sobre los productos asociados
--    tipo 'NXM'  = "lleva N, paga M" (2x1, 3x2, 4x2, 4x3, ...)
--    Las fechas son DATETIME en hora local de Argentina (la vigencia se verifica en cada cálculo).
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS promociones_reglas (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    nombre        VARCHAR(100) NOT NULL              COMMENT 'Nombre/descripción visible para el usuario',
    tipo          VARCHAR(4)   NOT NULL              COMMENT 'PORC | NXM',
    porcentaje    DECIMAL(5,2) NULL                  COMMENT 'Solo PORC: 0 < % <= 100 (2 decimales)',
    lleva         INT          NULL                  COMMENT 'Solo NXM: unidades que lleva el cliente (N)',
    paga          INT          NULL                  COMMENT 'Solo NXM: unidades que paga (M), 1 <= M < N',
    agrupaVariantes TINYINT(1) NOT NULL DEFAULT 0    COMMENT 'Solo NXM: 1 = acumula las variantes de una misma ficha para completar el grupo',
    vigenteDesde  DATETIME     NOT NULL              COMMENT 'Inicio de vigencia (hora Argentina)',
    vigenteHasta  DATETIME     NOT NULL              COMMENT 'Fin de vigencia (hora Argentina)',
    activa        TINYINT(1)   NOT NULL DEFAULT 1    COMMENT 'Interruptor manual; fuera de vigencia nunca aplica',
    fechaAlta     DATETIME     NOT NULL              COMMENT 'Alta del registro',
    usuario       VARCHAR(50)  NULL                  COMMENT 'Usuario que creó/modificó la regla',
    KEY ix_reglas_vigencia (activa, vigenteDesde, vigenteHasta),
    CONSTRAINT ck_reglas_tipo   CHECK (tipo IN ('PORC','NXM')),
    CONSTRAINT ck_reglas_fechas CHECK (vigenteHasta >= vigenteDesde),
    CONSTRAINT ck_reglas_porc   CHECK (tipo <> 'PORC' OR (porcentaje IS NOT NULL AND porcentaje > 0 AND porcentaje <= 100)),
    CONSTRAINT ck_reglas_nxm    CHECK (tipo <> 'NXM'  OR (lleva IS NOT NULL AND paga IS NOT NULL AND lleva >= 2 AND paga >= 1 AND paga < lleva))
) ENGINE=InnoDB DEFAULT CHARSET=latin1
  COMMENT='Promociones por cantidad (NxM) y por porcentaje';

-- ----------------------------------------------------------------------------
-- 2) Productos que participan de cada regla (un producto no puede repetirse en la misma regla)
--    Un producto SÍ puede estar en varias reglas: el motor elige la más conveniente para el cliente.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS promociones_reglas_productos (
    id           INT AUTO_INCREMENT PRIMARY KEY,
    fk_regla     INT NOT NULL,
    fk_producto  INT NOT NULL,
    UNIQUE KEY ux_regla_producto (fk_regla, fk_producto),
    KEY ix_reglas_prod_producto (fk_producto),
    CONSTRAINT fk_reglasprod_regla    FOREIGN KEY (fk_regla)    REFERENCES promociones_reglas(id) ON DELETE CASCADE,
    CONSTRAINT fk_reglasprod_producto FOREIGN KEY (fk_producto) REFERENCES Productos(id)
) ENGINE=InnoDB DEFAULT CHARSET=latin1
  COMMENT='Productos asociados a cada regla de promoción';

-- ----------------------------------------------------------------------------
-- 3) Auditoría de qué promoción se aplicó a cada línea de VENTA.
--    Es un snapshot (nombre, tipo, parámetros, ahorro): sigue siendo válido aunque la regla se edite o elimine.
--    Para NXM se registra en la línea cobrada y en la bonificada (permite reunir el par al recargar).
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ventas_promo_detalle (
    id               INT AUTO_INCREMENT PRIMARY KEY,
    fk_ventaDetalle  BIGINT NOT NULL                COMMENT 'ventasDetalle.linea',
    fk_regla         INT NULL                       COMMENT 'Regla de origen (sin FK: puede eliminarse)',
    nombre           VARCHAR(100) NOT NULL,
    tipo             VARCHAR(4)   NOT NULL,
    porcentaje       DECIMAL(5,2) NULL,
    lleva            INT NULL,
    paga             INT NULL,
    cantBonificada   DECIMAL(18,4) NULL             COMMENT 'Unidades bonificadas de la operación para este producto (NXM)',
    ahorro           DECIMAL(18,4) NOT NULL         COMMENT 'Importe sin IVA que se dejó de cobrar en la línea',
    KEY ix_vpd_linea (fk_ventaDetalle),
    CONSTRAINT fk_vpd_linea FOREIGN KEY (fk_ventaDetalle) REFERENCES ventasDetalle(linea) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1
  COMMENT='Promociones aplicadas por línea de venta (auditoría)';

-- ----------------------------------------------------------------------------
-- 4) Ídem para PEDIDOS. ON DELETE CASCADE es necesario: al editar un pedido el sistema
--    elimina y vuelve a crear sus líneas.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pedidos_promo_detalle (
    id               INT AUTO_INCREMENT PRIMARY KEY,
    fk_pedidoDetalle BIGINT NOT NULL                COMMENT 'pedidoDetalle.linea',
    fk_regla         INT NULL,
    nombre           VARCHAR(100) NOT NULL,
    tipo             VARCHAR(4)   NOT NULL,
    porcentaje       DECIMAL(5,2) NULL,
    lleva            INT NULL,
    paga             INT NULL,
    cantBonificada   DECIMAL(18,4) NULL,
    ahorro           DECIMAL(18,4) NOT NULL,
    KEY ix_ppd_linea (fk_pedidoDetalle),
    CONSTRAINT fk_ppd_linea FOREIGN KEY (fk_pedidoDetalle) REFERENCES pedidoDetalle(linea) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1
  COMMENT='Promociones aplicadas por línea de pedido (auditoría)';

-- ----------------------------------------------------------------------------
-- 5) Parámetro general (apagado por defecto). Solo se inserta si no existe.
--    '1' = el motor evalúa promociones en Ventas, Venta minorista y Pedidos.
-- ----------------------------------------------------------------------------
-- El id se calcula porque en varias bases parametros.id no es AUTO_INCREMENT.
INSERT INTO parametros (id, modulo, parametro, valor)
SELECT t.nuevo, 'promociones', 'descuentos', '0'
FROM (SELECT IFNULL(MAX(id), 0) + 1 AS nuevo,
             IFNULL(SUM(modulo = 'promociones' AND parametro = 'descuentos'), 0) AS existe
      FROM parametros) t
WHERE t.existe = 0;

-- ----------------------------------------------------------------------------
-- 6) Ítem de menú "ABM Promociones por Cantidad / Descuento" (hijo del menú Productos = 200)
--    y asignación a los mismos tipos de usuario que ya tienen el menú "ABM Promociones" (212).
-- ----------------------------------------------------------------------------
INSERT INTO menuPermisos (id, funcion, nombreControl, Url)
SELECT 213, 'ABM Promociones por Cantidad / Descuento', 'promocionesDescuentosToolStripMenuItem', '/Productos/PromocionesDescuentos'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM menuPermisos WHERE id = 213);

INSERT INTO tipoDeUsuariosPermisos (fk_tipoUsuario, fk_menuPermiso)
SELECT t.fk_tipoUsuario, 213
FROM tipoDeUsuariosPermisos t
WHERE t.fk_menuPermiso = 212
  AND NOT EXISTS (SELECT 1 FROM tipoDeUsuariosPermisos x WHERE x.fk_tipoUsuario = t.fk_tipoUsuario AND x.fk_menuPermiso = 213);
