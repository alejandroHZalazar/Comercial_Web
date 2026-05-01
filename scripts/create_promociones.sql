-- ============================================================
-- Módulo Promociones / Combos
-- Ejecutar en la BD MySQL (ale) antes de usar el módulo
-- ============================================================

-- 1. Agregar columna esPromocion a productos (si no existe)
ALTER TABLE productos
  ADD COLUMN IF NOT EXISTS esPromocion TINYINT(1) NOT NULL DEFAULT 0;

-- 2. Tabla de promociones (cabecera)
CREATE TABLE IF NOT EXISTS promociones (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    fk_producto INT NOT NULL,
    activa      TINYINT(1) NOT NULL DEFAULT 1,
    fechaDesde  DATE NULL,
    fechaHasta  DATE NULL,
    FOREIGN KEY (fk_producto) REFERENCES productos(id)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- 3. Slots de cada promoción
CREATE TABLE IF NOT EXISTS promociones_slots (
    id                INT AUTO_INCREMENT PRIMARY KEY,
    fk_promocion      INT NOT NULL,
    numero            INT NOT NULL,
    cantidadRequerida DECIMAL(18,4) NOT NULL,
    descripcion       VARCHAR(100) NULL,
    FOREIGN KEY (fk_promocion) REFERENCES promociones(id)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- 4. Productos elegibles por slot
CREATE TABLE IF NOT EXISTS promociones_slot_productos (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    fk_slot     INT NOT NULL,
    fk_producto INT NOT NULL,
    FOREIGN KEY (fk_slot)     REFERENCES promociones_slots(id),
    FOREIGN KEY (fk_producto) REFERENCES productos(id)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- 5. Componentes elegidos al vender una promo (auditoría + stock)
CREATE TABLE IF NOT EXISTS ventas_promo_componentes (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    fk_ventaDetalle BIGINT NOT NULL,
    fk_slot         INT NOT NULL,
    fk_producto     INT NOT NULL,
    cantidad        DECIMAL(18,4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
