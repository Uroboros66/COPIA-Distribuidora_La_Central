namespace Distribuidora_La_Central.Web.Models
{
    public class Traslado
    {
        public int idTraslado { get; set; }
        public int codigoProducto { get; set; }
        public int idBodegaOrigen { get; set; }
        public int idBodegaDestino { get; set; }
        public int cantidad { get; set; }
        public DateTime? fechaTraslado { get; set; }
        public int realizadoPor { get; set; }
        public string estado { get; set; }
    }
}