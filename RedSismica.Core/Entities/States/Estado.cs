// En: RedSismica.Core/States/Estado.cs
using RedSismica.Core.Entities; // Necesitaremos esto
using System;
using System.Collections.Generic; // Para 'List' o 'array'

namespace RedSismica.Core.States
{
    public abstract class Estado
    {
        public string NombreEstado { get; set; }
        public string Ambito { get; set; }

        public virtual void registrarEstadoBloqueado(EventoSismico ctx, List<CambioDeEstado> cambiosEstado, DateTime fechaHoraActual, Empleado responsable)
        {
            // Por defecto, los estados no hacen nada
        }

        // REQ: Método para revertir el bloqueo
        public virtual void desbloquear(EventoSismico ctx, List<CambioDeEstado> cambiosEstado, DateTime fechaHoraActual)
        {
            // Por defecto, los estados no se pueden desbloquear
        }

        public virtual void rechazar(List<CambioDeEstado> ce, EventoSismico es, DateTime fechaHoraActual, Empleado responsable)
        {
            // Por defecto, los estados no implementan esto
        }

        // FLUJOS ALTERNATIVOS
        // Firma para Derivar (la implementará Bloqueado)
        public virtual void derivar(List<CambioDeEstado> ce, EventoSismico es, DateTime fechaHoraActual, Empleado responsable)
        { }
        // Firma para Confirmar (la implementará Bloqueado)
        public virtual void confirmar(List<CambioDeEstado> ce, EventoSismico es, DateTime fechaHoraActual, Empleado responsable)
        { }

        public virtual void RegistrarAutomatico() { }
        public virtual void Confirmar() { }
        public virtual void Derivar() { }
        public virtual void ControlarTiempo() { }
        public virtual void Anular() { }
        public virtual void Cerrar() { }
        public virtual void RegistrarPendienteCierre() { }
        public virtual void AutoConfirmar() { }
        public virtual void ConfirmarRevision() { }

        public virtual void EsEventoSimico(DateTime fechaHoraActual, string nombreAS) { }
    }
}