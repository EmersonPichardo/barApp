namespace barApp.Models
{
    public class RequestVenta : Venta
    {
        public _Factura[] Factura { get; set; }

        public class _Factura : Factura
        {
            public int idUsuario { get; set; }
        }
    }
}