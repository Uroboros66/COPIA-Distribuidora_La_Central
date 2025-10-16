namespace Distribuidora_La_Central.Shared.Models
{
    public class Producto
    {
        public int codigoProducto { get; set; }
        public string descripcion { get; set; }
        public int cantidad { get; set; }
        public string categoria { get; set; }
        public decimal descuento { get; set; }
        public decimal costo { get; set; }
        //public int items { get; set; }
        public string bodega { get; set; }
        public int idProveedor { get; set; }
    }
}