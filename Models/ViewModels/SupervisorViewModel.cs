using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Models.ViewModels
{
    public class SupervisorViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; }
        public bool TieneAcceso { get; set; }  
    }

    public class CartillaMovilViewModel
    {
        public CARTILLA Cartilla { get; set; }
        public List<DETALLE_CARTILLA> DetalleCartillas { get; set; }
    }

    public class LoteDto
    {
        public int LoteId { get; set; }
        public string NombreLote { get; set; }
    }



}