-- ============================================================================
-- Generación automática de códigos de barras
-- ----------------------------------------------------------------------------
--   * Solo agrega dos filas en la tabla parametros (y, opcionalmente, un índice). No modifica productos
--     ni regenera códigos existentes.
--   * Compatible con MySQL 5.5 y 8.x. Funciona tanto si parametros.id es AUTO_INCREMENT como si no
--     (en varias bases no lo es), porque el id se calcula aquí.
--   * Se puede ejecutar más de una vez: no duplica filas.
--   * Ejecutar sobre cada base donde se quiera usar.
--   * Para ACTIVAR: pantalla Configuración → Parámetros, o
--       UPDATE parametros SET valor='1' WHERE modulo='productos' AND parametro='generaCodigoBarras';
-- ============================================================================

-- 1) Interruptor (apagado por defecto)
INSERT INTO parametros (id, modulo, parametro, valor)
SELECT t.nuevo, 'productos', 'generaCodigoBarras', '0'
FROM (SELECT IFNULL(MAX(id), 0) + 1 AS nuevo,
             IFNULL(SUM(modulo = 'productos' AND parametro = 'generaCodigoBarras'), 0) AS existe
      FROM parametros) t
WHERE t.existe = 0;

-- 2) Próximo número a probar (contador). Su fila se bloquea durante cada alta para evitar duplicados.
INSERT INTO parametros (id, modulo, parametro, valor)
SELECT t.nuevo, 'productos', 'proximoCodigoBarras', '1'
FROM (SELECT IFNULL(MAX(id), 0) + 1 AS nuevo,
             IFNULL(SUM(modulo = 'productos' AND parametro = 'proximoCodigoBarras'), 0) AS existe
      FROM parametros) t
WHERE t.existe = 0;

-- 3) OPCIONAL: acelera la verificación de códigos y la búsqueda por escáner (índice NO único).
--    Si el índice ya existe, MySQL informa "Duplicate key name": se puede ignorar.
CREATE INDEX IX_Productos_codBarras ON Productos (codBarras);
