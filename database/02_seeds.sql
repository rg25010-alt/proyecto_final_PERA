
-- SISTEMA PERA
-- Datos iniciales
-- Fase 1: Roles del sistema

INSERT INTO roles (codigo, nombre, descripcion)
VALUES
(
  'ADMIN_ACADEMICA',
  'Administración Académica',
  'Gestiona expedientes de estudiantes'
),
(
  'COORDINACION_PERA',
  'Coordinación PERA',
  'Coordina y asigna tutores'
),
(
  'JEFATURA',
  'Jefatura de Departamento',
  'Supervisa docentes e indicadores'
),
(
  'DOCENTE',
  'Docente Tutor',
  'Programa tutorías y registra avances'
),
(
  'ESTUDIANTE',
  'Estudiante',
  'Consulta su información PERA'
)
ON CONFLICT (codigo) DO NOTHING;



-- DEPARTAMENTOS
INSERT INTO departamentos (nombre)
VALUES ('Departamento de Ingeniería y Arquitectura')
ON CONFLICT (nombre) DO NOTHING;

-- CARRERAS
INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT id_departamento, 'Ingeniería de Sistemas Informáticos'
FROM departamentos
WHERE nombre = 'Departamento de Ingeniería y Arquitectura'
ON CONFLICT (nombre_carrera) DO NOTHING;



-- ==========================================
-- DEPARTAMENTOS ACADÉMICOS DE LA FMO
-- ==========================================

INSERT INTO departamentos (nombre)
VALUES
('Departamento de Medicina'),
('Departamento de Ciencias Económicas'),
('Departamento de Ciencias y Humanidades'),
('Departamento de Jurisprudencia y Ciencias Sociales'),
('Departamento de Ciencias Agronómicas'),
('Departamento de Ciencias Naturales y Matemática'),
('Departamento de Química y Farmacia')
ON CONFLICT (nombre) DO NOTHING;


-- ==========================================
-- CARRERAS DE INGENIERÍA Y ARQUITECTURA
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT d.id_departamento, c.nombre_carrera
FROM departamentos d
CROSS JOIN (
    VALUES
    ('Ingeniería Industrial'),
    ('Ingeniería Civil'),
    ('Arquitectura'),
    ('Ingeniería Eléctrica'),
    ('Ingeniería Mecánica')
) AS c(nombre_carrera)
WHERE d.nombre = 'Departamento de Ingeniería y Arquitectura'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- CARRERAS DE CIENCIAS ECONÓMICAS
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT d.id_departamento, c.nombre_carrera
FROM departamentos d
CROSS JOIN (
    VALUES
    ('Licenciatura en Economía'),
    ('Licenciatura en Administración de Empresas'),
    ('Licenciatura en Mercadeo Internacional'),
    ('Licenciatura en Contaduría Pública'),
    ('Licenciatura en Logística Comercial Internacional')
) AS c(nombre_carrera)
WHERE d.nombre = 'Departamento de Ciencias Económicas'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- CARRERAS DEL DEPARTAMENTO DE MEDICINA
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT d.id_departamento, c.nombre_carrera
FROM departamentos d
CROSS JOIN (
    VALUES
    ('Doctorado en Medicina'),
    ('Licenciatura en Fisioterapia y Terapia Ocupacional'),
    ('Licenciatura en Laboratorio Clínico'),
    ('Licenciatura en Anestesiología e Inhaloterapia')
) AS c(nombre_carrera)
WHERE d.nombre = 'Departamento de Medicina'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- JURISPRUDENCIA Y CIENCIAS SOCIALES
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT
    id_departamento,
    'Licenciatura en Ciencias Jurídicas'
FROM departamentos
WHERE nombre = 'Departamento de Jurisprudencia y Ciencias Sociales'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- DEPARTAMENTO DE CIENCIAS AGRONÓMICAS
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT
    id_departamento,
    'Ingeniería Agronómica'
FROM departamentos
WHERE nombre = 'Departamento de Ciencias Agronómicas'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- CIENCIAS NATURALES Y MATEMÁTICA
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT d.id_departamento, c.nombre_carrera
FROM departamentos d
CROSS JOIN (
    VALUES
    ('Licenciatura en Matemática'),
    ('Licenciatura en Biología'),
    ('Licenciatura en Física'),
    ('Licenciatura en Ciencias Químicas'),
    ('Profesorado en Matemática para Tercer Ciclo de Educación Básica y Educación Media'),
    ('Profesorado en Física para Tercer Ciclo de Educación Básica y Educación Media'),
    ('Profesorado en Biología para Tercer Ciclo de Educación Básica y Educación Media'),
    ('Profesorado en Química para Tercer Ciclo de Educación Básica y Educación Media')
) AS c(nombre_carrera)
WHERE d.nombre = 'Departamento de Ciencias Naturales y Matemática'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- DEPARTAMENTO DE QUÍMICA Y FARMACIA
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT
    id_departamento,
    'Licenciatura en Química y Farmacia'
FROM departamentos
WHERE nombre = 'Departamento de Química y Farmacia'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- DEPARTAMENTO DE CIENCIAS Y HUMANIDADES
-- ==========================================

INSERT INTO carreras (id_departamento, nombre_carrera)
SELECT d.id_departamento, c.nombre_carrera
FROM departamentos d
CROSS JOIN (
    VALUES
    ('Licenciatura en Sociología'),
    ('Licenciatura en Lenguas Modernas: Especialidad en Francés e Inglés'),
    ('Licenciatura en Letras'),
    ('Licenciatura en Psicología'),
    ('Licenciatura en Educación Inicial y Parvularia'),
    ('Profesorado en Ciencias Sociales para Tercer Ciclo de Educación Básica y Educación Media'),
    ('Profesorado en Idioma Inglés para Tercer Ciclo de Educación Básica y Educación Media')
) AS c(nombre_carrera)
WHERE d.nombre = 'Departamento de Ciencias y Humanidades'
ON CONFLICT (nombre_carrera) DO NOTHING;


-- ==========================================
-- USUARIOS DE PRUEBA DEL SISTEMA PERA
-- FASE 1 - PUNTO 1
-- Solo para ambiente de desarrollo
-- CONTRASEÑA DE PRUEBA: PERA-Demo-2026!SoloPruebas
-- ==========================================

INSERT INTO usuarios (
    id_rol,
    id_departamento,
    nombres,
    apellidos,
    correo,
    clave_hash
)
SELECT
    r.id_rol,
    d.id_departamento,
    v.nombres,
    v.apellidos,
    v.correo,
    v.clave_hash
FROM (
    VALUES
    (
        'ADMIN_ACADEMICA',
        NULL,
        'Admin',
        'Prueba',
        'admin@pera.test',
        '$2a$12$oS962zz1ntpbKg/3yUcSUeMnrgn2gcFmCg5lQPhfwcwBGRyQEE4aG'
    ),
    (
        'COORDINACION_PERA',
        'Departamento de Ingeniería y Arquitectura',
        'Coordinador',
        'Prueba',
        'coordinacion@pera.test',
        '$2a$12$CwzOYG3C85ZmTUt2hfKWkOamf5r40RtJjTkv7jJ1syQn5z5pe918O'
    ),
    (
        'JEFATURA',
        'Departamento de Ingeniería y Arquitectura',
        'Jefatura',
        'Prueba',
        'jefatura@pera.test',
        '$2a$12$DFkCbd1dvd.ncZ.3IoqBKO88YFyE0KCG1lqg9P.qNbAHhqWe5og6G'
    ),
    (
        'DOCENTE',
        'Departamento de Ingeniería y Arquitectura',
        'Docente',
        'Prueba',
        'docente@pera.test',
        '$2a$12$aEE46wsfcY/4OApVSBvYCOcsLXoudknCcNNqmnJOwCzbyCX4bmyh2'
    ),
    (
        'ESTUDIANTE',
        NULL,
        'Estudiante',
        'Prueba',
        'estudiante@pera.test',
        '$2a$12$KGZeXBdGcqfOSiwCJRvrn.5MlK4mVdOlUIGk/ih.ML3SDfcLDbgae'
    )
) AS v(
    codigo_rol,
    nombre_departamento,
    nombres,
    apellidos,
    correo,
    clave_hash
)
JOIN roles r
    ON r.codigo = v.codigo_rol
LEFT JOIN departamentos d
    ON d.nombre = v.nombre_departamento
ON CONFLICT DO NOTHING;
