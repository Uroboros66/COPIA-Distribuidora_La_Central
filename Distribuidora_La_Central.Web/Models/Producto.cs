namespace Distribuidora_La_Central.Web.Models
{
    public class Producto
    {
        public int codigoProducto { get; set; }
        public string descripcion { get; set; }
        public int cantidad { get; set; }
        public int idCategoria { get; set; }
        public decimal descuento { get; set; }
        public decimal costo { get; set; }
        public int items { get; set; }
        public int idBodega { get; set; }
        public int idProveedor { get; set; }

        // Nuevas propiedades
        public string unidadMedida { get; set; }    // NUEVO
        public decimal margenGanancia { get; set; }  // NUEVO

    }
}