using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Models.ViewModels
{
    public class CartillaDetalleViewModel
    {
        public DateTime Fecha { get; set; }
        public string Observaciones { get; set; }
        public int ObraId { get; set; }
        public int ActividadId { get; set; }
        public int EstadoFinalId { get; set; }
        public List<int> ItemVerifIds { get; set; }

        // Otras propiedades necesarias para la vista

        public List<ItemVerifDetalleViewModel> Detalles { get; set; }
    }

    public class ItemVerifDetalleViewModel
    {
        public int ItemVerifId { get; set; }
        public bool EstadoIto { get; set; }
        public bool EstadoOtec { get; set; }

        // Otras propiedades necesarias para los detalles
    }
}