//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace barApp
{
    using System;
    using System.Collections.Generic;
    
    public partial class Usuario
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Usuario()
        {
            this.Venta = new HashSet<Venta>();
            this.Creditos = new HashSet<Creditos>();
        }
    
        public int idUsuario { get; set; }
        public string nombre { get; set; }
        public Nullable<int> idRol { get; set; }
        public Nullable<bool> activo { get; set; }
        public string contrasena { get; set; }
        public Nullable<bool> resetContrasena { get; set; }
        public string idTarjeta { get; set; }
        public Nullable<bool> EnvioCorreo { get; set; }
        public string Correo { get; set; }
    
        public virtual Roles Roles { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Venta> Venta { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Creditos> Creditos { get; set; }
    }
}
