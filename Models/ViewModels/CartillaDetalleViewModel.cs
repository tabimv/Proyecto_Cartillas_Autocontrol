using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Models.ViewModels
{
    public class CartillaDetalleViewModel
    {
        public int ActividadId { get; set; }
        public List<CARTILLA> Cartillas { get; set; }
        public List<DETALLE_CARTILLA> DetallesCartilla { get; set; }

    }
}