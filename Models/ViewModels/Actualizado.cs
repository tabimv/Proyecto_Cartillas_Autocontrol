using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Proyecto_Cartilla_Autocontrol.Models.ViewModels
{
        public class CartillaActualizada
        {
            public int CartillaId { get; set; }
            public DateTime Fecha { get; set; }
            public string Observaciones { get; set; }
            public string ObservacionesPriv { get; set; }
            public string RutaPdf { get; set; }
            public int ObraId { get; set; }
            public int ActividadId { get; set; }
            public int EstadoFinalId { get; set; }
        }

        // Modelo de la entidad DetalleCartilla
        public class DetalleCartillaActualizada
        {
            public int DetalleCartillaId { get; set; }
            public bool EstadoOtec { get; set; }
            public bool EstadoIto { get; set; }
            public bool EstadoSupv { get; set; }
            public DateTime FechaRev { get; set; }
            public int ItemVerifId { get; set; }
            public int CartillaId { get; set; }
            public int InmuebleId { get; set; }
        }

        // Modelo de vista para el formulario de creación
        public class CartillaViewModelActualizada
        {
            public CARTILLA Cartilla { get; set; }
            public List<SelectListItem> Obras { get; set; }
            public List<SelectListItem> Actividades { get; set; }
            public List<SelectListItem> Lotes { get; set; }
            public List<SelectListItem> Inmuebles { get; set; }
            public List<SelectListItem> ItemsVerif { get; set; }
        }
    
}