-- ============================================================
-- Gestor Financiero Personal — Schema v8
-- El periodo pasa a tener ciclo de vida real: cerrado significa
-- solo lectura, se cierra solo al vencer y se puede reabrir.
--   - dia_corte  : el periodo arranca ese dia del mes (1 = mes calendario).
--   - dias_gracia: margen tras fecha_fin antes del cierre automatico.
--   - reabierto  : un periodo reabierto a mano no vuelve a cerrarse solo,
--                  o el cierre automatico desharia la reapertura al instante.
-- Aplicar sobre GestorIngresosDB (schema v7 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

ALTER TABLE usuarios
    ADD COLUMN dia_corte   TINYINT UNSIGNED NOT NULL DEFAULT 1,
    ADD COLUMN dias_gracia TINYINT UNSIGNED NOT NULL DEFAULT 5;

ALTER TABLE periodos
    ADD COLUMN reabierto TINYINT(1) NOT NULL DEFAULT 0;
