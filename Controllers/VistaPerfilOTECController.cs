using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using Proyecto_Cartilla_Autocontrol.Models;


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class VistaPerfilOTECController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();
        // GET: VistaPerfilOTEC
        public async Task<ActionResult> Index()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            ViewBag.UsuarioAutenticado = usuarioAutenticado;

            if (Session["UsuarioAutenticado"] != null)
            {
                var cARTILLA = db.CARTILLA.Include(c => c.ACTIVIDAD).Include(c => c.ESTADO_FINAL).Include(c => c.OBRA)
                               .Include(c => c.OBRA.USUARIO)
                               .Where(c => c.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id));
                return View(await cARTILLA.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
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
            return RedirectToAction("Index"); // O alguna otra acción adecuada
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
                        // Actualizar la información de la Cartilla en la base de datos
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;

                        // Verificar si el estado final es "Aprobado" (id 1)
                        if (viewModel.Cartilla.ESTADO_FINAL.descripcion == "Aprobada")
                        {
                            // Verificar si al menos un campo de aprobación está en falso
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_otec == false || detalle.estado_ito == false))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobada. Debido a que no todos los valores se encuentra aprobados.";
                                return RedirectToAction("Index");
                            }
                        }

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

                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult ConfirmarEliminarCartilla(int id)
        {
            using (var dbContext = new ObraManzanoFinal())
            {
                var cartilla = dbContext.CARTILLA.Include(c => c.DETALLE_CARTILLA).Include(c => c.ACTIVIDAD).Include(c => c.OBRA).Include(c => c.ESTADO_FINAL).FirstOrDefault(c => c.cartilla_id == id);
                if (cartilla != null)
                {
                    return View(cartilla);
                }
                else
                {
                    ModelState.AddModelError("", "No se encontró la cartilla con el ID proporcionado");
                    return RedirectToAction("Index"); // O alguna otra acción adecuada
                }
            }
        }


        [HttpPost]
        public ActionResult EliminarCartilla(int id)
        {
            try
            {
                using (var dbContext = new ObraManzanoFinal())
                {
                    // Obtener la Cartilla y sus detalles por ID
                    var cartilla = dbContext.CARTILLA.Include(c => c.DETALLE_CARTILLA).Include(c => c.ACTIVIDAD).Include(c => c.OBRA).Include(c => c.ESTADO_FINAL).FirstOrDefault(c => c.cartilla_id == id);

                    if (cartilla != null)
                    {
                        // Eliminar los detalles de la Cartilla
                        dbContext.DETALLE_CARTILLA.RemoveRange(cartilla.DETALLE_CARTILLA);

                        // Eliminar la Cartilla
                        dbContext.CARTILLA.Remove(cartilla);

                        // Guardar los cambios en la base de datos
                        dbContext.SaveChanges();

                        // Redirigir a la página de índice u otra acción
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Manejar si no se encuentra la Cartilla con el ID proporcionado
                        ModelState.AddModelError("", "No se encontró la cartilla con el ID proporcionado");
                        return RedirectToAction("Index"); // O alguna otra acción adecuada
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción que pueda ocurrir al eliminar en la base de datos
                ModelState.AddModelError("", "Error al eliminar la cartilla: " + ex.Message);
                return RedirectToAction("Index"); // O alguna otra acción adecuada
            }
        }

        public ActionResult CrearCartilla()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];

                CartillasViewModel viewModel = new CartillasViewModel();
                viewModel.DetalleCartillas = new List<DETALLE_CARTILLA>();
                // Puedes agregar instancias de DETALLE_CARTILLA según sea necesario
                viewModel.DetalleCartillas.Add(new DETALLE_CARTILLA());


                // Realiza una consulta a tu base de datos para obtener el valor deseado
                using (var dbContext = new ObraManzanoFinal())  // Reemplaza 'TuDbContext' con el nombre de tu contexto de base de datos
                {
                    viewModel.ActividadesList = dbContext.ACTIVIDAD
                      .Include(a => a.OBRA.USUARIO)
                      .Where(a => a.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                      .Where(a => a.estado == "A")
                      .ToList();

                    viewModel.ElementosVerificacion = dbContext.ITEM_VERIF
                        .Include(i => i.ACTIVIDAD.OBRA.USUARIO)
                        .Where(i => i.ACTIVIDAD.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                        .ToList();

                    viewModel.InmuebleList = dbContext.INMUEBLE.ToList();

                    viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();
                    viewModel.ObraList = dbContext.OBRA
                     .Include(p => p.USUARIO)
                     .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                     .ToList();

                }

                return View(viewModel);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        public ActionResult CrearCartilla(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"]; 
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
                                c.ESTADO_FINAL.descripcion == "Aprobada");

                            if (existeCartilla)
                            {
                                ModelState.AddModelError("", "Ya existe una misma Cartilla con un Estado Final de Visto Bueno.");

                                viewModel.ActividadesList = dbContext.ACTIVIDAD
                                          .Include(a => a.OBRA.USUARIO)
                                          .Where(a => a.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                                          .Where(a => a.estado == "A")
                                          .ToList();

                                viewModel.ElementosVerificacion = dbContext.ITEM_VERIF
                                    .Include(i => i.ACTIVIDAD.OBRA.USUARIO)
                                    .Where(i => i.ACTIVIDAD.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                                    .ToList();

                                viewModel.InmuebleList = dbContext.INMUEBLE.ToList();

                                viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();
                                viewModel.ObraList = dbContext.OBRA
                                 .Include(p => p.USUARIO)
                                 .Where(o => o.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id))
                                 .ToList();

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
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
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

        public JsonResult GetCombinacionesElementosInmuebles(int actividadId)
        {
            try
            {
                using (var context = new ObraManzanoFinal())
                {
                    var query = from iv in context.ITEM_VERIF
                                join a in context.ACTIVIDAD on iv.ACTIVIDAD_actividad_id equals a.actividad_id
                                join o in context.OBRA on a.OBRA_obra_id equals o.obra_id
                                join i in context.INMUEBLE on o.obra_id equals i.OBRA_obra_id
                                where iv.ACTIVIDAD_actividad_id == actividadId
                                select new
                                {
                                    iv.item_verif_id,
                                    iv.elemento_verificacion,
                                    i.inmueble_id,
                                    i.tipo_inmueble
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


    }
}