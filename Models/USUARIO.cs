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

    public partial class USUARIO
    {
        public int usuario_id { get; set; }

        [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
        public string contraseña { get; set; }
        public int PERFIL_perfil_id { get; set; }
        public int OBRA_obra_id { get; set; }
        public string PERSONA_rut { get; set; }
    
        public virtual OBRA OBRA { get; set; }
        public virtual PERFIL PERFIL { get; set; }
        public virtual PERSONA PERSONA { get; set; }
    }
}
