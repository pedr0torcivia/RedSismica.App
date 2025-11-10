// En: RedSismica.Core/Entities/Usuario.cs
namespace RedSismica.Core.Entities
{
    public class Usuario
    {
        public string? Contraseña { get; set; }
        public string? NombreUsuario { get; set; }

        public Empleado? empleado { get; set; }
    }
}