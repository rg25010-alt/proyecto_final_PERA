-- ============================================================================
-- PERA | 01_esquema.sql | PostgreSQL 17
-- Universidad de El Salvador - Facultad Multidisciplinaria Oriental
-- Etapa 1: 22 tablas, PK, FK, UNIQUE, CHECK e indices.
-- Basado en Documento Final Proyecto PERA (Google Docs) y Avance 1 BD2.
-- ADAPTACIONES EXPLICITAS al modelo previo:
--   * Un expediente/intento PERA puede tener muchos tutores (asignacion_docentes).
--   * Una sesion puede tener varios tutores (sesion_docentes) y estudiantes
--     (participantes_sesion), evitando duplicar el expediente.
--   * Cada estudiante del sistema tiene cuenta propia: id_usuario NOT NULL.
--   * Se elimina nombre_completo de estudiantes (3FN: vive en usuarios).
--   * Se agrega Coordinacion como rol y se habilita la notificacion temprana.
--   * Se agregan habilitaciones_egreso y auditoria_eventos para RF-03/RNF-04.
--   * solicitudes_reprogramacion referencia a participantes_sesion para garantizar
--     integridad de pertenencia a la sesion.
--   * Se agrega refresh_tokens (tabla 22) para autenticacion JWT + Refresh Token
--     con rotacion y revocacion; el Access Token (JWT) es stateless.
--   * Borrador permite crear sesion + participantes en una transaccion y luego
--     publicarla como Programada.
-- IMPORTANTE: Las reglas ENTRE tablas (UV acumuladas, choques de horario,
-- calificacion final, emision de alertas, conteo de intentos_fallidos) van en
-- 02_reglas.sql.
-- No contiene DROP ni borra datos existentes. Ejecutar UNA SOLA VEZ
-- sobre una pera_db VACIA y con -v ON_ERROR_STOP=1.
-- ============================================================================

BEGIN;
SET LOCAL search_path TO public;

-- 01
CREATE TABLE departamentos (
    id_departamento integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre varchar(100) NOT NULL UNIQUE,
    activo boolean NOT NULL DEFAULT true,
    CONSTRAINT ck_departamento_nombre CHECK (btrim(nombre) <> '')
);

-- 02
CREATE TABLE carreras (
    id_carrera integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_departamento integer NOT NULL REFERENCES departamentos(id_departamento) ON DELETE RESTRICT,
    nombre_carrera varchar(120) NOT NULL UNIQUE,
    CONSTRAINT ck_carrera_nombre CHECK (btrim(nombre_carrera) <> '')
);

-- 03
CREATE TABLE roles (
    id_rol smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo varchar(30) NOT NULL UNIQUE,
    nombre varchar(80) NOT NULL,
    descripcion varchar(200),
    CONSTRAINT ck_roles_codigo CHECK (codigo IN
        ('ADMIN_ACADEMICA','COORDINACION_PERA','JEFATURA','DOCENTE','ESTUDIANTE'))
);

-- 04
-- id_departamento en usuarios aplica solo a roles administrativos (ej. JEFATURA).
-- Para docentes, el departamento autoritativo se registra en la tabla docentes.
CREATE TABLE usuarios (
    id_usuario bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_rol smallint NOT NULL REFERENCES roles(id_rol) ON DELETE RESTRICT,
    id_departamento integer REFERENCES departamentos(id_departamento) ON DELETE RESTRICT,
    nombres varchar(80) NOT NULL,
    apellidos varchar(80) NOT NULL,
    correo varchar(160) NOT NULL,
    clave_hash varchar(255) NOT NULL,
    activo boolean NOT NULL DEFAULT true,
    creado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_usuario_nombres CHECK (btrim(nombres) <> '' AND btrim(apellidos) <> ''),
    CONSTRAINT ck_usuario_correo CHECK (position('@' IN correo) > 1 AND btrim(correo) = correo),
    CONSTRAINT ck_usuario_clave_hash CHECK (char_length(clave_hash) >= 20)
);
-- Comparacion de correos sin diferenciar mayusculas/minusculas.
CREATE UNIQUE INDEX ux_usuarios_correo_ci ON usuarios (lower(correo));
CREATE INDEX ix_usuarios_id_rol ON usuarios(id_rol);
CREATE INDEX ix_usuarios_id_departamento ON usuarios(id_departamento);

-- 05
-- correo_contacto es opcional para correo personal alterno al institucional (usuarios.correo).
-- intentos_fallidos es desnormalizacion controlada: mantener sincronizada solo por trigger en 02_reglas.sql.
CREATE TABLE estudiantes (
    carnet varchar(20) PRIMARY KEY,
    id_carrera integer NOT NULL REFERENCES carreras(id_carrera) ON DELETE RESTRICT,
    id_usuario bigint NOT NULL UNIQUE REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    correo_contacto varchar(160),
    cum_actual numeric(4,2) NOT NULL,
    intentos_fallidos smallint NOT NULL DEFAULT 0,
    registrado_en timestamptz NOT NULL DEFAULT now(),
    id_usuario_registra bigint NOT NULL REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    CONSTRAINT ck_estudiante_carnet CHECK (btrim(carnet) <> ''),
    CONSTRAINT ck_estudiante_correo_contacto CHECK (
        correo_contacto IS NULL OR (position('@' IN correo_contacto) > 1 AND btrim(correo_contacto) = correo_contacto)
    ),
    CONSTRAINT ck_estudiante_cum CHECK (cum_actual BETWEEN 0.00 AND 10.00),
    CONSTRAINT ck_estudiante_intentos CHECK (intentos_fallidos >= 0)
);
CREATE INDEX ix_estudiantes_id_carrera ON estudiantes(id_carrera);
CREATE INDEX ix_estudiantes_usuario_registra ON estudiantes(id_usuario_registra);

-- 06
CREATE TABLE docentes (
    id_docente bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_usuario bigint NOT NULL UNIQUE REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    id_departamento integer NOT NULL REFERENCES departamentos(id_departamento) ON DELETE RESTRICT,
    codigo_docente varchar(25) NOT NULL UNIQUE,
    activo boolean NOT NULL DEFAULT true,
    creado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_docente_codigo CHECK (btrim(codigo_docente) <> '')
);
CREATE INDEX ix_docentes_id_departamento ON docentes(id_departamento);

-- 07
-- Reglas vigentes de UV: NO se insertan rangos inventados.
-- Los limites CUM oficiales deben cargarse tras ser verificados con la UES.
CREATE TABLE reglas_uv (
    id_regla integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cum_min numeric(4,2) NOT NULL,
    cum_max numeric(4,2) NOT NULL,
    uv_max smallint NOT NULL,
    vigente boolean NOT NULL DEFAULT true,
    CONSTRAINT ck_regla_cum CHECK (
       cum_min BETWEEN 0 AND 10 AND cum_max BETWEEN 0 AND 10 AND cum_min <= cum_max
    ),
    CONSTRAINT ck_regla_uv CHECK (uv_max IN (8,12,16)),
    CONSTRAINT ex_reglas_uv_sin_solape EXCLUDE USING gist (
       numrange(cum_min, cum_max, '[]') WITH &&
    ) WHERE (vigente)
);

-- 08
CREATE TABLE asignaturas (
    id_asignatura integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo varchar(20) NOT NULL UNIQUE,
    nombre varchar(120) NOT NULL,
    unidades_valorativas smallint NOT NULL,
    CONSTRAINT ck_asignatura_codigo CHECK (btrim(codigo) <> ''),
    CONSTRAINT ck_asignatura_nombre CHECK (btrim(nombre) <> ''),
    CONSTRAINT ck_asignatura_uv CHECK (unidades_valorativas > 0)
);

-- 09
CREATE TABLE inscripciones (
    id_inscripcion bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    carnet_estudiante varchar(20) NOT NULL REFERENCES estudiantes(carnet) ON DELETE RESTRICT,
    id_asignatura integer NOT NULL REFERENCES asignaturas(id_asignatura) ON DELETE RESTRICT,
    periodo_academico varchar(20) NOT NULL,
    fecha_inscripcion date NOT NULL DEFAULT current_date,
    estado varchar(12) NOT NULL DEFAULT 'Activa',
    CONSTRAINT uq_inscripcion_unica UNIQUE (carnet_estudiante,id_asignatura,periodo_academico),
    CONSTRAINT ck_inscripcion_periodo CHECK (btrim(periodo_academico) <> ''),
    CONSTRAINT ck_inscripcion_estado CHECK (estado IN ('Activa','Retirada','Anulada'))
);
CREATE INDEX ix_inscripciones_asignatura ON inscripciones(id_asignatura);
CREATE INDEX ix_inscripciones_carnet_periodo ON inscripciones(carnet_estudiante,periodo_academico);

-- 10
-- Representa UN proceso/intento PERA, independientemente de cuantos tutores haya.
CREATE TABLE asignaciones_tutoria (
    id_asignacion bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    carnet_estudiante varchar(20) NOT NULL REFERENCES estudiantes(carnet) ON DELETE RESTRICT,
    id_usuario_asignador bigint REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    periodo_academico varchar(20) NOT NULL,
    fecha_referencia date NOT NULL DEFAULT current_date,
    numero_intento smallint NOT NULL,
    nota_final numeric(4,2),
    estado_pera varchar(12) NOT NULL DEFAULT 'En Curso',
    creado_en timestamptz NOT NULL DEFAULT now(),
    actualizado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_asignacion_intento UNIQUE (carnet_estudiante,periodo_academico,numero_intento),
    CONSTRAINT ck_asignacion_periodo CHECK (btrim(periodo_academico) <> ''),
    CONSTRAINT ck_asignacion_intento CHECK (numero_intento >= 1),
    CONSTRAINT ck_asignacion_nota CHECK (nota_final BETWEEN 0 AND 10),
    CONSTRAINT ck_asignacion_estado CHECK (estado_pera IN ('En Curso','Aprobado','Reprobado')),
    -- Umbral 7.0 proveniente de Avance 1. RF-11 del documento vivo no
    -- muestra numero legible: confirmar con docente antes de produccion.
    CONSTRAINT ck_asignacion_nota_estado CHECK (
       (nota_final IS NULL AND estado_pera = 'En Curso') OR
       (nota_final >= 7.00 AND estado_pera = 'Aprobado') OR
       (nota_final < 7.00 AND estado_pera = 'Reprobado')
    )
);
CREATE INDEX ix_asignaciones_estudiante ON asignaciones_tutoria(carnet_estudiante);
CREATE UNIQUE INDEX ux_asignacion_un_proceso_en_curso ON asignaciones_tutoria(carnet_estudiante, periodo_academico) WHERE estado_pera = 'En Curso';
CREATE INDEX ix_asignaciones_asignador ON asignaciones_tutoria(id_usuario_asignador);

-- 11
-- Adaptacion al RF-05: 1 asignacion -> N tutores, cada tutor con su area.
CREATE TABLE asignacion_docentes (
    id_asignacion bigint NOT NULL REFERENCES asignaciones_tutoria(id_asignacion) ON DELETE RESTRICT,
    id_docente bigint NOT NULL REFERENCES docentes(id_docente) ON DELETE RESTRICT,
    area_refuerzo varchar(160) NOT NULL,
    activo boolean NOT NULL DEFAULT true,
    fecha_asignacion date NOT NULL DEFAULT current_date,
    PRIMARY KEY (id_asignacion,id_docente),
    CONSTRAINT ck_asignacion_docente_area CHECK (btrim(area_refuerzo) <> '')
);
CREATE INDEX ix_asignacion_docentes_docente ON asignacion_docentes(id_docente);

-- 12
-- Se admite Borrador para montar en una sola transaccion la sesion y sus
-- docentes/participantes. Solo Programada/Reprogramada aparece en agendas.
CREATE TABLE sesiones_tutoria (
    id_sesion bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_docente_creador bigint NOT NULL REFERENCES docentes(id_docente) ON DELETE RESTRICT,
    fecha_inicio timestamptz NOT NULL,
    fecha_fin timestamptz NOT NULL,
    modalidad varchar(12) NOT NULL,
    ubicacion_enlace varchar(255),
    estado_sesion varchar(15) NOT NULL DEFAULT 'Borrador',
    creado_en timestamptz NOT NULL DEFAULT now(),
    actualizado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_sesion_fechas CHECK (fecha_fin > fecha_inicio),
    CONSTRAINT ck_sesion_modalidad CHECK (modalidad IN ('Presencial','Virtual')),
    CONSTRAINT ck_sesion_estado CHECK (estado_sesion IN
       ('Borrador','Programada','Reprogramada','Cancelada','Realizada')),
    CONSTRAINT ck_sesion_enlace CHECK (
       modalidad <> 'Virtual' OR
       (ubicacion_enlace IS NOT NULL AND btrim(ubicacion_enlace) <> '')
    )
);
CREATE INDEX ix_sesiones_fechas ON sesiones_tutoria(fecha_inicio,fecha_fin);
CREATE INDEX ix_sesiones_creador ON sesiones_tutoria(id_docente_creador);

-- 13
-- Adaptacion al RF-05: una sesion puede incluir VARIOS tutores.
CREATE TABLE sesion_docentes (
    id_sesion bigint NOT NULL REFERENCES sesiones_tutoria(id_sesion) ON DELETE RESTRICT,
    id_docente bigint NOT NULL REFERENCES docentes(id_docente) ON DELETE RESTRICT,
    es_responsable boolean NOT NULL DEFAULT false,
    PRIMARY KEY (id_sesion,id_docente)
);
CREATE INDEX ix_sesion_docentes_docente ON sesion_docentes(id_docente);
CREATE UNIQUE INDEX ux_sesion_un_responsable ON sesion_docentes(id_sesion)
    WHERE (es_responsable);

-- 14
-- Una sesion tiene N participantes, cada uno corresponde a UN intento PERA.
CREATE TABLE participantes_sesion (
    id_participacion bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_sesion bigint NOT NULL REFERENCES sesiones_tutoria(id_sesion) ON DELETE RESTRICT,
    id_asignacion bigint NOT NULL REFERENCES asignaciones_tutoria(id_asignacion) ON DELETE RESTRICT,
    CONSTRAINT uq_participante_unico UNIQUE (id_sesion,id_asignacion)
);
CREATE INDEX ix_participantes_asignacion ON participantes_sesion(id_asignacion);

-- 15
CREATE TABLE asistencias (
    id_asistencia bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_participacion bigint NOT NULL UNIQUE REFERENCES participantes_sesion(id_participacion) ON DELETE RESTRICT,
    id_docente_registra bigint NOT NULL REFERENCES docentes(id_docente) ON DELETE RESTRICT,
    asistio boolean NOT NULL,
    fecha_registro timestamptz NOT NULL DEFAULT now(),
    fecha_modificacion timestamptz,
    observaciones text,
    CONSTRAINT ck_asistencia_fechas CHECK (
      fecha_modificacion IS NULL OR fecha_modificacion >= fecha_registro
    )
);
CREATE INDEX ix_asistencias_docente ON asistencias(id_docente_registra);

-- 16
CREATE TABLE bitacoras_avance (
    id_bitacora bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_participacion bigint NOT NULL REFERENCES participantes_sesion(id_participacion) ON DELETE RESTRICT,
    id_docente_autor bigint NOT NULL REFERENCES docentes(id_docente) ON DELETE RESTRICT,
    fecha_registro timestamptz NOT NULL DEFAULT now(),
    descripcion text NOT NULL,
    calificacion numeric(4,2),
    acuerdos text,
    CONSTRAINT ck_bitacora_descripcion CHECK (btrim(descripcion) <> ''),
    CONSTRAINT ck_bitacora_nota CHECK (calificacion BETWEEN 0 AND 10)
);
CREATE INDEX ix_bitacoras_participacion_fecha ON bitacoras_avance(id_participacion,fecha_registro);
CREATE INDEX ix_bitacoras_autor ON bitacoras_avance(id_docente_autor);

-- 17
-- Se permiten notificaciones por sesiones Y alertas tempranas por estudiante.
CREATE TABLE notificaciones (
    id_notificacion bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_sesion bigint REFERENCES sesiones_tutoria(id_sesion) ON DELETE RESTRICT,
    carnet_estudiante varchar(20) REFERENCES estudiantes(carnet) ON DELETE RESTRICT,
    id_usuario_destino bigint REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    correo_destino varchar(160),
    tipo varchar(25) NOT NULL,
    mensaje text NOT NULL,
    fecha_programada timestamptz NOT NULL DEFAULT now(),
    fecha_envio timestamptz,
    estado_envio varchar(12) NOT NULL DEFAULT 'Pendiente',
    CONSTRAINT ck_notificacion_tipo CHECK (tipo IN
      ('Programacion','Cambio','Cancelacion','ParticipanteAgregado',
       'TutorAgregado','AlertaTemprana','ResultadoPERA','SolicitudReprogramacion')),
    CONSTRAINT ck_notificacion_estado CHECK (estado_envio IN
      ('Pendiente','Enviado','Error')),
    CONSTRAINT ck_notificacion_mensaje CHECK (btrim(mensaje) <> ''),
    CONSTRAINT ck_notificacion_origen CHECK (
      id_sesion IS NOT NULL OR carnet_estudiante IS NOT NULL
    ),
    CONSTRAINT ck_notificacion_destino CHECK (
      id_usuario_destino IS NOT NULL OR
      (correo_destino IS NOT NULL AND btrim(correo_destino) <> '')
    ),
    CONSTRAINT ck_notificacion_envio CHECK (
      estado_envio <> 'Enviado' OR fecha_envio IS NOT NULL
    )
);
CREATE INDEX ix_notificaciones_destino_estado ON notificaciones(id_usuario_destino,estado_envio);
CREATE INDEX ix_notificaciones_sesion ON notificaciones(id_sesion);
CREATE INDEX ix_notificaciones_estudiante ON notificaciones(carnet_estudiante);

-- 18
-- Umbral parametrico para alertas de CUM de riesgo (RF-16).
-- No se coloca un numero inventado: hay que cargar el umbral oficial.
CREATE TABLE parametros_pera (
    id_parametro integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo varchar(40) NOT NULL UNIQUE,
    valor_numerico numeric(4,2) NOT NULL,
    id_usuario_actualiza bigint REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    actualizado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_parametro_codigo CHECK (codigo = 'UMBRAL_CUM_RIESGO'),
    CONSTRAINT ck_parametro_valor CHECK (valor_numerico BETWEEN 0.00 AND 10.00)
);

-- 19
-- RF-03. Modulo opcional en frontend, persistencia prevista desde el inicio.
CREATE TABLE habilitaciones_egreso (
    id_habilitacion bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_asignacion bigint NOT NULL UNIQUE REFERENCES asignaciones_tutoria(id_asignacion) ON DELETE RESTRICT,
    id_usuario_jefatura bigint NOT NULL REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    habilitado boolean NOT NULL,
    observacion text,
    registrado_en timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_habilitaciones_jefatura ON habilitaciones_egreso(id_usuario_jefatura);

-- 20
-- Solicitudes de reprogramacion iniciadas por estudiante (proceso opcional 3.3.4).
-- Referencia a participantes_sesion para asegurar que el estudiante pertenece a la sesion.
CREATE TABLE solicitudes_reprogramacion (
    id_solicitud bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_participacion bigint NOT NULL REFERENCES participantes_sesion(id_participacion) ON DELETE RESTRICT,
    fecha_propuesta_inicio timestamptz NOT NULL,
    fecha_propuesta_fin timestamptz NOT NULL,
    motivo text NOT NULL,
    estado varchar(12) NOT NULL DEFAULT 'Pendiente',
    id_usuario_resuelve bigint REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    respuesta text,
    solicitado_en timestamptz NOT NULL DEFAULT now(),
    resuelto_en timestamptz,
    CONSTRAINT ck_solicitud_fechas CHECK (fecha_propuesta_fin > fecha_propuesta_inicio),
    CONSTRAINT ck_solicitud_motivo CHECK (btrim(motivo) <> ''),
    CONSTRAINT ck_solicitud_estado CHECK (estado IN ('Pendiente','Aprobada','Rechazada','Cancelada')),
    CONSTRAINT ck_solicitud_resolucion CHECK (
      (estado = 'Pendiente' AND id_usuario_resuelve IS NULL AND resuelto_en IS NULL)
      OR (estado <> 'Pendiente' AND resuelto_en IS NOT NULL)
    )
);
CREATE UNIQUE INDEX ux_solicitud_pendiente ON solicitudes_reprogramacion(id_participacion)
    WHERE estado = 'Pendiente';
CREATE INDEX ix_solicitud_estado ON solicitudes_reprogramacion(estado,solicitado_en);

-- 21
-- RNF-04. Triggers auditan cambios; el backend debe pasar la identidad verificada de sesion.
CREATE TABLE auditoria_eventos (
    id_evento bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_usuario_actor bigint REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    entidad varchar(80) NOT NULL,
    id_registro varchar(100),
    accion varchar(15) NOT NULL,
    descripcion text,
    ocurrido_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_auditoria_accion CHECK (accion IN ('INSERT','UPDATE','DELETE','LOGIN','LOGOUT','OTRA')),
    CONSTRAINT ck_auditoria_entidad CHECK (btrim(entidad) <> '')
);
CREATE INDEX ix_auditoria_eventos_fecha ON auditoria_eventos(ocurrido_en);
CREATE INDEX ix_auditoria_eventos_actor ON auditoria_eventos(id_usuario_actor);

-- 22
-- Sesiones persistentes para autenticacion con Refresh Token (rotacion y revocacion).
-- El Access Token (JWT) es stateless y no se almacena en BD.
-- token_hash guarda SHA-256 en hexadecimal (64 chars), nunca el token en texto plano.
CREATE TABLE refresh_tokens (
    id_refresh_token bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_usuario bigint NOT NULL REFERENCES usuarios(id_usuario) ON DELETE RESTRICT,
    token_hash char(64) NOT NULL UNIQUE,
    creado_en timestamptz NOT NULL DEFAULT now(),
    expira_en timestamptz NOT NULL,
    revocado_en timestamptz,
    reemplazado_por bigint REFERENCES refresh_tokens(id_refresh_token) ON DELETE RESTRICT,
    CONSTRAINT ck_refresh_token_hash CHECK (char_length(btrim(token_hash)) = 64),
    CONSTRAINT ck_refresh_token_expira CHECK (expira_en > creado_en),
    CONSTRAINT ck_refresh_token_revocado CHECK (
        revocado_en IS NULL OR revocado_en >= creado_en
    ),
    CONSTRAINT ck_refresh_token_reemplazo CHECK (
        reemplazado_por IS NULL OR revocado_en IS NOT NULL
    )
);
CREATE INDEX ix_refresh_tokens_usuario ON refresh_tokens(id_usuario);
CREATE INDEX ix_refresh_tokens_activos ON refresh_tokens(id_usuario, expira_en)
    WHERE revocado_en IS NULL;

COMMIT;