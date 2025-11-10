// En: RedSismica.Core/Entities/ClasificacionSismo.cs
namespace RedSismica.Core.Entities
{
    public class ClasificacionSismo
    {
        // Mapeamos 'REAL' de la BBDD a 'double'.
        // Si prefieres 'float', podemos usar 'float'. 
        // Usaré 'double' por ser el estándar para 'REAL'.
        public double KmProfundidadDesde { get; set; }
        public double KmProfundidadHasta { get; set; }
        public string? Nombre { get; set; }

        public string? getNombreClasificacion() => this.Nombre;
    }
}