using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace barApp.Models
{
    public class Inventario
    {
        public string IdProducto { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
    }
}