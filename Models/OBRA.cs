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
    public partial class OBRA
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OBRA()
        {
            this.ACCESO_OBRAS = new HashSet<ACCESO_OBRAS>();
            this.ACTIVIDAD = new HashSet<ACTIVIDAD>();
            this.CARTILLA = new HashSet<CARTILLA>();
            this.LOTE_INMUEBLE = new HashSet<LOTE_INMUEBLE>();
            this.RESPONSABLE = new HashSet<RESPONSABLE>();
        }
    
        public int obra_id { get; set; }

        [Required(ErrorMessage = "Por favor, ingrese el nombre de la obra")]
        public string nombre_obra { get; set; }

        [Required(ErrorMessage = "Por favor, ingrese la dirección")]
        public string direccion { get; set; }
        public int COMUNA_comuna_id { get; set; }
        public string tipo_proyecto { get; set; }
        public Nullable<decimal> total_deptos { get; set; }
        public Nullable<decimal> total_viv { get; set; }
        public string entidad_patrocinante { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ACCESO_OBRAS> ACCESO_OBRAS { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ACTIVIDAD> ACTIVIDAD { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CARTILLA> CARTILLA { get; set; }
        public virtual COMUNA COMUNA { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<LOTE_INMUEBLE> LOTE_INMUEBLE { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RESPONSABLE> RESPONSABLE { get; set; }
    }
}
