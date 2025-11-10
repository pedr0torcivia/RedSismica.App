// En: RedSismica.Core/Entities/Usuario.cs

namespace RedSismica.Core.Entities
{
    public class Sesion
    {
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public Usuario usuarioLogueado { get; set; }
    }
}