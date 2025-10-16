namespace Distribuidora_La_Central.Web.Models
{
    public class Proveedor
    {
        public int idProveedor { get; set; }
        public string nombre { get; set; }
        public string razonSocial { get; set; }
        public string contacto { get; set; }
        public string telefono { get; set; }
        public DateTime diaIngreso { get; set; }
    }
}
