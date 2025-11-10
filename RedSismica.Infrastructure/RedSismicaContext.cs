// En: RedSismica.Infrastructure/RedSismicaContext.cs
using Microsoft.EntityFrameworkCore;
using RedSismica.Core.Entities;
using RedSismica.Core.States;
using System;
using System.Linq;

namespace RedSismica.Infrastructure
{
    public class RedSismicaContext : DbContext
    {
        // --- 1. CONSTRUCTOR ---
        public RedSismicaContext(DbContextOptions<RedSismicaContext> options)
            : base(options)
        {
        }

        // --- 2. Mapeo de Clases a Tablas ---
        public DbSet<AlcanceSismo> Alcances { get; set; }
        public DbSet<ClasificacionSismo> Clasificaciones { get; set; }
        public DbSet<EstacionSismologica> Estaciones { get; set; }
        public DbSet<OrigenDeGeneracion> Origenes { get; set; }
        public DbSet<TipoDeDato> TiposDeDato { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Sismografo> Sismografos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<EventoSismico> EventosSismicos { get; set; }
        public DbSet<CambioDeEstado> CambiosDeEstado { get; set; }
        public DbSet<SerieTemporal> SeriesTemporales { get; set; }
        public DbSet<MuestraSismica> MuestrasSismicas { get; set; }
        public DbSet<DetalleMuestra> DetallesMuestra { get; set; }


        // --- 3. Mapeo Fino (Dominio <-> BBDD) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AlcanceSismo>(e =>
            {
                e.ToTable("Alcances");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
            });

            modelBuilder.Entity<ClasificacionSismo>(e =>
            {
                e.ToTable("Clasificaciones");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
            });

            modelBuilder.Entity<OrigenDeGeneracion>(e =>
            {
                e.ToTable("Origenes");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
            });

            modelBuilder.Entity<TipoDeDato>(e =>
            {
                e.ToTable("TiposDeDato");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property(p => p.nombreUnidadMedida).HasColumnName("NombreUnidadMedida");
            });

            modelBuilder.Entity<Usuario>(e =>
            {
                e.ToTable("Usuarios");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.HasOne(p => p.empleado)
                 .WithOne()
                 .HasForeignKey<Empleado>("UsuarioId");
            });

            modelBuilder.Entity<Empleado>(e =>
            {
                e.ToTable("Empleados");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("UsuarioId").IsRequired(false);
            });

            modelBuilder.Entity<EstacionSismologica>(e =>
            {
                e.ToTable("Estaciones");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
            });

            modelBuilder.Entity<Sismografo>(e =>
            {
                e.ToTable("Sismografos");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("EstacionId").IsRequired(false);

                e.HasOne(p => p.estacionSismologica)
                 .WithMany()
                 .HasForeignKey("EstacionId");

                // --- CORRECCIÓN ---
                // Definimos la relación desde el PADRE (Sismografo)
                e.Navigation(p => p.seriesTemporales).AutoInclude(false);

                e.HasMany(p => p.seriesTemporales)
                 .WithOne() // SerieTemporal no tiene propiedad Sismografo
                 .HasForeignKey("SismografoId")
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired(false);
            });

            modelBuilder.Entity<EventoSismico>(e =>
            {
                e.ToTable("EventosSismicos");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("AlcanceId").IsRequired(false);
                e.Property<string>("ClasificacionId").IsRequired(false);
                e.Property<string>("OrigenId").IsRequired(false);
                e.Property<string>("ResponsableId").IsRequired(false);

                e.HasOne(p => p.Alcance).WithMany().HasForeignKey("AlcanceId");
                e.HasOne(p => p.Clasificacion).WithMany().HasForeignKey("ClasificacionId");
                e.HasOne(p => p.Origen).WithMany().HasForeignKey("OrigenId");
                e.HasOne(p => p.Responsable).WithMany().HasForeignKey("ResponsableId");

                e.Property(p => p.EstadoActual)
                    .HasColumnName("EstadoActualNombre")
                    .HasConversion(v => v.NombreEstado, v => ConvertirStringAEstado(v));

                // --- CORRECCIÓN ---
                // Definimos la relación desde el PADRE (EventoSismico)
                e.HasMany(p => p.cambioEstado)
                 .WithOne()
                 .HasForeignKey("EventoSismicoId")
                 .IsRequired(false);

                e.HasMany(p => p.serieTemporal)
                 .WithOne()
                 .HasForeignKey("EventoSismicoId")
                 .IsRequired(false);
            });

            modelBuilder.Entity<CambioDeEstado>(e =>
            {
                e.ToTable("CambiosDeEstado");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("ResponsableId").IsRequired(false);
                e.Property<string>("EventoSismicoId").IsRequired(false); // FK definida en el padre

                e.Property(p => p.Estado)
                    .HasColumnName("EstadoNombre")
                    .HasConversion(v => v.NombreEstado, v => ConvertirStringAEstado(v));

                e.Ignore(p => p.MotivoRechazoServicio);

                e.HasOne<Empleado>().WithMany().HasForeignKey("ResponsableId");
            });

            modelBuilder.Entity<SerieTemporal>(e =>
            {
                e.ToTable("SeriesTemporales");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");

                // Definimos las FKs pero NO las relaciones
                // (ya se definieron en los padres)
                e.Property<string>("SismografoId").IsRequired(false);
                e.Property<string>("EventoSismicoId").IsRequired(false);

                // --- CORRECCIÓN ---
                // Definimos la relación desde el PADRE (SerieTemporal)
                e.HasMany(p => p.muestrasSismicas)
                 .WithOne()
                 .HasForeignKey("SerieTemporalId")
                 .IsRequired(false);

                e.Property(p => p.FrecuenciaMuestro).HasColumnName("FrecuenciaMuestreo");
                e.Ignore(p => p.FechaHoraInicioRegistroMuestras);
            });

            modelBuilder.Entity<MuestraSismica>(e =>
            {
                e.ToTable("MuestrasSismicas");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("SerieTemporalId").IsRequired(false);

                // --- CORRECCIÓN ---
                // Definimos la relación desde el PADRE (MuestraSismica)
                e.HasMany(p => p.detalleMuestraSismica)
                 .WithOne()
                 .HasForeignKey("MuestraSismicaId")
                 .IsRequired(false);
            });

            modelBuilder.Entity<DetalleMuestra>(e =>
            {
                e.ToTable("DetallesMuestra");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("MuestraSismicaId").IsRequired(false);
                e.Property<string>("TipoDeDatoId").IsRequired(false);

                e.HasOne(p => p.TipoDeDato).WithMany().HasForeignKey("TipoDeDatoId");

                e.Ignore(p => p.MuestraSismicaId_temp);
            });

            modelBuilder.Entity<Sismografo>(e =>
            {
                e.ToTable("Sismografos");
                e.Property<string>("Id").ValueGeneratedOnAdd();
                e.HasKey("Id");
                e.Property<string>("EstacionId").IsRequired(false);

                e.HasOne(p => p.estacionSismologica)
                 .WithMany()
                 .HasForeignKey("EstacionId");

                // --- CORRECCIÓN DE MAPEO ---
                // Ahora la relación es bidireccional, así que la
                // definimos con WithOne(s => s.Sismografo)
                e.HasMany(p => p.seriesTemporales)
                 .WithOne(s => s.Sismografo) // <-- El nuevo mapeo
                 .HasForeignKey("SismografoId")
                 .IsRequired(false);
            });
        }

        // --- 4. Método Ayudante para el Patrón State ---
        private Estado ConvertirStringAEstado(string nombreEstado)
        {
            switch (nombreEstado)
            {
                case "Autodetectado": return new Autodetectado { NombreEstado = "Autodetectado" };
                case "Bloqueado": return new Bloqueado { NombreEstado = "Bloqueado" };
                case "Rechazado": return new Rechazado { NombreEstado = "Rechazado" };
                case "Evento sin revisión": return new EventoSinRevision { NombreEstado = "Evento sin revisión" };
                case "Autoconfirmado": return new Autoconfirmado { NombreEstado = "Autoconfirmado" };
                case "Pendiente de cierre": return new PendienteDeCierre { NombreEstado = "Pendiente de cierre" };
                case "Pendiente de revisión": return new PendienteDeRevision { NombreEstado = "Pendiente de revisión" };
                case "Derivado": return new Derivado { NombreEstado = "Derivado" };
                case "Confirmado": return new Confirmado { NombreEstado = "Confirmado" };
                case "Cerrado": return new Cerrado { NombreEstado = "Cerrado" };

                default:
                    throw new NotSupportedException($"El estado '{nombreEstado}' no es reconocido.");
            }
        }
    }
}