// En: RedSismica.Core/States/Bloqueado.cs
using RedSismica.Core.Entities;
using System;
using System.Linq;

namespace RedSismica.Core.States
{
    public class Bloqueado : Estado
    {
        public Bloqueado()
        {
            this.NombreEstado = "Bloqueado";
        }

        // --- CORRECCIÓN DE FIRMA ---
        // REQ 1: Implementación de la reversión (desbloquear)
        public override void desbloquear(EventoSismico ctx, List<CambioDeEstado> cambiosEstado, DateTime fechaHoraActual)
        {
            // 1. Encontrar el 'CambioDeEstado' de "Bloqueado" (usa la lista pasada)
            var cambioActualBloqueado = cambiosEstado
                .FirstOrDefault(ce => ce.Estado?.NombreEstado == this.NombreEstado && ce.esEstadoActual());

            // 2. Encontrar el 'CambioDeEstado' anterior ("Autodetectado") (usa la lista pasada)
            var cambioAnterior = cambiosEstado
                .OrderByDescending(ce => ce.FechaHoraInicio)
                .FirstOrDefault(ce => ce.Estado?.NombreEstado == "Autodetectado");

            if (cambioActualBloqueado != null)
            {
                // 3. Eliminar el cambio (usa el 'ctx' para acceder a la lista)
                ctx.cambioEstado.Remove(cambioActualBloqueado);
            }

            if (cambioAnterior != null)
            {
                // 4. Re-abrir el cambio "Autodetectado"
                cambioAnterior.setFechaHoraFin(default(DateTime));

                // 5. Revertir el estado (usa el 'ctx')
                ctx.setEstado(cambioAnterior.Estado);
            }
        }

        // --- IMPLEMENTACIÓN DEL FLUJO DE RECHAZO ---
        // (Flujo: EventoSismico -> rechazar() -> Bloqueado)
        public override void rechazar(List<CambioDeEstado> ce, EventoSismico es, DateTime fechaHoraActual, Empleado responsable)
        {
            // Flujo: Bloqueado -> (loop) esEstadoActual() -> Array de CambioDeEstado
            // (Buscamos el cambio 'Bloqueado' que está abierto)
            var cambioAbierto = ce.FirstOrDefault(c => c.esEstadoActual());

            // Flujo: Bloqueado -> setFechaHoraFin() -> CambioDeEstado Actual
            cambioAbierto?.setFechaHoraFin(fechaHoraActual);

            // Flujo: Bloqueado -> new(): Rechazado
            var nuevoEstado = new Rechazado(); // 'Rechazado' es una clase que hereda de Estado

            // Flujo: Bloqueado -> crearCambioEstado()
            var nuevoCambio = this.crearCambioEstado(fechaHoraActual, responsable, nuevoEstado);

            // Flujo: Bloqueado -> AgregarCambioEstado() -> EventoSismico
            es.AgregarCambioEstado(nuevoCambio);

            // Flujo: Bloqueado -> setEstado() -> EventoSismico
            es.setEstado(nuevoEstado);
        }

        // Flujo: Bloqueado -> crearCambioEstado(...)
        private CambioDeEstado crearCambioEstado(DateTime fechaHoraInicio, Empleado responsable, Estado estadoActual)
        {
            // Flujo: Bloqueado -> new(): CambioDeEstado
            return new CambioDeEstado
            {
                FechaHoraInicio = fechaHoraInicio,
                Estado = estadoActual,
                Responsable = responsable
            };
        }
        // FLUJOS ALTERNATIVOS
        public override void confirmar(List<CambioDeEstado> ce, EventoSismico es, DateTime fechaHoraActual, Empleado responsable)
        {
            // Busca el cambio 'Bloqueado' abierto
            var cambioAbierto = ce.FirstOrDefault(c => c.esEstadoActual());
            // Cierra el cambio 'Bloqueado'
            cambioAbierto?.setFechaHoraFin(fechaHoraActual);

            // Crea el nuevo estado
            var nuevoEstado = new Confirmado();
            // Crea el nuevo historial
            var nuevoCambio = this.crearCambioEstado(fechaHoraActual, responsable, nuevoEstado);

            // Actualiza el evento
            es.AgregarCambioEstado(nuevoCambio);
            es.setEstado(nuevoEstado);
        }

        public override void derivar(List<CambioDeEstado> ce, EventoSismico es, DateTime fechaHoraActual, Empleado responsable)
        {
            var cambioAbierto = ce.FirstOrDefault(c => c.esEstadoActual());
            cambioAbierto?.setFechaHoraFin(fechaHoraActual);

            var nuevoEstado = new Derivado();
            var nuevoCambio = this.crearCambioEstado(fechaHoraActual, responsable, nuevoEstado);

            es.AgregarCambioEstado(nuevoCambio);
            es.setEstado(nuevoEstado);
        }
    }
   }
