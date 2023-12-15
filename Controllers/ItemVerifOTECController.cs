using System;
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
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;
using Microsoft.Win32;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ItemVerifOTECController : Controller
    {
        private ObraManzanoDicEntities db = new ObraManzanoDicEntities();

        public async Task<ActionResult> ItemLista()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var itemsGroupedByActivity = await db.ITEM_VERIF
                .Include(i => i.ACTIVIDAD.OBRA.USUARIO)
                  .Where(i => i.ACTIVIDAD.OBRA.USUARIO.Any(r => r.PERFIL.rol == usuarioAutenticado.PERFIL.rol))
                .OrderBy(e => e.ACTIVIDAD_actividad_id)
                .GroupBy(e => e.ACTIVIDAD_actividad_id)
                .Select(g => g.FirstOrDefault())
                .ToListAsync();


                return View(itemsGroupedByActivity);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> ItemDetails(int actividadId)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var actividadSeleccionado = await db.ACTIVIDAD.FindAsync(actividadId);
                if (actividadSeleccionado == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var items = await db.ITEM_VERIF
                    .Where(a => a.ACTIVIDAD_actividad_id == actividadId)
                    .Include(i => i.ACTIVIDAD.OBRA.USUARIO)
                    .Where(i => i.ACTIVIDAD.OBRA.USUARIO.Any(r => r.PERFIL.rol == usuarioAutenticado.PERFIL.rol))
                    .OrderBy(a => a.label)
                    .ToListAsync();


                ViewBag.ActividadSeleccionado = actividadSeleccionado;

                return View(items);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> ConfirmarEliminarItems(int actividadId)
        {
            var actividad = await db.ACTIVIDAD.FindAsync(actividadId); // Obtén la actividad por su ID
            if (actividad == null)
            {
                return HttpNotFound(); // Manejar el caso donde la actividad no existe
            }

            return View(actividad);
        }

        [HttpPost]
        public async Task<ActionResult> EliminarItemsPorActividad(int actividadId)
        {
            // Encuentra y elimina todos los registros de ITEM_VERIF asociados a la actividadId
            var itemsAEliminar = await db.ITEM_VERIF
                .Where(e => e.ACTIVIDAD_actividad_id == actividadId)
                .ToListAsync();

            if (itemsAEliminar == null || !itemsAEliminar.Any())
            {
                return HttpNotFound(); // O el código de error que desees si no se encuentran elementos para eliminar
            }

            // Elimina los registros asociados a la actividadId
            db.ITEM_VERIF.RemoveRange(itemsAEliminar);
            await db.SaveChangesAsync();

            return RedirectToAction("ItemLista"); // Redirige a la vista de la lista de ítems
        }

        public async Task<ActionResult> EditarPorActividad(int? actividadId)
        {
            if (actividadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Obtener todos los registros asociados a la actividad
            var registros = await db.ITEM_VERIF.Where(item => item.ACTIVIDAD_actividad_id == actividadId).ToListAsync();

            if (registros == null || registros.Count == 0)
            {
                return HttpNotFound();
            }

            ViewBag.ACTIVIDAD_actividad_id = actividadId;
            return View("EditarPorActividad", registros);
        }

        // POST: ItemVerif/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarPorActividad(List<ITEM_VERIF> registros)
        {
            if (registros == null)
            {
                // Manejar el caso en que la lista de registros es null, por ejemplo, redirigir a una página de error
                return RedirectToAction("Error");
            }

            if (ModelState.IsValid)
            {
                foreach (var item in registros)
                {
                    // Adjunta el objeto al contexto para que EF pueda rastrear los cambios
                    db.Entry(item).State = EntityState.Modified;
                }

                await db.SaveChangesAsync();
                return RedirectToAction("ItemLista");
            }

            // Si hay errores de validación, es posible que necesites volver a cargar la lista desde la base de datos y volver a mostrar la vista
            // Esto es para asegurarse de que los cambios no confirmados no se pierdan
            ViewBag.ACTIVIDAD_actividad_id = registros.FirstOrDefault()?.ACTIVIDAD_actividad_id;
            return View("EditarPorActividad", registros);
        }

        public ActionResult Crear()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                var obrasAsociadas = db.ACTIVIDAD
                    .Include(a => a.OBRA.USUARIO)
                    .Where(a => a.OBRA.USUARIO.Any(r => r.PERFIL.rol == usuarioAutenticado.PERFIL.rol))
                    .Where(a => a.estado == "A")
                    .ToList();

                ViewBag.ObrasAsociadas = new SelectList(obrasAsociadas, "actividad_id", "nombre_actividad");

                return View();

            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarRegistros(FormCollection form)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"]; 

                try
                {
                    if (ModelState.IsValid)
                    {
                        int actividadId = int.Parse(form["ActividadId"]);

                        // Obtener la cantidad de registros que se están guardando
                        int registrosAGuardar = (form.Count - 1) / 2; // Restar 1 porque 'ActividadId' también está en el formulario

                        for (int i = 0; i < registrosAGuardar; i++)
                        {
                            var nuevoItem = new ITEM_VERIF
                            {
                                elemento_verificacion = form[$"ItemsVerif[{i}].ElementoVerificacion"],
                                label = form[$"ItemsVerif[{i}].Label"],
                                ACTIVIDAD_actividad_id = actividadId
                            };

                            db.ITEM_VERIF.Add(nuevoItem);
                        }

                        db.SaveChanges();
                        return RedirectToAction("ItemLista");
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores aquí
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los registros: " + ex.Message);
                }

                var obrasAsociadas = db.ACTIVIDAD
                  .Include(a => a.OBRA.USUARIO)
                  .Where(a => a.OBRA.USUARIO.Any(r => r.PERFIL.rol == usuarioAutenticado.PERFIL.rol))
                  .Where(a => a.estado == "A")
                  .ToList();

                ViewBag.ObrasAsociadas = new SelectList(obrasAsociadas, "actividad_id", "nombre_actividad");
                return View("Crear");
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
                if (iTEM_VERIF == null)
                {
                    return HttpNotFound();
                }

                var actividades = db.ACTIVIDAD.Include(a => a.OBRA.RESPONSABLE)
                  .Where(a => a.OBRA.USUARIO.Any(r => r.PERFIL.rol == usuarioAutenticado.PERFIL.rol)).ToList(); // Obtén la lista de actividades desde tu base de datos

                // Crear una lista de objetos anónimos con los campos que necesitas
                var listaActividades = actividades.Select(a => new
                {
                    ActividadId = a.actividad_id,
                    CodigoYNombre = $"{a.codigo_actividad} - {a.nombre_actividad}"
                }).ToList();

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(listaActividades, "ActividadId", "CodigoYNombre");

                return View(iTEM_VERIF);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        // POST: ItemVerif/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "item_verif_id,elemento_verificacion,label,ACTIVIDAD_actividad_id")] ITEM_VERIF iTEM_VERIF)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                if (ModelState.IsValid)
                {
                    db.Entry(iTEM_VERIF).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("ItemLista");
                }

                var actividades = db.ACTIVIDAD.Include(a => a.OBRA.USUARIO)
                    .Where(a => a.OBRA.USUARIO.Any(r => r.PERFIL.rol == usuarioAutenticado.PERFIL.rol)).ToList(); // Obtén la lista de actividades desde tu base de datos

                // Crear una lista de objetos anónimos con los campos que necesitas
                var listaActividades = actividades.Select(a => new
                {
                    ActividadId = a.actividad_id,
                    CodigoYNombre = $"{a.codigo_actividad} - {a.nombre_actividad}"
                }).ToList();

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(listaActividades, "ActividadId", "CodigoYNombre", iTEM_VERIF.ACTIVIDAD_actividad_id);
                return View(iTEM_VERIF);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
            if (iTEM_VERIF == null)
            {
                return HttpNotFound();
            }
            return View(iTEM_VERIF);
        }

        // POST: ItemVerif/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            ITEM_VERIF iTEM_VERIF = await db.ITEM_VERIF.FindAsync(id);
            db.ITEM_VERIF.Remove(iTEM_VERIF);
            await db.SaveChangesAsync();
            return RedirectToAction("ItemLista");
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