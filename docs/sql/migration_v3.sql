-- ============================================================
-- Gestor Financiero Personal — Schema v3
-- Sobres consumibles: un gasto marcado como sobre se va
-- consumiendo con registros de consumo a lo largo del mes.
-- Aplicar sobre GestorIngresosDB (schema v2 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

-- Una version previa de este script creaba una tabla de presupuestos por categoria.
-- Ese modelo se descarto: el sobre es el gasto mismo, no una asignacion aparte.
DROP TABLE IF EXISTS presupuestos;

-- Marca que distingue un sobre consumible (Transporte $20, que se va gastando
-- durante el mes) de un gasto puntual ya ejecutado (un libro de $5).
ALTER TABLE gastos ADD COLUMN es_sobre TINYINT(1) NOT NULL DEFAULT 0;

-- Cada consumo descuenta del sobre al que pertenece.
CREATE TABLE IF NOT EXISTS consumos (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    gasto_id    INT           NOT NULL,
    monto       DECIMAL(15,2) NOT NULL,
    fecha       DATE          NOT NULL,
    descripcion VARCHAR(255)  NOT NULL DEFAULT '',
    CONSTRAINT fk_con_gasto FOREIGN KEY (gasto_id) REFERENCES gastos(id) ON DELETE CASCADE,
    INDEX idx_consumos_gasto (gasto_id)
);
