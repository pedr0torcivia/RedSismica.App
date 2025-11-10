// En: RedSismica.Core/Entities/Sismografo.cs
namespace RedSismica.Core.Entities
{
    public class Sismografo
    {
        public DateTime FechaAdquisicion { get; set; }
        public string? IdentificadorSismografo { get; set; }
        public string? NroSerie { get; set; }

        // --- CORRECCIÓN ---
        // Inicializamos la lista para que no sea null
        public List<SerieTemporal> seriesTemporales { get; set; } = new List<SerieTemporal>();

        public EstacionSismologica? estacionSismologica { get; set; }

        // SerieTemporal -> getnombreEstacion() -> Sismografo
        public string getnombreEstacion()
        {
            if (this.estacionSismologica == null)
                return "Estación [Sismógrafo no asignado]";

            // Flujo: Sismografo -> getnombreEstacion() -> EstacionSismologica
            return this.estacionSismologica.getnombreEstacion() ?? "Estación [Nombre N/D]";
        }
    }
}