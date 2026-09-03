-- ============================================================
-- Gestor Financiero Personal — Schema v9
-- Prestar mas dinero a la misma persona no deberia obligar a
-- crear otra deuda. Una ampliacion sube monto_original y queda
-- registrada aparte para no perder el rastro de que la deuda
-- de 150 fueron en realidad 100 y luego 50 mas.
-- Aplicar sobre GestorIngresosDB (schema v8 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

CREATE TABLE deuda_ampliaciones (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    deuda_id    INT           NOT NULL,
    monto       DECIMAL(15,2) NOT NULL,
    fecha       DATE          NOT NULL,
    descripcion VARCHAR(255)  NOT NULL DEFAULT '',
    CONSTRAINT fk_ampliacion_deuda FOREIGN KEY (deuda_id) REFERENCES deudas(id) ON DELETE CASCADE
);

CREATE INDEX idx_ampliaciones_deuda ON deuda_ampliaciones (deuda_id);
