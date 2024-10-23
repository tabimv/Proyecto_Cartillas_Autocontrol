using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Models.ViewModels
{
    //public class CartillaDetalleViewModel
    //{
    //    public int ActividadId { get; set; }
    //    public List<CARTILLA> Cartillas { get; set; }
    //    public List<DETALLE_CARTILLA> DetallesCartilla { get; set; }

    //}

    public class CartillasViewModel
    {
        public CARTILLA Cartilla { get; set; }

        public List<DETALLE_CARTILLA> DetalleCartillas { get; set; }
        public List<ACTIVIDAD> ActividadesList { get; set; }
        public List<ITEM_VERIF> ElementosVerificacion { get; set; }
        public List<INMUEBLE> InmuebleList { get; set; }
        public List<OBRA> ObraList { get; set; }
        public List<ESTADO_FINAL> EstadoFinalList { get; set; }
        public List<LOTE_INMUEBLE> LoteInmuebleList { get; set; }


        // Propiedad para almacenar el ID de la obra seleccionada
        public int SelectedObraId { get; set; }
        public int SelectedLoteInmuebleId { get; set; }
        // Propiedades para manejar los campos booleanos en las vistas

        // Propiedad para almacenar la abreviatura del lote de inmueble seleccionado
        public string SelectedLoteInmuebleAbreviatura { get; set; }
        public bool EstadoIto { get; set; }
        public bool EstadoOtec { get; set; }

        public int LoteId { get; set; }
        public int InmuebleId { get; set; }

    }

   
    public class CartillaViewModel
    {
        public int CartillaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Observaciones { get; set; }

        public string Observaciones_priv { get; set; }
        public int ObraId { get; set; }
        public int ActividadId { get; set; }
        public int EstadoFinalId { get; set; }

        public List<DetalleCartillaViewModel> Detalles { get; set; }
    }

    public class DetalleCartillaViewModel
    {
        public int CARTILLACartillaId { get; set; }
        public int DetalleCartillaId { get; set; }
        public bool EstadoOtec { get; set; }
        public bool EstadoIto { get; set; }
        public int ItemVerifId { get; set; }
        public string InmuebleId { get; set; }
    }


    public class CreateCartillaViewModel
    {
      
        [Display(Name = "Inmueble ID")]
        public string InmuebleId { get; set; }
    }


    public class CrearCartillaViewModel
    {
        public IEnumerable<OBRA> Obras { get; set; }
        public IEnumerable<ACTIVIDAD> Actividades { get; set; }
    }

  

}