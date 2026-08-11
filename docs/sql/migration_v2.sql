-- ============================================================
-- Gestor Financiero Personal — Schema v2
-- Aplicar sobre GestorIngresosDB (usuarios ya debe existir)
-- ============================================================

USE GestorIngresosDB;

-- ─── Limpiar tablas viejas ────────────────────────────────────
DROP TABLE IF EXISTS pagos_deuda;
DROP TABLE IF EXISTS deuda_movimientos;
DROP TABLE IF EXISTS gastos;
DROP TABLE IF EXISTS ingresos;
DROP TABLE IF EXISTS periodos;
DROP TABLE IF EXISTS categorias_gasto;
DROP TABLE IF EXISTS deudas;
DROP TABLE IF EXISTS movimientos;

-- ─── Periodos ─────────────────────────────────────────────────
CREATE TABLE periodos (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    nombre        VARCHAR(50)    NOT NULL,
    fecha_inicio  DATE           NOT NULL,
    fecha_fin     DATE           NOT NULL,
    sueldo_base   DECIMAL(15,2)  NOT NULL DEFAULT 0,
    saldo_inicial DECIMAL(15,2)  NOT NULL DEFAULT 0,
    estado        ENUM('ABIERTO','CERRADO') NOT NULL DEFAULT 'ABIERTO'
);

-- ─── Categorías de gasto ──────────────────────────────────────
CREATE TABLE categorias_gasto (
    id     INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL
);

INSERT INTO categorias_gasto (nombre) VALUES
  ('Alimentacion'), ('Transporte'), ('Salud'), ('Educacion'),
  ('Entretenimiento'), ('Servicios basicos'), ('Ropa'), ('Deuda / Abono'), ('Otro');

-- ─── Deudas ───────────────────────────────────────────────────
CREATE TABLE deudas (
    id                INT AUTO_INCREMENT PRIMARY KEY,
    nombre            VARCHAR(100)   NOT NULL,
    acreedor          VARCHAR(100)   NOT NULL,
    monto_original    DECIMAL(15,2)  NOT NULL,
    monto_pagado      DECIMAL(15,2)  NOT NULL DEFAULT 0,
    fecha_inicio      DATE           NOT NULL,
    fecha_vencimiento DATE           NULL,
    estado            ENUM('ACTIVA','PAGADA') NOT NULL DEFAULT 'ACTIVA',
    descripcion       VARCHAR(255)   NOT NULL DEFAULT ''
);

-- ─── Ingresos ─────────────────────────────────────────────────
CREATE TABLE ingresos (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    periodo_id  INT            NOT NULL,
    monto       DECIMAL(15,2)  NOT NULL,
    fecha       DATE           NOT NULL,
    descripcion VARCHAR(255)   NOT NULL DEFAULT '',
    tipo        ENUM('SUELDO','EXTRA','OTRO') NOT NULL DEFAULT 'OTRO',
    CONSTRAINT fk_ing_periodo FOREIGN KEY (periodo_id) REFERENCES periodos(id) ON DELETE CASCADE
);

-- ─── Gastos (deuda_id != NULL = es un Abono) ──────────────────
CREATE TABLE gastos (
    id           INT AUTO_INCREMENT PRIMARY KEY,
    periodo_id   INT            NOT NULL,
    categoria_id INT            NULL,
    deuda_id     INT            NULL,
    monto        DECIMAL(15,2)  NOT NULL,
    fecha        DATE           NOT NULL,
    descripcion  VARCHAR(255)   NOT NULL DEFAULT '',
    CONSTRAINT fk_gas_periodo   FOREIGN KEY (periodo_id)   REFERENCES periodos(id)         ON DELETE CASCADE,
    CONSTRAINT fk_gas_categoria FOREIGN KEY (categoria_id) REFERENCES categorias_gasto(id) ON DELETE SET NULL,
    CONSTRAINT fk_gas_deuda     FOREIGN KEY (deuda_id)     REFERENCES deudas(id)           ON DELETE SET NULL
);

-- ─── Usuario inicial ──────────────────────────────────────────
INSERT IGNORE INTO usuarios (id, username, password_hash)
VALUES (1, 'admin', SHA2('admin123', 256));
