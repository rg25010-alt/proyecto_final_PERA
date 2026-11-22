using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pera.Backend.Modelos;

namespace Pera.Backend.Datos;

public partial class PeraDbContext : DbContext
{
    public PeraDbContext(DbContextOptions<PeraDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AsignacionDocente> AsignacionDocentes { get; set; }

    public virtual DbSet<AsignacionesTutorium> AsignacionesTutoria { get; set; }

    public virtual DbSet<Asignatura> Asignaturas { get; set; }

    public virtual DbSet<Asistencia> Asistencias { get; set; }

    public virtual DbSet<AuditoriaEvento> AuditoriaEventos { get; set; }

    public virtual DbSet<BitacorasAvance> BitacorasAvances { get; set; }

    public virtual DbSet<Carrera> Carreras { get; set; }

    public virtual DbSet<Departamento> Departamentos { get; set; }

    public virtual DbSet<Docente> Docentes { get; set; }

    public virtual DbSet<Estudiante> Estudiantes { get; set; }

    public virtual DbSet<HabilitacionesEgreso> HabilitacionesEgresos { get; set; }

    public virtual DbSet<Inscripcione> Inscripciones { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<ParametrosPera> ParametrosPeras { get; set; }

    public virtual DbSet<ParticipantesSesion> ParticipantesSesions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<ReglasUv> ReglasUvs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SesionDocente> SesionDocentes { get; set; }

    public virtual DbSet<SesionesTutorium> SesionesTutoria { get; set; }

    public virtual DbSet<SolicitudesReprogramacion> SolicitudesReprogramacions { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AsignacionDocente>(entity =>
        {
            entity.HasKey(e => new { e.IdAsignacion, e.IdDocente }).HasName("asignacion_docentes_pkey");

            entity.ToTable("asignacion_docentes");

            entity.HasIndex(e => e.IdDocente, "ix_asignacion_docentes_docente");

            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.IdDocente).HasColumnName("id_docente");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.AreaRefuerzo)
                .HasMaxLength(160)
                .HasColumnName("area_refuerzo");
            entity.Property(e => e.FechaAsignacion)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("fecha_asignacion");

            entity.HasOne(d => d.IdAsignacionNavigation).WithMany(p => p.AsignacionDocentes)
                .HasForeignKey(d => d.IdAsignacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("asignacion_docentes_id_asignacion_fkey");

            entity.HasOne(d => d.IdDocenteNavigation).WithMany(p => p.AsignacionDocentes)
                .HasForeignKey(d => d.IdDocente)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("asignacion_docentes_id_docente_fkey");
        });

        modelBuilder.Entity<AsignacionesTutorium>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion).HasName("asignaciones_tutoria_pkey");

            entity.ToTable("asignaciones_tutoria");

            entity.HasIndex(e => e.IdUsuarioAsignador, "ix_asignaciones_asignador");

            entity.HasIndex(e => e.CarnetEstudiante, "ix_asignaciones_estudiante");

            entity.HasIndex(e => new { e.CarnetEstudiante, e.PeriodoAcademico, e.NumeroIntento }, "uq_asignacion_intento").IsUnique();

            entity.HasIndex(e => new { e.CarnetEstudiante, e.PeriodoAcademico }, "ux_asignacion_un_proceso_en_curso")
                .IsUnique()
                .HasFilter("((estado_pera)::text = 'En Curso'::text)");

            entity.Property(e => e.IdAsignacion)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_asignacion");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.CarnetEstudiante)
                .HasMaxLength(20)
                .HasColumnName("carnet_estudiante");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.EstadoPera)
                .HasMaxLength(12)
                .HasDefaultValueSql("'En Curso'::character varying")
                .HasColumnName("estado_pera");
            entity.Property(e => e.FechaReferencia)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("fecha_referencia");
            entity.Property(e => e.IdUsuarioAsignador).HasColumnName("id_usuario_asignador");
            entity.Property(e => e.NotaFinal)
                .HasPrecision(4, 2)
                .HasColumnName("nota_final");
            entity.Property(e => e.NumeroIntento).HasColumnName("numero_intento");
            entity.Property(e => e.PeriodoAcademico)
                .HasMaxLength(20)
                .HasColumnName("periodo_academico");

            entity.HasOne(d => d.CarnetEstudianteNavigation).WithMany(p => p.AsignacionesTutoria)
                .HasForeignKey(d => d.CarnetEstudiante)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("asignaciones_tutoria_carnet_estudiante_fkey");

            entity.HasOne(d => d.IdUsuarioAsignadorNavigation).WithMany(p => p.AsignacionesTutoria)
                .HasForeignKey(d => d.IdUsuarioAsignador)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("asignaciones_tutoria_id_usuario_asignador_fkey");
        });

        modelBuilder.Entity<Asignatura>(entity =>
        {
            entity.HasKey(e => e.IdAsignatura).HasName("asignaturas_pkey");

            entity.ToTable("asignaturas");

            entity.HasIndex(e => e.Codigo, "asignaturas_codigo_key").IsUnique();

            entity.Property(e => e.IdAsignatura)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_asignatura");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasColumnName("codigo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
            entity.Property(e => e.UnidadesValorativas).HasColumnName("unidades_valorativas");
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.HasKey(e => e.IdAsistencia).HasName("asistencias_pkey");

            entity.ToTable("asistencias");

            entity.HasIndex(e => e.IdParticipacion, "asistencias_id_participacion_key").IsUnique();

            entity.HasIndex(e => e.IdDocenteRegistra, "ix_asistencias_docente");

            entity.Property(e => e.IdAsistencia)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_asistencia");
            entity.Property(e => e.Asistio).HasColumnName("asistio");
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.IdDocenteRegistra).HasColumnName("id_docente_registra");
            entity.Property(e => e.IdParticipacion).HasColumnName("id_participacion");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");

            entity.HasOne(d => d.IdDocenteRegistraNavigation).WithMany(p => p.Asistencia)
                .HasForeignKey(d => d.IdDocenteRegistra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("asistencias_id_docente_registra_fkey");

            entity.HasOne(d => d.IdParticipacionNavigation).WithOne(p => p.Asistencia)
                .HasForeignKey<Asistencia>(d => d.IdParticipacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("asistencias_id_participacion_fkey");
        });

        modelBuilder.Entity<AuditoriaEvento>(entity =>
        {
            entity.HasKey(e => e.IdEvento).HasName("auditoria_eventos_pkey");

            entity.ToTable("auditoria_eventos");

            entity.HasIndex(e => e.IdUsuarioActor, "ix_auditoria_eventos_actor");

            entity.HasIndex(e => e.OcurridoEn, "ix_auditoria_eventos_fecha");

            entity.Property(e => e.IdEvento)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_evento");
            entity.Property(e => e.Accion)
                .HasMaxLength(15)
                .HasColumnName("accion");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Entidad)
                .HasMaxLength(80)
                .HasColumnName("entidad");
            entity.Property(e => e.IdRegistro)
                .HasMaxLength(100)
                .HasColumnName("id_registro");
            entity.Property(e => e.IdUsuarioActor).HasColumnName("id_usuario_actor");
            entity.Property(e => e.OcurridoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("ocurrido_en");

            entity.HasOne(d => d.IdUsuarioActorNavigation).WithMany(p => p.AuditoriaEventos)
                .HasForeignKey(d => d.IdUsuarioActor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("auditoria_eventos_id_usuario_actor_fkey");
        });

        modelBuilder.Entity<BitacorasAvance>(entity =>
        {
            entity.HasKey(e => e.IdBitacora).HasName("bitacoras_avance_pkey");

            entity.ToTable("bitacoras_avance");

            entity.HasIndex(e => e.IdDocenteAutor, "ix_bitacoras_autor");

            entity.HasIndex(e => new { e.IdParticipacion, e.FechaRegistro }, "ix_bitacoras_participacion_fecha");

            entity.Property(e => e.IdBitacora)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_bitacora");
            entity.Property(e => e.Acuerdos).HasColumnName("acuerdos");
            entity.Property(e => e.Calificacion)
                .HasPrecision(4, 2)
                .HasColumnName("calificacion");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.IdDocenteAutor).HasColumnName("id_docente_autor");
            entity.Property(e => e.IdParticipacion).HasColumnName("id_participacion");

            entity.HasOne(d => d.IdDocenteAutorNavigation).WithMany(p => p.BitacorasAvances)
                .HasForeignKey(d => d.IdDocenteAutor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("bitacoras_avance_id_docente_autor_fkey");

            entity.HasOne(d => d.IdParticipacionNavigation).WithMany(p => p.BitacorasAvances)
                .HasForeignKey(d => d.IdParticipacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("bitacoras_avance_id_participacion_fkey");
        });

        modelBuilder.Entity<Carrera>(entity =>
        {
            entity.HasKey(e => e.IdCarrera).HasName("carreras_pkey");

            entity.ToTable("carreras");

            entity.HasIndex(e => e.NombreCarrera, "carreras_nombre_carrera_key").IsUnique();

            entity.Property(e => e.IdCarrera)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_carrera");
            entity.Property(e => e.IdDepartamento).HasColumnName("id_departamento");
            entity.Property(e => e.NombreCarrera)
                .HasMaxLength(120)
                .HasColumnName("nombre_carrera");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Carreras)
                .HasForeignKey(d => d.IdDepartamento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("carreras_id_departamento_fkey");
        });

        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(e => e.IdDepartamento).HasName("departamentos_pkey");

            entity.ToTable("departamentos");

            entity.HasIndex(e => e.Nombre, "departamentos_nombre_key").IsUnique();

            entity.Property(e => e.IdDepartamento)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_departamento");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Docente>(entity =>
        {
            entity.HasKey(e => e.IdDocente).HasName("docentes_pkey");

            entity.ToTable("docentes");

            entity.HasIndex(e => e.CodigoDocente, "docentes_codigo_docente_key").IsUnique();

            entity.HasIndex(e => e.IdUsuario, "docentes_id_usuario_key").IsUnique();

            entity.HasIndex(e => e.IdDepartamento, "ix_docentes_id_departamento");

            entity.Property(e => e.IdDocente)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_docente");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CodigoDocente)
                .HasMaxLength(25)
                .HasColumnName("codigo_docente");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.IdDepartamento).HasColumnName("id_departamento");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Docentes)
                .HasForeignKey(d => d.IdDepartamento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("docentes_id_departamento_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Docente)
                .HasForeignKey<Docente>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("docentes_id_usuario_fkey");
        });

        modelBuilder.Entity<Estudiante>(entity =>
        {
            entity.HasKey(e => e.Carnet).HasName("estudiantes_pkey");

            entity.ToTable("estudiantes");

            entity.HasIndex(e => e.IdUsuario, "estudiantes_id_usuario_key").IsUnique();

            entity.HasIndex(e => e.IdCarrera, "ix_estudiantes_id_carrera");

            entity.HasIndex(e => e.IdUsuarioRegistra, "ix_estudiantes_usuario_registra");

            entity.Property(e => e.Carnet)
                .HasMaxLength(20)
                .HasColumnName("carnet");
            entity.Property(e => e.CorreoContacto)
                .HasMaxLength(160)
                .HasColumnName("correo_contacto");
            entity.Property(e => e.CumActual)
                .HasPrecision(4, 2)
                .HasColumnName("cum_actual");
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.IdUsuarioRegistra).HasColumnName("id_usuario_registra");
            entity.Property(e => e.IntentosFallidos).HasColumnName("intentos_fallidos");
            entity.Property(e => e.RegistradoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("registrado_en");

            entity.HasOne(d => d.IdCarreraNavigation).WithMany(p => p.Estudiantes)
                .HasForeignKey(d => d.IdCarrera)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("estudiantes_id_carrera_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.EstudianteIdUsuarioNavigation)
                .HasForeignKey<Estudiante>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("estudiantes_id_usuario_fkey");

            entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.EstudianteIdUsuarioRegistraNavigations)
                .HasForeignKey(d => d.IdUsuarioRegistra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("estudiantes_id_usuario_registra_fkey");
        });

        modelBuilder.Entity<HabilitacionesEgreso>(entity =>
        {
            entity.HasKey(e => e.IdHabilitacion).HasName("habilitaciones_egreso_pkey");

            entity.ToTable("habilitaciones_egreso");

            entity.HasIndex(e => e.IdAsignacion, "habilitaciones_egreso_id_asignacion_key").IsUnique();

            entity.HasIndex(e => e.IdUsuarioJefatura, "ix_habilitaciones_jefatura");

            entity.Property(e => e.IdHabilitacion)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_habilitacion");
            entity.Property(e => e.Habilitado).HasColumnName("habilitado");
            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.IdUsuarioJefatura).HasColumnName("id_usuario_jefatura");
            entity.Property(e => e.Observacion).HasColumnName("observacion");
            entity.Property(e => e.RegistradoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("registrado_en");

            entity.HasOne(d => d.IdAsignacionNavigation).WithOne(p => p.HabilitacionesEgreso)
                .HasForeignKey<HabilitacionesEgreso>(d => d.IdAsignacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("habilitaciones_egreso_id_asignacion_fkey");

            entity.HasOne(d => d.IdUsuarioJefaturaNavigation).WithMany(p => p.HabilitacionesEgresos)
                .HasForeignKey(d => d.IdUsuarioJefatura)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("habilitaciones_egreso_id_usuario_jefatura_fkey");
        });

        modelBuilder.Entity<Inscripcione>(entity =>
        {
            entity.HasKey(e => e.IdInscripcion).HasName("inscripciones_pkey");

            entity.ToTable("inscripciones");

            entity.HasIndex(e => e.IdAsignatura, "ix_inscripciones_asignatura");

            entity.HasIndex(e => new { e.CarnetEstudiante, e.PeriodoAcademico }, "ix_inscripciones_carnet_periodo");

            entity.HasIndex(e => new { e.CarnetEstudiante, e.IdAsignatura, e.PeriodoAcademico }, "uq_inscripcion_unica").IsUnique();

            entity.Property(e => e.IdInscripcion)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_inscripcion");
            entity.Property(e => e.CarnetEstudiante)
                .HasMaxLength(20)
                .HasColumnName("carnet_estudiante");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .HasDefaultValueSql("'Activa'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaInscripcion)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("fecha_inscripcion");
            entity.Property(e => e.IdAsignatura).HasColumnName("id_asignatura");
            entity.Property(e => e.PeriodoAcademico)
                .HasMaxLength(20)
                .HasColumnName("periodo_academico");

            entity.HasOne(d => d.CarnetEstudianteNavigation).WithMany(p => p.Inscripciones)
                .HasForeignKey(d => d.CarnetEstudiante)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("inscripciones_carnet_estudiante_fkey");

            entity.HasOne(d => d.IdAsignaturaNavigation).WithMany(p => p.Inscripciones)
                .HasForeignKey(d => d.IdAsignatura)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("inscripciones_id_asignatura_fkey");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion).HasName("notificaciones_pkey");

            entity.ToTable("notificaciones");

            entity.HasIndex(e => new { e.IdUsuarioDestino, e.EstadoEnvio }, "ix_notificaciones_destino_estado");

            entity.HasIndex(e => e.CarnetEstudiante, "ix_notificaciones_estudiante");

            entity.HasIndex(e => e.IdSesion, "ix_notificaciones_sesion");

            entity.Property(e => e.IdNotificacion)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_notificacion");
            entity.Property(e => e.CarnetEstudiante)
                .HasMaxLength(20)
                .HasColumnName("carnet_estudiante");
            entity.Property(e => e.CorreoDestino)
                .HasMaxLength(160)
                .HasColumnName("correo_destino");
            entity.Property(e => e.EstadoEnvio)
                .HasMaxLength(12)
                .HasDefaultValueSql("'Pendiente'::character varying")
                .HasColumnName("estado_envio");
            entity.Property(e => e.FechaEnvio).HasColumnName("fecha_envio");
            entity.Property(e => e.FechaProgramada)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_programada");
            entity.Property(e => e.IdSesion).HasColumnName("id_sesion");
            entity.Property(e => e.IdUsuarioDestino).HasColumnName("id_usuario_destino");
            entity.Property(e => e.Mensaje).HasColumnName("mensaje");
            entity.Property(e => e.Tipo)
                .HasMaxLength(25)
                .HasColumnName("tipo");

            entity.HasOne(d => d.CarnetEstudianteNavigation).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.CarnetEstudiante)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notificaciones_carnet_estudiante_fkey");

            entity.HasOne(d => d.IdSesionNavigation).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.IdSesion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notificaciones_id_sesion_fkey");

            entity.HasOne(d => d.IdUsuarioDestinoNavigation).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.IdUsuarioDestino)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notificaciones_id_usuario_destino_fkey");
        });

        modelBuilder.Entity<ParametrosPera>(entity =>
        {
            entity.HasKey(e => e.IdParametro).HasName("parametros_pera_pkey");

            entity.ToTable("parametros_pera");

            entity.HasIndex(e => e.Codigo, "parametros_pera_codigo_key").IsUnique();

            entity.Property(e => e.IdParametro)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_parametro");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Codigo)
                .HasMaxLength(40)
                .HasColumnName("codigo");
            entity.Property(e => e.IdUsuarioActualiza).HasColumnName("id_usuario_actualiza");
            entity.Property(e => e.ValorNumerico)
                .HasPrecision(4, 2)
                .HasColumnName("valor_numerico");

            entity.HasOne(d => d.IdUsuarioActualizaNavigation).WithMany(p => p.ParametrosPeras)
                .HasForeignKey(d => d.IdUsuarioActualiza)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("parametros_pera_id_usuario_actualiza_fkey");
        });

        modelBuilder.Entity<ParticipantesSesion>(entity =>
        {
            entity.HasKey(e => e.IdParticipacion).HasName("participantes_sesion_pkey");

            entity.ToTable("participantes_sesion");

            entity.HasIndex(e => e.IdAsignacion, "ix_participantes_asignacion");

            entity.HasIndex(e => new { e.IdSesion, e.IdAsignacion }, "uq_participante_unico").IsUnique();

            entity.Property(e => e.IdParticipacion)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_participacion");
            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.IdSesion).HasColumnName("id_sesion");

            entity.HasOne(d => d.IdAsignacionNavigation).WithMany(p => p.ParticipantesSesions)
                .HasForeignKey(d => d.IdAsignacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("participantes_sesion_id_asignacion_fkey");

            entity.HasOne(d => d.IdSesionNavigation).WithMany(p => p.ParticipantesSesions)
                .HasForeignKey(d => d.IdSesion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("participantes_sesion_id_sesion_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.IdRefreshToken).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => new { e.IdUsuario, e.ExpiraEn }, "ix_refresh_tokens_activos").HasFilter("(revocado_en IS NULL)");

            entity.HasIndex(e => e.IdUsuario, "ix_refresh_tokens_usuario");

            entity.HasIndex(e => e.TokenHash, "refresh_tokens_token_hash_key").IsUnique();

            entity.Property(e => e.IdRefreshToken)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_refresh_token");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.ExpiraEn).HasColumnName("expira_en");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.ReemplazadoPor).HasColumnName("reemplazado_por");
            entity.Property(e => e.RevocadoEn).HasColumnName("revocado_en");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasColumnName("token_hash");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("refresh_tokens_id_usuario_fkey");

            entity.HasOne(d => d.ReemplazadoPorNavigation).WithMany(p => p.InverseReemplazadoPorNavigation)
                .HasForeignKey(d => d.ReemplazadoPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("refresh_tokens_reemplazado_por_fkey");
        });

        modelBuilder.Entity<ReglasUv>(entity =>
        {
            entity.HasKey(e => e.IdRegla).HasName("reglas_uv_pkey");

            entity.ToTable("reglas_uv");

            entity.Property(e => e.IdRegla)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_regla");
            entity.Property(e => e.CumMax)
                .HasPrecision(4, 2)
                .HasColumnName("cum_max");
            entity.Property(e => e.CumMin)
                .HasPrecision(4, 2)
                .HasColumnName("cum_min");
            entity.Property(e => e.UvMax).HasColumnName("uv_max");
            entity.Property(e => e.Vigente)
                .HasDefaultValue(true)
                .HasColumnName("vigente");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Codigo, "roles_codigo_key").IsUnique();

            entity.Property(e => e.IdRol)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_rol");
            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<SesionDocente>(entity =>
        {
            entity.HasKey(e => new { e.IdSesion, e.IdDocente }).HasName("sesion_docentes_pkey");

            entity.ToTable("sesion_docentes");

            entity.HasIndex(e => e.IdDocente, "ix_sesion_docentes_docente");

            entity.HasIndex(e => e.IdSesion, "ux_sesion_un_responsable")
                .IsUnique()
                .HasFilter("es_responsable");

            entity.Property(e => e.IdSesion).HasColumnName("id_sesion");
            entity.Property(e => e.IdDocente).HasColumnName("id_docente");
            entity.Property(e => e.EsResponsable).HasColumnName("es_responsable");

            entity.HasOne(d => d.IdDocenteNavigation).WithMany(p => p.SesionDocentes)
                .HasForeignKey(d => d.IdDocente)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sesion_docentes_id_docente_fkey");

            entity.HasOne(d => d.IdSesionNavigation).WithOne(p => p.SesionDocente)
                .HasForeignKey<SesionDocente>(d => d.IdSesion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sesion_docentes_id_sesion_fkey");
        });

        modelBuilder.Entity<SesionesTutorium>(entity =>
        {
            entity.HasKey(e => e.IdSesion).HasName("sesiones_tutoria_pkey");

            entity.ToTable("sesiones_tutoria");

            entity.HasIndex(e => e.IdDocenteCreador, "ix_sesiones_creador");

            entity.HasIndex(e => new { e.FechaInicio, e.FechaFin }, "ix_sesiones_fechas");

            entity.Property(e => e.IdSesion)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_sesion");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.EstadoSesion)
                .HasMaxLength(15)
                .HasDefaultValueSql("'Borrador'::character varying")
                .HasColumnName("estado_sesion");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.IdDocenteCreador).HasColumnName("id_docente_creador");
            entity.Property(e => e.Modalidad)
                .HasMaxLength(12)
                .HasColumnName("modalidad");
            entity.Property(e => e.UbicacionEnlace)
                .HasMaxLength(255)
                .HasColumnName("ubicacion_enlace");

            entity.HasOne(d => d.IdDocenteCreadorNavigation).WithMany(p => p.SesionesTutoria)
                .HasForeignKey(d => d.IdDocenteCreador)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sesiones_tutoria_id_docente_creador_fkey");
        });

        modelBuilder.Entity<SolicitudesReprogramacion>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud).HasName("solicitudes_reprogramacion_pkey");

            entity.ToTable("solicitudes_reprogramacion");

            entity.HasIndex(e => new { e.Estado, e.SolicitadoEn }, "ix_solicitud_estado");

            entity.HasIndex(e => e.IdParticipacion, "ux_solicitud_pendiente")
                .IsUnique()
                .HasFilter("((estado)::text = 'Pendiente'::text)");

            entity.Property(e => e.IdSolicitud)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_solicitud");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .HasDefaultValueSql("'Pendiente'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaPropuestaFin).HasColumnName("fecha_propuesta_fin");
            entity.Property(e => e.FechaPropuestaInicio).HasColumnName("fecha_propuesta_inicio");
            entity.Property(e => e.IdParticipacion).HasColumnName("id_participacion");
            entity.Property(e => e.IdUsuarioResuelve).HasColumnName("id_usuario_resuelve");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.Respuesta).HasColumnName("respuesta");
            entity.Property(e => e.ResueltoEn).HasColumnName("resuelto_en");
            entity.Property(e => e.SolicitadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("solicitado_en");

            entity.HasOne(d => d.IdParticipacionNavigation).WithOne(p => p.SolicitudesReprogramacion)
                .HasForeignKey<SolicitudesReprogramacion>(d => d.IdParticipacion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("solicitudes_reprogramacion_id_participacion_fkey");

            entity.HasOne(d => d.IdUsuarioResuelveNavigation).WithMany(p => p.SolicitudesReprogramacions)
                .HasForeignKey(d => d.IdUsuarioResuelve)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("solicitudes_reprogramacion_id_usuario_resuelve_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("usuarios_pkey");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.IdDepartamento, "ix_usuarios_id_departamento");

            entity.HasIndex(e => e.IdRol, "ix_usuarios_id_rol");

            entity.Property(e => e.IdUsuario)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id_usuario");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(80)
                .HasColumnName("apellidos");
            entity.Property(e => e.ClaveHash)
                .HasMaxLength(255)
                .HasColumnName("clave_hash");
            entity.Property(e => e.Correo)
                .HasMaxLength(160)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.IdDepartamento).HasColumnName("id_departamento");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Nombres)
                .HasMaxLength(80)
                .HasColumnName("nombres");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdDepartamento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("usuarios_id_departamento_fkey");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("usuarios_id_rol_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
