-- ============================================================
-- Gestor Financiero Personal — Schema v6
-- Deudas en dos direcciones: las que debo y las que me deben.
-- Aplicar sobre GestorIngresosDB (schema v5 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

-- DEBO: pagar reduce el saldo del periodo (se registra como gasto).
-- ME_DEBEN: cobrar lo aumenta (se registra como ingreso).
-- Las deudas que ya existian son todas del tipo original, DEBO.
ALTER TABLE deudas ADD COLUMN tipo ENUM('DEBO','ME_DEBEN') NOT NULL DEFAULT 'DEBO';

-- Un cobro es un ingreso ligado a la deuda, igual que un abono es un gasto ligado a ella.
ALTER TABLE ingresos ADD COLUMN deuda_id INT NULL;
ALTER TABLE ingresos ADD CONSTRAINT fk_ing_deuda FOREIGN KEY (deuda_id) REFERENCES deudas(id) ON DELETE SET NULL;
