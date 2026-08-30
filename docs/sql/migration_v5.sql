-- ============================================================
-- Gestor Financiero Personal — Schema v5
-- Cuentas de usuario: registro propio, perfil con avatar y
-- segundo factor TOTP (Google Authenticator y compatibles).
-- Aplicar sobre GestorIngresosDB (schema v4 ya debe existir)
-- ============================================================

USE GestorIngresosDB;

ALTER TABLE usuarios
    ADD COLUMN email        VARCHAR(160)  NULL,
    -- Solo el nombre del archivo; el binario vive en el volumen de avatares.
    ADD COLUMN avatar       VARCHAR(120)  NULL,
    -- Secreto TOTP en Base32. Se guarda al iniciar el alta del segundo factor,
    -- pero totp_activo solo pasa a 1 cuando el usuario confirma un codigo valido.
    ADD COLUMN totp_secret  VARCHAR(64)   NULL,
    ADD COLUMN totp_activo  TINYINT(1)    NOT NULL DEFAULT 0;

-- MySQL permite varios NULL en un indice unico, asi que el email sigue siendo opcional.
CREATE UNIQUE INDEX uq_usuarios_email ON usuarios (email);
