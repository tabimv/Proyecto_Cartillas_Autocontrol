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
    
    public partial class DETALLE_CARTILLA
    {
        public int detalle_cartilla_id { get; set; }
        public bool estado_otec { get; set; }
        public bool estado_ito { get; set; }
        public bool estado_supv { get; set; }
        public int ITEM_VERIF_item_verif_id { get; set; }
        public int CARTILLA_cartilla_id { get; set; }
        public int INMUEBLE_inmueble_id { get; set; }
    
        public virtual CARTILLA CARTILLA { get; set; }
        public virtual INMUEBLE INMUEBLE { get; set; }
        public virtual ITEM_VERIF ITEM_VERIF { get; set; }
    }
}
