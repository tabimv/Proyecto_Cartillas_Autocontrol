using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class RevisionMovilController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();


        public static bool actualizacionEditCartilla = false;

        [HttpGet]
        public ActionResult EditarCartillaMovilAdmin()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtrar las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var UsuarioCartillas = db.CARTILLA
                    .Include(c => c.OBRA.ACCESO_OBRAS)
                    .Include(c => c.ACTIVIDAD)
                    .Where(c => c.OBRA.ACCESO_OBRAS.Any(ac => ac.USUARIO.usuario_id == usuarioAutenticado.usuario_id))
                    .ToList();

                // Obtener los registros de detalle de cartillas
                var detalleCartillas = db.DETALLE_CARTILLA.ToList();

                

                // Crear el modelo para la vista
                var viewModel = new CartillaMovilViewModel
                {
                    Cartilla = new CARTILLA(),
                    DetalleCartillas = detalleCartillas
                };

                // Establecemos el SelectList para CARTILLA en ViewBag usando el ViewModel
                ViewBag.CARTILLA_id = new SelectList(UsuarioCartillas.Select(c => new
                {
                    cartilla_id = c.cartilla_id,
                    DisplayField = c.cartilla_id + " - " + c.ACTIVIDAD.nombre_actividad
                }), "cartilla_id", "DisplayField", null);

                // Deja el DropDownList de lotes vacío inicialmente
                ViewBag.LOTES = new SelectList(Enumerable.Empty<SelectListItem>(), "lote_id", "abreviatura");

                // Deja el DropDownList de inmuebles vacío inicialmente
                ViewBag.INMUEBLES = new SelectList(Enumerable.Empty<SelectListItem>(), "inmueble_id", "codigo_inmueble");

                var estadosFinales = db.ESTADO_FINAL.ToList();
                ViewBag.EstadoFinalSelectList = new SelectList(estadosFinales, "estado_final_id", "descripcion");

                // Establecer la bandera global para indicar que se ha realizado una actualización
                actualizacionEditCartilla = true;
                // Pasar el ViewModel a la vista
                return View(viewModel);
            }
            else
            {
                // Manejar el caso en el que el usuario no esté autenticado
                return RedirectToAction("Login", "Account");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarCartillaMovilAdmin(CartillaMovilViewModel model)
        {
            if (Session["UsuarioAutenticado"] != null)
            {

                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                if (ModelState.IsValid)
                {
                    // Iniciar una transacción
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            bool cambiosRealizados = false;
                            // Obtener el ID de la cartilla seleccionada
                            int cartillaId = model.Cartilla.cartilla_id;

                            // Buscar la cartilla en la base de datos
                            var cartilla = db.CARTILLA.Find(cartillaId);

                            if (cartilla != null)
                            {
                                // Actualizar observaciones solo si han cambiado
                                if (cartilla.observaciones != model.Cartilla.observaciones ||
                                    cartilla.observaciones_priv != model.Cartilla.observaciones_priv)
                                {
                                    cartilla.observaciones = model.Cartilla.observaciones ?? string.Empty;
                                    cartilla.observaciones_priv = model.Cartilla.observaciones_priv ?? string.Empty;
                                    db.Entry(cartilla).State = EntityState.Modified;
                                    cambiosRealizados = true;
                                }

                                // Aquí es donde actualizas enviar_correo
                                if (cartilla.enviar_correo != model.Cartilla.enviar_correo)
                                {
                                    cartilla.enviar_correo = model.Cartilla.enviar_correo;
                                    db.Entry(cartilla).State = EntityState.Modified;
                                    cambiosRealizados = true;
                                }


                                // Validaciones antes de realizar los cambios
                                if (model.Cartilla.ESTADO_FINAL_estado_final_id == 1) // Estado Aprobado
                                {
                                    // Verificar si algún campo de aprobación es null
                                    if (cartilla.DETALLE_CARTILLA.Any(detalle => detalle.estado_autocontrol == null || detalle.estado_ito == null || detalle.estado_supv == null))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado a menos que todos sus campos hayan sido revisados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }

                                    // Verificar si al menos un campo de aprobación está en false
                                    if (cartilla.DETALLE_CARTILLA.Any(detalle => detalle.estado_autocontrol == false || detalle.estado_ito == false || detalle.estado_supv == false))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado. Debido a que no todos los valores se encuentran aprobados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }
                                }
                                else if (model.Cartilla.ESTADO_FINAL_estado_final_id == 2) // Estado Rechazado
                                {
                                    // Verificar si algún campo de aprobación es null
                                    if (cartilla.DETALLE_CARTILLA.Any(detalle => detalle.estado_autocontrol == null || detalle.estado_ito == null || detalle.estado_supv == null))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazado a menos que todos sus campos hayan sido revisados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }

                                    // Verificar si todos los campos están aprobados
                                    if (cartilla.DETALLE_CARTILLA.All(detalle => detalle.estado_autocontrol == true && detalle.estado_ito == true && detalle.estado_supv == true))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazada. Todos los valores se encuentran aprobados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }
                                }

                                // Actualizar estado_final_id si ha cambiado
                                if (cartilla.ESTADO_FINAL_estado_final_id != model.Cartilla.ESTADO_FINAL_estado_final_id)
                                {
                                    cartilla.ESTADO_FINAL_estado_final_id = model.Cartilla.ESTADO_FINAL_estado_final_id;
                                    db.Entry(cartilla).State = EntityState.Modified;
                                    cambiosRealizados = true;
                                }


                                // Obtener los detalles existentes en la base de datos
                                var existingDetalles = db.DETALLE_CARTILLA
                                    .Where(d => d.CARTILLA_cartilla_id == model.Cartilla.cartilla_id)
                                    .ToList();

                                // Recorrer los detalles del modelo
                                foreach (var detalleModel in model.DetalleCartillas)
                                {
                                    var existingDetalle = existingDetalles.FirstOrDefault(d => d.detalle_cartilla_id == detalleModel.detalle_cartilla_id);
                                    if (existingDetalle != null)
                                    {
                                        bool hasChanged = false;

                                        // Verificar si estado_supv ha cambiado
                                        if (existingDetalle.estado_supv != detalleModel.estado_supv)
                                        {
                                            if (existingDetalle.estado_supv != null)
                                            {
                                                detalleModel.estado_supv = existingDetalle.estado_supv;
                                            }
                                            else
                                            {
                                                existingDetalle.estado_supv = detalleModel.estado_supv;
                                                hasChanged = true;

                                                // Verificar rut_spv y fecha_supv
                                                if ((detalleModel.estado_supv == true || detalleModel.estado_supv == false) &&
                                                    existingDetalle.rut_spv == null && !string.IsNullOrEmpty(usuarioAutenticado.PERSONA_rut))
                                                {
                                                    existingDetalle.rut_spv = usuarioAutenticado.PERSONA_rut;
                                                }

                                                if (existingDetalle.fecha_supv == null)
                                                {
                                                    existingDetalle.fecha_supv = DateTime.Now;
                                                }
                                            }
                                        }

                                        // Verificar si estado_autocontrol ha cambiado
                                        if (existingDetalle.estado_autocontrol != detalleModel.estado_autocontrol)
                                        {
                                            if (existingDetalle.estado_autocontrol != null)
                                            {
                                                detalleModel.estado_autocontrol = existingDetalle.estado_autocontrol;
                                            }
                                            else
                                            {
                                                existingDetalle.estado_autocontrol = detalleModel.estado_autocontrol;
                                                hasChanged = true;

                                                // Verificar fecha_ac
                                                if (existingDetalle.fecha_autocontrol == null)
                                                {
                                                    existingDetalle.fecha_autocontrol = DateTime.Now;
                                                }
                                            }
                                        }


                                        // Verificar si estado_autocontrol ha cambiado
                                        if (existingDetalle.estado_ito != detalleModel.estado_ito)
                                        {
                                            if (existingDetalle.estado_ito != null)
                                            {
                                                detalleModel.estado_ito = existingDetalle.estado_ito;
                                            }
                                            else
                                            {
                                                existingDetalle.estado_ito = detalleModel.estado_ito;
                                                hasChanged = true;

                                                // Verificar fecha_ac
                                                if (existingDetalle.fecha_fto == null)
                                                {
                                                    existingDetalle.fecha_fto = DateTime.Now;
                                                }
                                            }
                                        }


                                        if (hasChanged)
                                        {
                                            existingDetalle.fecha_rev = DateTime.Now;
                                            db.Entry(existingDetalle).State = EntityState.Modified;
                                            cambiosRealizados = true;
                                        }
                                    }
                                }

                                // Eliminar detalles que ya no están presentes
                                foreach (var detalle in existingDetalles)
                                {
                                    if (!model.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                                    {
                                        if (detalle.estado_supv != null || detalle.rut_spv != null)
                                        {
                                            continue;
                                        }
                                        db.DETALLE_CARTILLA.Remove(detalle);
                                        cambiosRealizados = true;
                                    }
                                }

                                // Guardar cambios si hubo modificaciones
                                if (cambiosRealizados)
                                {
                                    db.SaveChanges();
                                    transaction.Commit();

                                    // Verificar si enviar_correo es true antes de enviar correos
                                    if (cartilla != null && cartilla.enviar_correo == true)
                                    {
                                        EnviarCorreosAutomatizados();
                                    }

                                    return RedirectToAction("EditarCartillaMovilAdmin", "RevisionMovil");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", "Ocurrió un error al guardar los cambios: " + ex.Message);
                        }
                    }
                }


                // Filtrar las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var UsuarioCartillas = db.CARTILLA
                    .Include(c => c.OBRA.ACCESO_OBRAS)
                    .Include(c => c.ACTIVIDAD)
                    .Where(c => c.OBRA.ACCESO_OBRAS.Any(ac => ac.USUARIO.usuario_id == usuarioAutenticado.usuario_id))
                    .ToList();

                // Obtener los registros de detalle de cartillas
                var detalleCartillas = db.DETALLE_CARTILLA.ToList();


               

                // Crear el modelo para la vista
                var viewModel = new CartillaMovilViewModel
                {
                    Cartilla = new CARTILLA(),
                    DetalleCartillas = detalleCartillas
                };

               

                // Establecemos el SelectList para CARTILLA en ViewBag usando el ViewModel
                ViewBag.CARTILLA_id = new SelectList(UsuarioCartillas.Select(c => new
                {
                    cartilla_id = c.cartilla_id,
                    DisplayField = c.cartilla_id + " - " + c.ACTIVIDAD.nombre_actividad
                }), "cartilla_id", "DisplayField", null);

                // Deja el DropDownList de lotes vacío inicialmente
                ViewBag.LOTES = new SelectList(Enumerable.Empty<SelectListItem>(), "lote_id", "abreviatura");

                // Deja el DropDownList de inmuebles vacío inicialmente
                ViewBag.INMUEBLES = new SelectList(Enumerable.Empty<SelectListItem>(), "inmueble_id", "codigo_inmueble");

                var estadosFinales = db.ESTADO_FINAL.ToList();
                ViewBag.EstadoFinalSelectList = new SelectList(estadosFinales, "estado_final_id", "descripcion");

                return View(model);
            }
            else
            {
                // Manejar el caso en el que el usuario no esté autenticado
                return RedirectToAction("Login", "Account");
            }
        }


        [HttpGet]
        public ActionResult EditarCartillaMovilAutocontrol()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtrar las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var UsuarioCartillas = db.CARTILLA
                    .Include(c => c.OBRA.ACCESO_OBRAS)
                    .Include(c => c.ACTIVIDAD)
                    .Where(c => c.OBRA.ACCESO_OBRAS.Any(ac => ac.USUARIO.usuario_id == usuarioAutenticado.usuario_id))
                    .ToList();

                // Obtener los registros de detalle de cartillas
                var detalleCartillas = db.DETALLE_CARTILLA.ToList();



                // Crear el modelo para la vista
                var viewModel = new CartillaMovilViewModel
                {
                    Cartilla = new CARTILLA(),
                    DetalleCartillas = detalleCartillas
                };

                // Establecemos el SelectList para CARTILLA en ViewBag usando el ViewModel
                ViewBag.CARTILLA_id = new SelectList(UsuarioCartillas.Select(c => new
                {
                    cartilla_id = c.cartilla_id,
                    DisplayField = c.cartilla_id + " - " + c.ACTIVIDAD.nombre_actividad
                }), "cartilla_id", "DisplayField", null);

                // Deja el DropDownList de lotes vacío inicialmente
                ViewBag.LOTES = new SelectList(Enumerable.Empty<SelectListItem>(), "lote_id", "abreviatura");

                // Deja el DropDownList de inmuebles vacío inicialmente
                ViewBag.INMUEBLES = new SelectList(Enumerable.Empty<SelectListItem>(), "inmueble_id", "codigo_inmueble");

                var estadosFinales = db.ESTADO_FINAL.ToList();
                ViewBag.EstadoFinalSelectList = new SelectList(estadosFinales, "estado_final_id", "descripcion");

                // Establecer la bandera global para indicar que se ha realizado una actualización
                actualizacionEditCartilla = true;
                // Pasar el ViewModel a la vista
                return View(viewModel);
            }
            else
            {
                // Manejar el caso en el que el usuario no esté autenticado
                return RedirectToAction("Login", "Account");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarCartillaMovilAutocontrol(CartillaMovilViewModel model)
        {
            if (Session["UsuarioAutenticado"] != null)
            {

                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                if (ModelState.IsValid)
                {
                    // Iniciar una transacción
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            bool cambiosRealizados = false;
                            // Obtener el ID de la cartilla seleccionada
                            int cartillaId = model.Cartilla.cartilla_id;

                            // Buscar la cartilla en la base de datos
                            var cartilla = db.CARTILLA.Find(cartillaId);

                            if (cartilla != null)
                            {
                                // Actualizar observaciones solo si han cambiado
                                if (cartilla.observaciones != model.Cartilla.observaciones ||
                                    cartilla.observaciones_priv != model.Cartilla.observaciones_priv)
                                {
                                    cartilla.observaciones = model.Cartilla.observaciones ?? string.Empty;
                                    cartilla.observaciones_priv = model.Cartilla.observaciones_priv ?? string.Empty;
                                    db.Entry(cartilla).State = EntityState.Modified;
                                    cambiosRealizados = true;
                                }

                                // Aquí es donde actualizas enviar_correo
                                if (cartilla.enviar_correo != model.Cartilla.enviar_correo)
                                {
                                    cartilla.enviar_correo = model.Cartilla.enviar_correo;
                                    db.Entry(cartilla).State = EntityState.Modified;
                                    cambiosRealizados = true;
                                }


                                // Validaciones antes de realizar los cambios
                                if (model.Cartilla.ESTADO_FINAL_estado_final_id == 1) // Estado Aprobado
                                {
                                    // Verificar si algún campo de aprobación es null
                                    if (cartilla.DETALLE_CARTILLA.Any(detalle => detalle.estado_autocontrol == null || detalle.estado_ito == null || detalle.estado_supv == null))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado a menos que todos sus campos hayan sido revisados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }

                                    // Verificar si al menos un campo de aprobación está en false
                                    if (cartilla.DETALLE_CARTILLA.Any(detalle => detalle.estado_autocontrol == false || detalle.estado_ito == false || detalle.estado_supv == false))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado. Debido a que no todos los valores se encuentran aprobados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }
                                }
                                else if (model.Cartilla.ESTADO_FINAL_estado_final_id == 2) // Estado Rechazado
                                {
                                    // Verificar si algún campo de aprobación es null
                                    if (cartilla.DETALLE_CARTILLA.Any(detalle => detalle.estado_autocontrol == null || detalle.estado_ito == null || detalle.estado_supv == null))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazado a menos que todos sus campos hayan sido revisados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }

                                    // Verificar si todos los campos están aprobados
                                    if (cartilla.DETALLE_CARTILLA.All(detalle => detalle.estado_autocontrol == true && detalle.estado_ito == true && detalle.estado_supv == true))
                                    {
                                        TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Rechazada. Todos los valores se encuentran aprobados.";
                                        return RedirectToAction("Index", "Cartilla");
                                    }
                                }

                                // Actualizar estado_final_id si ha cambiado
                                if (cartilla.ESTADO_FINAL_estado_final_id != model.Cartilla.ESTADO_FINAL_estado_final_id)
                                {
                                    cartilla.ESTADO_FINAL_estado_final_id = model.Cartilla.ESTADO_FINAL_estado_final_id;
                                    db.Entry(cartilla).State = EntityState.Modified;
                                    cambiosRealizados = true;
                                }


                                // Obtener los detalles existentes en la base de datos
                                var existingDetalles = db.DETALLE_CARTILLA
                                    .Where(d => d.CARTILLA_cartilla_id == model.Cartilla.cartilla_id)
                                    .ToList();

                                // Recorrer los detalles del modelo
                                foreach (var detalleModel in model.DetalleCartillas)
                                {
                                    var existingDetalle = existingDetalles.FirstOrDefault(d => d.detalle_cartilla_id == detalleModel.detalle_cartilla_id);
                                    if (existingDetalle != null)
                                    {
                                        bool hasChanged = false;

                                     
                                        // Verificar si estado_autocontrol ha cambiado
                                        if (existingDetalle.estado_autocontrol != detalleModel.estado_autocontrol)
                                        {
                                            if (existingDetalle.estado_autocontrol != null)
                                            {
                                                detalleModel.estado_autocontrol = existingDetalle.estado_autocontrol;
                                            }
                                            else
                                            {
                                                existingDetalle.estado_autocontrol = detalleModel.estado_autocontrol;
                                                hasChanged = true;

                                                // Verificar fecha_ac
                                                if (existingDetalle.fecha_autocontrol == null)
                                                {
                                                    existingDetalle.fecha_autocontrol = DateTime.Now;
                                                }
                                            }
                                        }


                                        // Verificar si estado_autocontrol ha cambiado
                                        if (existingDetalle.estado_ito != detalleModel.estado_ito)
                                        {
                                            if (existingDetalle.estado_ito != null)
                                            {
                                                detalleModel.estado_ito = existingDetalle.estado_ito;
                                            }
                                            else
                                            {
                                                existingDetalle.estado_ito = detalleModel.estado_ito;
                                                hasChanged = true;

                                                // Verificar fecha_ac
                                                if (existingDetalle.fecha_fto == null)
                                                {
                                                    existingDetalle.fecha_fto = DateTime.Now;
                                                }
                                            }
                                        }


                                        if (hasChanged)
                                        {
                                            db.Entry(existingDetalle).State = EntityState.Modified;
                                            cambiosRealizados = true;
                                        }
                                    }
                                }

                                // Eliminar detalles que ya no están presentes
                                foreach (var detalle in existingDetalles)
                                {
                                    if (!model.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                                    {
                                        if (detalle.estado_supv != null || detalle.rut_spv != null)
                                        {
                                            continue;
                                        }
                                        db.DETALLE_CARTILLA.Remove(detalle);
                                        cambiosRealizados = true;
                                    }
                                }

                                // Guardar cambios si hubo modificaciones
                                if (cambiosRealizados)
                                {
                                    db.SaveChanges();
                                    transaction.Commit();

                                   
                                    return RedirectToAction("EditarCartillaMovilAutocontrol", "RevisionMovil");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", "Ocurrió un error al guardar los cambios: " + ex.Message);
                        }
                    }
                }


                // Filtrar las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var UsuarioCartillas = db.CARTILLA
                    .Include(c => c.OBRA.ACCESO_OBRAS)
                    .Include(c => c.ACTIVIDAD)
                    .Where(c => c.OBRA.ACCESO_OBRAS.Any(ac => ac.USUARIO.usuario_id == usuarioAutenticado.usuario_id))
                    .ToList();

                // Obtener los registros de detalle de cartillas
                var detalleCartillas = db.DETALLE_CARTILLA.ToList();




                // Crear el modelo para la vista
                var viewModel = new CartillaMovilViewModel
                {
                    Cartilla = new CARTILLA(),
                    DetalleCartillas = detalleCartillas
                };



                // Establecemos el SelectList para CARTILLA en ViewBag usando el ViewModel
                ViewBag.CARTILLA_id = new SelectList(UsuarioCartillas.Select(c => new
                {
                    cartilla_id = c.cartilla_id,
                    DisplayField = c.cartilla_id + " - " + c.ACTIVIDAD.nombre_actividad
                }), "cartilla_id", "DisplayField", null);

                // Deja el DropDownList de lotes vacío inicialmente
                ViewBag.LOTES = new SelectList(Enumerable.Empty<SelectListItem>(), "lote_id", "abreviatura");

                // Deja el DropDownList de inmuebles vacío inicialmente
                ViewBag.INMUEBLES = new SelectList(Enumerable.Empty<SelectListItem>(), "inmueble_id", "codigo_inmueble");

                var estadosFinales = db.ESTADO_FINAL.ToList();
                ViewBag.EstadoFinalSelectList = new SelectList(estadosFinales, "estado_final_id", "descripcion");

                return View(model);
            }
            else
            {
                // Manejar el caso en el que el usuario no esté autenticado
                return RedirectToAction("Login", "Account");
            }
        }


        [HttpGet]
        public JsonResult ObtenerCamposPorCartilla(int cartillaId)
        {
            // Obtener la cartilla seleccionada
            var cartilla = db.CARTILLA.Include(c => c.ESTADO_FINAL)
                                      .Include(c => c.OBRA)
                                      .FirstOrDefault(c => c.cartilla_id == cartillaId);

            if (cartilla == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            // Lista completa de estados finales con la opción seleccionada
            var estadoFinales = db.ESTADO_FINAL.ToList();
            var estadoFinalSelectList = estadoFinales.Select(e => new SelectListItem
            {
                Text = e.descripcion,
                Value = e.estado_final_id.ToString(),
                Selected = e.estado_final_id == cartilla.ESTADO_FINAL_estado_final_id // Marca el estado actual como seleccionado
            }).ToList();

            // Devolver los datos de la cartilla seleccionada junto con los estados finales
            return Json(new
            {
                observaciones = cartilla.observaciones,
                observaciones_priv = cartilla.observaciones_priv,
                nombre_obra = cartilla.OBRA.nombre_obra,
                fecha = cartilla.fecha,
                estadoFinalSelectList = estadoFinalSelectList,
                enviar_correo = cartilla.enviar_correo
            }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public JsonResult ObtenerLotesPorCartilla(int cartillaId)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];

                // Obtener la obra asociada a la cartilla
                var cartilla = db.CARTILLA.Include(c => c.OBRA)
                                           .FirstOrDefault(c => c.cartilla_id == cartillaId);

                if (cartilla != null)
                {
                    // Filtrar lotes a los que el usuario tiene acceso
                    var lotes = db.LOTE_INMUEBLE
                        .Include(l => l.OBRA.ACCESO_OBRAS)
                        .Where(l => l.OBRA_obra_id == cartilla.OBRA.obra_id &&
                                    l.OBRA.ACCESO_OBRAS.Any(ac => ac.usuario_id == usuarioAutenticado.usuario_id))
                        .Select(l => new
                        {
                            l.lote_id,
                            l.abreviatura // Cambia esto por la propiedad que desees mostrar
                        })
                        .ToList();

                    return Json(lotes, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new List<object>(), JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult ObtenerInmueblesPorLote(int loteId)
        {
            // Obtener los inmuebles asociados al lote desde la base de datos
            var inmuebles = db.INMUEBLE
                              .Where(i => i.LOTE_INMUEBLE_lote_id == loteId) // Filtrar por lote_id
                              .Select(i => new
                              {
                                  inmueble_id = i.inmueble_id,
                                  codigo_inmueble = i.codigo_inmueble // Ajusta según tu modelo
                              })
                              .ToList();

            return Json(inmuebles, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult ObtenerRevisionDos(int cartillaId, int loteId, int inmuebleId)
        {
            try
            {
                // Buscar el valor de revisionDos para el grupo de cartilla, lote e inmueble
                var detalle = db.DETALLE_CARTILLA
                    .Where(d => d.CARTILLA_cartilla_id == cartillaId && d.INMUEBLE.LOTE_INMUEBLE.lote_id == loteId && d.INMUEBLE_inmueble_id == inmuebleId)
                    .FirstOrDefault();

                if (detalle != null)
                {
                    // Devolver el estado de revisionDos
                    return Json(new { success = true, revisionDos = detalle.revision_dos }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "No se encontró el detalle." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult ActualizarRevisionDos(int cartillaId, int loteId, int inmuebleId, bool revisionDos)
        {
            try
            {
                // Obtener los registros de Detalle_Cartilla que coinciden con los filtros
                var detalles = db.DETALLE_CARTILLA
                    .Where(d => d.CARTILLA_cartilla_id == cartillaId &&
                                d.INMUEBLE.LOTE_INMUEBLE.lote_id == loteId &&
                                d.INMUEBLE_inmueble_id == inmuebleId).ToList();

                // Actualizar el campo revision_dos para los registros encontrados
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
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult ObtenerDetalleCartilla(int cartillaId, int? loteId, int? inmuebleId)
        {
            var query = db.DETALLE_CARTILLA.Where(dc => dc.CARTILLA_cartilla_id == cartillaId);

            if (loteId.HasValue)
            {
                query = query.Where(dc => dc.INMUEBLE.LOTE_INMUEBLE_lote_id == loteId.Value);
            }

            if (inmuebleId.HasValue)
            {
                query = query.Where(dc => dc.INMUEBLE_inmueble_id == inmuebleId.Value);
            }

            var detalleList = query.Select(dc => new
            {
                dc.detalle_cartilla_id,
                dc.estado_supv,
                dc.estado_autocontrol,
                dc.estado_ito,
                dc.revision_dos,
                // Otros campos que desees incluir
            }).ToList();

            return Json(detalleList, JsonRequestBehavior.AllowGet);
        }


        //enviar correo
        public void EnviarCorreosAutomatizados()
        {
            if (actualizacionEditCartilla)
            {
                var detallesConEstadoSupv = db.DETALLE_CARTILLA
                   .Where(dc => dc.estado_supv == true && dc.ITEM_VERIF.tipo_item == true && (dc.correo_enviado_ac == false || dc.correo_enviado_ac == null)
                   && (dc.correo_enviado_supv == false || dc.correo_enviado_supv == null))
                   .Include(dc => dc.ITEM_VERIF)
                   .Include(dc => dc.CARTILLA)
                   .Include(dc => dc.INMUEBLE.LOTE_INMUEBLE)
                   .ToList();

                var groupedDetalles = detallesConEstadoSupv
                    .GroupBy(dc => new { dc.INMUEBLE.LOTE_INMUEBLE.lote_id, dc.INMUEBLE.inmueble_id, dc.CARTILLA_cartilla_id });

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
                    var obspublicas = primerDetalle.CARTILLA.observaciones;
                    var obspriv = primerDetalle.CARTILLA.observaciones_priv;

                    var correoAutocontrol = ObtenerCorreoPorPerfilYObra(primerDetalle.CARTILLA.OBRA_obra_id, 3);
                    if (!string.IsNullOrEmpty(correoAutocontrol))
                    {
                        var correoSupervisor = ObtenerCorreoSupervisorAutenticado(primerDetalle.CARTILLA.OBRA_obra_id);

                        if (!string.IsNullOrEmpty(correoSupervisor))
                        {
                            var nombreCompletoSupervisor = ObtenerNombreCompletoSupervisorAutenticado();

                            EnviarCorreo(nombreActividad, codigoActividad, nombreObra, loteInmueble, inmueble, cartillaId, fechaSupv, correoAutocontrol, correoSupervisor, nombreCompletoSupervisor, obspublicas, obspriv);
                        }

                        foreach (var detalle in grupo)
                        {
                            detalle.correo_enviado_ac = true;
                            detalle.correo_enviado_supv = true;
                        }
                    }
                }

                db.SaveChanges();

                actualizacionEditCartilla = false;
            }
        }


        private string ObtenerNombreCompletoSupervisorAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            if (usuarioAutenticado != null)
            {
                var persona = usuarioAutenticado.PERSONA;
                if (persona != null)
                {
                    return $"{persona.nombre} {persona.apeliido_paterno} {persona.apellido_materno}";
                }
            }
            return null;
        }


      

        private string ObtenerCorreoSupervisorAutenticado(int obraId)
        {
            if (Session != null && Session["UsuarioAutenticado"] is USUARIO usuarioAutenticado)
            {
                // Obtener las IDs de obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                // Verifica si la obra solicitada está en la lista de obras a las que el usuario tiene acceso
                if (obrasAcceso.Contains(obraId))
                {
                    // Buscar el supervisor asociado a la obra con el perfil específico
                    var correoSupervisor = (from u in db.USUARIO
                                            join a in db.ACCESO_OBRAS on u.usuario_id equals a.usuario_id
                                            join p in db.PERSONA on u.PERSONA_rut equals p.rut
                                            where a.obra_id == obraId
                                              && u.PERFIL_perfil_id == 4
                                              && a.usuario_id == usuarioAutenticado.usuario_id
                                            select p.correo).FirstOrDefault();

                    return correoSupervisor;
                }
            }

            return null;
        }



        private string ObtenerCorreoPorPerfilYObra(int obraId, int perfilId)
        {
            // Buscar el correo del usuario con el perfil dado y la obra especificada
            var correo = (from a in db.ACCESO_OBRAS
                          join u in db.USUARIO on a.usuario_id equals u.usuario_id
                          join p in db.PERSONA on u.PERSONA_rut equals p.rut
                          where a.obra_id == obraId
                            && u.PERFIL_perfil_id == perfilId
                          select p.correo).FirstOrDefault();

            return correo;
        }



        private void EnviarCorreo(string nombreActividad, string codigoActividad, string nombreobra, string loteInmueble, string inmueble, int cartillaid, DateTime? fechasupv, string destinatarioAutocontrol, string destinatarioSupervisor, string nombreSupervisor, string observacionesPublicas = null, string observacionesPrivadas = null)
        {
            string asunto = $"V°B° SUPV {codigoActividad} - {nombreActividad} {nombreobra}";
            string body = $"Estimad@,\n\nInformo a usted la aprobación de lote de inmueble {loteInmueble} del inmueble {inmueble} para la cartilla de actividad {codigoActividad} {nombreActividad}." +
                         $"\n\nCon fecha {fechasupv?.ToString("dd-MM-yyyy") ?? "N/A"} para obra {nombreobra}.";

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

            body += $"\n\nSaludos cordiales,\n\n {nombreSupervisor}.";

            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "uraa qkpw rnyd asvb");
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com");
                        mailMessage.Subject = asunto;
                        mailMessage.Body = body;
                        mailMessage.To.Add(destinatarioAutocontrol);
                        mailMessage.CC.Add(destinatarioSupervisor);

                        // Agregar mensaje de consola para saber cuándo se está intentando enviar el correo
                        Console.WriteLine($"Intentando enviar correo a {destinatarioAutocontrol} y CC a {destinatarioSupervisor}...");

                        smtpClient.Send(mailMessage);

                        // Agregar mensaje de consola para confirmar que el correo se envió correctamente
                        Console.WriteLine($"Correo enviado exitosamente a {destinatarioAutocontrol} y CC a {destinatarioSupervisor}.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Capturar excepciones y mostrar mensaje de error en consola
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
            }
        }


    }
}