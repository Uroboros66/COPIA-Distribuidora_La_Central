using Microsoft.VisualBasic;

namespace Distribuidora_La_Central.Web.Models
{
    public class Compra
    {
        public int idCompra { get; set; }
        public int idProveedor { get; set; }
        public DateTime fechaCompra { get; set; } = DateTime.Now;
        public decimal TotalCompra { get; set; }
        public string Estado { get; set; } = "Pendiente";

        public DateTime? FechaPago { get; set; }
        public string MetodoPago { get; set; } = string.Empty;






        public List<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();


    }
}