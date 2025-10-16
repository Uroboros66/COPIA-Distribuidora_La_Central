namespace Distribuidora_La_Central.Shared.Models
{
    public class Cliente
    {
        public int codigoCliente { get; set; }
        public string cedula { get; set; }
        public string nombre { get; set; }
        public string apellido { get; set; }
        public string tipoCliente { get; set; }
        public string telefono { get; set; }
        public string direccion { get; set; }
        public int creado_por { get; set; } // IdUsuario
    }
}