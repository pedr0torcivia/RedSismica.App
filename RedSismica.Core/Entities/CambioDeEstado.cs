using RedSismica.Core.States;
using System;

namespace RedSismica.Core.Entities
{
    public class CambioDeEstado
    {
        public DateTime FechaHoraFin { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public String? MotivoRechazoServicio { get; set; }
        public Estado? Estado { get; set; }

        // --- CORRECCIÓN CRÍTICA ---
        // Añadido para que tu flujo "innegociable" compile.
        // La BBDD ya lo esperaba (FK_CambiosDeEstado_Empleados_ResponsableId)
        public Empleado? Responsable { get; set; }


        // --- MÉTODOS REQUERIDOS POR EL FLUJO ---

        // 11. Usado por Autodetectado -> buscarCambioAbierto
        public bool esEstadoActual()
        {
            // Un estado es "actual" si no tiene fecha de fin
            return this.FechaHoraFin == default(DateTime);
        }

        // 12. Usado por Autodetectado -> buscarCambioAbierto
        public void setFechaHoraFin(DateTime fechaHoraActual)
        {
            this.FechaHoraFin = fechaHoraActual;
        }
    }
}