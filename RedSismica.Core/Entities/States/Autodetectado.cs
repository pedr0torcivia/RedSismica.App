// En: RedSismica.Core/States/Autodetectado.cs
using RedSismica.Core.Entities;
using System;
using System.Linq;

namespace RedSismica.Core.States
{
    public class Autodetectado : Estado
    {
        public Autodetectado()
        {
            this.NombreEstado = "Autodetectado";
        }

        // --- CORRECCIÓN DE FIRMA ---
        // 9. Esta es la anulación que dispara la secuencia
        public override void registrarEstadoBloqueado(EventoSismico ctx, List<CambioDeEstado> cambiosEstado, DateTime fechaHoraActual, Empleado responsable)
        {
            // 10. Autodetectado -> buscarCambioAbierto()
            // Usa la lista 'cambiosEstado' pasada por parámetro
            CambioDeEstado? cambioAbierto = this.buscarCambioAbierto(cambiosEstado);

            // 12. Autodetectado -> setFechaHoraFin()
            cambioAbierto?.setFechaHoraFin(fechaHoraActual);

            // 13. Autodetectado -> crearEstadoBloqueado()
            Estado nuevoEstado = this.crearEstadoBloqueado();

            // 15. Autodetectado -> crearCambioEstado()
            CambioDeEstado nuevoCambio = this.crearCambioEstado(fechaHoraActual, responsable, nuevoEstado);

            // 17. Autodetectado -> AgregarCambioEstado()
            // Llama al método del contexto 'ctx'
            ctx.AgregarCambioEstado(nuevoCambio);

            // 18. Autodetectado -> setEstado()
            // Llama al método del contexto 'ctx'
            ctx.setEstado(nuevoEstado);
        }

        // 10. buscarCambioAbierto
        // --- CORRECCIÓN DE FIRMA ---
        private CambioDeEstado? buscarCambioAbierto(List<CambioDeEstado> cambiosEstado)
        {
            // 11. (loop) esEstadoActual()
            // Usa la lista 'cambiosEstado' pasada por parámetro
            return cambiosEstado.FirstOrDefault(ce => ce.esEstadoActual());
        }

        // 13. crearEstadoBloqueado
        private Estado crearEstadoBloqueado()
        {
            // 14. new(): Bloqueado
            return new Bloqueado();
        }

        // 15. crearCambioEstado
        private CambioDeEstado crearCambioEstado(DateTime fechaHoraInicio, Empleado responsable, Estado estadoActual)
        {
            // 16. new(): CambioDeEstado
            return new CambioDeEstado
            {
                FechaHoraInicio = fechaHoraInicio,
                Estado = estadoActual,
                Responsable = responsable
            };
        }
    }
}