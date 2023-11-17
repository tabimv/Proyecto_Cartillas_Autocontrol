﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System.Data.SqlClient;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class CartillaController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();

        // GET: Cartilla
        public async Task<ActionResult> Index()
        {
            var cARTILLA = db.CARTILLA.Include(c => c.ACTIVIDAD).Include(c => c.ESTADO_FINAL).Include(c => c.OBRA);
            return View(await cARTILLA.ToListAsync());
        }


        public ActionResult CrearCartilla()
        {
            CartillasViewModel viewModel = new CartillasViewModel();
            viewModel.DetalleCartillas = new List<DETALLE_CARTILLA>();
            // Puedes agregar instancias de DETALLE_CARTILLA según sea necesario
            viewModel.DetalleCartillas.Add(new DETALLE_CARTILLA());


            // Realiza una consulta a tu base de datos para obtener el valor deseado
            using (var dbContext = new ObraManzanoNoviembre())  // Reemplaza 'TuDbContext' con el nombre de tu contexto de base de datos
            {
                // Supongamos que tienes una entidad llamada Configuracion con una propiedad ItemVerifId
                viewModel.ActividadesList = dbContext.ACTIVIDAD.ToList();
                viewModel.ElementosVerificacion = dbContext.ITEM_VERIF.ToList();
                viewModel.InmuebleList = dbContext.INMUEBLE.ToList();
                viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();


            }

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult CrearCartilla(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var dbContext = new ObraManzanoNoviembre())  // Reemplaza 'TuDbContext' con el nombre de tu contexto de base de datos
                    {
                        // Guardar la CARTILLA
                        dbContext.CARTILLA.Add(viewModel.Cartilla);
                        dbContext.SaveChanges();

                        // Asignar el ID de la CARTILLA a cada DETALLE_CARTILLA
                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            detalleCartilla.CARTILLA_cartilla_id = viewModel.Cartilla.cartilla_id;
                            dbContext.DETALLE_CARTILLA.Add(detalleCartilla);
                            detalleCartilla.estado_ito = false;
                            detalleCartilla.estado_otec = false;
                        }

                        // Guardar los cambios en DETALLE_CARTILLA
                        dbContext.SaveChanges();
                    }

                    // Redirigir a la página de índice u otra acción
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Manejar cualquier excepción que pueda ocurrir al guardar en la base de datos
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            // Si el modelo no es válido, vuelve a la vista con los errores
            return View(viewModel);
        }

        public ActionResult GetObraByActividadId(int actividadId)
        {
            try
            {
                // Buscar la actividad por su ID en la base de datos
                ACTIVIDAD actividad = db.ACTIVIDAD.FirstOrDefault(a => a.actividad_id == actividadId);

                if (actividad != null)
                {
                    // Si se encuentra la actividad, obtén la obra asociada a ella
                    OBRA obra = db.OBRA.FirstOrDefault(o => o.obra_id == actividad.OBRA_obra_id);

                    if (obra != null)
                    {
                        // Devuelve los detalles de la obra en formato JSON
                        return Json(new { obraId = obra.obra_id, nombreObra = obra.nombre_obra }, JsonRequestBehavior.AllowGet);
                    }
                }

                // En caso de no encontrar la actividad u obra, devuelve un error o un valor por defecto
                return Json(new { error = "No se encontró la obra asociada a esta actividad" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error que pueda surgir
                return Json(new { error = "Ocurrió un error al obtener la obra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetElementosVerificacionByActividad(int actividadId)
        {
            // Realiza la lógica para obtener los elementos de verificación por la actividad seleccionada
            var elementos = db.ITEM_VERIF.Where(iv => iv.ACTIVIDAD_actividad_id == actividadId).ToList();

            // Devuelve los elementos de verificación en formato JSON
            var jsonData = elementos.Select(e => new { value = e.item_verif_id, text = e.elemento_verificacion }).ToList();
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }


        public ActionResult GetInmuebleByObra(int obraID)
        {
            // Realiza la lógica para obtener los elementos de verificación por la actividad seleccionada
            var elementos = db.INMUEBLE.Where(iv => iv.OBRA_obra_id == obraID).ToList();

            // Devuelve los elementos de verificación en formato JSON
            var jsonData = elementos.Select(i => new { value = i.inmueble_id, text = i.inmueble_id }).ToList();
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCombinacionesElementosInmuebles()
        {
            List<dynamic> result = new List<dynamic>();

            using (var context = new ObraManzanoNoviembre()) // Reemplaza 'TuContextoDeBaseDeDatos' con tu contexto de Entity Framework
            {
                var query = from iv in context.ITEM_VERIF
                            from i in context.INMUEBLE
                            select new
                            {
                                iv.item_verif_id,
                                iv.elemento_verificacion,
                                iv.label,
                                i.inmueble_id,
                                i.tipo_inmueble
                            };

                result = query.ToList<dynamic>();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
