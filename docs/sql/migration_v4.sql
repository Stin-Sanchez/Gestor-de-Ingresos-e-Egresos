-- ============================================================
-- Gestor Financiero Personal — Schema v4
-- Multiusuario real: separa los datos de cada usuario.
-- La app WinForms era single-tenant (todos veian los mismos
-- periodos/deudas). La web si aisla por usuario_id.
-- Aplicar sobre GestorIngresosDB (schema v3 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

-- periodos es la raiz: ingresos y gastos cuelgan de periodo_id con
-- ON DELETE CASCADE, asi que basta con usuario_id aqui para aislarlos.
ALTER TABLE periodos ADD COLUMN usuario_id INT NOT NULL DEFAULT 1;
ALTER TABLE periodos ADD CONSTRAINT fk_per_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE;
-- Un usuario no puede tener dos periodos para el mismo mes (ObtenerOCrearPeriodo
-- consulta y luego inserta, asi que sin esta restriccion dos requests simultaneas
-- podrian crear el periodo dos veces).
CREATE UNIQUE INDEX uq_periodo_usuario_mes ON periodos (usuario_id, fecha_inicio);

-- deudas no cuelga de periodos, necesita su propio usuario_id.
ALTER TABLE deudas ADD COLUMN usuario_id INT NOT NULL DEFAULT 1;
ALTER TABLE deudas ADD CONSTRAINT fk_deu_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE;

-- password_hash pasa de SHA-256 (64 hex chars) a soportar tambien BCrypt (60 chars,
-- pero con prefijo variable); 255 da margen sin tener que volver a migrar.
ALTER TABLE usuarios MODIFY COLUMN password_hash VARCHAR(255) NOT NULL;
