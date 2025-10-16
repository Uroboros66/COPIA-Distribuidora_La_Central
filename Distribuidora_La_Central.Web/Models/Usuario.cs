namespace Distribuidora_La_Central.Web.Models
{
    public class Usuario
    {
        public int idUsuario { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string rol { get; set; } = string.Empty;
        public string codigoAcceso { get; set; } = string.Empty;
    }
}
