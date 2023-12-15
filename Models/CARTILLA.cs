//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Proyecto_Cartilla_Autocontrol.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public partial class CARTILLA
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CARTILLA()
        {
            this.DETALLE_CARTILLA = new HashSet<DETALLE_CARTILLA>();
        }
    
        public int cartilla_id { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        public System.DateTime fecha { get; set; }
        public string observaciones { get; set; }
        public int OBRA_obra_id { get; set; }
        public int ACTIVIDAD_actividad_id { get; set; }
        public int ESTADO_FINAL_estado_final_id { get; set; }
    
        public virtual ACTIVIDAD ACTIVIDAD { get; set; }
        public virtual ESTADO_FINAL ESTADO_FINAL { get; set; }
        public virtual OBRA OBRA { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DETALLE_CARTILLA> DETALLE_CARTILLA { get; set; }
    }
}
