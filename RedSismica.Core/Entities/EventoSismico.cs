// En: RedSismica.Core/Entities/EventoSismico.cs
using RedSismica.Core.States;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedSismica.Core.Entities
{
    public class EventoSismico
    {
        // ---------------------------------------------------
        // Atributos de Datos (de tu lista de clases)
        // ---------------------------------------------------

        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraActualizacion { get; set; }

        // Usaré 'double' para 'Float'/'REAL'
        public double LatitudEpicentro { get; set; }
        public double LongitudEpicentro { get; set; }
        public double LatitudHipocentro { get; set; }
        public double LongitudHipocentro { get; set; }
        public double ValorMagnitud { get; set; }

        // ---------------------------------------------------
        // Atributos de Relación y Estado 
        // (Necesarios para el Patrón State y el flujo)
        // ---------------------------------------------------

        // -- Relaciones (para el flujo 'getNombreAlcance', etc. y BBDD) --

        public AlcanceSismo? Alcance { get; set; }
        public ClasificacionSismo? Clasificacion { get; set; }
        public OrigenDeGeneracion? Origen { get; set; }
        public Empleado? Responsable { get; set; }

        // -- Patrón State --

        // El estado actual en el que se encuentra el evento
        public Estado? EstadoActual { get; set; }

        // El historial de cambios de estado (para el método AgregarCambioEstado)
        public List<CambioDeEstado> cambioEstado { get; set; } = new List<CambioDeEstado>();

        public List<SerieTemporal> serieTemporal { get; set; } = new List<SerieTemporal>();


        // Getters individuales (estilo C# con '=>')
        public DateTime GetFechaHora() => this.FechaHoraInicio;
        public double GetLatitudEpicentro() => this.LatitudEpicentro;
        public double GetLongitudEpicentro() => this.LongitudEpicentro;
        public double GetLatitudHipocentro() => this.LatitudHipocentro;
        public double GetLongitudHipocentro() => this.LongitudHipocentro;
        public double GetMagnitud() => this.ValorMagnitud;

        public object GetDatosOcurrencia()
        {
            // Devolvemos un objeto anónimo con todos los campos
            // que especificaste en tu flujo.
            return new
            {
                FechaHora = this.GetFechaHora(),
                Magnitud = this.GetMagnitud(),
                LatitudEpicentro = this.GetLatitudEpicentro(),
                LongitudEpicentro = this.GetLongitudEpicentro(),
                LatitudHipocentro = this.GetLatitudHipocentro(),
                LongitudHipocentro = this.GetLongitudHipocentro(),

                // También incluimos datos de relaciones
                // (que el Repositorio ya cargó con 'Include')
                Clasificacion = this.Clasificacion?.Nombre ?? "N/D",
                Origen = this.Origen?.Nombre ?? "N/D"
            };
        }


        public void registrarEstadoBloqueado(DateTime fechaHoraActual, Empleado responsable)
        {
            // --- CORRECCIÓN DE LLAMADA ---
            // 9. Evento Seleccionado -> Autodetectado (Estado Concreto)
            // Llama al estado actual, pasándole 'this' como 'ctx'
            // y 'this.cambioEstado' como 'cambiosEstado'.
            this.EstadoActual?.registrarEstadoBloqueado(this, this.cambioEstado, fechaHoraActual, responsable);
        }

        // REQ 1: Implementación de la reversión
        public void desbloquear(DateTime fechaHoraActual)
        {
            // --- CORRECCIÓN DE LLAMADA ---
            // Le pasa el contexto y la lista de cambios al estado actual
            this.EstadoActual?.desbloquear(this, this.cambioEstado, fechaHoraActual);
        }

        // 17. Autodetectado -> EventoSismico
        public void AgregarCambioEstado(CambioDeEstado ce)
        {
            this.cambioEstado.Add(ce);
        }

        // 18. Autodetectado -> EventoSismico
        public void setEstado(Estado nuevoEstado)
        {
            this.EstadoActual = nuevoEstado;
        }

        // Flujo: Gestor -> getDetalleEventoSismico() -> EventoSeleccionado
        public string getDetalleEventoSismico()
        {
            var sb = new StringBuilder();

            sb.AppendLine("--- Evento Sísmico ---");
            sb.AppendLine($"Inicio: {this.GetFechaHora():dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Magnitud: {this.GetMagnitud()} (Mw)");
            sb.AppendLine($"Alcance: {this.Alcance?.getNombreAlcance() ?? "N/D"}");
            sb.AppendLine($"Clasificación: {this.Clasificacion?.getNombreClasificacion() ?? "N/D"}");
            sb.AppendLine($"Origen: {this.Origen?.getNombreOrigen() ?? "N/D"}");

            string infoSeries = this.obtenerDatosSeriesTemporales(); // Llamada a método privado
            string infoAgrupada = this.AgruparInformacionSeriesPorEstacion(infoSeries);

            sb.AppendLine(infoAgrupada);
            return sb.ToString();
        }

        // Flujo: EventoSeleccionado -> obtenerDatosSeriesTemporales() -> EventoSeleccionado
        // (Este método AHORA ES PRIVADO)
        private string obtenerDatosSeriesTemporales()
        {
            var sbSeries = new StringBuilder();

            // 1. Obtenemos los grupos
            var grupos = this.getSeriesAgrupadasPorEstacion(); // Llama al método público

            // 2. Iteramos los grupos (Loop 1)
            foreach (var grupoEstacion in grupos)
            {
                string nombreEstacion = grupoEstacion.Key;
                sbSeries.AppendLine($"\n  Estación: {nombreEstacion}");

                // Iteramos las series de ESE grupo
                foreach (var serie in grupoEstacion)
                {
                    // Flujo: EventoSeleccionado -> getSeries() -> SerieTemporal
                    sbSeries.Append(serie.getSeries());
                }
            }
            return sbSeries.ToString();
        }

        // Flujo 5.3: El Gestor usa esto para el sismograma
        public IEnumerable<IGrouping<string, SerieTemporal>> getSeriesAgrupadasPorEstacion()
        {
            var seriesAgrupadas = this.serieTemporal
                .GroupBy(s => s.Sismografo?.getnombreEstacion() ?? "Estacion_Desconocida");

            return seriesAgrupadas;
        }

        // Flujo: Evento-> AgruparInformaciónSeriesPorEstacion() -> Evento
        public string AgruparInformacionSeriesPorEstacion(string infoSeries)
        {
            return $"--- Datos de Series Temporales ---{infoSeries}\n";
        }

        public void rechazar(DateTime fechaHoraActual, Empleado responsable)
        {
            // Llama al método del estado actual (ej. 'Bloqueado'),
            // pasándole 'this' como 'es' (EventoSismico) y 
            // 'this.cambioEstado' como 'ce' (la lista).
            this.EstadoActual?.rechazar(this.cambioEstado, this, fechaHoraActual, responsable);
        }


        // ... (obtenerDatosSeriesTemporales, getSeriesAgrupadasPorEstacion, etc.) ...

        // --- MÉTODO NUEVO (Requisito 1) ---
        // (Llamado por FinCU para mostrar el resumen final)
        public string getDetalleCompletoConHistorial()
        {
            // 1. Obtener el bloque de detalles estándar
            // (Reutilizamos el método que ya teníamos)
            string detallesBasicos = this.getDetalleEventoSismico();

            // 2. Crear el bloque de historial
            var sbHistorial = new StringBuilder();
            sbHistorial.AppendLine("\n--- Historial de Estados ---");

            // Ordenamos el historial por fecha de inicio
            var historialOrdenado = this.cambioEstado
                                        .OrderBy(ce => ce.FechaHoraInicio);

            foreach (var cambio in historialOrdenado)
            {
                // Formateamos la fecha de fin
                string fechaFin = cambio.FechaHoraFin == default(DateTime)
                                ? " (Actual)"
                                : $" (Fin: {cambio.FechaHoraFin:HH:mm:ss})";

                sbHistorial.AppendLine(
                    $"* {cambio.FechaHoraInicio:dd/MM/yy HH:mm:ss}: " +
                    $"{(cambio.Estado?.NombreEstado ?? "N/D")}" +
                    $" por {(cambio.Responsable?.Nombre ?? "Sistema")}" +
                    fechaFin
                );
            }

            // 3. Devolver el resumen combinado
            return detallesBasicos + sbHistorial.ToString();
        }
        // FLUJOS ALTERNATIVOS
        public void confirmar(DateTime fechaHoraActual, Empleado responsable)
        {
            this.EstadoActual?.confirmar(this.cambioEstado, this, fechaHoraActual, responsable);
        }

        public void derivar(DateTime fechaHoraActual, Empleado responsable)
        {
            this.EstadoActual?.derivar(this.cambioEstado, this, fechaHoraActual, responsable);
        }
    }
}