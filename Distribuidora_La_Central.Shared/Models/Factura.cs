namespace Distribuidora_La_Central.Shared.Models
{
    public class Factura
    {
        public int codigoFactura { get; set; }
        public int codigoCliente { get; set; }
        public DateTime fecha { get; set; } =
        DateTime.Now;
        public decimal totalFactura { get; set; }
        public decimal saldo { get; set; }
        public string tipo { get; set; }
        public string estado { get; set; }

        public Cliente Cliente { get; set; }

        // O si prefieres no tener el objeto completo, solo el nombre:
        public string ClienteNombre { get; set; }
    }


}