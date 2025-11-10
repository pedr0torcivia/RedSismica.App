// En: RedSismica.Core/Entities/DetalleMuestra.cs
using System.ComponentModel.DataAnnotations.Schema; // Necesitas este 'using'

namespace RedSismica.Core.Entities
{
    public class DetalleMuestra
    {
        public double Valor { get; set; }
        public TipoDeDato? TipoDeDato { get; set; }

        // --- AÑADIR ESTA LÍNEA ---
        // (Esto es solo para el importador,
        // EF Core lo ignorará gracias a [NotMapped])
        [NotMapped]
        public string MuestraSismicaId_temp { get; set; }

        public string getDatos()
        {
            if (this.TipoDeDato == null)
            {
                return "      - [Dato no disponible]";
            }

            // Flujo: Detalle Muestra -> getDatos() -> TipoDeDato
            var tipoDatoObj = this.TipoDeDato.getDatos();

            return $"      - {tipoDatoObj ?? "N/D"}: {this.Valor}";
        }
    }
}