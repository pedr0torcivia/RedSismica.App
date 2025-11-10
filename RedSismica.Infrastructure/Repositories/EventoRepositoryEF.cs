// En: RedSismica.Infrastructure/Repositories/EventoRepositoryEF.cs
using Microsoft.EntityFrameworkCore;
using RedSismica.Core.Entities;
using RedSismica.Core.States;
using System.Collections.Generic;
using System.Linq;

namespace RedSismica.Infrastructure.Repositories
{
    public class EventoRepositoryEF
    {
        private readonly RedSismicaContext _context;

        public EventoRepositoryEF(RedSismicaContext context)
        {
            _context = context;
        }

        public List<EventoSismico> BuscarEventosAutodetectadosNoRevisados()
        {
            // 1. Cargamos TODOS los eventos y sus Includes a la memoria
            var todosLosEventos = _context.EventosSismicos
                .Include(e => e.Alcance)
                .Include(e => e.Clasificacion)
                .Include(e => e.Origen)
                .Include(e => e.Responsable)
                .Include(e => e.cambioEstado)

                // --- ¡LA CARGA DE DATOS AHORA ESTÁ COMPLETA! ---
                .Include(e => e.serieTemporal)
                    .ThenInclude(st => st.Sismografo) // <-- AHORA FUNCIONA
                        .ThenInclude(s => s.estacionSismologica) // <-- AHORA FUNCIONA

                .Include(e => e.serieTemporal)
                    .ThenInclude(st => st.muestrasSismicas)
                        .ThenInclude(ms => ms.detalleMuestraSismica)
                            .ThenInclude(dm => dm.TipoDeDato)

                .ToList(); // <-- Forzamos la evaluación en cliente

            // 2. Filtramos la lista EN MEMORIA (Esto ya funcionaba)
            var eventosFiltrados = todosLosEventos
                .Where(e => e.EstadoActual?.NombreEstado == "Autodetectado")
                .ToList();

            return eventosFiltrados;
        }

        public Empleado BuscarEmpleadoPorUsuario(string nombreUsuario)
        {
            var usuario = _context.Usuarios
                .Include(u => u.empleado)
                .FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

            return usuario?.empleado;
        }
    }
}