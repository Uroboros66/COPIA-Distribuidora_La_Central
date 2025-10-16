﻿namespace Distribuidora_La_Central.Web.Models
{
    public class Abono
    {
        public int idAbono { get; set; }
        public int codigoFactura { get; set; }
        public decimal montoAbono { get; set; }
        public DateTime fechaAbono { get; set; }
    }
}