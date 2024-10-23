using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System.Data;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;



namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class CartillaActualizadaController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

      

        public ActionResult CrearCartilla()
        {
            CartillasViewModel viewModel = new CartillasViewModel();
            viewModel.DetalleCartillas = new List<DETALLE_CARTILLA>();
            // Puedes agregar instancias de DETALLE_CARTILLA según sea necesario
            viewModel.DetalleCartillas.Add(new DETALLE_CARTILLA());


            // Realiza una consulta a tu base de datos para obtener el valor deseado
            using (var dbContext = new ObraManzanoFinal())  // Reemplaza con el nombre de tu contexto de base de datos
            {
                // Supongamos que tienes una entidad llamada Configuracion con una propiedad ItemVerifId
                viewModel.ActividadesList = dbContext.ACTIVIDAD.Where(a => a.estado == "A").ToList();
                viewModel.ElementosVerificacion = dbContext.ITEM_VERIF.ToList();
                viewModel.InmuebleList = dbContext.INMUEBLE.ToList();
                viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();
                viewModel.ObraList = dbContext.OBRA
              .Where(obra => obra.nombre_obra != "Oficina Central" && obra.ACTIVIDAD.Any())
              .ToList();
                viewModel.LoteInmuebleList = dbContext.LOTE_INMUEBLE.ToList();


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
                    using (var dbContext = new ObraManzanoFinal())  // Reemplaza 'TuDbContext' con el nombre de tu contexto de base de datos
                    {
                        // Verificar si ya existe una cartilla con las mismas combinaciones de FK y un estado final diferente de 1
                        bool existeCartilla = dbContext.CARTILLA.Any(c =>
                            c.OBRA_obra_id == viewModel.Cartilla.OBRA_obra_id &&
                            c.ACTIVIDAD_actividad_id == viewModel.Cartilla.ACTIVIDAD_actividad_id &&
                            c.ESTADO_FINAL_estado_final_id == 1);

                        if (existeCartilla)
                        {
                            ModelState.AddModelError("", "Ya existe una misma Cartilla con un Estado Final de Visto Bueno.");

                            // Recargar las listas necesarias para volver a mostrar la vista con los datos
                            viewModel.ActividadesList = dbContext.ACTIVIDAD.Where(a => a.estado == "A").ToList();
                            viewModel.ElementosVerificacion = dbContext.ITEM_VERIF.ToList();
                            viewModel.InmuebleList = dbContext.INMUEBLE.ToList();
                            viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();
                            viewModel.ObraList = dbContext.OBRA
               .Where(obra => obra.nombre_obra != "Oficina Central" && obra.ACTIVIDAD.Any())
               .ToList();
                            viewModel.LoteInmuebleList = dbContext.LOTE_INMUEBLE.ToList();


                            return View(viewModel);
                        }

                        // Guardar la CARTILLA
                        viewModel.Cartilla.fecha = DateTime.Now;
                        dbContext.CARTILLA.Add(viewModel.Cartilla);
                        dbContext.SaveChanges();

                        // Asignar el ID de la CARTILLA a cada DETALLE_CARTILLA
                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            detalleCartilla.CARTILLA_cartilla_id = viewModel.Cartilla.cartilla_id;
                            dbContext.DETALLE_CARTILLA.Add(detalleCartilla);
                            detalleCartilla.estado_supv = null;
                            detalleCartilla.estado_ito = null;
                            detalleCartilla.estado_autocontrol = null;
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

        public ActionResult GetActividadByObra(int obraId)
        {
            try
            {
                // Buscar la actividad por su ID en la base de datos
                OBRA obra = db.OBRA.FirstOrDefault(a => a.obra_id == obraId);

                if (obra != null)
                {
                    // Si se encuentra la actividad, obtén la obra asociada a ella
                    ACTIVIDAD actividad = db.ACTIVIDAD.FirstOrDefault(o => o.OBRA_obra_id == obra.obra_id);

                    if (actividad != null)
                    {
                        // Devuelve los detalles de la obra en formato JSON
                        return Json(new { actividadId = actividad.actividad_id, nombreActividad = actividad.nombre_actividad }, JsonRequestBehavior.AllowGet);
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

        public ActionResult ConsultaRevision(int id)
        {
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);
            if (cartilla != null)
            {
                var viewModel = new CartillasViewModel
                {
                    Cartilla = cartilla,
                    DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList(),
                    ActividadesList = db.ACTIVIDAD.ToList(),
                    ElementosVerificacion = db.ITEM_VERIF.ToList(),
                    InmuebleList = db.INMUEBLE.ToList(),
                    EstadoFinalList = db.ESTADO_FINAL.ToList()
                };
                int obraId = cartilla.OBRA_obra_id;
                ViewBag.LoteList = new SelectList(
                    db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id)))
                                    .Select(l => new { l.lote_id, l.abreviatura }).ToList(),
                    "lote_id", "abreviatura"
                );

                ViewBag.InmueblePorLote = new SelectList(
                    db.INMUEBLE.Where(i => i.LOTE_INMUEBLE.OBRA_obra_id == obraId && i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id))
                               .Select(i => new { i.inmueble_id, i.codigo_inmueble }).ToList(),
                    "inmueble_id", "codigo_inmueble"
                );
                return View(viewModel);
            }
            return RedirectToAction("Index", "Cartilla");
        }


        [HttpPost]
        public ActionResult ConsultaRevision(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
          
            // Si llegamos aquí, hay un error, retornamos a la vista con el ViewModel
            ViewBag.LoteList = new SelectList(db.LOTE_INMUEBLE.ToList(), "lote_id", "abreviatura");
            ViewBag.InmueblePorLote = new SelectList(db.INMUEBLE.ToList(), "inmueble_id", "codigo_inmueble");
            viewModel.ActividadesList = db.ACTIVIDAD.ToList();
            viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
            viewModel.InmuebleList = db.INMUEBLE.ToList();
            viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();
            return View(viewModel);
        }


        public JsonResult GetCombinacionesElementosInmuebles(int actividadId)
        {
            try
            {
                using (var context = new ObraManzanoFinal())
                {
                    var query = from iv in context.ITEM_VERIF
                                join a in context.ACTIVIDAD on iv.ACTIVIDAD_actividad_id equals a.actividad_id
                                join o in context.OBRA on a.OBRA_obra_id equals o.obra_id
                                join li in context.LOTE_INMUEBLE on o.obra_id equals li.OBRA_obra_id
                                join i in context.INMUEBLE on li.lote_id equals i.LOTE_INMUEBLE_lote_id
                                where iv.ACTIVIDAD_actividad_id == actividadId
                                select new
                                {
                                    iv.item_verif_id,
                                    iv.elemento_verificacion,
                                    i.inmueble_id,
                                    i.codigo_inmueble
                                };

                    var result = query.ToList();
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener combinaciones: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditarCartillaMovil(int id)
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
                int obraId = cartilla.OBRA_obra_id;
                // Filtrar los lotes por la obra seleccionada
                var lotesPorObra = db.LOTE_INMUEBLE
                .Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id)))
                .ToList();
                // Filtrar los inmuebles por la obra seleccionada y la cartilla
                var inmueblesPorObra = db.INMUEBLE
                    .Where(l => l.LOTE_INMUEBLE.OBRA_obra_id == obraId && l.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id))
                    .ToList();
                // Crear el SelectList solo con los lotes filtrados
                ViewBag.LoteList = new SelectList(lotesPorObra, "lote_id", "abreviatura");
                ViewBag.InmueblePorLote = new SelectList(inmueblesPorObra, "inmueble_id", "codigo_inmueble");
                return View(viewModel);
            }
            // Manejar si no se encuentra la Cartilla con el ID proporcionado
            return RedirectToAction("Index", "Cartilla"); // O alguna otra acción adecuada
        }

        [HttpPost]
        public ActionResult EditarCartillaMovil(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var dbContext = new ObraManzanoFinal())
                    {
                        // Actualizar la información de la Cartilla en la base de datos
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;
                        // Verificar si el estado final es "Aprobado" (id 1)
                        if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 1)
                        {
                            // Verificar si algún campo de aprobación es null
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_autocontrol == null || detalle.estado_ito == null || detalle.estado_supv == null))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado a menos que todos sus campos hayan sido revisados";
                                return RedirectToAction("Index", "Cartilla");
                            }
                            // Verificar si al menos un campo de aprobación está en false
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_autocontrol == false || detalle.estado_ito == false || detalle.estado_supv == false))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado. Debido a que no todos los valores se encuentran aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }
                        // Verificar si el estado final es "Rechazado" (estado 2)
                        else if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 2)
                        {
                            // Verificar si algún campo de aprobación es null
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_autocontrol == null || detalle.estado_ito == null || detalle.estado_supv == null))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazado a menos que todos sus campos hayan sido revisados";
                                return RedirectToAction("Index", "Cartilla");
                            }
                            // Si todos los valores son verdaderos, redirigir con un mensaje de error
                            if (viewModel.DetalleCartillas.All(detalle => detalle.estado_autocontrol == true && detalle.estado_ito == true && detalle.estado_supv == true))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazada. Todos los valores se encuentran aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }
                        int obraId = viewModel.Cartilla.OBRA_obra_id;
                        var lotesPorObra = db.LOTE_INMUEBLE
                            .Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id)))
                            .ToList();
                        var inmueblesPorObra = db.INMUEBLE
                            .Where(l => l.LOTE_INMUEBLE.OBRA_obra_id == obraId && l.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id))
                            .ToList();
                        // Actualizar el SelectList de los lotes filtrados
                        ViewBag.LoteList = new SelectList(lotesPorObra, "lote_id", "abreviatura");
                        // Actualizar el SelectList de los inmuebles filtrados
                        ViewBag.InmueblePorLote = new SelectList(inmueblesPorObra, "inmueble_id", "codigo_inmueble");
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
                                // Detalle de Cartilla existente, comparar con los valores actuales de la base de datos
                                var dbDetalle = dbContext.DETALLE_CARTILLA.AsNoTracking().FirstOrDefault(d => d.detalle_cartilla_id == detalleCartilla.detalle_cartilla_id);

                                if (dbDetalle != null && (
                                    dbDetalle.estado_ito != detalleCartilla.estado_ito ||
                                    dbDetalle.estado_supv != detalleCartilla.estado_supv ||
                                    dbDetalle.estado_autocontrol != detalleCartilla.estado_autocontrol))
                                {
                                    // Si hay cambios, actualizar la fecha de revisión
                                    detalleCartilla.fecha_rev = DateTime.Now;
                                }
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

        public ActionResult EditarCartilla(int id)
        {
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);
            if (cartilla != null)
            {
                var viewModel = new CartillasViewModel
                {
                    Cartilla = cartilla,
                    DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList(),
                    ActividadesList = db.ACTIVIDAD.ToList(),
                    ElementosVerificacion = db.ITEM_VERIF.ToList(),
                    InmuebleList = db.INMUEBLE.ToList(),
                    EstadoFinalList = db.ESTADO_FINAL.ToList()
                };
                int obraId = cartilla.OBRA_obra_id;
                ViewBag.LoteList = new SelectList(
                    db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id)))
                                    .Select(l => new { l.lote_id, l.abreviatura }).ToList(),
                    "lote_id", "abreviatura"
                );

                ViewBag.InmueblePorLote = new SelectList(
                    db.INMUEBLE.Where(i => i.LOTE_INMUEBLE.OBRA_obra_id == obraId && i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id))
                               .Select(i => new { i.inmueble_id, i.codigo_inmueble }).ToList(),
                    "inmueble_id", "codigo_inmueble"
                );
                return View(viewModel);
            }
            return RedirectToAction("Index", "Cartilla");
        }


        [HttpPost]
        public ActionResult EditarCartilla(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var dbContext = new ObraManzanoFinal())
                    {
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;



                        // Validación para Estado Final igual a Aprobado
                        if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 1)
                        {
                            if (viewModel.DetalleCartillas.Any(detalle =>
                                detalle.estado_autocontrol == null ||
                                detalle.estado_ito == null ||
                                detalle.estado_supv == null ||
                                detalle.estado_autocontrol == false ||
                                detalle.estado_ito == false ||
                                detalle.estado_supv == false))
                            {
                                TempData["ErrorMessage"] =
                                    "La Cartilla no puede tener Estado Final igual a Aprobado a menos que todos sus campos hayan sido revisados y aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }
                        // Validación para Estado Final igual a Rechazado
                        else if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 2)
                        {
                            if (viewModel.DetalleCartillas.Any(detalle =>
                                detalle.estado_autocontrol == null ||
                                detalle.estado_ito == null ||
                                detalle.estado_supv == null))
                            {
                                TempData["ErrorMessage"] =
                                    "La Cartilla no puede tener Estado Final igual a Rechazado a menos que todos sus campos hayan sido revisados.";
                                return RedirectToAction("Index");
                            }

                            if (viewModel.DetalleCartillas.All(detalle =>
                                detalle.estado_autocontrol == true &&
                                detalle.estado_ito == true &&
                                detalle.estado_supv == true))
                            {
                                TempData["ErrorMessage"] =
                                    "La Cartilla no puede tener Estado Final igual a Rechazada. Todos los valores se encuentran aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }


                        var existingDetalles = dbContext.DETALLE_CARTILLA
                            .Where(d => d.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id)
                            .ToDictionary(d => d.detalle_cartilla_id, d => d);

                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            if (existingDetalles.TryGetValue(detalleCartilla.detalle_cartilla_id, out var existingDetalle))
                            {
                                // Verificar si estado_autocontrol ha cambiado de null a cualquier valor, y fecha_autocontrol es null
                                if ((existingDetalle.estado_autocontrol == null && detalleCartilla.estado_autocontrol.HasValue) ||
                                    (existingDetalle.estado_autocontrol.HasValue && detalleCartilla.estado_autocontrol != existingDetalle.estado_autocontrol))
                                {
                                    // Actualizar estado_autocontrol y establecer fecha_autocontrol si es null
                                    existingDetalle.estado_autocontrol = detalleCartilla.estado_autocontrol ?? false; // Conversión explícita
                                    if (detalleCartilla.estado_autocontrol.HasValue && detalleCartilla.fecha_autocontrol == null)
                                    {
                                        existingDetalle.fecha_autocontrol = DateTime.Now;
                                    }
                                    dbContext.Entry(existingDetalle).State = EntityState.Modified;
                                }


                                // Verificar si estado_ito ha cambiado de null a cualquier valor, y fecha_fto es null
                                if ((existingDetalle.estado_ito == null && detalleCartilla.estado_ito.HasValue) ||
                                    (existingDetalle.estado_ito.HasValue && detalleCartilla.estado_ito != existingDetalle.estado_ito))
                                {
                                    // Actualizar estado_ito y establecer fecha_fto si es null
                                    existingDetalle.estado_ito = detalleCartilla.estado_ito ?? false; // Conversión explícita
                                    if (detalleCartilla.estado_ito.HasValue && detalleCartilla.fecha_fto == null)
                                    {
                                        existingDetalle.fecha_fto = DateTime.Now;
                                    }
                                    dbContext.Entry(existingDetalle).State = EntityState.Modified;
                                }

                                // Verificar si estado_supv ha cambiado de null a cualquier valor, y fecha_supv es null
                                if ((existingDetalle.estado_supv == null && detalleCartilla.estado_supv.HasValue) ||
                                    (existingDetalle.estado_supv.HasValue && detalleCartilla.estado_supv != existingDetalle.estado_supv))
                                {
                                    // Actualizar estado_supv y establecer fecha_supv si es null
                                    existingDetalle.estado_supv = detalleCartilla.estado_supv ?? false; // Conversión explícita
                                    if (detalleCartilla.estado_supv.HasValue && detalleCartilla.fecha_supv == null)
                                    {
                                        existingDetalle.fecha_supv = DateTime.Now;
                                    }

                                    // Obtener el rut del usuario autenticado solo si estado_supv ha cambiado
                                    var rutSPV = ObtenerRutUsuarioAutenticado();
                                    if (!string.IsNullOrEmpty(rutSPV))
                                    {
                                        existingDetalle.rut_spv = rutSPV;
                                    }

                                    dbContext.Entry(existingDetalle).State = EntityState.Modified;
                                }
                            }
                            else
                            {
                                detalleCartilla.CARTILLA_cartilla_id = viewModel.Cartilla.cartilla_id;
                                detalleCartilla.fecha_autocontrol = detalleCartilla.estado_autocontrol.HasValue && detalleCartilla.estado_autocontrol.Value ? DateTime.Now : (DateTime?)null;
                                detalleCartilla.fecha_fto = detalleCartilla.estado_ito.HasValue && detalleCartilla.estado_ito.Value ? DateTime.Now : (DateTime?)null;
                                detalleCartilla.fecha_supv = detalleCartilla.estado_supv.HasValue && detalleCartilla.estado_supv.Value ? DateTime.Now : (DateTime?)null;
                                dbContext.DETALLE_CARTILLA.Add(detalleCartilla);
                            }
                        }

                        // Eliminar detalles que no están en la lista actual
                        var detallesParaEliminar = existingDetalles.Values
                            .Where(d => !viewModel.DetalleCartillas.Any(v => v.detalle_cartilla_id == d.detalle_cartilla_id))
                            .ToList();
                        foreach (var detalle in detallesParaEliminar)
                        {
                            dbContext.DETALLE_CARTILLA.Remove(detalle);
                        }
                        
                        dbContext.SaveChanges();
                        // Verificar si enviar_correo es true antes de enviar correos
                        var cartilla = dbContext.CARTILLA
                                                .FirstOrDefault(c => c.cartilla_id == viewModel.Cartilla.cartilla_id);

                        if (cartilla != null && cartilla.enviar_correo == true)
                        {
                            EnviarCorreoAutomaticamente();
                        }

                        return RedirectToAction("Index", "Cartilla");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            // Si llegamos aquí, hay un error, retornamos a la vista con el ViewModel
            ViewBag.LoteList = new SelectList(db.LOTE_INMUEBLE.ToList(), "lote_id", "abreviatura");
            ViewBag.InmueblePorLote = new SelectList(db.INMUEBLE.ToList(), "inmueble_id", "codigo_inmueble");
            viewModel.ActividadesList = db.ACTIVIDAD.ToList();
            viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
            viewModel.InmuebleList = db.INMUEBLE.ToList();
            viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();
            return View(viewModel);
        }




        [HttpPost]
        public JsonResult ActualizarRevisionDos(int loteId, int inmuebleId, bool revisionDos)
        {
            try
            {
                // Obtener los registros de detalle_cartilla correspondientes
                var detalles = db.DETALLE_CARTILLA
                    .Where(d => d.INMUEBLE.LOTE_INMUEBLE_lote_id == loteId && d.INMUEBLE.inmueble_id == inmuebleId)
                    .ToList();

                // Actualizar el campo REVISION_DOS de los registros
                foreach (var detalle in detalles)
                {
                    detalle.revision_dos = revisionDos;
                }

                // Guardar los cambios en la base de datos
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerEstadoRevisionDos(int loteId, int inmuebleId)
        {
            try
            {
                // Obtener el estado de revisión_dos para el lote e inmueble especificados
                bool revisionDos = db.DETALLE_CARTILLA
                    .Any(d => d.INMUEBLE.LOTE_INMUEBLE_lote_id == loteId && d.INMUEBLE.inmueble_id == inmuebleId && d.revision_dos);

                return Json(new { revisionDos = revisionDos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        public ActionResult EditarCartillas(int id)
        {
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);
            if (cartilla != null)
            {
                var viewModel = new CartillasViewModel
                {
                    Cartilla = cartilla,
                    DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList(),
                    ActividadesList = db.ACTIVIDAD.ToList(),
                    ElementosVerificacion = db.ITEM_VERIF.ToList(),
                    InmuebleList = db.INMUEBLE.ToList(),
                    EstadoFinalList = db.ESTADO_FINAL.ToList()
                };

                int obraId = cartilla.OBRA_obra_id;
                ViewBag.LoteList = new SelectList(
                    db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id)))
                                    .Select(l => new { l.lote_id, l.abreviatura }).ToList(),
                    "lote_id", "abreviatura"
                );

                ViewBag.InmueblePorLote = new SelectList(
                    db.INMUEBLE.Where(i => i.LOTE_INMUEBLE.OBRA_obra_id == obraId && i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id))
                               .Select(i => new { i.inmueble_id, i.codigo_inmueble }).ToList(),
                    "inmueble_id", "codigo_inmueble"
                );



                return View(viewModel);
            }
            return RedirectToAction("Index", "Cartilla");
        }

        [HttpPost]
        public async Task<ActionResult> EditarCartillas(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var dbContext = new ObraManzanoFinal())
                    {
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;

                        if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 1)
                        {
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_autocontrol == null ||
                                                                          detalle.estado_ito == null ||
                                                                          detalle.estado_supv == null ||
                                                                          detalle.estado_autocontrol == false ||
                                                                          detalle.estado_ito == false ||
                                                                          detalle.estado_supv == false))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado a menos que todos sus campos hayan sido revisados y aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }
                        else if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 2)
                        {
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_autocontrol == null ||
                                                                          detalle.estado_ito == null ||
                                                                          detalle.estado_supv == null))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazado a menos que todos sus campos hayan sido revisados.";
                                return RedirectToAction("Index", "Cartilla");
                            }

                            if (viewModel.DetalleCartillas.All(detalle => detalle.estado_autocontrol == true &&
                                                                          detalle.estado_ito == true &&
                                                                          detalle.estado_supv == true))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazada. Todos los valores se encuentran aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }

                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            if (detalleCartilla.detalle_cartilla_id == 0)
                            {
                                detalleCartilla.CARTILLA_cartilla_id = viewModel.Cartilla.cartilla_id;
                                dbContext.DETALLE_CARTILLA.Add(detalleCartilla);
                            }
                            else
                            {
                                var dbDetalle = await dbContext.DETALLE_CARTILLA
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(d => d.detalle_cartilla_id == detalleCartilla.detalle_cartilla_id);

                                if (dbDetalle != null)
                                {
                                    bool estadoCambiado = false;

                                    // Verificar si estado_autocontrol ha cambiado
                                    if (dbDetalle.estado_autocontrol != detalleCartilla.estado_autocontrol)
                                    {
                                        if (detalleCartilla.fecha_autocontrol == null)
                                        {
                                            detalleCartilla.fecha_autocontrol = DateTime.Now;
                                        }
                                        estadoCambiado = true;
                                    }
                                    else
                                    {
                                        // Mantener fecha_autocontrol existente si ya tiene valor
                                        detalleCartilla.fecha_autocontrol = dbDetalle.fecha_autocontrol;
                                    }

                                    // Verificar si estado_ito ha cambiado
                                    if (dbDetalle.estado_ito != detalleCartilla.estado_ito)
                                    {
                                        if (detalleCartilla.fecha_fto == null)
                                        {
                                            detalleCartilla.fecha_fto = DateTime.Now;
                                        }
                                        estadoCambiado = true;
                                    }
                                    else
                                    {
                                        // Mantener fecha_fto existente si ya tiene valor
                                        detalleCartilla.fecha_fto = dbDetalle.fecha_fto;
                                    }

                                    // Verificar si estado_supv ha cambiado
                                    if (dbDetalle.estado_supv != detalleCartilla.estado_supv)
                                    {
                                        if (detalleCartilla.fecha_supv == null)
                                        {
                                            detalleCartilla.fecha_supv = DateTime.Now;
                                        }
                                        estadoCambiado = true;

                                        // Obtener el rut del usuario autenticado solo si estado_supv ha cambiado
                                        var rutSPV = ObtenerRutUsuarioAutenticado();
                                        if (!string.IsNullOrEmpty(rutSPV))
                                        {
                                            detalleCartilla.rut_spv = rutSPV;
                                        }
                                    }
                                    else
                                    {
                                        // Mantener fecha_supv existente si ya tiene valor
                                        detalleCartilla.fecha_supv = dbDetalle.fecha_supv;
                                    }

                                    // Actualizar fecha_rev si ha cambiado algún estado
                                    if (estadoCambiado || dbDetalle.estado_ito != detalleCartilla.estado_ito || dbDetalle.estado_supv != detalleCartilla.estado_supv || dbDetalle.estado_autocontrol != detalleCartilla.estado_autocontrol)
                                    {
                                        detalleCartilla.fecha_rev = DateTime.Now;
                                        dbContext.Entry(detalleCartilla).State = EntityState.Modified;
                                    }
                                }
                            }
                        }

                        var detallesParaEliminar = await dbContext.DETALLE_CARTILLA
                            .Where(d => d.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id)
                            .ToListAsync();

                        foreach (var detalle in detallesParaEliminar)
                        {
                            if (!viewModel.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                            {
                                dbContext.DETALLE_CARTILLA.Remove(detalle);
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        EnviarCorreoAutomaticamente();
                        return RedirectToAction("Index", "Cartilla");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            int obraId = viewModel.Cartilla.OBRA_obra_id;
            ViewBag.LoteList = new SelectList(
                db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id)))
                                .Select(l => new { l.lote_id, l.abreviatura }).ToList(),
                "lote_id", "abreviatura"
            );

            ViewBag.InmueblePorLote = new SelectList(
                db.INMUEBLE.Where(i => i.LOTE_INMUEBLE.OBRA_obra_id == obraId && i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id))
                           .Select(i => new { i.inmueble_id, i.codigo_inmueble }).ToList(),
                "inmueble_id", "codigo_inmueble"
            );
            viewModel.ActividadesList = db.ACTIVIDAD.ToList();
            viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
            viewModel.InmuebleList = db.INMUEBLE.ToList();
            viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();
            return View(viewModel);
        }


        // Método para obtener el correo del usuario autenticado
        private string ObtenerCorreoUsuarioAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            if (usuarioAutenticado != null)
            {
                return usuarioAutenticado.PERSONA.correo;
            }
            return null;
        }

        private string ObtenerNombreUsuarioAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            if (usuarioAutenticado != null)
            {
                
                return $"{usuarioAutenticado.PERSONA.nombre} {usuarioAutenticado.PERSONA.apeliido_paterno} {usuarioAutenticado.PERSONA.apellido_materno}";
            }
            return null;
        }


        private void EnviarCorreoAutomaticamente()
        {
            try
            {
                using (var dbContext = new ObraManzanoFinal())
                {
                    // Query para detalles que cumplen con el criterio
                    var detallesConEstadoSupv = dbContext.DETALLE_CARTILLA
                        .Where(dc => dc.estado_supv == true && dc.ITEM_VERIF.tipo_item == true &&
                                     (dc.correo_enviado_ac == false || dc.correo_enviado_ac == null) &&
                                     (dc.correo_enviado_supv == false || dc.correo_enviado_supv == null))
                        .Include(dc => dc.ITEM_VERIF)
                        .Include(dc => dc.CARTILLA)
                        .Include(dc => dc.INMUEBLE.LOTE_INMUEBLE)
                        .ToList();

                    // Agrupar detalles por ciertos criterios
                    var groupedDetalles = detallesConEstadoSupv
                        .GroupBy(dc => new { dc.INMUEBLE.LOTE_INMUEBLE.lote_id, dc.CARTILLA_cartilla_id, dc.INMUEBLE.inmueble_id });

                    foreach (var grupo in groupedDetalles)
                    {
                        var primerDetalle = grupo.First();
                        var nombreActividad = primerDetalle.CARTILLA.ACTIVIDAD.nombre_actividad;
                        var codigoActividad = primerDetalle.CARTILLA.ACTIVIDAD.codigo_actividad;
                        var nombreObra = primerDetalle.CARTILLA.OBRA.nombre_obra;
                        var cartillaId = primerDetalle.CARTILLA.cartilla_id;
                        var fechaSupv = primerDetalle.fecha_supv;
                        var loteInmueble = primerDetalle.INMUEBLE.LOTE_INMUEBLE.abreviatura;
                        var inmueble = primerDetalle.INMUEBLE.codigo_inmueble;
                        var observacionesPublicas = primerDetalle.CARTILLA.observaciones;
                        var observacionesPrivadas = primerDetalle.CARTILLA.observaciones_priv;

                        // Obtener el correo del perfil de autocontrol para la obra asociada
                        var correoAutocontrol = (from a in dbContext.ACCESO_OBRAS
                                                 join u in dbContext.USUARIO on a.usuario_id equals u.usuario_id
                                                 where a.obra_id == primerDetalle.CARTILLA.OBRA_obra_id
                                                   && u.PERFIL.rol == "Autocontrol"
                                                 select u.PERSONA.correo)
                                                .FirstOrDefault();

                        // Obtener el correo del usuario autenticado desde la sesión
                        var correoUsuarioAutenticado = ObtenerCorreoUsuarioAutenticado();
                        // Obtener el nombre del usuario autenticado desde la sesión
                        var nombreUsuarioAutenticado = ObtenerNombreUsuarioAutenticado();

                        // Asegúrate de que el correoAutocontrol no sea nulo antes de enviar el correo
                        if (!string.IsNullOrEmpty(correoAutocontrol))
                        {
                            // Llamar a EnviarCorreoExito con los parámetros necesarios
                            EnviarCorreoExito(nombreActividad, codigoActividad, nombreObra, loteInmueble, inmueble, observacionesPublicas, observacionesPrivadas, fechaSupv, correoAutocontrol, correoUsuarioAutenticado, nombreUsuarioAutenticado);

                            // Actualizar flags de correo enviado en la base de datos
                            foreach (var detalle in grupo)
                            {
                                detalle.correo_enviado_ac = true;
                                detalle.correo_enviado_supv = true;
                                dbContext.Entry(detalle).State = EntityState.Modified;
                            }

                            dbContext.SaveChanges(); // Guardar cambios en la base de datos
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correos automáticamente: " + ex.Message);
                // Manejar la excepción según los requisitos de tu aplicación
            }
        }


        private void EnviarCorreoExito(string nombreActividad, string codigoActividad, string nombreObra, string loteInmueble, string inmueble, string observacionesPublicas, string observacionesPrivadas, DateTime? fechaSupv, string correoAutocontrol, string correoUsuarioAutenticado, string nombreUsuarioAutenticado)
        {
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "uraa qkpw rnyd asvb");
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com");
                    // Reemplazar el correo fijo por el correo de autocontrol
                    mailMessage.To.Add(correoAutocontrol);
                    mailMessage.CC.Add(correoUsuarioAutenticado);

                    // Construir el asunto y el cuerpo del correo
                    string asunto = $"V°B° SUPV {codigoActividad} - {nombreActividad} {nombreObra}";
                    string body = $"Estimad@,\n\nInformo a usted la aprobación de lote de inmueble {loteInmueble} del inmueble {inmueble} para la cartilla de actividad {codigoActividad} {nombreActividad}." +
                                  $"\n\nCon fecha {fechaSupv?.ToString("dd-MM-yyyy") ?? "N/A"} para obra {nombreObra}.";

                    if (!string.IsNullOrEmpty(observacionesPublicas) || !string.IsNullOrEmpty(observacionesPrivadas))
                    {
                        body += $"\n\nObservaciones:";
                        if (!string.IsNullOrEmpty(observacionesPublicas))
                        {
                            body += $"\nPúblicas: {observacionesPublicas}";
                        }
                        if (!string.IsNullOrEmpty(observacionesPrivadas))
                        {
                            body += $"\nPrivadas: {observacionesPrivadas}";
                        }
                    }

                    body += $"\n\nSaludos cordiales,\n\n {nombreUsuarioAutenticado}.";

                    mailMessage.Subject = asunto;
                    mailMessage.Body = body;

                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Manejar el error al enviar el correo electrónico
                Console.WriteLine("Error al enviar el correo: " + ex.Message);
            }
        }


        private string ObtenerNombreSupervisor(string rutSupervisor)
        {
            var supervisor = db.USUARIO.FirstOrDefault(u => u.PERSONA_rut == rutSupervisor);
            if (supervisor != null)
            {
                return $"{supervisor.PERSONA.nombre} {supervisor.PERSONA.apeliido_paterno} {supervisor.PERSONA.apellido_materno}";// Asumiendo que 'nombre_completo' es el campo que contiene el nombre del supervisor
            }
            return "Nombre de Supervisor Desconocido"; // O maneja la situación según tus necesidades
        }


        private string ObtenerRutUsuarioAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            if (usuarioAutenticado != null)
            {
                return usuarioAutenticado.PERSONA_rut;
            }
            return null;
        }

        public JsonResult GetInmueblesByLote(int loteId)
        {
            var inmuebles = db.INMUEBLE.Where(a => a.LOTE_INMUEBLE_lote_id == loteId)
                                  .Select(a => new
                                  {
                                      Value = a.inmueble_id,
                                      Text = a.codigo_inmueble
                                  })
                                  .ToList();
            return Json(inmuebles, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCodigoInmueble(int inmuebleId)
        {
            // Aquí obtienes el código del inmueble por su ID
            var codigoInmueble = db.INMUEBLE
                                    .Where(a => a.inmueble_id == inmuebleId)
                                    .Select(a => a.codigo_inmueble)
                                    .FirstOrDefault();
            // Devuelves el código del inmueble como un objeto JSON
            return Json(codigoInmueble, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDetalleCartillasByInmueble(int inmuebleId)
        {
            var detalles = db.DETALLE_CARTILLA
                .Where(d => d.INMUEBLE_inmueble_id == inmuebleId)
                .Select(d => new
                {
                    ItemVerificacion = d.ITEM_VERIF.elemento_verificacion,
                    EstadoSupv = d.estado_supv,
                    EstadoOtec = d.estado_autocontrol,
                    EstadoIto = d.estado_ito,
                    NumeroVivienda = d.INMUEBLE.codigo_inmueble
                })
                .ToList();

            return Json(detalles, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarCambiosAutomaticamente(List<DETALLE_CARTILLA> detalles)
        {
            try
            {
                // Guardar los detalles recibidos automáticamente en la base de datos
                using (var dbContext = new ObraManzanoFinal())
                {
                    foreach (var detalle in detalles)
                    {
                        dbContext.Entry(detalle).State = EntityState.Modified;
                    }
                    dbContext.SaveChanges();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public JsonResult GetLotesByObra(int obraId)
        {
            var lotes = db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId)
                .Select(l => new
                {
                    Value = l.lote_id,
                    Text = l.abreviatura
                })
                .ToList();

            return Json(lotes, JsonRequestBehavior.AllowGet);
        }

        // Acción para actualizar los detalles por lote
        public ActionResult ActualizarDetallesPorLote(int loteId)
        {
            // Filtra los detalles por el ID del lote seleccionado
            var detallesFiltrados = db.DETALLE_CARTILLA
                .Where(d => d.INMUEBLE.LOTE_INMUEBLE_lote_id == loteId)
                .ToList();

            // Devuelve los detalles filtrados como JSON
            return Json(detallesFiltrados, JsonRequestBehavior.AllowGet);
        }

       
    }
}