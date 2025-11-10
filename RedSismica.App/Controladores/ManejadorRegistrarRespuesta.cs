using RedSismica.App.Services;
using RedSismica.Core.Entities;
using RedSismica.Core.States;
using RedSismica.Infrastructure;
using RedSismica.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedSismica.App.Controladores
{
    public class ManejadorRegistrarRespuesta
    {
        // --- Atributos de Estado (Memoria del Caso de Uso) ---
        private List<EventoSismico> eventosAutodetectadosNoRevisados;
        private EventoSismico eventoSeleccionado;
        private Empleado usuarioLogueado;
        private Sesion sesionActual;

        // --- Dependencias (Inyectadas) ---
        private readonly RedSismicaContext _context;
        private readonly EventoRepositoryEF _repository;
        private readonly CU_GenerarSismograma _generadorSismograma;

        public ManejadorRegistrarRespuesta(
            RedSismicaContext context,
            EventoRepositoryEF repository,
            CU_GenerarSismograma generadorSismograma)
        {
            _context = context;
            _repository = repository;
            _generadorSismograma = generadorSismograma;
        }

        // --- PASO 1: Inicia el Caso de Uso ---
        public void RegistrarNuevaRevision(PantallaNuevaRevision pantalla)
        {
            if (this.usuarioLogueado == null)
            {
                buscarUsuarioLogueado();
            }
            buscarEventosAutoDetecNoRev(pantalla);
        }

        // --- PASO 2: El usuario selecciona un evento ---
        public void TomarSeleccionEvento(int indice, PantallaNuevaRevision pantalla)
        {
            if (indice < 0 || indice >= eventosAutodetectadosNoRevisados.Count)
                return;

            var nuevoEventoSeleccionado = eventosAutodetectadosNoRevisados[indice];
            if (this.eventoSeleccionado == nuevoEventoSeleccionado)
                return;

            // REQ 1: Revertir selección anterior
            if (this.eventoSeleccionado != null)
            {
                revertirBloqueoEventoAnterior();
            }

            this.eventoSeleccionado = nuevoEventoSeleccionado;

            // 3. Gestor -> actualizarEventoBloqueado()
            actualizarEventoBloqueado(pantalla);

            // --- INICIO DE TU NUEVO FLUJO ---
            // Gestor -> buscarDetalleEventoSismico()
            buscarDetallesEventoSismico(pantalla);
            // (Este método llama internamente a generarSismograma)

            // Flujo: "El manejador habilita la opcion... de forma individual"
            habilitarOpcionVisualizarMapa(pantalla);
            habilitarModificacionAlcance(pantalla);
            habilitarModificacionMagnitud(pantalla);
            habilitarModificacionOrigen(pantalla);

            // Flujo: "Luego el control solicitará la seleccion de una de estas acciones"
            solicitarSeleccionAcciones(pantalla);
        }

        // --- PASO 3: Opciones (No-Op) ---
        public void TomarDecisionVisualizarMapa(bool deseaVer, PantallaNuevaRevision pantalla) { /* ... */ }
        public void TomarOpcionModificacionAlcance(bool modificar, PantallaNuevaRevision pantalla) { /* ... */ }
        public void TomarOpcionModificacionMagnitud(bool modificar, PantallaNuevaRevision pantalla) { /* ... */ }
        public void TomarOpcionModificacionOrigen(bool modificar, PantallaNuevaRevision pantalla) { /* ... */ }

        // --- PASO 4: Acción final ---
        // (Llamado desde Pantalla.btnConfirmar_Click)
        public void TomarOpcionAccion(int opcion, PantallaNuevaRevision pantalla)
        {
            if (!validarAccion(pantalla))
            {
                return;
            }
            registrarUsuario();

            // --- CORRECCIÓN: Switch para A6 y A7 ---
            switch (opcion)
            {
                case 1: // A6: Confirmar Evento
                    actualizarEstadoConfirmado(pantalla);
                    break;
                case 2: // Rechazar Evento
                    actualizarEstadoRechazado(pantalla);
                    break;
                case 3: // A7: Solicitar Revisión a Experto
                    actualizarEstadoDerivado(pantalla);
                    break;
                default:
                    pantalla.MostrarMensaje("Opción no válida.");
                    return;
            }

            FinCU(pantalla);
        }

        // =================================================================
        // --- MÉTODOS PRIVADOS (Lógica interna del flujo) ---
        // =================================================================

        // 7. Gestor -> getFechaHOra():DateTime -> Gestor
        private DateTime getFechaHora()
        {
            return DateTime.Now;
        }

        // 4. Gestor -> buscarUsuarioLogueado() -> Gestor
        private void buscarUsuarioLogueado()
        {
            if (this.sesionActual == null)
            {
                this.sesionActual = new Sesion
                {
                    FechaHoraInicio = DateTime.Now,
                    usuarioLogueado = new Usuario { NombreUsuario = "operador1" }
                };
            }
            this.usuarioLogueado = _repository.BuscarEmpleadoPorUsuario(
                sesionActual.usuarioLogueado.NombreUsuario);
            if (this.usuarioLogueado == null)
            {
                this.usuarioLogueado = new Empleado { Nombre = "Operador", Apellido = "Default" };
            }
        }

        private void buscarEventosAutoDetecNoRev(PantallaNuevaRevision pantalla)
        {
            this.eventosAutodetectadosNoRevisados =
                _repository.BuscarEventosAutodetectadosNoRevisados();
            var listaParaGrilla = new List<object>();
            foreach (var evento in this.eventosAutodetectadosNoRevisados)
            {
                listaParaGrilla.Add(evento.GetDatosOcurrencia());
            }
            ordenarEventos();
            pantalla.SolicitarSeleccionEvento(listaParaGrilla);
        }

        private void ordenarEventos()
        {
            this.eventosAutodetectadosNoRevisados =
                this.eventosAutodetectadosNoRevisados
                    .OrderBy(e => e.GetFechaHora())
                    .ToList();
        }

        private void actualizarEventoBloqueado(PantallaNuevaRevision pantalla)
        {
            var fechaActual = getFechaHora();
            eventoSeleccionado.registrarEstadoBloqueado(fechaActual, this.usuarioLogueado);
            try
            {
                _context.SaveChanges();
                pantalla.MostrarMensaje("El Evento ha sido Bloqueado para su revision");
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al guardar el bloqueo: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void revertirBloqueoEventoAnterior()
        {
            if (this.eventoSeleccionado == null) return;
            this.eventoSeleccionado.desbloquear(getFechaHora());
        }

        // Flujo: Gestor -> buscarDetalleEventoSismico() -> Gestor
        private void buscarDetallesEventoSismico(PantallaNuevaRevision pantalla)
        {
            // 5.1 y 5.2
            string detallesCompletos = eventoSeleccionado.getDetalleEventoSismico();
            pantalla.MostrarDetalleEventoSismico(detallesCompletos);

            // 5.3
            generarSismograma(pantalla);
        }

        private string? generarSismograma(PantallaNuevaRevision pantalla)
        {
            try
            {
                string rutaImagen = _generadorSismograma.Ejecutar("Sismograma General");
                pantalla.MostrarSismograma(rutaImagen); // Se muestra aquí
                return rutaImagen;
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al generar sismograma: {ex.Message}");
                return null;
            }
        }

        // --- MÉTODOS PARA HABILITACIÓN INDIVIDUAL ---

        // Flujo: Gestor -> habilitarOpcionVisualizarMapa() -> Gestor
        private void habilitarOpcionVisualizarMapa(PantallaNuevaRevision pantalla)
        {
            // Flujo: Gestor -> opcionMostrarMapa() -> Pantalla
            pantalla.opcionMostrarMapa();
        }

        // Flujo: Gestor -> habilitarModificaciónAlcance() -> Gestor
        private void habilitarModificacionAlcance(PantallaNuevaRevision pantalla)
        {
            // Flujo: Gestor -> opcionModificaciónAlcance() -> Pantalla
            pantalla.OpcionModificacionAlcance();
        }

        // Flujo: Gestor -> habilitarModificaciónMagnitud() -> Gestor
        private void habilitarModificacionMagnitud(PantallaNuevaRevision pantalla)
        {
            // Flujo: Gestor -> opcionModificaciónMagnitud() -> Pantalla
            pantalla.OpcionModificacionMagnitud();
        }

        // Flujo: Gestor -> habilitarModificaciónOrigen() -> Gestor
        private void habilitarModificacionOrigen(PantallaNuevaRevision pantalla)
        {
            // Flujo: Gestor -> opcionModificaciónOrigen() -> Pantalla
            pantalla.OpcionModificacionOrigen();
        }

        // Flujo: Gestor -> solicitarSeleccionAcciones() -> Pantalla
        private void solicitarSeleccionAcciones(PantallaNuevaRevision pantalla)
        {
            pantalla.SolicitarSeleccionAcciones();
            pantalla.MostrarBotonCancelar();
        }

        // --- MÉTODOS DEL FLUJO FINAL ---

        // Flujo: Gestor -> validarAccion() -> Gestor
        private bool validarAccion(PantallaNuevaRevision pantalla)
        {
            if (eventoSeleccionado == null)
            {
                pantalla.MostrarMensaje("Error: No hay ningún evento seleccionado.");
                return false;
            }

            // Flujo: "valida que exista magnitud, alcance y origen"
            if (eventoSeleccionado.ValorMagnitud == 0 ||
                eventoSeleccionado.Alcance == null ||
                eventoSeleccionado.Origen == null)
            {
                pantalla.MostrarMensaje("Error: Faltan datos (Magnitud, Alcance u Origen) en el evento.");
                return false;
            }
            return true;
        }

        // Flujo: Gestor -> registrarUsuario() -> Gestor
        private void registrarUsuario()
        {
            // (Nos aseguramos de que el usuario esté cargado)
            if (this.usuarioLogueado == null)
            {
                buscarUsuarioLogueado();
            }
        }

        // Flujo: Gestor -> actualizarEstadoRechazado() -> Gestor
        private void actualizarEstadoRechazado(PantallaNuevaRevision pantalla)
        {
            try
            {
                // Flujo: Gestor -> getFechaHora() -> Gestor
                var fechaActual = getFechaHora();

                // Flujo: Gestor -> rechazar(...) -> Evento Seleccionado
                eventoSeleccionado.rechazar(fechaActual, this.usuarioLogueado);

                _context.SaveChanges();
                pantalla.MostrarMensaje("Evento Rechazado.");
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al rechazar el evento: {ex.Message}");
            }
        }

        // FLUJOS ALTERNATIVOS

        private void actualizarEstadoConfirmado(PantallaNuevaRevision pantalla)
        {
            try
            {
                var fechaActual = getFechaHora();
                eventoSeleccionado.confirmar(fechaActual, this.usuarioLogueado);
                _context.SaveChanges();
                pantalla.MostrarMensaje("Evento Confirmado.");
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al confirmar el evento: {ex.Message}");
            }
        }

        // --- Flujo A7 (Paso 50) ---
        private void actualizarEstadoDerivado(PantallaNuevaRevision pantalla)
        {
            try
            {
                var fechaActual = getFechaHora();
                eventoSeleccionado.derivar(fechaActual, this.usuarioLogueado);
                _context.SaveChanges();
                pantalla.MostrarMensaje("Evento Derivado a Experto.");
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al derivar el evento: {ex.Message}");
            }
        }


        public void CancelarRevisionActual()
        {
            // Si hay un evento seleccionado (Bloqueado)...
            if (this.eventoSeleccionado != null)
            {
                // ...lo revertimos a "Autodetectado"
                revertirBloqueoEventoAnterior();
                try
                {
                    // Guardamos la reversión en la BBDD
                    _context.SaveChanges();
                }
                catch (Exception)
                {
                    // Manejar error si la reversión falla
                }
            }
        }


        // Flujo: Gestor -> FinCU() -> Gestor
        private void FinCU(PantallaNuevaRevision pantalla)
        {
            string mensajeFinal = "Fin del Caso de Uso.";

            // --- REQUISITO 1: Mostrar resumen ---
            if (eventoSeleccionado != null)
            {
                // Nos aseguramos de que el historial de cambios
                // esté totalmente cargado desde la BBDD antes de mostrarlo
                try
                {
                    _context.Entry(eventoSeleccionado)
                            .Collection(e => e.cambioEstado)
                            .Load();
                }
                catch (Exception) { /* Ignorar si falla la carga */ }

                // Generamos el texto del resumen completo
                mensajeFinal = eventoSeleccionado.getDetalleCompletoConHistorial();
            }

            // Reseteamos las variables de memoria
            this.eventoSeleccionado = null;
            this.eventosAutodetectadosNoRevisados = null;

            // Reiniciamos la UI
            pantalla.RestaurarEstadoInicial();
            pantalla.ReiniciarVistaParaNuevoCU();

            // Mostramos el resumen final
            pantalla.MostrarMensaje(mensajeFinal);
        }
    }
}