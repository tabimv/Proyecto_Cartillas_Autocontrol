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
using ClosedXML.Excel;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ItemVerifAutocontrolController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        public async Task<ActionResult> ItemLista()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = await db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToListAsync();

                // Filtra los ítems basándote en las actividades asociadas a las obras a las que el usuario tiene acceso
                var itemsGroupedByActivity = await db.ITEM_VERIF
                    .Include(i => i.ACTIVIDAD.OBRA)
                    .Where(i => obrasAcceso.Contains(i.ACTIVIDAD.OBRA_obra_id)) // Filtra por obras a las que el usuario tiene acceso
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


        public ActionResult ExportToExcel()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtra las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var obrasAcceso = db.ACCESO_OBRAS
                                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                    .Select(a => a.obra_id)
                                    .ToList();

                var items = db.ITEM_VERIF.Include(r => r.ACTIVIDAD)
                    .Where(c => obrasAcceso.Contains(c.ACTIVIDAD.OBRA_obra_id))                 
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Items");
                    worksheet.Cell(1, 1).Value = "Label";
                    worksheet.Cell(1, 2).Value = "Elemento Verificación";
                    worksheet.Cell(1, 3).Value = "Item de tipo administrativo";
                    worksheet.Cell(1, 4).Value = "Actividad Asociada";



                    int row = 2;
                    foreach (var item in items)
                    {
                        worksheet.Cell(row, 1).Value = item.label;
                        worksheet.Cell(row, 2).Value = item.elemento_verificacion;
                        worksheet.Cell(row, 3).Value = item.tipo_item ? "Sí" : "No";
                        worksheet.Cell(row, 4).Value = item.ACTIVIDAD.codigo_actividad + " - " + item.ACTIVIDAD.nombre_actividad;


                        row++;
                    }

                    // Ajustar el ancho de las columnas según el contenido
                    worksheet.Columns().AdjustToContents();

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "ItemsVerificación.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }
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

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = await db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToListAsync();

                // Busca la actividad seleccionada
                var actividadSeleccionado = await db.ACTIVIDAD.FindAsync(actividadId);
                if (actividadSeleccionado == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                // Filtra los ítems basándote en la actividad seleccionada y las obras a las que el usuario tiene acceso
                var items = await db.ITEM_VERIF
                    .Where(a => a.ACTIVIDAD_actividad_id == actividadId)
                    .Include(i => i.ACTIVIDAD.OBRA)
                    .Where(i => obrasAcceso.Contains(i.ACTIVIDAD.OBRA_obra_id)) // Filtra por obras a las que el usuario tiene acceso
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
            // Verifica si hay algún detalle de cartilla asociado a los ítems que se van a eliminar
            var detalleCartillaRelacionado = await db.DETALLE_CARTILLA
                .AnyAsync(d => d.ITEM_VERIF.ACTIVIDAD_actividad_id == actividadId);

            if (detalleCartillaRelacionado)
            {
                // Si hay detalles de cartilla relacionados, muestra un mensaje de error y vuelve a cargar la vista actual
                ViewBag.ErrorMessage = "No se pueden eliminar los ítems porque están relacionados con una Cartilla de Autocontrol.";

                // Obtén la actividad correspondiente
                var actividad = await db.ACTIVIDAD.FindAsync(actividadId);
                if (actividad == null)
                {
                    return HttpNotFound(); // Manejar el caso donde la actividad no existe
                }

                // Devuelve la vista actual con el mensaje de error y el modelo correspondiente
                return View("ConfirmarEliminarItems", actividad);
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarPorActividad(List<ITEM_VERIF> registros)
        {
            if (registros == null)
            {
                return RedirectToAction("Error");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    foreach (var item in registros)
                    {
                        // Obtener el objeto original desde la base de datos
                        var originalItem = await db.ITEM_VERIF.FindAsync(item.item_verif_id);

                        if (originalItem != null)
                        {
                            // Actualizar las propiedades necesarias
                            originalItem.label = item.label;
                            originalItem.elemento_verificacion = item.elemento_verificacion;
                            originalItem.tipo_item = item.tipo_item;

                            // Marcar el objeto como modificado
                            db.Entry(originalItem).State = EntityState.Modified;
                        }
                    }

                    // Guardar los cambios en la base de datos
                    await db.SaveChangesAsync();

                    return RedirectToAction("ItemLista");
                }
                catch (Exception ex)
                {
                    // Manejar cualquier excepción aquí
                    return RedirectToAction("Error");
                }
            }

            // Si hay errores de validación, regresar a la vista con los datos existentes
            ViewBag.ACTIVIDAD_actividad_id = registros.FirstOrDefault()?.ACTIVIDAD_actividad_id;
            return View("EditarPorActividad", registros);
        }

        public ActionResult Crear()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];

                var obrasAcceso = db.ACCESO_OBRAS
                                 .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                 .Select(a => a.obra_id)
                                 .ToList();

                var actividadesAsociadas = db.ACTIVIDAD
                   .Where(c => obrasAcceso.Contains(c.OBRA_obra_id))
                   .Where(a => a.estado == "A")
                    .ToList();

                var obrasAsociadas = db.OBRA
                    .Where(c => obrasAcceso.Contains(c.obra_id))
                    .ToList();


                ViewBag.ObraList = new SelectList(obrasAsociadas.Where(o => o.ACTIVIDAD.Any()), "obra_id", "nombre_obra");
                ViewBag.Actividades = new SelectList(actividadesAsociadas, "actividad_id", "nombre_actividad");

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
                var obrasAcceso = db.ACCESO_OBRAS
                            .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                            .Select(a => a.obra_id)
                            .ToList();

                try
                {
                    if (ModelState.IsValid)
                    {
                        int actividadId = int.Parse(form["ActividadId"]);

                        // Obtener la cantidad de registros que se están guardando
                        int registrosAGuardar = (form.Count - 1) / 3; // Restar 1 porque 'ActividadId' también está en el formulario

                        for (int i = 0; i < registrosAGuardar; i++)
                        {
                            var nuevoItem = new ITEM_VERIF
                            {
                                elemento_verificacion = form[$"ItemsVerif[{i}].ElementoVerificacion"],
                                label = form[$"ItemsVerif[{i}].Label"],
                                tipo_item = form[$"ItemsVerif[{i}].TipoItem"] == "1", // Convertir a booleano (true si es "1", false si es "0")
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

                var actividadesAsociadas = db.ACTIVIDAD
                     .Where(c => obrasAcceso.Contains(c.OBRA_obra_id))
                     .Where(a => a.estado == "A")
                      .ToList();

                var obrasAsociadas = db.OBRA
                    .Where(c => obrasAcceso.Contains(c.obra_id))
                    .ToList();

                ViewBag.ObraList = new SelectList(obrasAsociadas.Where(o => o.ACTIVIDAD.Any()), "obra_id", "nombre_obra");
                ViewBag.Actividades = new SelectList(actividadesAsociadas, "actividad_id", "nombre_actividad");

                return View("Crear");
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }


        public ActionResult CrearItems()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];

                var obrasAcceso = db.ACCESO_OBRAS
                        .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                        .Select(a => a.obra_id)
                        .ToList();

                var actividadesAsociadas = db.ACTIVIDAD
                        .Where(c => obrasAcceso.Contains(c.OBRA_obra_id))
                        .Where(a => a.estado == "A")
                         .ToList();

                var obrasUsuarioIds = db.ACCESO_OBRAS
                              .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                              .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                              .Distinct()
                              .ToList();

                // Obtener las obras que tienen al menos una actividad con estado "A"
                var obrasConActividadesA = db.OBRA
                .Where(o => o.ACTIVIDAD.Any(a => a.estado == "A"))
                .Select(o => o.obra_id) // Solo obtener el ID de la obra
                .ToList();

                // Obtener todas las actividades relacionadas con las obras filtradas que tienen estado "A"
                var actividades = db.ACTIVIDAD
                    .Where(a => a.estado == "A" && obrasConActividadesA.Contains(a.OBRA_obra_id))
                    .ToList();

                // Crear una lista de SelectListItem para las obras filtradas
                var obrasSelectList = new SelectList(db.OBRA
                    .Where(o => obrasConActividadesA.Contains(o.obra_id) && obrasUsuarioIds.Contains(o.obra_id)),
                    "obra_id",
                    "nombre_obra");

                // Asignar la lista de obras filtradas al ViewBag
                ViewBag.ObraList = obrasSelectList;



                // Crear una lista de SelectListItem para las actividades filtradas
                var actividadesSelectList = new SelectList(actividades, "actividad_id", "nombre_actividad");

                // Asignar la lista de actividades filtradas al ViewBag
                ViewBag.Actividades = actividadesSelectList;

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
        public ActionResult GuardarRegistrosItems(FormCollection form)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                var obrasUsuarioIds = db.ACCESO_OBRAS
                             .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                             .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                             .Distinct()
                             .ToList();

                try
                {
                    if (ModelState.IsValid)
                    {
                        int actividadId = int.Parse(form["ActividadId"]);
                        int totalRegistros = int.Parse(form["TotalRegistros"]);


                        if (!VerificarCartilla(actividadId))
                        {
                            ModelState.AddModelError(string.Empty, "No se puede crear items para esta Actividad su Cartilla relacionada tiene estado Rechazada.");
                            // Regresar a la vista con los datos y mensajes de error
                            ViewBag.Actividades = new SelectList(db.ACTIVIDAD.Where(a => a.estado == "A").ToList(), "actividad_id", "nombre_actividad");
                            // Filtra las obras basadas en obrasUsuarioIds y que tengan actividades
                            var obrasFiltradas = db.OBRA.Where(o => obrasUsuarioIds.Contains(o.obra_id) && o.ACTIVIDAD.Any()).ToList();

                            // Asigna la lista filtrada al ViewBag para usarla en la vista
                            ViewBag.ObraList = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
                            return View("CrearItems");
                        }

                        // Obtener la cantidad de registros que se están guardando
                        int registrosAGuardar = totalRegistros; // Restar 1 porque 'ActividadId' también está en el formulario

                        for (int i = 0; i < registrosAGuardar; i++)
                        {
                            var nuevoItem = new ITEM_VERIF
                            {
                                elemento_verificacion = form[$"ItemsVerif[{i}].ElementoVerificacion"],
                                label = form[$"ItemsVerif[{i}].Label"],
                                tipo_item = form[$"ItemsVerif[{i}].TipoItem"] == "1", // Convertir a booleano (true si es "1", false si es "0")
                                ACTIVIDAD_actividad_id = actividadId

                            };

                            if (db.ITEM_VERIF.Any(item => item.label == nuevoItem.label && item.ACTIVIDAD_actividad_id == nuevoItem.ACTIVIDAD_actividad_id))
                            {
                                ModelState.AddModelError(string.Empty, $"Ya existe un registro con el label '{nuevoItem.label}' para esta actividad.");
                                // Regresar a la vista con los datos y mensajes de error
                                ViewBag.Actividades = new SelectList(db.ACTIVIDAD.Where(a => a.estado == "A").ToList(), "actividad_id", "nombre_actividad");
                                // Filtra las obras basadas en obrasUsuarioIds y que tengan actividades
                                var obrasFiltradas = db.OBRA.Where(o => obrasUsuarioIds.Contains(o.obra_id) && o.ACTIVIDAD.Any()).ToList();

                                // Asigna la lista filtrada al ViewBag para usarla en la vista
                                ViewBag.ObraList = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
                                return View("CrearItems");
                            }

                            db.ITEM_VERIF.Add(nuevoItem);
                        }

                        db.SaveChanges();
                        return RedirectToAction("ItemLista");
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    // Manejo de errores aquí
                    var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);
                    var fullErrorMessage = string.Join("; ", errorMessages);
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los registros: " + exceptionMessage);
                }
                catch (Exception ex)
                {
                    // Manejo de errores aquí
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los registros: " + ex.Message);
                }

                var actividadesAsociadas = db.ACTIVIDAD
                    .Where(c => obrasUsuarioIds.Contains(c.OBRA_obra_id))
                    .Where(a => a.estado == "A")
                     .ToList();

             



                // Obtener las obras que tienen al menos una actividad con estado "A"
                var obrasConActividadesA = db.OBRA
                .Where(c => obrasUsuarioIds.Contains(c.obra_id))
                .Select(o => o.obra_id) // Solo obtener el ID de la obra
                .ToList();

                // Obtener todas las actividades relacionadas con las obras filtradas que tienen estado "A"
                var actividades = db.ACTIVIDAD
                    .Where(a => a.estado == "A" && obrasConActividadesA.Contains(a.OBRA_obra_id)).Where(c => obrasUsuarioIds.Contains(c.OBRA_obra_id))
                    .ToList();

                // Crear una lista de SelectListItem para las obras filtradas
                var obrasSelectList = new SelectList(db.OBRA.Where(o => obrasConActividadesA.Contains(o.obra_id)), "obra_id", "nombre_obra");

                // Asignar la lista de obras filtradas al ViewBag
                ViewBag.ObraList = obrasSelectList;

                // Crear una lista de SelectListItem para las actividades filtradas
                var actividadesSelectList = new SelectList(actividades, "actividad_id", "nombre_actividad");

                // Asignar la lista de actividades filtradas al ViewBag
                ViewBag.Actividades = actividadesSelectList;
                return View("CrearItems");

            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }


        private bool VerificarCartilla(int actividadId)
        {
            // Lógica para verificar si la actividad tiene una cartilla relacionada
            // y si esa cartilla tiene estado_final_id = 2
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.ACTIVIDAD_actividad_id == actividadId);
            if (cartilla != null && cartilla.ESTADO_FINAL_estado_final_id == 2)
            {
                // Si la cartilla existe y tiene estado_final_id = 2, no permitir crear items
                return false;
            }

            // Permitir crear items si no hay cartilla relacionada o si la cartilla no tiene estado_final_id = 2
            return true;
        }


        public async Task<ActionResult> Edit(int? id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasUsuarioIds = db.ACCESO_OBRAS
                          .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                          .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                          .Distinct()
                          .ToList();

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
                .Where(c => obrasUsuarioIds.Contains(c.OBRA_obra_id)).ToList(); // Obtén la lista de actividades desde tu base de datos

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
        public async Task<ActionResult> Edit([Bind(Include = "item_verif_id,elemento_verificacion,label,tipo_item,ACTIVIDAD_actividad_id")] ITEM_VERIF iTEM_VERIF)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasUsuarioIds = db.ACCESO_OBRAS
                        .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                        .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                        .Distinct()
                        .ToList();

                if (ModelState.IsValid)
                {
                    db.Entry(iTEM_VERIF).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("ItemLista");
                }

                var actividades = db.ACTIVIDAD.Include(a => a.OBRA)
            .Where(c => obrasUsuarioIds.Contains(c.OBRA_obra_id)).ToList(); // Obtén la lista de actividades desde tu base de datos

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