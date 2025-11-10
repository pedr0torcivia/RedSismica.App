// En: RedSismica.Core/Entities/TipoDeDato.cs
namespace RedSismica.Core.Entities
{
    public class TipoDeDato
    {
        public string? Denominacion { get; set; }
        public string? nombreUnidadMedida { get; set; } // Tu esquema lo llama 'NombreUnidadMedida'
        public double ValorUmbral { get; set; }

        public string? getDatos() => this.getDenominacion();
        public string? getDenominacion() => this.Denominacion;
    }
}