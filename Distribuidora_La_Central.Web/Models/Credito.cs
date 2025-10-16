namespace Distribuidora_La_Central.Web.Models
{
    public class Credito
    {
        public int idCredito { get; set; }
        public int codigoFactura { get; set; }
        public DateTime fechaInicial { get; set; }
        public DateTime fechaFinal { get; set; }
        public decimal saldoMaximo { get; set; }
        public string estado { get; set; }
    }
}