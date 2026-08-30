-- Crea solo la tabla usuarios (sin el GRANT de migration.sql, innecesario en Docker:
-- el usuario de la app ya se crea via MYSQL_USER/MYSQL_PASSWORD del contenedor).
-- migration_v2/v3/v4 se montan despues de este archivo y asumen que usuarios ya existe.
CREATE TABLE IF NOT EXISTS usuarios (
    id            INT PRIMARY KEY AUTO_INCREMENT,
    username      VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(64)  NOT NULL,
    created_at    DATETIME DEFAULT CURRENT_TIMESTAMP
);
