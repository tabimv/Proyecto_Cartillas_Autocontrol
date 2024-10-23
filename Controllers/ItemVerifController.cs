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
using System.Text.RegularExpressions;


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ItemVerifController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: ItemVerif
        public async Task<ActionResult> Index()
        {
            var iTEM_VERIF = db.ITEM_VERIF.Include(i => i.ACTIVIDAD).OrderBy(i => i.label).ThenBy(i => i.ACTIVIDAD_actividad_id);
            return View(await iTEM_VERIF.ToListAsync());
        }

        public ActionResult ExportToExcel()
        {
            var items = db.ITEM_VERIF.Include(r => r.ACTIVIDAD).ToList();

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

                // Obtén los items verificados filtrados por las obras a las que el usuario tiene acceso
                var itemsGroupedByActivity = await db.ITEM_VERIF
                    .Where(i => db.ACTIVIDAD
                        .Where(a => obrasAcceso.Contains(a.OBRA.obra_id))
                        .Select(a => a.actividad_id)
                        .Contains(i.ACTIVIDAD_actividad_id))
                    .OrderBy(i => i.ACTIVIDAD_actividad_id)
                    .GroupBy(i => i.ACTIVIDAD_actividad_id)
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


        public class AlphanumericComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                string[] partsX = Regex.Split(x, @"([0-9]+)");
                string[] partsY = Regex.Split(y, @"([0-9]+)");

                int length = Math.Min(partsX.Length, partsY.Length);
                for (int i = 0; i < length; i++)
                {
                    if (partsX[i] != partsY[i])
                    {
                        if (int.TryParse(partsX[i], out int numX) && int.TryParse(partsY[i], out int numY))
                        {
                            return numX.CompareTo(numY);
                        }
                        else
                        {
                            return partsX[i].CompareTo(partsY[i]);
                        }
                    }
                }

                return partsX.Length.CompareTo(partsY.Length);
            }
        }


        public async Task<ActionResult> ItemDetails(int actividadId)
        {
            var actividadSeleccionado = await db.ACTIVIDAD.FindAsync(actividadId);
            if (actividadSeleccionado == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }

            var items = await db.ITEM_VERIF
                .Where(a => a.ACTIVIDAD_actividad_id == actividadId)
                .ToListAsync();

            items = items.OrderBy(a => a.label, new AlphanumericComparer()).ToList();

            ViewBag.ActividadSeleccionado = actividadSeleccionado;

            return View(items);
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


        private bool ActividadExists(int id)
        {
            return db.ACTIVIDAD.Any(e => e.actividad_id == id);
        }

        // GET: ItemVerif/Details/5
        public async Task<ActionResult> Details(int? id)
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

        // GET: ItemVerif/Create
        public ActionResult Create()
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

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad");
                ViewBag.ACTIVIDAD_nombre_actividad = new SelectList(db.ACTIVIDAD, "actividad_id", "nombre_actividad");
                return View();
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        // POST: ItemVerif/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "item_verif_id,elemento_verificacion,label,ACTIVIDAD_actividad_id")] ITEM_VERIF iTEM_VERIF)
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
                    db.ITEM_VERIF.Add(iTEM_VERIF);
                    await db.SaveChangesAsync();
                    return RedirectToAction("ItemLista");
                }

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(db.ACTIVIDAD, "actividad_id", "codigo_actividad", iTEM_VERIF.ACTIVIDAD_actividad_id);
                return View(iTEM_VERIF);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }



        // GET: ItemVerif/Edit/5
        public async Task<ActionResult> Edit(int? id)
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

            var actividades = db.ACTIVIDAD.ToList(); // Obtén la lista de actividades desde tu base de datos

            // Crear una lista de objetos anónimos con los campos que necesitas
            var listaActividades = actividades.Select(a => new
            {
                ActividadId = a.actividad_id,
                CodigoYNombre = $"{a.codigo_actividad} - {a.nombre_actividad}"
            }).ToList();

            ViewBag.ACTIVIDAD_actividad_id = new SelectList(listaActividades, "ActividadId", "CodigoYNombre");

            return View(iTEM_VERIF);
        }

        // POST: ItemVerif/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "item_verif_id,elemento_verificacion,label,tipo_item,ACTIVIDAD_actividad_id")] ITEM_VERIF iTEM_VERIF)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iTEM_VERIF).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("ItemLista");
            }

            var actividades = db.ACTIVIDAD.ToList(); // Obtén la lista de actividades desde tu base de datos

            // Crear una lista de objetos anónimos con los campos que necesitas
            var listaActividades = actividades.Select(a => new
            {
                ActividadId = a.actividad_id,
                CodigoYNombre = $"{a.codigo_actividad} - {a.nombre_actividad}"
            }).ToList();

            ViewBag.ACTIVIDAD_actividad_id = new SelectList(listaActividades, "ActividadId", "CodigoYNombre", iTEM_VERIF.ACTIVIDAD_actividad_id);
            return View(iTEM_VERIF);
        }

        // GET: ItemVerif/Delete/5
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

            if (iTEM_VERIF == null)
            {
                return HttpNotFound();
            }
          
            bool tieneCartillaRelacionadas = db.DETALLE_CARTILLA.Any(c => c.ITEM_VERIF_item_verif_id == id);

            if (tieneCartillaRelacionadas)
            {

                ViewBag.ErrorMessage = "No se puede eliminar este Item porque está relacionada con una Cartilla de Autocontrol.";
                return View("Delete", iTEM_VERIF); // Mostrar vista de eliminación con el mensaje de error

            }
            else
            {
                db.ITEM_VERIF.Remove(iTEM_VERIF);
                await db.SaveChangesAsync();
                return RedirectToAction("ItemLista");
            }
        }


        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Palabras que deben permanecer en minúscula
            var lowercaseExceptions = new HashSet<string> { "a", "de", "en", "y", "o", "con", "por", "para", "al", "del", "un", "una", "los", "las" };

            input = input.Trim().ToLower();
            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                // Capitalizar la primera palabra o palabras que no están en la lista de excepciones
                if (i == 0 || !lowercaseExceptions.Contains(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }

        public ActionResult Crear()
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
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarRegistros(FormCollection form)
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


                try
                {
                    if (ModelState.IsValid)
                    {
                        // Transformar los campos para que la primera letra sea mayúscula
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


                            return View("Crear");
                        }

                        // Obtener la cantidad de registros que se están guardando
                        int registrosAGuardar = totalRegistros; // Restar 1 porque 'ActividadId' también está en el formulario

                        for (int i = 0; i < registrosAGuardar; i++)
                        {
                            var nuevoItem = new ITEM_VERIF
                            {
                                elemento_verificacion = CapitalizeFirstLetter(form[$"ItemsVerif[{i}].ElementoVerificacion"]),
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

                                return View("Crear");
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

                // En caso de errores, regresar a la vista con los datos y mensajes de error
                ViewBag.Actividades = new SelectList(db.ACTIVIDAD.Where(a => a.estado == "A").ToList(), "actividad_id", "nombre_actividad");
                ViewBag.ObraList = new SelectList(db.OBRA.Where(o => o.ACTIVIDAD.Any()), "obra_id", "nombre_obra");
                return View("Crear");
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
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


        [HttpPost]
        public ActionResult VerificarEstadoCartilla(int actividadId)
        {
            // Verificar si existe una cartilla asociada a la actividad seleccionada
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.ACTIVIDAD_actividad_id == actividadId);

            if (cartilla != null && cartilla.ESTADO_FINAL.descripcion == "Rechazada")
            {
                return Json(new { cartillaRechazada = true });
            }

            return Json(new { cartillaRechazada = false });
        }


        [HttpPost]
        public ActionResult ValidarLabel(string label, int actividadId)
        {
            // Verificar si ya existe un registro con el mismo label y actividad_id en la base de datos
            bool labelExistente = db.ITEM_VERIF.Any(item => item.label == label && item.ACTIVIDAD_actividad_id == actividadId);

            // Devolver una respuesta JSON indicando si el label ya existe o no
            return Json(new { existe = labelExistente });
        }



        public JsonResult GetActividadesByObra(int obraId)
        {
            var actividades = db.ACTIVIDAD.Where(a => a.OBRA_obra_id == obraId && a.estado == "A")
                                  .Select(a => new
                                  {
                                      Value = a.actividad_id,
                                      Text = a.nombre_actividad
                                  })
                                  .ToList();

            return Json(actividades, JsonRequestBehavior.AllowGet);
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
