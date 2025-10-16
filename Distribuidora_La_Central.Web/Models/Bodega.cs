namespace Distribuidora_La_Central.Web.Models
{
    public class Bodega
    {
        public int idBodega { get; set; }
        public string nombre { get; set; }
        public string ubicacion { get; set; }
        public int responsable { get; set; }
        public DateTime fecha { get; set; }
    }
}