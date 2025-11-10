// En: RedSismica.Core/Entities/AlcanceSismo.cs
namespace RedSismica.Core.Entities
{
    public class AlcanceSismo
    {
        public string? Descripcion { get; set; } 
        public string? Nombre { get; set; }

        public string? getNombreAlcance() => this.Nombre;
    }
}