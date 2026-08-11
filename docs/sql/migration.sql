-- ============================================================
-- Gestor de Ingresos y Egresos — Script de Migración Completa
-- ============================================================

CREATE DATABASE IF NOT EXISTS GestorIngresosDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_spanish_ci;

-- Dar permisos al usuario stinjoss sobre esta BD (ejecutar como root)
GRANT ALL PRIVILEGES ON GestorIngresosDB.* TO 'stinjoss'@'%';
FLUSH PRIVILEGES;

USE GestorIngresosDB;

-- Tabla de usuarios
CREATE TABLE IF NOT EXISTS usuarios (
    id           INT PRIMARY KEY AUTO_INCREMENT,
    username     VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(64) NOT NULL,
    created_at   DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de movimientos
-- Si ya tenías la tabla sin usuario_id, ejecuta el ALTER de abajo por separado
CREATE TABLE IF NOT EXISTS movimientos (
    id          INT PRIMARY KEY AUTO_INCREMENT,
    fecha       DATE           NOT NULL,
    nombre      VARCHAR(100)   NOT NULL,
    monto       DECIMAL(15, 2) NOT NULL,
    tipo        ENUM('INGRESO', 'EGRESO') NOT NULL,
    descripcion TEXT,
    usuario_id  INT            NOT NULL DEFAULT 1,
    created_at  DATETIME       DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_mov_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

-- ─── Migración si la tabla movimientos ya existía ──────────────────────────────
-- Descomenta solo si la tabla ya existía sin la columna usuario_id:
-- ALTER TABLE movimientos ADD COLUMN IF NOT EXISTS usuario_id INT NOT NULL DEFAULT 1;
-- ALTER TABLE movimientos ADD CONSTRAINT fk_mov_usuario
--     FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE;
-- ──────────────────────────────────────────────────────────────────────────────

-- Usuario inicial: admin / admin123
-- (SHA2 usa el mismo algoritmo que PasswordHelper.cs en la app)
INSERT IGNORE INTO usuarios (id, username, password_hash)
VALUES (1, 'admin', SHA2('admin123', 256));

-- Para cambiar la contraseña desde MySQL:
-- UPDATE usuarios SET password_hash = SHA2('nueva_contraseña', 256) WHERE username = 'admin';

-- ─── Módulo de Deudas ─────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS deudas (
    id             INT PRIMARY KEY AUTO_INCREMENT,
    usuario_id     INT            NOT NULL,
    persona        VARCHAR(100)   NOT NULL,
    direccion      ENUM('ME_DEBEN','DEBO') NOT NULL,
    monto_actual   DECIMAL(15,2)  NOT NULL DEFAULT 0,
    descripcion    TEXT,
    fecha_creacion DATETIME       DEFAULT CURRENT_TIMESTAMP,
    activa         TINYINT(1)     NOT NULL DEFAULT 1,
    CONSTRAINT fk_deuda_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS deuda_movimientos (
    id       INT PRIMARY KEY AUTO_INCREMENT,
    deuda_id INT            NOT NULL,
    monto    DECIMAL(15,2)  NOT NULL,
    nota     TEXT,
    fecha    DATETIME       DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_dm_deuda FOREIGN KEY (deuda_id) REFERENCES deudas(id) ON DELETE CASCADE
);
