﻿using System;
using System.Collections.Generic;
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
    }

    public class CartillaViewModel
    {
        public int CartillaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Observaciones { get; set; }
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
}