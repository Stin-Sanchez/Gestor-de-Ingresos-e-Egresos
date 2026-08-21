-- ============================================================
-- Gestor Financiero Personal — Schema v3
-- Presupuestos por categoria y periodo
-- Aplicar sobre GestorIngresosDB (schema v2 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

CREATE TABLE IF NOT EXISTS presupuestos (
    id           INT AUTO_INCREMENT PRIMARY KEY,
    periodo_id   INT           NOT NULL,
    categoria_id INT           NOT NULL,
    monto        DECIMAL(15,2) NOT NULL,
    UNIQUE KEY uq_periodo_categoria (periodo_id, categoria_id),
    CONSTRAINT fk_pre_periodo   FOREIGN KEY (periodo_id)   REFERENCES periodos(id)         ON DELETE CASCADE,
    CONSTRAINT fk_pre_categoria FOREIGN KEY (categoria_id) REFERENCES categorias_gasto(id) ON DELETE CASCADE
);
