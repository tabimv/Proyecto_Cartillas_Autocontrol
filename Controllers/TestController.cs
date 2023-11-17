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
using System.Data.Entity.Infrastructure;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class TestController : Controller
    {
        private ObraManzanoNoviembre db = new ObraManzanoNoviembre();
        // GET: Test
        public ActionResult Index()
        {
            List<CartillaViewModel> model = ObtenerDatosDesdeBaseDeDatos();
            return View(model);
        }


        private List<CartillaViewModel> ObtenerDatosDesdeBaseDeDatos()
        {
            // Lógica para obtener datos desde la base de datos y llenar el ViewModel
            // Asegúrate de agrupar por cartilla_id y mapear los datos correspondientes
            // a las propiedades del ViewModel.

            // Ejemplo simplificado:
            List<CartillaViewModel> cartillas = db.CARTILLA_DETALLE_VIEW
             .GroupBy(c => c.cartilla_id)
             .Select(group => new CartillaViewModel
             {
                 CartillaId = group.Key,
                 Fecha = group.FirstOrDefault().fecha,
                 Observaciones = group.FirstOrDefault().observaciones,
                 ObraId = group.FirstOrDefault().OBRA_obra_id,
                 ActividadId = group.FirstOrDefault().ACTIVIDAD_actividad_id,
                 EstadoFinalId = group.FirstOrDefault().ESTADO_FINAL_estado_final_id,
                 Detalles = group.Select(d => new DetalleCartillaViewModel
                 {
                     CARTILLACartillaId = d.CARTILLA_cartilla_id ?? 0,
                     DetalleCartillaId = d.detalle_cartilla_id ?? 0,
                     EstadoOtec = d.estado_otec ?? false,
                     EstadoIto = d.estado_ito ?? false,
                     ItemVerifId = d.ITEM_VERIF_item_verif_id ?? 0,
                     InmuebleId = d.INMUEBLE_inmueble_id ?? ""
                 }).ToList()
             })
             .ToList();

            return cartillas;
        }

        public void CrearNuevaCartilla(CartillaViewModel viewModel)
        {
            using (var dbContext = new ObraManzanoNoviembre())  // Reemplaza 'TuDbContext' con el nombre de tu contexto de base de datos
            {
                // Llamada al procedimiento almacenado mediante Entity Framework
                dbContext.Database.ExecuteSqlCommand(
                    "CrearNuevaCartilla @fecha, @observaciones, @OBRA_obra_id, @ACTIVIDAD_actividad_id, " +
                    "@ESTADO_FINAL_estado_final_id, @estado_otec, @estado_ito, @ITEM_VERIF_item_verif_id, @INMUEBLE_inmueble_id",
                    new SqlParameter("@fecha", viewModel.Fecha),
                    new SqlParameter("@observaciones", viewModel.Observaciones),
                    new SqlParameter("@OBRA_obra_id", viewModel.ObraId),
                    new SqlParameter("@ACTIVIDAD_actividad_id", viewModel.ActividadId),
                    new SqlParameter("@ESTADO_FINAL_estado_final_id", viewModel.EstadoFinalId),
                    new SqlParameter("@estado_otec", viewModel.Detalles[0].EstadoOtec),  // Suponiendo que tienes al menos un detalle en el ViewModel
                    new SqlParameter("@estado_ito", viewModel.Detalles[0].EstadoIto),
                    new SqlParameter("@ITEM_VERIF_item_verif_id", viewModel.Detalles[0].ItemVerifId),
                    new SqlParameter("@INMUEBLE_inmueble_id", viewModel.Detalles[0].InmuebleId)
                );
            }
        }

        public ActionResult Create()
        {
            // Puedes personalizar esta lógica según tus necesidades
            var viewModel = new CartillaViewModel
            {
                // Inicializa propiedades según sea necesario
            };

            return View(viewModel);
        }


        [HttpPost]
        public ActionResult Create(CartillaViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Guardar la nueva cartilla
                var nuevaCartilla = new CARTILLA
                {
                    fecha = viewModel.Fecha,
                    observaciones = viewModel.Observaciones,
                    OBRA_obra_id = viewModel.ObraId,
                    ACTIVIDAD_actividad_id = viewModel.ActividadId,
                    ESTADO_FINAL_estado_final_id = viewModel.EstadoFinalId
                };

                db.CARTILLA.Add(nuevaCartilla);
                db.SaveChanges();

                // Obtener el ID de la nueva cartilla
                int nuevaCartillaId = nuevaCartilla.cartilla_id;

                // Cargar los detalles asociados a la nueva cartilla
                viewModel.Detalles = db.DETALLE_CARTILLA
                    .Where(d => d.CARTILLA_cartilla_id == nuevaCartillaId)
                    .Select(detalle => new DetalleCartillaViewModel
                    {
                        DetalleCartillaId = detalle.detalle_cartilla_id,
                        EstadoOtec = detalle.estado_otec,
                        EstadoIto = detalle.estado_ito,
                        ItemVerifId = detalle.ITEM_VERIF_item_verif_id,
                        InmuebleId = detalle.INMUEBLE_inmueble_id
                    })
                    .ToList();

                // Guardar los detalles asociados a la nueva cartilla (si es necesario)
                if (viewModel.Detalles != null && viewModel.Detalles.Any())
                {
                    foreach (var detalle in viewModel.Detalles)
                    {
                        var nuevoDetalle = new DETALLE_CARTILLA
                        {
                            estado_otec = detalle.EstadoOtec,
                            estado_ito = detalle.EstadoIto,
                            ITEM_VERIF_item_verif_id = detalle.ItemVerifId,
                            INMUEBLE_inmueble_id = detalle.InmuebleId,
                            CARTILLA_cartilla_id = nuevaCartillaId
                        };

                        db.DETALLE_CARTILLA.Add(nuevoDetalle);
                    }

                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            // Si hay errores de validación, cargar los detalles actuales y regresar a la vista con el ViewModel para mostrar los mensajes de error
            viewModel.Detalles = ObtenerDetallesActuales(viewModel.CartillaId);
            return View(viewModel);
        }

        private List<DetalleCartillaViewModel> ObtenerDetallesActuales(int cartillaId)
        {
            return db.DETALLE_CARTILLA
                .Where(d => d.CARTILLA_cartilla_id == cartillaId)
                .Select(detalle => new DetalleCartillaViewModel
                {
                    DetalleCartillaId = detalle.detalle_cartilla_id,
                    EstadoOtec = detalle.estado_otec,
                    EstadoIto = detalle.estado_ito,
                    ItemVerifId = detalle.ITEM_VERIF_item_verif_id,
                    InmuebleId = detalle.INMUEBLE_inmueble_id
                })
                .ToList();
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
            var jsonData = elementos.Select(i => new { value = i.inmueble_id, text = i.inmueble_id}).ToList();
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }






        public ActionResult EditarCartilla(int id)
        {
            // Obtener la Cartilla que se quiere editar por su ID
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);

            if (cartilla != null)
            {
                CartillasViewModel viewModel = new CartillasViewModel();
                viewModel.Cartilla = cartilla;

                // Obtener los detalles de la Cartilla que se quiere editar
                viewModel.DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList();

                // Obtener otras listas necesarias para el formulario (actividades, elementos de verificación, inmuebles, etc.)
                viewModel.ActividadesList = db.ACTIVIDAD.ToList();
                viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
                viewModel.InmuebleList = db.INMUEBLE.ToList();
                viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();

                return View(viewModel);
            }

            // Manejar si no se encuentra la Cartilla con el ID proporcionado
            return RedirectToAction("Index", "Cartilla"); // O alguna otra acción adecuada
        }

        [HttpPost]
        public ActionResult EditarCartilla(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var dbContext = new ObraManzanoNoviembre())
                    {
                        // Actualizar la información de la Cartilla en la base de datos
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;

                        // Actualizar o agregar los detalles de la Cartilla en la base de datos
                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            if (detalleCartilla.detalle_cartilla_id == 0)
                            {
                                // Nuevo detalle de Cartilla, agregarlo
                                detalleCartilla.CARTILLA_cartilla_id = viewModel.Cartilla.cartilla_id;
                                dbContext.DETALLE_CARTILLA.Add(detalleCartilla);
                            }
                            else
                            {
                                // Detalle de Cartilla existente, marcar como modificado
                                dbContext.Entry(detalleCartilla).State = EntityState.Modified;
                            }
                        }

                        // Eliminar detalles de la Cartilla que se hayan quitado en la edición
                        foreach (var detalle in dbContext.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id))
                        {
                            if (!viewModel.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                            {
                                dbContext.DETALLE_CARTILLA.Remove(detalle);
                            }
                        }

                        // Guardar los cambios en la base de datos
                        dbContext.SaveChanges();

                        return RedirectToAction("Index", "Cartilla");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            return View(viewModel);
        }


        // Recuerda implementar Dispose para liberar recursos del DbContext
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
