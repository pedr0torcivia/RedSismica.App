// En: RedSismica.Core/Entities/SerieTemporal.cs
using System.Text;

namespace RedSismica.Core.Entities
{
    public class SerieTemporal
    {
        public int CondicionAlarma { get; set; }
        public DateTime FechaHoraInicioRegistroMuestras { get; set; }
        public DateTime FechaHoraRegistro { get; set; }
        public double FrecuenciaMuestro { get; set; }
        
        // --- CORRECCIÓN ---
        // Inicializamos la lista para que no sea null
        public List<MuestraSismica> muestrasSismicas { get; set; } = new List<MuestraSismica>();

        public Sismografo? Sismografo { get; set; }

        // Flujo: EventoSeleccionado -> getSeries() -> SerieTemporal
        // Este método implementa el LOOP 2
        public string getSeries()
        {
            var sb = new StringBuilder();

            // INICIA OTRO LOOP (Mientras existan muestras)
            foreach (var muestra in this.muestrasSismicas)
            {
                // Flujo: SerieTemporal -> getDatos() -> Muestras
                sb.Append(muestra.getDatos());
            }
            return sb.ToString();
        }
    }
}