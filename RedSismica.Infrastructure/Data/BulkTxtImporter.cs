// En: RedSismica.Infrastructure/Data/BulkTxtImporter.cs
using RedSismica.Core.Entities;
using RedSismica.Core.States;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

// (Ya no necesitamos 'using EFCore.BulkExtensions;')

namespace RedSismica.Infrastructure.Data
{
    public static class BulkTxtImporter
    {
        private static readonly string FormatoFecha = "yyyy-MM-dd HH:mm:ss";
        private static readonly string FormatoFechaMuestra = "yyyy-MM-dd HH:mm:ss.fff";
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        // Mapas para vincular IDs de TXT con entidades RECIÉN CREADAS
        private static Dictionary<string, EventoSismico> eventoMap;
        private static Dictionary<string, SerieTemporal> serieMap;
        private static Dictionary<string, MuestraSismica> muestraMap;

        public static void Run(RedSismicaContext ctx, string importPath)
        {
            eventoMap = new Dictionary<string, EventoSismico>();
            serieMap = new Dictionary<string, SerieTemporal>();
            muestraMap = new Dictionary<string, MuestraSismica>();

            using (var transaction = ctx.Database.BeginTransaction())
            {
                try
                {
                    // 1. Catálogos (Sin dependencias)
                    ImportarAlcances(ctx, Path.Combine(importPath, "alcances.txt"));
                    ImportarClasificaciones(ctx, Path.Combine(importPath, "clasificaciones.txt"));
                    ImportarOrigenes(ctx, Path.Combine(importPath, "origenes.txt"));
                    ImportarTiposDeDato(ctx, Path.Combine(importPath, "tiposDeDato.txt"));
                    ImportarEstaciones(ctx, Path.Combine(importPath, "estaciones.txt"));
                    ImportarUsuarios(ctx, Path.Combine(importPath, "usuarios.txt"));

                    // 2. Entidades con FKs a Catálogos
                    ImportarEmpleados(ctx, Path.Combine(importPath, "empleados.txt"));
                    ImportarSismografos(ctx, Path.Combine(importPath, "sismografos.txt"));

                    // 3. Núcleo del Negocio (con dependencias complejas)
                    var eventos = LeerEventos(ctx, Path.Combine(importPath, "eventos.txt"));
                    var series = LeerSeriesTemporales(ctx, Path.Combine(importPath, "seriesTemporales.txt"));
                    var muestras = LeerMuestrasSismicas(ctx, Path.Combine(importPath, "muestrasSismicas.txt"));
                    var detalles = LeerDetallesMuestra(ctx, Path.Combine(importPath, "detallesMuestra.txt"));

                    // 4. VINCULACIÓN (Respetando tu Dominio Unidireccional)
                    VincularMuestras(muestras, detalles);
                    VincularSeries(series, muestras);
                    VincularEventos(eventos, series);

                    // --- CORRECCIÓN DE LÓGICA ---
                    // Cargamos los sismógrafos que ya están en la BBDD para actualizarlos
                    var sismografosDeLaBBDD = ctx.Sismografos.ToList();
                    VincularSismografos(sismografosDeLaBBDD, series);

                    // 5. Inserción Estándar (Reemplazo de BulkInsert)

                    // Añadimos solo los NUEVOS eventos raíz.
                    // EF descubrirá las series, muestras y detalles anidados.
                    ctx.AddRange(eventos.Select(e => e.Entidad));

                    // NO añadimos las series/muestras/detalles aquí.
                    // ctx.AddRange(series.Select(s => s.Entidad)); // <--- MAL
                    // ctx.AddRange(muestras.Select(m => m.Entidad)); // <--- MAL
                    // ctx.AddRange(detalles); // <--- MAL

                    // Guardamos los cambios. Esto insertará los Eventos Y
                    // actualizará los Sismógrafos (añadiendo sus series hijas).
                    ctx.SaveChanges();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Error en BulkTxtImporter: {ex.InnerException?.Message ?? ex.Message}", ex);
                }
            }
        }

        // --- MÉTODOS DE VINCULACIÓN ---

        private static void VincularMuestras(
            List<ImportWrapper<MuestraSismica>> muestras,
            List<DetalleMuestra> detalles)
        {
            foreach (var detalle in detalles)
            {
                // Buscamos la muestra padre y añadimos el detalle a su lista
                if (muestraMap.TryGetValue(detalle.MuestraSismicaId_temp, out var muestraPadre))
                {
                    // Asignación Unidireccional (Padre -> Hijo)
                    muestraPadre.detalleMuestraSismica.Add(detalle);
                }
            }
        }

        private static void VincularSeries(
            List<ImportWrapper<SerieTemporal>> series,
            List<ImportWrapper<MuestraSismica>> muestras)
        {
            foreach (var muestra in muestras)
            {
                if (serieMap.TryGetValue(muestra.SerieTemporalId_temp, out var seriePadre))
                {
                    // Asignación Unidireccional (Padre -> Hijo)
                    seriePadre.muestrasSismicas.Add(muestra.Entidad);
                }
            }
        }

        private static void VincularEventos(
            List<ImportWrapper<EventoSismico>> eventos,
            List<ImportWrapper<SerieTemporal>> series)
        {
            foreach (var serie in series)
            {
                if (eventoMap.TryGetValue(serie.EventoSismicoId_temp, out var eventoPadre))
                {
                    // Asignación Unidireccional (Padre -> Hijo)
                    eventoPadre.serieTemporal.Add(serie.Entidad);
                }
            }
        }

        private static void VincularSismografos(
            List<Sismografo> sismografos,
            List<ImportWrapper<SerieTemporal>> series)
        {
            var sismografoMap = sismografos.ToDictionary(s => s.IdentificadorSismografo, s => s);
            foreach (var serie in series)
            {
                if (sismografoMap.TryGetValue(serie.SismografoId_temp, out var sismografoPadre))
                {
                    sismografoPadre.seriesTemporales.Add(serie.Entidad);
                }
            }
        }

        // --- MÉTODOS DE LECTURA (Implementación completa) ---

        private static List<ImportWrapper<EventoSismico>> LeerEventos(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return new List<ImportWrapper<EventoSismico>>();

            var alcances = ctx.Alcances.ToList();
            var clasificaciones = ctx.Clasificaciones.ToList();
            var origenes = ctx.Origenes.ToList();
            var empleados = ctx.Empleados.ToList();

            var eventosWrappers = new List<ImportWrapper<EventoSismico>>();

            var lineas = File.ReadAllLines(file).Select(line => line.Split(';'));

            foreach (var p in lineas)
            {
                // Buscamos las FKs
                var responsable = empleados.FirstOrDefault(em => em.Mail == p[11]);

                // 1. Creamos el estado inicial
                var estadoInicial = new Autodetectado { NombreEstado = "Autodetectado" };

                // 2. Creamos el evento
                var evento = new EventoSismico
                {
                    FechaHoraInicio = DateTime.ParseExact(p[1], FormatoFecha, Culture),
                    FechaHoraActualizacion = DateTime.ParseExact(p[2], FormatoFecha, Culture),
                    LatitudEpicentro = double.Parse(p[3], Culture),
                    LongitudEpicentro = double.Parse(p[4], Culture),
                    LatitudHipocentro = double.Parse(p[5], Culture),
                    LongitudHipocentro = double.Parse(p[6], Culture),
                    ValorMagnitud = double.Parse(p[7], Culture),
                    Alcance = alcances.FirstOrDefault(a => a.Nombre == p[8]),
                    Clasificacion = clasificaciones.FirstOrDefault(c => c.Nombre == p[9]),
                    Origen = origenes.FirstOrDefault(o => o.Nombre == p[10]),
                    Responsable = responsable,
                    EstadoActual = estadoInicial
                };

                // --- ESTA ES LA CORRECCIÓN CLAVE (Punto 1) ---
                // 3. Creamos el PRIMER cambio de estado (el que faltaba)
                var cambioInicial = new CambioDeEstado
                {
                    FechaHoraInicio = evento.FechaHoraInicio, // Usamos la fecha de inicio del evento
                    Estado = estadoInicial,
                    Responsable = null // El primer estado es automático (Sistema)
                };

                // 4. Lo añadimos al historial del evento
                evento.cambioEstado.Add(cambioInicial);
                // ------------------------------------------

                // 5. Preparamos el wrapper
                var wrapper = new ImportWrapper<EventoSismico>(p[0], evento);
                eventosWrappers.Add(wrapper);

                // Llenamos el mapa para que otros nos encuentren
                eventoMap[wrapper.TempID] = wrapper.Entidad;
            }

            return eventosWrappers;
        }

        private static List<ImportWrapper<SerieTemporal>> LeerSeriesTemporales(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return new List<ImportWrapper<SerieTemporal>>(); // Retorna lista vacía

            var series = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new ImportWrapper<SerieTemporal>(p[0], // TempID
                    new SerieTemporal
                    {
                        FechaHoraRegistro = DateTime.ParseExact(p[3], FormatoFecha, Culture),
                        CondicionAlarma = int.Parse(p[4]),
                        FrecuenciaMuestro = double.Parse(p[5], Culture),
                        FechaHoraInicioRegistroMuestras = DateTime.ParseExact(p[6], FormatoFecha, Culture)
                    })
                {
                    EventoSismicoId_temp = p[1], // Guardamos FK temporal
                    SismografoId_temp = p[2]  // Guardamos FK temporal
                }
                ).ToList();

            foreach (var s in series) serieMap[s.TempID] = s.Entidad;
            return series;
        }

        private static List<ImportWrapper<MuestraSismica>> LeerMuestrasSismicas(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return new List<ImportWrapper<MuestraSismica>>(); // Retorna lista vacía

            var muestras = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new ImportWrapper<MuestraSismica>(p[0], // TempID
                    new MuestraSismica
                    {
                        FechaHoraMuestra = DateTime.ParseExact(p[2], FormatoFechaMuestra, Culture)
                    })
                {
                    SerieTemporalId_temp = p[1] // Guardamos FK temporal
                }
                ).ToList();

            foreach (var m in muestras) muestraMap[m.TempID] = m.Entidad;
            return muestras;
        }

        private static List<DetalleMuestra> LeerDetallesMuestra(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return new List<DetalleMuestra>(); // Retorna lista vacía

            var tiposDeDato = ctx.TiposDeDato.ToList();

            var detalles = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new DetalleMuestra
                {
                    MuestraSismicaId_temp = p[0], // Guardamos FK temporal
                    TipoDeDato = tiposDeDato.FirstOrDefault(t => t.Denominacion == p[1]),
                    Valor = double.Parse(p[2], Culture)
                }).ToList();

            return detalles;
        }


        // --- MÉTODOS DE CATÁLOGO (Reemplazando BulkInsert) ---

        private static void ImportarAlcances(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;
            var alcances = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new AlcanceSismo { Nombre = p[0], Descripcion = p[1] }).ToList();
            ctx.AddRange(alcances);
            ctx.SaveChanges();
        }

        private static void ImportarClasificaciones(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;
            var clasificaciones = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new ClasificacionSismo
                {
                    Nombre = p[0],
                    KmProfundidadDesde = double.Parse(p[1], Culture),
                    KmProfundidadHasta = double.Parse(p[2], Culture)
                }).ToList();
            ctx.AddRange(clasificaciones);
            ctx.SaveChanges();
        }

        private static void ImportarOrigenes(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;
            var origenes = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new OrigenDeGeneracion
                {
                    Nombre = p[0],
                    Descripcion = p[1]
                }).ToList();
            ctx.AddRange(origenes);
            ctx.SaveChanges();
        }

        private static void ImportarTiposDeDato(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;
            var tipos = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new TipoDeDato
                {
                    Denominacion = p[0],
                    nombreUnidadMedida = p[1],
                    ValorUmbral = double.Parse(p[2], Culture)
                }).ToList();
            ctx.AddRange(tipos);
            ctx.SaveChanges();
        }

        private static void ImportarEstaciones(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;
            var estaciones = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new EstacionSismologica
                {
                    CodigoEstacion = p[0],
                    Nombre = p[1],
                    Latitud = double.Parse(p[2], Culture),
                    Longitud = double.Parse(p[3], Culture),
                    FechaSolicitudCertificacion = DateTime.ParseExact(p[4], FormatoFecha, Culture),
                    DocumentoCertificacionAdq = p[5],
                    NroCertificacionAdquisicion = p[6]
                }).ToList();
            ctx.AddRange(estaciones);
            ctx.SaveChanges();
        }

        private static void ImportarUsuarios(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;
            var usuarios = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new Usuario
                {
                    NombreUsuario = p[0],
                    Contraseña = p[1]
                }).ToList();
            ctx.AddRange(usuarios);
            ctx.SaveChanges();
        }

        private static void ImportarEmpleados(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;

            var usuarios = ctx.Usuarios.ToList();
            var empleadosParaInsertar = new List<Empleado>();
            var lineas = File.ReadAllLines(file).Select(line => line.Split(';'));

            foreach (var p in lineas)
            {
                var usuarioAsociado = usuarios.FirstOrDefault(u => u.NombreUsuario == p[4]);
                if (usuarioAsociado != null)
                {
                    var nuevoEmpleado = new Empleado
                    {
                        Nombre = p[0],
                        Apellido = p[1],
                        Mail = p[2],
                        Telefono = int.Parse(p[3]),
                    };
                    empleadosParaInsertar.Add(nuevoEmpleado);
                    usuarioAsociado.empleado = nuevoEmpleado;
                }
            }
            ctx.AddRange(empleadosParaInsertar);
            ctx.SaveChanges();
        }

        private static void ImportarSismografos(RedSismicaContext ctx, string file)
        {
            if (!File.Exists(file)) return;

            var estaciones = ctx.Estaciones.ToList();
            var sismografos = File.ReadAllLines(file)
                .Select(line => line.Split(';'))
                .Select(p => new Sismografo
                {
                    IdentificadorSismografo = p[0],
                    NroSerie = p[1],
                    FechaAdquisicion = DateTime.ParseExact(p[2], FormatoFecha, Culture),
                    estacionSismologica = estaciones.FirstOrDefault(e => e.CodigoEstacion == p[3])
                }).ToList();
            ctx.AddRange(sismografos);
            ctx.SaveChanges();
        }
    }

    // --- CLASES HELPER (Definición completa) ---
    internal class ImportWrapper<T>
    {
        public string TempID { get; set; }
        public T Entidad { get; set; }

        // FKs temporales (solo se usan durante la importación)
        public string? EventoSismicoId_temp { get; set; }
        public string? SismografoId_temp { get; set; }
        public string? SerieTemporalId_temp { get; set; }

        public ImportWrapper(string tempID, T entidad)
        {
            TempID = tempID;
            Entidad = entidad;
        }
    }
}