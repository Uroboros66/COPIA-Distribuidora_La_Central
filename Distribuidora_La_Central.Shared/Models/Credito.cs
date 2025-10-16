namespace Distribuidora_La_Central.Shared.Models
{
    public class Credito
    {
        public int idCredito { get; set; }
        public int codigoFactura { get; set; }
        public DateTime fechaInicial { get; set; }
        public DateTime fechaFinal { get; set; }
        public decimal saldoMaximo { get; set; }
        public decimal saldoActual { get; set; } // Agregar este campo
        public string estado { get; set; }
    }
}
