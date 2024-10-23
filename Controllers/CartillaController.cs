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
using ClosedXML.Excel;
using System.Diagnostics;
using DocumentFormat.OpenXml.Drawing;
using BitMiracle.LibTiff.Classic;
using DocumentFormat.OpenXml.EMMA;
using System.Net.Mail;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class CartillaController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: Cartilla
        public async Task<ActionResult> Index()
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

                var cARTILLA = db.CARTILLA.Include(c => c.ACTIVIDAD).Include(c => c.ESTADO_FINAL).Include(c => c.OBRA)
                     .Where(a => obrasAcceso.Contains(a.OBRA.obra_id));
                return View(await cARTILLA.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public ActionResult ExportToExcel()
        {
            var cartillas = db.CARTILLA.Include(r => r.DETALLE_CARTILLA).Include(r => r.ACTIVIDAD).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Cartillas");
                worksheet.Cell(1, 1).Value = "ID Cartilla";
                worksheet.Cell(1, 2).Value = "Fecha de creación";
                worksheet.Cell(1, 3).Value = "Obra Asociada";
                worksheet.Cell(1, 4).Value = "Actividad Asociada";
                worksheet.Cell(1, 5).Value = "Fecha modificación";
                worksheet.Cell(1, 6).Value = "Estado Final";


                int row = 2;
                foreach (var cartilla in cartillas)
                {
                    worksheet.Cell(row, 1).Value = cartilla.cartilla_id;
                    worksheet.Cell(row, 2).Value = cartilla.fecha;
                    worksheet.Cell(row, 3).Value = cartilla.OBRA.nombre_obra;
                    worksheet.Cell(row, 4).Value = $"{cartilla.ACTIVIDAD.codigo_actividad} {cartilla.ACTIVIDAD.nombre_actividad}";
                    worksheet.Cell(row, 5).Value = cartilla.fecha_modificacion;
                    worksheet.Cell(row, 6).Value = cartilla.ESTADO_FINAL.descripcion;


                    row++;
                }


                // Ajustar el ancho de las columnas según el contenido
                worksheet.Columns().AdjustToContents();

                var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);

                var fileName = "CartillasAutocontrol.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream.ToArray(), contentType, fileName);
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
                using (var dbContext = new ObraManzanoFinal())  // Reemplaza con el nombre de tu contexto de base de datos
                {

                    var obrasUsuarioIds = dbContext.ACCESO_OBRAS
                              .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                              .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                              .Distinct()
                              .ToList();
                    // Supongamos que tienes una entidad llamada Configuracion con una propiedad ItemVerifId
                    viewModel.ActividadesList = dbContext.ACTIVIDAD
                               .Where(a => obrasUsuarioIds.Contains(a.OBRA.obra_id))
                               .Where(a => a.estado == "A" && a.ITEM_VERIF.Any() &&
                                           !dbContext.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id) &&
                                           a.OBRA.LOTE_INMUEBLE.Any()).ToList();

                    viewModel.ElementosVerificacion = dbContext.ITEM_VERIF
                                       .Where(i => obrasUsuarioIds.Contains(i.ACTIVIDAD.OBRA.obra_id)).ToList();


                    viewModel.InmuebleList = dbContext.INMUEBLE.ToList();
                    viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();

                    viewModel.ObraList = dbContext.OBRA
                                .Where(obra =>
                                               obra.ACTIVIDAD.Any(a => a.ITEM_VERIF.Any() &&
                                               !dbContext.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id)) &&
                                               obra.LOTE_INMUEBLE.Any())
                                 .Where(o => obrasUsuarioIds.Contains(o.obra_id))
                                .ToList();

                    viewModel.LoteInmuebleList = dbContext.LOTE_INMUEBLE
                                      .Where(o => obrasUsuarioIds.Contains(o.OBRA.obra_id)).ToList();
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
                            // Recuperar las IDs de las obras a las que el usuario tiene acceso
                            var obrasUsuarioIds = dbContext.ACCESO_OBRAS
                                .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                                .Distinct()
                                .ToList();

                            // Verificar si ya existe una cartilla con las mismas combinaciones de FK y un estado final diferente de 1
                            bool existeCartilla = dbContext.CARTILLA.Any(c =>
                                c.OBRA_obra_id == viewModel.Cartilla.OBRA_obra_id &&
                                c.ACTIVIDAD_actividad_id == viewModel.Cartilla.ACTIVIDAD_actividad_id &&
                                c.ESTADO_FINAL_estado_final_id == 1);
                            if (existeCartilla)
                            {
                                ModelState.AddModelError("", "Ya existe una misma Cartilla con un Estado Final de Visto Bueno.");

                                // Recargar las listas necesarias para volver a mostrar la vista con los datos
                                viewModel.ActividadesList = dbContext.ACTIVIDAD
                                .Where(a => obrasUsuarioIds.Contains(a.OBRA.obra_id))
                                .Where(a => a.estado == "A" && a.ITEM_VERIF.Any() &&
                                            !dbContext.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id) &&
                                            a.OBRA.LOTE_INMUEBLE.Any()).ToList();


                                viewModel.ElementosVerificacion = dbContext.ITEM_VERIF
                                        .Where(i => obrasUsuarioIds.Contains(i.ACTIVIDAD.OBRA.obra_id)).ToList();
                                viewModel.InmuebleList = dbContext.INMUEBLE.ToList();
                                viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();

                                viewModel.ObraList = dbContext.OBRA
                                .Where(obra =>
                                               obra.ACTIVIDAD.Any(a => a.ITEM_VERIF.Any() &&
                                               !dbContext.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id)) &&
                                               obra.LOTE_INMUEBLE.Any())
                                 .Where(o => obrasUsuarioIds.Contains(o.obra_id))
                                .ToList();
                                viewModel.LoteInmuebleList = dbContext.LOTE_INMUEBLE
                                       .Where(o => obrasUsuarioIds.Contains(o.OBRA.obra_id)).ToList();
                                return View(viewModel);
                            }
                            bool existeCartillaAutocontrol = dbContext.CARTILLA.Any(c =>
                                   c.OBRA_obra_id == viewModel.Cartilla.OBRA_obra_id &&
                                   c.ACTIVIDAD_actividad_id == viewModel.Cartilla.ACTIVIDAD_actividad_id);
                            if (existeCartillaAutocontrol)
                            {
                                ModelState.AddModelError("", "Ya existe una cartilla de autocontrol asociada a esta actividad y obra seleccionada.");

                                // Recargar las listas necesarias para volver a mostrar la vista con los datos
                                viewModel.ActividadesList = dbContext.ACTIVIDAD
                                 .Where(a => obrasUsuarioIds.Contains(a.OBRA.obra_id))
                                 .Where(a => a.estado == "A" && a.ITEM_VERIF.Any() &&
                                             !dbContext.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id) &&
                                             a.OBRA.LOTE_INMUEBLE.Any()).ToList();
                                viewModel.ElementosVerificacion = dbContext.ITEM_VERIF
                                          .Where(i => obrasUsuarioIds.Contains(i.ACTIVIDAD.OBRA.obra_id)).ToList();
                                viewModel.InmuebleList = dbContext.INMUEBLE.ToList();
                                viewModel.EstadoFinalList = dbContext.ESTADO_FINAL.ToList();
                                viewModel.ObraList = dbContext.OBRA
                               .Where(obra =>
                                              obra.ACTIVIDAD.Any(a => a.ITEM_VERIF.Any() &&
                                              !dbContext.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id)) &&
                                              obra.LOTE_INMUEBLE.Any())
                                .Where(o => obrasUsuarioIds.Contains(o.obra_id))
                               .ToList();
                                viewModel.LoteInmuebleList = dbContext.LOTE_INMUEBLE
                                        .Where(o => obrasUsuarioIds.Contains(o.OBRA.obra_id)).ToList();

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
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }
        public ActionResult GetActividadByObra(int obraId)
        {
            try
            {
                // Buscar las actividades asociadas a la obra que tienen elementos de verificación asociados
                var actividades = db.ACTIVIDAD
                    .Where(a => a.OBRA_obra_id == obraId && a.ITEM_VERIF.Any() && !db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id))
                    .OrderBy(a => a.nombre_actividad)
                    .ToList();

                if (actividades.Any())
                {
                    // Devolver las actividades encontradas en formato JSON
                    var actividadesJson = actividades.Select(a => new
                    {
                        actividadId = a.actividad_id,
                        // Truncar el nombre de la actividad a 80 caracteres y agregar puntos suspensivos si es necesario
                        nombreActividad = (a.nombre_actividad.Length > 80) ? a.nombre_actividad.Substring(0, 80) + "..." : a.nombre_actividad
                    });

                    return Json(actividadesJson, JsonRequestBehavior.AllowGet);
                }

                // Si no se encuentran actividades asociadas a la obra con elementos de verificación, devuelve un mensaje de error
                return Json(new { error = "No se encontraron actividades asociadas a esta obra con elementos de verificación" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error que pueda surgir
                return Json(new { error = "Ocurrió un error al obtener las actividades: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult GetActividadByObrasActual(int obraId)
        {
            try
            {
                // Buscar las actividades asociadas a la obra que tienen elementos de verificación asociados
                var actividades = db.ACTIVIDAD
                   .Where(a => a.OBRA_obra_id == obraId && a.ITEM_VERIF.Any() && !db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id))
                   .ToList();
                if (actividades.Any())
                {
                    // Devuelve las actividades encontradas en formato JSON
                    return Json(actividades.Select(a => new { actividadId = a.actividad_id, nombreActividad = a.nombre_actividad }), JsonRequestBehavior.AllowGet);
                }
                // Si no se encuentran actividades asociadas a la obra con elementos de verificación, devuelve un mensaje de error
                return Json(new { error = "No se encontraron actividades asociadas a esta obra con elementos de verificación" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error que pueda surgir
                return Json(new { error = "Ocurrió un error al obtener las actividades: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult GetActividadByObrasinverif(int obraId)
        {
            try
            {
                // Buscar las actividades asociadas a la obra en la base de datos
                var actividades = db.ACTIVIDAD.Where(a => a.OBRA_obra_id == obraId).ToList();
                if (actividades.Any())
                {
                    // Devuelve las actividades encontradas en formato JSON
                    return Json(actividades.Select(a => new { actividadId = a.actividad_id, nombreActividad = a.nombre_actividad }), JsonRequestBehavior.AllowGet);
                }
                // Si no se encuentran actividades asociadas a la obra, devuelve un mensaje de error
                return Json(new { error = "No se encontraron actividades asociadas a esta obra" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error que pueda surgir
                return Json(new { error = "Ocurrió un error al obtener las actividades: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult GetActividadByObras(int obraId)
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

        public JsonResult GetCombinacionesElementosInmuebles(int actividadId)
        {
            try
            {
                using (var context = new ObraManzanoFinal())
                {
                    // Obtener el tipo de actividad
                    var tipoActividad = context.ACTIVIDAD.Where(a => a.actividad_id == actividadId).Select(a => a.tipo_actividad).FirstOrDefault();

                    // Imprimir el tipo de actividad para depuración
                    Debug.WriteLine("Tipo de Actividad: " + tipoActividad);

                    // Consulta basada en el tipo de actividad
                    var query = from iv in context.ITEM_VERIF
                                join a in context.ACTIVIDAD on iv.ACTIVIDAD_actividad_id equals a.actividad_id
                                join o in context.OBRA on a.OBRA_obra_id equals o.obra_id
                                join li in context.LOTE_INMUEBLE on o.obra_id equals li.OBRA_obra_id
                                join i in context.INMUEBLE on li.lote_id equals i.LOTE_INMUEBLE_lote_id
                                where iv.ACTIVIDAD_actividad_id == actividadId
                                 && (
                                     (tipoActividad == "P" && li.tipo_bloque == "Proyecto") ||
                                     (tipoActividad == "I" && (li.tipo_bloque == "Manzana" || li.tipo_bloque == "Torre"))
                                 )
                                select new
                                {
                                    iv.item_verif_id,
                                    iv.elemento_verificacion,
                                    i.inmueble_id,
                                    i.codigo_inmueble
                                };

                    var result = query.ToList();

                    // Agregar mensajes de depuración
                    Debug.WriteLine("Cantidad de resultados encontrados: " + result.Count);
                    Debug.WriteLine("Resultados obtenidos: ");
                    foreach (var item in result)
                    {
                        Debug.WriteLine("Item Verificación ID: " + item.item_verif_id);
                        Debug.WriteLine("Elemento de Verificación: " + item.elemento_verificacion);
                        Debug.WriteLine("Inmueble ID: " + item.inmueble_id);
                        Debug.WriteLine("Código Inmueble: " + item.codigo_inmueble);
                    }

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Agregar mensaje de error
                Console.WriteLine("Error al obtener combinaciones: " + ex.Message);
                return Json(new { error = "Error al obtener combinaciones: " + ex.Message }, JsonRequestBehavior.AllowGet);
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
            var elementos = db.INMUEBLE.Where(iv => iv.LOTE_INMUEBLE.OBRA_obra_id == obraID).ToList();

            // Devuelve los elementos de verificación en formato JSON
            var jsonData = elementos.Select(i => new { value = i.inmueble_id, text = i.inmueble_id }).ToList();
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
                    using (var dbContext = new ObraManzanoFinal())
                    {
                        // Actualizar la información de la Cartilla en la base de datos
                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;

                        // Verificar si el estado final es "Aprobado" (id 1)
                        if (viewModel.Cartilla.ESTADO_FINAL_estado_final_id == 1)
                        {
                            // Verificar si al menos un campo de aprobación está en falso
                            if (viewModel.DetalleCartillas.Any(detalle => detalle.estado_autocontrol == false || detalle.estado_ito == false || detalle.estado_supv == false))
                            {
                                TempData["ErrorMessage"] = "La Cartilla no puede tener Estado Final igual a Aprobado. Debido a que no todos los valores se encuentra aprobados.";
                                return RedirectToAction("Index", "Cartilla");
                            }
                        }
                        // Actualizar o agregar los detalles de la Cartilla en la base de datos
                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {

                            // Obtener el detalle original de la base de datos
                            var detalleOriginal = dbContext.DETALLE_CARTILLA.AsNoTracking().FirstOrDefault(d => d.detalle_cartilla_id == detalleCartilla.detalle_cartilla_id);

                            if (detalleOriginal != null && detalleCartilla.estado_supv != detalleOriginal.estado_supv && detalleCartilla.fecha_supv == null)
                            {
                                detalleCartilla.fecha_supv = DateTime.Now;
                            }


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




        [HttpGet]
        public ActionResult ConfirmarEliminarCartilla(int id)
        {
            using (var dbContext = new ObraManzanoFinal())
            {
                var cartilla = dbContext.CARTILLA
                    .Include(c => c.DETALLE_CARTILLA)
                    .Include(c => c.ACTIVIDAD)
                    .Include(c => c.OBRA)
                    .Include(c => c.ESTADO_FINAL)
                    .FirstOrDefault(c => c.cartilla_id == id);

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
                    var cartilla = dbContext.CARTILLA
                        .Include(c => c.DETALLE_CARTILLA)
                        .Include(c => c.ACTIVIDAD)
                        .Include(c => c.OBRA)
                        .Include(c => c.ESTADO_FINAL)
                        .FirstOrDefault(c => c.cartilla_id == id);

                    if (cartilla != null)
                    {
                        // Eliminar los detalles de la Cartilla
                        dbContext.DETALLE_CARTILLA.RemoveRange(cartilla.DETALLE_CARTILLA);

                        // Eliminar los registros asociados en ACCESO_CARTILLA
                        var accesosCartilla = dbContext.ACCESO_CARTILLA.Where(a => a.CARTILLA_cartilla_id == id).ToList();
                        if (accesosCartilla.Any())
                        {
                            dbContext.ACCESO_CARTILLA.RemoveRange(accesosCartilla);
                        }

                        // Verificar si el estado de la ACTIVIDAD es 'B'
                        if (cartilla.ACTIVIDAD != null && cartilla.ACTIVIDAD.estado == "B")
                        {
                            // Cambiar el estado de la ACTIVIDAD a 'A'
                            cartilla.ACTIVIDAD.estado = "A";
                            dbContext.Entry(cartilla.ACTIVIDAD).State = EntityState.Modified;
                        }

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


        public ActionResult AsignarCartillaSupervisor(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var cartilla = db.CARTILLA.Find(id);
            if (cartilla == null)
            {
                return HttpNotFound();
            }

            int obraId = cartilla.OBRA_obra_id;
            var obra = db.OBRA.Find(obraId); // Asegúrate de tener la tabla `OBRA` y la relación correcta
            var actividad = db.ACTIVIDAD.Find(cartilla.ACTIVIDAD_actividad_id); // Asegúrate de tener la tabla `ACTIVIDAD` y la relación correcta

            var usuariosSupervisores = db.ACCESO_OBRAS
                .Where(a => a.obra_id == obraId)
                .Select(a => a.usuario_id)
                .Distinct()
                .Join(db.USUARIO.Include(u => u.PERSONA)
                    .Where(u => u.estado_usuario == true && u.PERFIL_perfil_id == 4),
                    usuarioId => usuarioId,
                    usuario => usuario.usuario_id,
                    (usuarioId, usuario) => new
                    {
                        UsuarioId = usuario.usuario_id,
                        NombreCompleto = usuario.PERSONA.nombre + " " + usuario.PERSONA.apeliido_paterno + " " + usuario.PERSONA.apellido_materno,
                        TieneAcceso = db.ACCESO_CARTILLA.Any(ac => ac.CARTILLA_cartilla_id == id && ac.USUARIO_usuario_id == usuario.usuario_id)
                    })
                .AsEnumerable() // Ejecutar la consulta en la base de datos
                .Select(u => new SupervisorViewModel
                {
                    UsuarioId = u.UsuarioId,
                    NombreCompleto = u.NombreCompleto,
                    TieneAcceso = u.TieneAcceso
                })
                .ToList();

            ViewBag.CARTILLA_cartilla_id = id;
            ViewBag.USUARIOS = usuariosSupervisores;
            ViewBag.OBRA_NOMBRE = obra != null ? obra.nombre_obra : "No disponible"; // Asegúrate de que `Nombre` es el campo correcto
            ViewBag.ACTIVIDAD_NOMBRE = actividad != null
                    ? actividad.codigo_actividad + " - " + actividad.nombre_actividad
                    : "No disponible";

            var model = new ACCESO_CARTILLA
            {
                CARTILLA_cartilla_id = id.Value
            };

            return View(model);
        }


        [HttpPost]
        public ActionResult AsignarCartillaSupervisor(ACCESO_CARTILLA model, int[] usuariosSeleccionados)
        {
            if (ModelState.IsValid)
            {
                // Obtener todos los accesos actuales para la cartilla
                var accesosActuales = db.ACCESO_CARTILLA
                    .Where(ac => ac.CARTILLA_cartilla_id == model.CARTILLA_cartilla_id)
                    .ToList();

                var nuevosAccesos = new List<ACCESO_CARTILLA>();
                var supervisoresParaCorreo = new List<int>();

                if (usuariosSeleccionados != null)
                {
                    // Añadir los accesos nuevos que han sido seleccionados
                    foreach (var usuarioId in usuariosSeleccionados)
                    {
                        // Comprobar si ya existe el acceso
                        var accesoActual = accesosActuales.FirstOrDefault(ac => ac.USUARIO_usuario_id == usuarioId);

                        if (accesoActual == null)
                        {
                            // Agregar nuevo acceso
                            accesoActual = new ACCESO_CARTILLA
                            {
                                USUARIO_usuario_id = usuarioId,
                                CARTILLA_cartilla_id = model.CARTILLA_cartilla_id,
                                correo_enviado = false // Establecer como falso inicialmente
                            };
                            nuevosAccesos.Add(accesoActual);
                        }

                        // Almacenar supervisores para enviar correo solo si no se ha enviado
                        if (!accesoActual.correo_enviado)
                        {
                            supervisoresParaCorreo.Add(usuarioId);
                            accesoActual.correo_enviado = true; // Marcar como enviado
                        }
                    }

                    // Agregar nuevos accesos a la base de datos
                    if (nuevosAccesos.Count > 0)
                    {
                        db.ACCESO_CARTILLA.AddRange(nuevosAccesos);
                    }

                    // Eliminar los accesos que han sido desmarcados
                    var accesosADesmarcar = accesosActuales
                        .Where(ac => !usuariosSeleccionados.Contains(ac.USUARIO_usuario_id))
                        .ToList();

                    foreach (var acceso in accesosADesmarcar)
                    {
                        db.ACCESO_CARTILLA.Remove(acceso);
                    }

                    // Guardar cambios en la base de datos
                    db.SaveChanges();

                    // Enviar correos a los supervisores que no han recibido el correo
                    foreach (var usuarioId in supervisoresParaCorreo)
                    {
                        var supervisor = db.USUARIO.Include(u => u.PERSONA)
                                                  .FirstOrDefault(u => u.usuario_id == usuarioId);

                        if (supervisor != null && supervisor.PERSONA != null)
                        {
                            EnviarCorreoAsignacionCartilla(supervisor, model.CARTILLA_cartilla_id);
                        }
                    }

                    return RedirectToAction("Index");
                }

                // Si no se seleccionó ningún usuario, eliminar todos los accesos actuales
                foreach (var acceso in accesosActuales)
                {
                    db.ACCESO_CARTILLA.Remove(acceso);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Si el model state no es válido, recargar la vista con los datos necesarios
            var cartilla = db.CARTILLA.Find(model.CARTILLA_cartilla_id);
            if (cartilla == null)
            {
                return HttpNotFound();
            }

            int obraId = cartilla.OBRA_obra_id;

            var usuariosSupervisores = db.ACCESO_OBRAS
                .Where(a => a.obra_id == obraId)
                .Select(a => a.usuario_id)
                .Distinct()
                .Join(db.USUARIO.Include(u => u.PERSONA)
                    .Where(u => u.estado_usuario == true && u.PERFIL_perfil_id == 4),
                    usuarioId => usuarioId,
                    usuario => usuario.usuario_id,
                    (usuarioId, usuario) => new SupervisorViewModel
                    {
                        UsuarioId = usuario.usuario_id,
                        NombreCompleto = usuario.PERSONA.nombre + " " + usuario.PERSONA.apeliido_paterno + " " + usuario.PERSONA.apellido_materno
                    })
                .ToList();

            ViewBag.CARTILLA_cartilla_id = model.CARTILLA_cartilla_id;
            ViewBag.USUARIOS = usuariosSupervisores;

            return View(model);
        }



        // Método para enviar el correo
        private void EnviarCorreoAsignacionCartilla(USUARIO supervisor, int cartillaId)
        {
            var cartilla = db.CARTILLA.Include(c => c.OBRA)
                             .Include(c => c.ACTIVIDAD)
                             .FirstOrDefault(c => c.cartilla_id == cartillaId);

            if (cartilla == null || supervisor == null || supervisor.PERSONA == null)
                return;

            // Obtener el nombre del usuario autenticado
            string usuarioAsignador = ObtenerNombreUsuarioAutenticado();

            // Información para el correo
            string asunto = $"Cartilla Asignada {cartilla.ACTIVIDAD.codigo_actividad} - {cartilla.ACTIVIDAD.nombre_actividad} {cartilla.OBRA.nombre_obra}";
            string body = $"Estimad@ {supervisor.PERSONA.nombre} {supervisor.PERSONA.apeliido_paterno} {supervisor.PERSONA.apellido_materno} ,\n\n" +
                          $"Informo a usted la asignación para la cartilla de actividad " +
                          $"{cartilla.ACTIVIDAD.codigo_actividad} {cartilla.ACTIVIDAD.nombre_actividad}, de la obra {cartilla.OBRA.nombre_obra}.\n\n" +
                          $"Cartilla de Autocontrol asignada por: {usuarioAsignador}.\n\n" +
                          "Saludos cordiales.";

            // Configuración del cliente SMTP
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
            smtpClient.Port = 587;
            smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "uraa qkpw rnyd asvb");
            smtpClient.EnableSsl = true;

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress("cartillas.obra.manzano@gmail.com"),
                Subject = asunto,
                Body = body,
                IsBodyHtml = false // El cuerpo del mensaje no es HTML
            };

            // Dirección del supervisor
            mailMessage.To.Add(new MailAddress(supervisor.PERSONA.correo));

            // Copia al asignador
            mailMessage.CC.Add(new MailAddress(ObtenerEmailUsuarioAutenticado()));

            smtpClient.Send(mailMessage);
        }

        // Método para obtener el nombre del usuario autenticado
        private string ObtenerNombreUsuarioAutenticado()
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

        // Método para obtener el email del usuario autenticado
        private string ObtenerEmailUsuarioAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            return usuarioAutenticado?.PERSONA?.correo;
        }



        // Método GET para mostrar el formulario
        public ActionResult CrearCartillaAutocontrol()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];

                // Obtener los IDs de las obras a las que el usuario tiene acceso
                var obrasUsuarioIds = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                    .Distinct()
                    .ToList();

                // Crear el ViewModel
                var model = new CrearCartillaViewModel
                {
                    Obras = db.OBRA
                        .Where(obra => obra.ACTIVIDAD.Any(a => a.ITEM_VERIF.Any() &&
                                                  !db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id)) &&
                                                  obra.LOTE_INMUEBLE.Any())
                        .Where(o => obrasUsuarioIds.Contains(o.obra_id))
                        .ToList(),

                    Actividades = db.ACTIVIDAD
                        .Where(a => obrasUsuarioIds.Contains(a.OBRA.obra_id))
                        .Where(a => a.estado == "A" && a.ITEM_VERIF.Any() &&
                                    !db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id) &&
                                    a.OBRA.LOTE_INMUEBLE.Any())
                        .OrderBy(a => a.nombre_actividad)
                        .ToList()
                };

                return View(model);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
        }

        // Método POST para procesar el formulario
        [HttpPost]
        public ActionResult CrearCartillaAutocontrol(int obra_id, int actividad_id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];

                // Obtener los IDs de las obras a las que el usuario tiene acceso
                var obrasUsuarioIds = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.OBRA.obra_id)  // Obtener IDs en lugar de objetos completos
                    .Distinct()
                    .ToList();

                try
                {
                    // Configura el procedimiento almacenado
                    db.Database.ExecuteSqlCommand("EXEC CrearCartillaYDetalleCartilla @obra_id, @actividad_id, @observaciones, @observaciones_priv",
                        new SqlParameter("@obra_id", obra_id),
                        new SqlParameter("@actividad_id", actividad_id),
                        new SqlParameter("@observaciones", DBNull.Value),
                        new SqlParameter("@observaciones_priv", DBNull.Value));

                    // Redirigir a la vista de índice de Cartilla después de la creación exitosa
                    return RedirectToAction("Index", "Cartilla");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error al crear la cartilla: " + ex.Message;
                }

                // Volver a cargar las listas para el caso en que el POST falle
                var model = new CrearCartillaViewModel
                {
                    Obras = db.OBRA
                        .Where(obra => obra.ACTIVIDAD.Any(a => a.ITEM_VERIF.Any() &&
                                                  !db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id)) &&
                                                  obra.LOTE_INMUEBLE.Any())
                        .Where(o => obrasUsuarioIds.Contains(o.obra_id))
                        .ToList(),

                    Actividades = db.ACTIVIDAD
                        .Where(a => obrasUsuarioIds.Contains(a.OBRA.obra_id))
                        .Where(a => a.estado == "A" && a.ITEM_VERIF.Any() &&
                                    !db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id) &&
                                    a.OBRA.LOTE_INMUEBLE.Any())
                        .ToList()
                };

                return View(model);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account");
            }
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
