//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace barApp
{
    using System;
    using System.Collections.Generic;
    
    public partial class Factura
    {
        public decimal numFactura { get; set; }
        public System.DateTime fecha { get; set; }
        public float IVA { get; set; }
        public float total { get; set; }
        public int numPago { get; set; }
        public Nullable<decimal> descuento { get; set; }
        public Nullable<int> idCuadre { get; set; }
    
        public virtual Cuadre Cuadre { get; set; }
        public virtual ModoPago ModoPago { get; set; }
    }
}
