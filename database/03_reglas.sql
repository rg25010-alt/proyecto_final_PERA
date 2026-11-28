CREATE OR REPLACE FUNCTION fn_validar_uv_inscripcion()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_cum               numeric(4,2);
    v_uv_max            smallint;
    v_uv_actuales       integer;
    v_uv_asignatura     smallint;
BEGIN
    -- Las inscripciones que no estén activas no consumen UV.
    IF NEW.estado <> 'Activa' THEN
        RETURN NEW;
    END IF;

    -- Obtener el CUM actual del estudiante.
    SELECT e.cum_actual
      INTO v_cum
      FROM estudiantes e
     WHERE e.carnet = NEW.carnet_estudiante;

    IF NOT FOUND THEN
        RAISE EXCEPTION
            'No existe el estudiante con carnet %.',
            NEW.carnet_estudiante;
    END IF;

    -- Buscar la regla vigente correspondiente al CUM.
    SELECT r.uv_max
      INTO v_uv_max
      FROM reglas_uv r
     WHERE r.vigente = true
       AND v_cum BETWEEN r.cum_min AND r.cum_max
     LIMIT 1;

    IF v_uv_max IS NULL THEN
        RAISE EXCEPTION
            'No existe una regla UV vigente para el CUM % del estudiante %.',
            v_cum,
            NEW.carnet_estudiante;
    END IF;

    -- Obtener las UV de la asignatura que se intenta inscribir.
    SELECT a.unidades_valorativas
      INTO v_uv_asignatura
      FROM asignaturas a
     WHERE a.id_asignatura = NEW.id_asignatura;

    IF NOT FOUND THEN
        RAISE EXCEPTION
            'No existe la asignatura con id %.',
            NEW.id_asignatura;
    END IF;

    -- Sumar las UV activas que ya tiene en ese mismo período.
    SELECT COALESCE(SUM(a.unidades_valorativas), 0)
      INTO v_uv_actuales
      FROM inscripciones i
      JOIN asignaturas a
        ON a.id_asignatura = i.id_asignatura
     WHERE i.carnet_estudiante = NEW.carnet_estudiante
       AND i.periodo_academico = NEW.periodo_academico
       AND i.estado = 'Activa'
       AND (
            TG_OP = 'INSERT'
            OR i.id_inscripcion <> NEW.id_inscripcion
       );

    -- Validar que la nueva inscripción no exceda el máximo.
    IF v_uv_actuales + v_uv_asignatura > v_uv_max THEN
        RAISE EXCEPTION
            'Limite de UV excedido. Estudiante: %, UV actuales: %, nueva asignatura: % UV, maximo permitido: % UV.',
            NEW.carnet_estudiante,
            v_uv_actuales,
            v_uv_asignatura,
            v_uv_max;
    END IF;

    RETURN NEW;
END;
$$;


DROP TRIGGER IF EXISTS trg_validar_uv_inscripcion
ON inscripciones;

CREATE TRIGGER trg_validar_uv_inscripcion
BEFORE INSERT OR UPDATE OF
    carnet_estudiante,
    id_asignatura,
    periodo_academico,
    estado
ON inscripciones
FOR EACH ROW
EXECUTE FUNCTION fn_validar_uv_inscripcion();

-- ============================================================================
-- PREVENCION DE CHOQUES DE HORARIO DE DOCENTES
-- ============================================================================

-- --------------------------------------------------------------------------
-- Valida al agregar o cambiar un docente dentro de una sesion.
-- --------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_validar_choque_docente_sesion()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_inicio timestamptz;
    v_fin timestamptz;
    v_estado varchar(15);
BEGIN
    SELECT
        s.fecha_inicio,
        s.fecha_fin,
        s.estado_sesion
    INTO
        v_inicio,
        v_fin,
        v_estado
    FROM sesiones_tutoria s
    WHERE s.id_sesion = NEW.id_sesion;

    IF NOT FOUND THEN
        RAISE EXCEPTION
            'No existe la sesion con id %.',
            NEW.id_sesion;
    END IF;

    -- Los borradores no bloquean horario.
    IF v_estado NOT IN ('Programada', 'Reprogramada') THEN
        RETURN NEW;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM sesion_docentes sd
        JOIN sesiones_tutoria s
          ON s.id_sesion = sd.id_sesion
        WHERE sd.id_docente = NEW.id_docente
          AND sd.id_sesion <> NEW.id_sesion
          AND s.estado_sesion IN ('Programada', 'Reprogramada')
          AND tstzrange(s.fecha_inicio, s.fecha_fin, '[)')
              && tstzrange(v_inicio, v_fin, '[)')
    ) THEN
        RAISE EXCEPTION
            'El docente % ya posee otra sesion que se cruza con el horario de la sesion %.',
            NEW.id_docente,
            NEW.id_sesion;
    END IF;

    RETURN NEW;
END;
$$;


DROP TRIGGER IF EXISTS trg_validar_choque_docente_sesion
ON sesion_docentes;

CREATE TRIGGER trg_validar_choque_docente_sesion
BEFORE INSERT OR UPDATE OF id_sesion, id_docente
ON sesion_docentes
FOR EACH ROW
EXECUTE FUNCTION fn_validar_choque_docente_sesion();

-- Valida los docentes cuando una sesion cambia horario o se publica.

CREATE OR REPLACE FUNCTION fn_validar_choque_horario_sesion()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_docente bigint;
BEGIN
    -- Solo interesa bloquear sesiones que forman parte de la agenda real.
    IF NEW.estado_sesion NOT IN ('Programada', 'Reprogramada') THEN
        RETURN NEW;
    END IF;

    FOR v_docente IN
        SELECT sd.id_docente
        FROM sesion_docentes sd
        WHERE sd.id_sesion = NEW.id_sesion
    LOOP

        IF EXISTS (
            SELECT 1
            FROM sesion_docentes sd
            JOIN sesiones_tutoria s
              ON s.id_sesion = sd.id_sesion
            WHERE sd.id_docente = v_docente
              AND sd.id_sesion <> NEW.id_sesion
              AND s.estado_sesion IN ('Programada', 'Reprogramada')
              AND tstzrange(s.fecha_inicio, s.fecha_fin, '[)')
                  && tstzrange(NEW.fecha_inicio, NEW.fecha_fin, '[)')
        ) THEN
            RAISE EXCEPTION
                'El docente % tiene un choque de horario con otra sesion.',
                v_docente;
        END IF;

    END LOOP;

    RETURN NEW;
END;
$$;


DROP TRIGGER IF EXISTS trg_validar_choque_horario_sesion
ON sesiones_tutoria;

CREATE TRIGGER trg_validar_choque_horario_sesion
BEFORE UPDATE OF fecha_inicio, fecha_fin, estado_sesion
ON sesiones_tutoria
FOR EACH ROW
EXECUTE FUNCTION fn_validar_choque_horario_sesion();


-- ============================================================================
-- 3. CALIFICACION FINAL Y SINCRONIZACION DE INTENTOS FALLIDOS
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_sincronizar_resultado_pera()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_intentos_fallidos integer;
BEGIN
    -- Si existe nota final, el estado se determina automaticamente.
    IF NEW.nota_final IS NOT NULL THEN
        IF NEW.nota_final >= 7.00 THEN
            NEW.estado_pera := 'Aprobado';
        ELSE
            NEW.estado_pera := 'Reprobado';
        END IF;
    ELSE
        NEW.estado_pera := 'En Curso';
    END IF;

    NEW.actualizado_en := now();

    RETURN NEW;
END;
$$;


DROP TRIGGER IF EXISTS trg_sincronizar_resultado_pera
ON asignaciones_tutoria;

CREATE TRIGGER trg_sincronizar_resultado_pera
BEFORE INSERT OR UPDATE OF nota_final
ON asignaciones_tutoria
FOR EACH ROW
EXECUTE FUNCTION fn_sincronizar_resultado_pera();


CREATE OR REPLACE FUNCTION fn_actualizar_intentos_fallidos()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_carnet varchar(20);
    v_total integer;
BEGIN
    v_carnet := COALESCE(NEW.carnet_estudiante, OLD.carnet_estudiante);

    SELECT COUNT(*)
      INTO v_total
      FROM asignaciones_tutoria a
     WHERE a.carnet_estudiante = v_carnet
       AND a.estado_pera = 'Reprobado';

    UPDATE estudiantes
       SET intentos_fallidos = v_total
     WHERE carnet = v_carnet;

    RETURN COALESCE(NEW, OLD);
END;
$$;


DROP TRIGGER IF EXISTS trg_actualizar_intentos_fallidos
ON asignaciones_tutoria;

CREATE TRIGGER trg_actualizar_intentos_fallidos
AFTER INSERT OR UPDATE OR DELETE
ON asignaciones_tutoria
FOR EACH ROW
EXECUTE FUNCTION fn_actualizar_intentos_fallidos();

-- ============================================================================
-- ALERTAS TEMPRANAS POR CUM DE RIESGO
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_generar_alerta_cum_riesgo()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_umbral numeric(4,2);
    v_id_usuario bigint;
    v_correo varchar(160);
BEGIN
    SELECT p.valor_numerico
      INTO v_umbral
      FROM parametros_pera p
     WHERE p.codigo = 'UMBRAL_CUM_RIESGO'
     LIMIT 1;


    IF v_umbral IS NULL THEN
        RETURN NEW;
    END IF;

    IF TG_OP = 'UPDATE' THEN
        IF NOT (
            OLD.cum_actual >= v_umbral
            AND NEW.cum_actual < v_umbral
        ) THEN
            RETURN NEW;
        END IF;
    END IF;

    -- En INSERT alerta solo si ya entra debajo del umbral.
    IF TG_OP = 'INSERT' AND NEW.cum_actual >= v_umbral THEN
        RETURN NEW;
    END IF;

    SELECT u.id_usuario, u.correo
      INTO v_id_usuario, v_correo
      FROM usuarios u
     WHERE u.id_usuario = NEW.id_usuario;

    INSERT INTO notificaciones (
        id_sesion,
        carnet_estudiante,
        id_usuario_destino,
        correo_destino,
        tipo,
        mensaje,
        fecha_programada,
        estado_envio
    )
    VALUES (
        NULL,
        NEW.carnet,
        v_id_usuario,
        v_correo,
        'AlertaTemprana',
        format(
            'Alerta temprana: el estudiante %s tiene un CUM de %s, inferior al umbral configurado de %s.',
            NEW.carnet,
            NEW.cum_actual,
            v_umbral
        ),
        now(),
        'Pendiente'
    );

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_alerta_cum_riesgo
ON estudiantes;

CREATE TRIGGER trg_alerta_cum_riesgo
AFTER INSERT OR UPDATE OF cum_actual
ON estudiantes
FOR EACH ROW
EXECUTE FUNCTION fn_generar_alerta_cum_riesgo();

-- ============================================================================
-- 5. TRAZABILIDAD AUTOMATICA EN AUDITORIA_EVENTOS
-- RNF-04
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_auditar_cambio()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_actor_text text;
    v_actor bigint;
    v_fila jsonb;
    v_id_registro text;
BEGIN
    -- Obtener el usuario autenticado que el backend colocó
    -- mediante set_config('app.id_usuario', ...).
    v_actor_text := current_setting('app.id_usuario', true);

    IF v_actor_text IS NOT NULL AND btrim(v_actor_text) <> '' THEN
        BEGIN
            v_actor := v_actor_text::bigint;
        EXCEPTION
            WHEN invalid_text_representation THEN
                v_actor := NULL;
        END;
    ELSE
        v_actor := NULL;
    END IF;

    -- Tomar la fila nueva o anterior dependiendo de la operación.
    IF TG_OP = 'DELETE' THEN
        v_fila := to_jsonb(OLD);
    ELSE
        v_fila := to_jsonb(NEW);
    END IF;

    -- Primer campo identificador enviado al trigger.
    IF TG_NARGS >= 1 THEN
        v_id_registro := v_fila ->> TG_ARGV[0];
    END IF;

    -- Para tablas con clave compuesta, agregar segundo identificador.
    IF TG_NARGS >= 2 THEN
        v_id_registro :=
            COALESCE(v_id_registro, '') ||
            ':' ||
            COALESCE(v_fila ->> TG_ARGV[1], '');
    END IF;

    INSERT INTO auditoria_eventos (
        id_usuario_actor,
        entidad,
        id_registro,
        accion,
        descripcion
    )
    VALUES (
        v_actor,
        TG_TABLE_NAME,
        v_id_registro,
        TG_OP,
        format(
            'Operacion %s realizada sobre %s.',
            TG_OP,
            TG_TABLE_NAME
        )
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;

    RETURN NEW;
END;
$$;

-- ESTUDIANTES
DROP TRIGGER IF EXISTS trg_auditar_estudiantes ON estudiantes;

CREATE TRIGGER trg_auditar_estudiantes
AFTER INSERT OR UPDATE OR DELETE
ON estudiantes
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('carnet');


-- INSCRIPCIONES
DROP TRIGGER IF EXISTS trg_auditar_inscripciones ON inscripciones;

CREATE TRIGGER trg_auditar_inscripciones
AFTER INSERT OR UPDATE OR DELETE
ON inscripciones
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_inscripcion');


-- ASIGNACIONES PERA
DROP TRIGGER IF EXISTS trg_auditar_asignaciones_tutoria
ON asignaciones_tutoria;

CREATE TRIGGER trg_auditar_asignaciones_tutoria
AFTER INSERT OR UPDATE OR DELETE
ON asignaciones_tutoria
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_asignacion');


-- DOCENTES ASIGNADOS
DROP TRIGGER IF EXISTS trg_auditar_asignacion_docentes
ON asignacion_docentes;

CREATE TRIGGER trg_auditar_asignacion_docentes
AFTER INSERT OR UPDATE OR DELETE
ON asignacion_docentes
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_asignacion', 'id_docente');


-- SESIONES
DROP TRIGGER IF EXISTS trg_auditar_sesiones_tutoria
ON sesiones_tutoria;

CREATE TRIGGER trg_auditar_sesiones_tutoria
AFTER INSERT OR UPDATE OR DELETE
ON sesiones_tutoria
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_sesion');


-- DOCENTES DE SESION
DROP TRIGGER IF EXISTS trg_auditar_sesion_docentes
ON sesion_docentes;

CREATE TRIGGER trg_auditar_sesion_docentes
AFTER INSERT OR UPDATE OR DELETE
ON sesion_docentes
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_sesion', 'id_docente');


-- PARTICIPANTES
DROP TRIGGER IF EXISTS trg_auditar_participantes_sesion
ON participantes_sesion;

CREATE TRIGGER trg_auditar_participantes_sesion
AFTER INSERT OR UPDATE OR DELETE
ON participantes_sesion
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_participacion');


-- ASISTENCIAS
DROP TRIGGER IF EXISTS trg_auditar_asistencias
ON asistencias;

CREATE TRIGGER trg_auditar_asistencias
AFTER INSERT OR UPDATE OR DELETE
ON asistencias
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_asistencia');


-- BITACORAS DE AVANCE
DROP TRIGGER IF EXISTS trg_auditar_bitacoras_avance
ON bitacoras_avance;

CREATE TRIGGER trg_auditar_bitacoras_avance
AFTER INSERT OR UPDATE OR DELETE
ON bitacoras_avance
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_bitacora');


-- NOTIFICACIONES
DROP TRIGGER IF EXISTS trg_auditar_notificaciones
ON notificaciones;

CREATE TRIGGER trg_auditar_notificaciones
AFTER INSERT OR UPDATE OR DELETE
ON notificaciones
FOR EACH ROW
EXECUTE FUNCTION fn_auditar_cambio('id_notificacion');