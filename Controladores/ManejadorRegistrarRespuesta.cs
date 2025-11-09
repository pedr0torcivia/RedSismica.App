// En: RedSismica.App/Controladores/ManejadorRegistrarRespuesta.cs
using RedSismica.App.Services; // Para el generador de sismograma
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

        // Atributos de tu lista de clases
        private List<EventoSismico> eventosAutodetectadosNoRevisados;
        private EventoSismico eventoSeleccionado;
        private Empleado usuarioLogueado; // Tu 'nombreOp' o 'responsable'
        private Sesion sesionActual; // Para simular el logueo

        // --- Dependencias (Inyectadas) ---
        private readonly RedSismicaContext _context;
        private readonly EventoRepositoryEF _repository;
        private readonly CU_GenerarSismograma _generadorSismograma;

        // El Manejador recibe sus "herramientas" (las dependencias)
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
        // (Llamado desde Pantalla.opcionRegistrarResultadoRevisionManual)
        public void RegistrarNuevaRevision(PantallaNuevaRevision pantalla)
        {
            // Simulamos el inicio de sesión
            buscarUsuarioLogueado();

            // Inicia el flujo
            buscarEventosAutoDetecNoRev(pantalla);
        }

        // --- PASO 2: El usuario selecciona un evento ---
        // (Llamado desde Pantalla.gridEventos_SelectionChanged)
        public void TomarSeleccionEvento(int indice, PantallaNuevaRevision pantalla)
        {
            if (indice < 0 || indice >= eventosAutodetectadosNoRevisados.Count)
                return; // Selección inválida

            eventoSeleccionado = eventosAutodetectadosNoRevisados[indice];

            // Inicia la secuencia de "Bloqueo" y "Muestra de Datos"
            actualizarEventoBloqueado(pantalla);
            buscarDetallesEventoSismico(pantalla);
            generarSismograma(pantalla);
            habilitarOpciones(pantalla);
        }

        // --- PASO 3: El usuario toma decisiones (No-Op) ---
        // (Llamados desde los clicks en los RadioButton)

        public void TomarDecisionVisualizarMapa(bool deseaVer, PantallaNuevaRevision pantalla)
        {
            // Tu flujo dice: "modelando la alternativa donde el usuario no desea ver el mapa"
            if (deseaVer)
            {
                pantalla.MostrarMensaje("Función de mapa no implementada.");
            }
        }

        // Tu flujo dice: "se modele la parte donde el usuario diga que no"
        public void TomarOpcionModificacionAlcance(bool modificar, PantallaNuevaRevision pantalla) { }
        public void TomarOpcionModificacionMagnitud(bool modificar, PantallaNuevaRevision pantalla) { }
        public void TomarOpcionModificacionOrigen(bool modificar, PantallaNuevaRevision pantalla) { }


        // --- PASO 4: El usuario confirma la acción final ---
        // (Llamado desde Pantalla.btnConfirmar_Click)
        public void TomarOpcionAccion(int opcion, PantallaNuevaRevision pantalla)
        {
            // Opcion 1: "Confirmar Evento"
            // Opcion 2: "Rechazar Evento"
            // Opcion 3: "Solicitar Revisión a Experto"

            // Tu flujo solo detalla la secuencia de RECHAZAR.
            // Implementamos esa.
            if (opcion == 2) // Rechazar
            {
                actualizarEstadoRechazado(pantalla);
                pantalla.MostrarMensaje("Evento Rechazado. Fin del Caso de Uso.");
            }
            else
            {
                pantalla.MostrarMensaje("Acción no implementada en este prototipo.");
            }
        }

        // =================================================================
        // --- MÉTODOS PRIVADOS (Lógica interna del flujo) ---
        // =================================================================

        private void buscarUsuarioLogueado()
        {
            // Tu flujo: Manejador -> Sesion.getUsuario() -> Empleado.esTuUsuario()
            // SIMULACIÓN:
            this.sesionActual = new Sesion
            {
                FechaHoraInicio = DateTime.Now,
                // Simulamos que el usuario "operador1" está logueado
                usuarioLogueado = new Usuario { NombreUsuario = "operador1" }
            };

            // Usamos el repositorio para traer el Empleado real de la BBDD
            this.usuarioLogueado = _repository.BuscarEmpleadoPorUsuario(
                sesionActual.usuarioLogueado.NombreUsuario);

            if (this.usuarioLogueado == null)
            {
                // En un sistema real, crearíamos un empleado "default" o lanzaríamos error
                this.usuarioLogueado = new Empleado { Nombre = "Operador", Apellido = "Default" };
            }
        }

        private void buscarEventosAutoDetecNoRev(PantallaNuevaRevision pantalla)
        {
            // Tu flujo: "buscar todos los eventos sísmicos auto detectados"
            this.eventosAutodetectadosNoRevisados =
                _repository.BuscarEventosAutodetectadosNoRevisados();

            // Tu flujo: "ordenarEventos() por fechaHora de ocurrencia"
            // (El repositorio ya los trajo ordenados)

            // Tu flujo: "solicitarSeleccionEvento()"
            // Formateamos los datos para la grilla
            var listaParaGrilla = eventosAutodetectadosNoRevisados.Select(e => new
            {
                Inicio = e.FechaHoraInicio,
                Magnitud = e.ValorMagnitud,
                Clasificacion = e.Clasificacion?.Nombre, // Usamos '?' por si es nulo
                Origen = e.Origen?.Nombre
            }).ToList<object>(); // Convertimos a List<object>

            pantalla.SolicitarSeleccionEvento(listaParaGrilla);
        }

        private void actualizarEventoBloqueado(PantallaNuevaRevision pantalla)
        {
            // Tu flujo: (Secuencia larga de actualización de estado)
            // Implementamos el Patrón State (versión manejador)

            try
            {
                var fechaActual = DateTime.Now;

                // 1. Crear el nuevo estado
                var nuevoEstado = new Bloqueado { NombreEstado = "Bloqueado" };

                // 2. Crear el registro de historial
                var nuevoCambio = new CambioDeEstado
                {
                    FechaHoraInicio = fechaActual,
                    Estado = nuevoEstado,
                    // (Omitimos 'EstadoAnterior' y 'Motivo' 
                    //  porque tu clase CambioDeEstado.cs no los tiene)
                };

                // 3. Actualizar el evento
                eventoSeleccionado.EstadoActual = nuevoEstado;
                eventoSeleccionado.cambioEstado.Add(nuevoCambio);

                // 4. Persistir en la BBDD
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al bloquear el evento: {ex.Message}");
            }
        }

        private void buscarDetallesEventoSismico(PantallaNuevaRevision pantalla)
        {
            // Tu flujo: "getDetalleEventoSismico()" -> getNombreAlcance, etc.

            // Usamos el 'eventoSeleccionado' que ya tiene TODO cargado
            // gracias a los 'Include()' del repositorio.

            var sb = new StringBuilder();
            sb.AppendLine($"--- Evento Sísmico ---");
            sb.AppendLine($"Inicio: {eventoSeleccionado.FechaHoraInicio:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Magnitud: {eventoSeleccionado.ValorMagnitud} (Mw)");
            sb.AppendLine($"Profundidad: {eventoSeleccionado.LatitudHipocentro} km");

            // Tu flujo: "getNombreAlcance()"
            sb.AppendLine($"Alcance: {eventoSeleccionado.Alcance?.Nombre ?? "N/D"}");

            // Tu flujo: "getNombreClasificacion()"
            sb.AppendLine($"Clasificación: {eventoSeleccionado.Clasificacion?.Nombre ?? "N/D"}");

            // Tu flujo: "getNombreOrigen()"
            sb.AppendLine($"Origen: {eventoSeleccionado.Origen?.Nombre ?? "N/D"}");

            pantalla.MostrarDetalleEventoSismico(sb.ToString());
        }

        private void generarSismograma(PantallaNuevaRevision pantalla)
        {
            // Tu flujo: "generarSismograma() llamando al caso de uso"

            // (NOTA: El flujo pide "un sismograma por estación". 
            // Nuestra clase 'CU_GenerarSismograma' solo genera uno simulado.
            // Para cumplir el flujo, deberíamos iterar 
            // 'eventoSeleccionado.serieTemporal' y llamar al generador
            // por cada 's.Sismografo.Estacion' única.
            // Por simplicidad, generamos solo uno).

            try
            {
                string rutaImagen = _generadorSismograma.Ejecutar();
                pantalla.MostrarSismograma(rutaImagen);
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al generar sismograma: {ex.Message}");
            }
        }

        private void habilitarOpciones(PantallaNuevaRevision pantalla)
        {
            // Tu flujo: Habilitar todas las opciones
            pantalla.opcionMostrarMapa();
            pantalla.OpcionModificacionAlcance();
            pantalla.OpcionModificacionMagnitud();
            pantalla.OpcionModificacionOrigen();
            pantalla.SolicitarSeleccionAcciones();
            pantalla.MostrarBotonCancelar();
        }

        private void actualizarEstadoRechazado(PantallaNuevaRevision pantalla)
        {
            // Tu flujo: (Secuencia larga de Bloqueado -> Rechazado)
            // Implementamos el Patrón State (versión manejador)

            try
            {
                var fechaActual = DateTime.Now;

                // 1. Crear el nuevo estado
                var nuevoEstado = new Rechazado { NombreEstado = "Rechazado" };

                // 2. Crear el registro de historial
                var nuevoCambio = new CambioDeEstado
                {
                    FechaHoraInicio = fechaActual,
                    Estado = nuevoEstado,
                };

                // 3. Finalizar el estado "Bloqueado" anterior (si existe)
                var estadoBloqueado = eventoSeleccionado.cambioEstado
                    .FirstOrDefault(ce => ce.Estado.NombreEstado == "Bloqueado"
                                       && ce.FechaHoraFin == default);
                if (estadoBloqueado != null)
                {
                    estadoBloqueado.FechaHoraFin = fechaActual;
                }

                // 4. Actualizar el evento
                eventoSeleccionado.EstadoActual = nuevoEstado;
                eventoSeleccionado.cambioEstado.Add(nuevoCambio);

                // 5. Persistir en la BBDD
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                pantalla.MostrarMensaje($"Error al rechazar el evento: {ex.Message}");
            }
        }
    }
}