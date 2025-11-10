// En: RedSismica.Core/Entities/Empleado.cs
namespace RedSismica.Core.Entities
{
    public class Empleado
    {
        public string? Apellido { get; set; }
        public string? Nombre { get; set; }
        public string? Mail { get; set; }
        public int Telefono { get; set; } // 'Integer' -> int
    }
}