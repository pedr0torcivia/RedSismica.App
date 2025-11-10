// En: RedSismica.Core/Entities/MuestraSismica.cs
using System.Text;

namespace RedSismica.Core.Entities
{
    public class MuestraSismica
    {
        public DateTime FechaHoraMuestra { get; set; }

        // --- CORRECCIÓN ---
        // Inicializamos la lista para que no sea null
        public List<DetalleMuestra> detalleMuestraSismica { get; set; } = new List<DetalleMuestra>();

        // Flujo: SerieTemporal -> getDatos() -> Muestras
        // Este método implementa el LOOP 3
        public string getDatos()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"    Muestra ({this.FechaHoraMuestra:HH:mm:ss.fff}):");

            // INICIA OTRO LOOP (Mientras existan DetalleMuestra)
            foreach (var detalle in this.detalleMuestraSismica)
            {
                // Flujo: Muestras -> getDatos() -> Detalle Muestra
                sb.AppendLine(detalle.getDatos());
            }
            return sb.ToString();
        }
    }
}