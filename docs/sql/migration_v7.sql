-- ============================================================
-- Gestor Financiero Personal — Schema v7
-- El sueldo deja de ser un campo del periodo y pasa a ser un
-- ingreso mas. Un solo numero por mes no admite a quien cobra
-- en quincena y fin de mes; los ingresos ya llevan fecha y
-- pueden ser varios, asi que el campo sobraba.
-- Aplicar sobre GestorIngresosDB (schema v6 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

-- Primero se conserva el valor como ingreso: si este INSERT fallara, el script
-- aborta antes del DROP y no se pierde nada.
INSERT INTO ingresos (periodo_id, monto, fecha, descripcion, tipo)
SELECT id, sueldo_base, fecha_inicio, 'Sueldo base', 'SUELDO'
FROM periodos
WHERE sueldo_base > 0;

ALTER TABLE periodos DROP COLUMN sueldo_base;
