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

    public partial class RESPONSABLE
    {
        public int responsable_id { get; set; }

        [Required(ErrorMessage = "Por favor, seleccione un cargo")]
        public string cargo { get; set; }

        [Required(ErrorMessage = "Por favor, seleccione una obra")]
        public int OBRA_obra_id { get; set; }

        [Required(ErrorMessage = "Por favor, seleccione una persona ")]
        public string PERSONA_rut { get; set; }
    
        public virtual OBRA OBRA { get; set; }
        public virtual PERSONA PERSONA { get; set; }
    }
}
