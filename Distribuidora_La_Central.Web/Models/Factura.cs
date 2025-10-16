namespace Distribuidora_La_Central.Web.Models
{
    public class Factura
    {
        public int codigoFactura { get; set; }
        public int codigoCliente { get; set; }
         //public string nombreFactura { get; set; }
        public DateTime fecha { get; set; }
        public decimal totalFactura { get; set; }
        public decimal saldo { get; set; }
        public string tipo { get; set; }
        public string estado { get; set; }

    }
}
