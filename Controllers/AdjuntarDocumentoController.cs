using Proyecto_Cartilla_Autocontrol.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Data;
using System.Threading.Tasks;



namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class AdjuntarDocumentoController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();


        public async Task<ActionResult> GestionarDocumento(int id)
        {
            var cartillaSeleccionada = await db.CARTILLA.FindAsync(id);
            if (cartillaSeleccionada == null)
            {
                return HttpNotFound();
            }
            ViewBag.CartillaSeleccionada = cartillaSeleccionada;

            var detalleCartilla = db.DETALLE_CARTILLA
                .Include(d => d.ITEM_VERIF)
                .Include(d => d.CARTILLA)
                .Where(d => d.CARTILLA.cartilla_id == id)
                .ToList();

            // Obtener la ruta de la carpeta donde están los documentos
            string folderPath = Server.MapPath("~/Content/documento/" + id);
            ViewBag.Documentos = Directory.Exists(folderPath) ? Directory.GetFiles(folderPath) : new string[0];

            // Comprobar si la carpeta existe y contiene archivos
            bool hayArchivos = Directory.Exists(folderPath) && Directory.GetFiles(folderPath).Any();

            // Guardar el valor en ViewBag
            ViewBag.HayArchivos = hayArchivos;

            return View(detalleCartilla);
        }


        [HttpPost]
        public ActionResult AdjuntarDocumento(IEnumerable<HttpPostedFileBase> files, int cartillaId)
        {
            if (files != null && files.Any(f => f != null && f.ContentLength > 0))
            {
                try
                {
                    // Crear la ruta de la subcarpeta específica para la cartilla
                    string folderPath = "~/Content/documento/" + cartillaId;  // La subcarpeta específica para la cartilla
                    string physicalFolderPath = Server.MapPath(folderPath);

                    // Verificar si la carpeta ya existe
                    if (!Directory.Exists(physicalFolderPath))
                    {
                        Directory.CreateDirectory(physicalFolderPath);
                    }

                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file.FileName); // Nombre sin extensión
                            string fileExtension = Path.GetExtension(file.FileName); // Extensión del archivo
                            string physicalPath = Path.Combine(physicalFolderPath, file.FileName);
                            int fileCount = 1;

                            // Verificar si el archivo ya existe y generar un nombre nuevo si es necesario
                            while (System.IO.File.Exists(physicalPath))
                            {
                                fileCount++;
                                string newFileName = $"{fileName} ({fileCount}){fileExtension}";
                                physicalPath = Path.Combine(physicalFolderPath, newFileName);
                            }

                            // Guardar el archivo en la subcarpeta de la cartilla con el nombre nuevo (si es necesario)
                            file.SaveAs(physicalPath);
                        }
                    }

                    // Guardar la ruta de la carpeta en la base de datos (no de un archivo individual)
                    var cartilla = db.CARTILLA.Find(cartillaId);
                    if (cartilla != null)
                    {
                        cartilla.ruta_pdf = folderPath;  // Guardar la ruta de la carpeta donde están los archivos
                        db.SaveChanges();
                    }

                    // Redirigir a la vista GestionarDocumento con el ID de la cartilla
                    return RedirectToAction("GestionarDocumento", new { id = cartillaId });
                }
                catch (Exception ex)
                {
                    // Manejar cualquier excepción que pueda ocurrir durante la subida de archivos
                    ViewBag.Error = "Hubo un error al subir los archivos: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Error = "Por favor, seleccione al menos un archivo.";
            }

            // En caso de error, redirigir a la vista GestionarDocumento con el ID de la cartilla
            return RedirectToAction("GestionarDocumento", new { id = cartillaId });
        }


        public ActionResult GetDocumentosAdjuntos(int cartillaId)
        {
            var cartilla = db.CARTILLA.Find(cartillaId);
            if (cartilla != null && !string.IsNullOrEmpty(cartilla.ruta_pdf))
            {
                string physicalFolderPath = Server.MapPath(cartilla.ruta_pdf);

                if (Directory.Exists(physicalFolderPath))
                {
                    var archivos = Directory.GetFiles(physicalFolderPath)
                                            .Select(f => Path.GetFileName(f))
                                            .ToList();
                    return Json(archivos, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new List<string>(), JsonRequestBehavior.AllowGet);
        }





        public async Task<ActionResult> GestionarDocumentoFiltrado(int id)
        {

            var cartillaSeleccionada = await db.CARTILLA.FindAsync(id);
            if (cartillaSeleccionada == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }
            ViewBag.CartillaSeleccionada = cartillaSeleccionada;

            var detalleCartilla = db.DETALLE_CARTILLA.Include(d => d.ITEM_VERIF).Include(d => d.CARTILLA).Where(d => d.CARTILLA.cartilla_id == id).ToList();

            // Obtener la ruta de la carpeta donde están los documentos
            string folderPath = Server.MapPath("~/Content/documento/" + id);
            ViewBag.Documentos = Directory.Exists(folderPath) ? Directory.GetFiles(folderPath) : new string[0];

            // Comprobar si la carpeta existe y contiene archivos
            bool hayArchivos = Directory.Exists(folderPath) && Directory.GetFiles(folderPath).Any();

            // Guardar el valor en ViewBag
            ViewBag.HayArchivos = hayArchivos;

            return View(detalleCartilla);
        }


        [HttpPost]
        public ActionResult AdjuntarDocumentoFiltrado(IEnumerable<HttpPostedFileBase> files, int cartillaId)
        {
            if (files != null && files.Any(f => f != null && f.ContentLength > 0))
            {
                try
                {
                    // Crear la ruta de la subcarpeta específica para la cartilla
                    string folderPath = "~/Content/documento/" + cartillaId;  // La subcarpeta específica para la cartilla
                    string physicalFolderPath = Server.MapPath(folderPath);

                    // Verificar si la carpeta ya existe
                    if (!Directory.Exists(physicalFolderPath))
                    {
                        Directory.CreateDirectory(physicalFolderPath);
                    }

                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file.FileName); // Nombre sin extensión
                            string fileExtension = Path.GetExtension(file.FileName); // Extensión del archivo
                            string physicalPath = Path.Combine(physicalFolderPath, file.FileName);
                            int fileCount = 1;

                            // Verificar si el archivo ya existe y generar un nombre nuevo si es necesario
                            while (System.IO.File.Exists(physicalPath))
                            {
                                fileCount++;
                                string newFileName = $"{fileName} ({fileCount}){fileExtension}";
                                physicalPath = Path.Combine(physicalFolderPath, newFileName);
                            }

                            // Guardar el archivo en la subcarpeta de la cartilla con el nombre nuevo (si es necesario)
                            file.SaveAs(physicalPath);
                        }
                    }

                    // Guardar la ruta de la carpeta en la base de datos (no de un archivo individual)
                    var cartilla = db.CARTILLA.Find(cartillaId);
                    if (cartilla != null)
                    {
                        cartilla.ruta_pdf = folderPath;  // Guardar la ruta de la carpeta donde están los archivos
                        db.SaveChanges();
                    }

                    // Redirigir a la vista GestionarDocumento con el ID de la cartilla
                    return RedirectToAction("GestionarDocumentoFiltrado", new { id = cartillaId });
                }
                catch (Exception ex)
                {
                    // Manejar cualquier excepción que pueda ocurrir durante la subida de archivos
                    ViewBag.Error = "Hubo un error al subir los archivos: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Error = "Por favor, seleccione al menos un archivo.";
            }

            // En caso de error, redirigir a la vista GestionarDocumento con el ID de la cartilla
            return RedirectToAction("GestionarDocumentoFiltrado", new { id = cartillaId });
        }
        
       

        public async Task<ActionResult> ListaCartillasPorActividad()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasAcceso = await db.ACCESO_OBRAS
               .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
               .Select(a => a.obra_id)
               .ToListAsync();


                var detalleCartillas = db.DETALLE_CARTILLA.Include(d => d.ITEM_VERIF).Include(d => d.CARTILLA.ACTIVIDAD.OBRA.RESPONSABLE)
                    .Include(d => d.CARTILLA)
                    .Where(d => d.CARTILLA.ACTIVIDAD_actividad_id == d.CARTILLA.ACTIVIDAD.actividad_id)
                   .Where(e => obrasAcceso.Contains(e.CARTILLA.OBRA_obra_id));
               

                return View(await detalleCartillas.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        [HttpPost]
        public JsonResult EliminarDocumento(string fileName, int cartillaId)
        {
            try
            {
                // Construir la ruta del archivo
                string carpetaDocumentos = Server.MapPath("~/Content/documento/" + cartillaId);
                string rutaArchivo = Path.Combine(carpetaDocumentos, fileName);

                // Verificar si el archivo existe
                if (System.IO.File.Exists(rutaArchivo))
                {
                    // Eliminar el archivo
                    System.IO.File.Delete(rutaArchivo);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "El archivo no existe." });
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return Json(new { success = false, message = ex.Message });
            }
        }



    }
}