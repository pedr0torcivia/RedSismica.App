// En: RedSismica.Core/Entities/EstacionSismologica.cs
namespace RedSismica.Core.Entities
{
    public class EstacionSismologica
    {
        public string? CodigoEstacion { get; set; }
        public string? DocumentoCertificacionAdq { get; set; }
        public DateTime FechaSolicitudCertificacion { get; set; } // 'TEXT' en BBDD, 'DateTime' en clase
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string? Nombre { get; set; }
        public string? NroCertificacionAdquisicion { get; set; }

        public string? getnombreEstacion() => this.Nombre;
    }
}