using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ClosedXML.Excel;
using Proyecto_Cartilla_Autocontrol.Models;
using OfficeOpenXml;
using System.Data.SqlClient;
using OfficeOpenXml.Style;



namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ObraController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: OBRA
        public async Task<ActionResult> Index()
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

                var oBRA = db.OBRA.Include(o => o.COMUNA)
                 .Where(o => obrasAcceso.Contains(o.obra_id));  
                return View(await oBRA.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> Obra()
        {
            // Comprueba si el usuario está autenticado
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtra las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var obrasAcceso = db.ACCESO_OBRAS
                                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                    .Select(a => a.obra_id)
                                    .ToList();

                // Busca las obras directamente y realiza la condición deseada
                var obras = await db.OBRA
                    .Where(o => obrasAcceso.Contains(o.obra_id))
                    .Include(o => o.COMUNA)
                    .ToListAsync();

                return View(obras);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> ReporteExcel(int obraId)
        {
            using (var context = new ObraManzanoFinal())
            {
                var obra = await context.OBRA.FindAsync(obraId);

                if (obra == null)
                {
                    return HttpNotFound();
                }

                // Obtener el nombre de la obra
                var nombreObra = await context.CARTILLA
                    .Include(co => co.OBRA)
                    .Where(co => co.OBRA_obra_id == obraId)
                    .Select(co => co.OBRA.nombre_obra)
                    .FirstOrDefaultAsync();

                // Llamar al procedimiento almacenado para obtener los datos
                var dataTable = new DataTable();
                using (var connection = new SqlConnection(context.Database.Connection.ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SPU_RESUMEN_CARTILLA", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@obra", obraId);

                        // Aumentar el tiempo de espera a 120 segundos
                        command.CommandTimeout = 120;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }

                // Generar el archivo Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("ResumenCartilla");

                    // Agregar título
                    var titleCell = worksheet.Cell("A1");
                    titleCell.Value = "Reporte de Revisión Cartilla de Autocontrol";
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 16;
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range("A1:M1").Merge();

                    // Agregar nombre de la obra
                    var obraCell = worksheet.Cell("A4");
                    obraCell.Value = "OBRA: " + nombreObra;
                    obraCell.Style.Font.FontSize = 12;
                    obraCell.Style.Font.Bold = true;

                    // Insertar los datos a partir de la celda A7
                    worksheet.Cell("A7").InsertTable(dataTable);

                    // Guardar el archivo en memoria
                    var memoryStream = new System.IO.MemoryStream();
                    workbook.SaveAs(memoryStream);

                    return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ResumenCartilla.xlsx");
                }
            }
        }



        public async Task<ActionResult> ReporteSupervisor(int obraId)
        {
            using (var context = new ObraManzanoFinal())
            {
                var obra = await context.OBRA.FindAsync(obraId);

                if (obra == null)
                {
                    return HttpNotFound();
                }

                // Obtener el nombre de la obra
                var nombreObra = await context.CARTILLA
                    .Include(co => co.OBRA)
                    .Where(co => co.OBRA_obra_id == obraId)
                    .Select(co => co.OBRA.nombre_obra)
                    .FirstOrDefaultAsync();

                // Llamar al procedimiento almacenado para obtener los datos
                var dataTable = new DataTable();
                using (var connection = new SqlConnection(context.Database.Connection.ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SPU_RESUMEN_SUPERV", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@obra", obraId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }

                // Generar el archivo Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("ResumenSupervisor");

                    // Agregar título
                    var titleCell = worksheet.Cell("A1");
                    titleCell.Value = "Reporte de Cartilla de Autocontrol para Supervisor";
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 16;
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range("A1:M1").Merge();

                    // Agregar nombre de la obra
                    var obraCell = worksheet.Cell("A4");
                    obraCell.Value = "OBRA: " + nombreObra;
                    obraCell.Style.Font.FontSize = 12;
                    obraCell.Style.Font.Bold = true;

                    // Insertar los datos a partir de la celda A7
                    worksheet.Cell("A7").InsertTable(dataTable);

                    var memoryStream = new System.IO.MemoryStream();
                    workbook.SaveAs(memoryStream);

                    return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ResumenSupervisor.xlsx");
                }
            }
        }




        public ActionResult ExportToExcel()
        {
            var obras = db.OBRA.Include(o => o.COMUNA).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Obras");

                worksheet.Cell(1, 1).Value = "Nombre Obra";
                worksheet.Cell(1, 2).Value = "Dirección";
                worksheet.Cell(1, 3).Value = "Nombre Comuna";
                worksheet.Cell(1, 4).Value = "Entidad Patrocinante";
                worksheet.Cell(1, 5).Value = "Tipo de Proyecto";
                worksheet.Cell(1, 6).Value = "Total de departamentos";
                worksheet.Cell(1, 7).Value = "Total de viviendas";

                int row = 2;
                foreach (var obra in obras)
                {
                    worksheet.Cell(row, 1).Value = obra.nombre_obra;
                    worksheet.Cell(row, 2).Value = obra.direccion;
                    worksheet.Cell(row, 3).Value = obra.COMUNA.nombre_comuna;
                    worksheet.Cell(row, 4).Value = obra.entidad_patrocinante;
                    worksheet.Cell(row, 5).Value = obra.tipo_proyecto;
                    worksheet.Cell(row, 6).Value = obra.total_deptos;
                    worksheet.Cell(row, 7).Value = obra.total_viv;
                    row++;
                }

                // Ajustar el ancho de las columnas según el contenido
                worksheet.Columns().AdjustToContents();

                var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);

                var fileName = "Obras.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

            // GET: OBRA/Details/5
            public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OBRA oBRA = await db.OBRA.FindAsync(id);
            if (oBRA == null)
            {
                return HttpNotFound();
            }
            return View(oBRA);
        }

        // GET: OBRA/Create
        public ActionResult Create()
        {
            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna");
            ViewBag.RegionList = new SelectList(db.REGION, "region_id", "nombre_region");
            return View();
        }

        // POST: OBRA/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "obra_id,nombre_obra,direccion,COMUNA_comuna_id,tipo_proyecto,total_deptos,total_viv,entidad_patrocinante")] OBRA oBRA)
        {
            // Transformar los campos para que la primera letra sea mayúscula
            oBRA.nombre_obra = CapitalizeFirstLetter(oBRA.nombre_obra);
            oBRA.direccion = CapitalizeFirstLetter(oBRA.direccion);
            oBRA.entidad_patrocinante = CapitalizeFirstLetter(oBRA.entidad_patrocinante);

            // Verificar si total_deptos o total_viv son NULL y establecerlos en 0 si es así
            if (oBRA.total_deptos == null)
            {
                oBRA.total_deptos = 0;
            }
            if (oBRA.total_viv == null)
            {
                oBRA.total_viv = 0;
            }

            if (ModelState.IsValid)
            {
                // Agregar la nueva obra a la base de datos
                db.OBRA.Add(oBRA);
                await db.SaveChangesAsync();

                // Obtener el usuario administrador (ajustar esto según tu lógica de obtención de usuario)
                var administrador = db.USUARIO.FirstOrDefault(u => u.PERFIL_perfil_id == 1);
                if (administrador != null)
                {
                    // Crear un nuevo registro en ACCESO_OBRAS para el usuario administrador
                    var accesoObra = new ACCESO_OBRAS
                    {
                        usuario_id = administrador.usuario_id,
                        obra_id = oBRA.obra_id
                    };

                    // Agregar el acceso de la obra a la base de datos
                    db.ACCESO_OBRAS.Add(accesoObra);
                    await db.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }

            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna", oBRA.COMUNA_comuna_id);
            return View(oBRA);
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


        public JsonResult GetComunasByRegion(int regionId)
        {
            var comunas = db.COMUNA.Where(c => c.REGION.region_id == regionId)
                                  .Select(c => new
                                  {
                                      Value = c.comuna_id,
                                      Text = c.nombre_comuna
                                  })
                                  .ToList();

            return Json(comunas, JsonRequestBehavior.AllowGet);
        }



        // GET: OBRA/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OBRA oBRA = await db.OBRA.FindAsync(id);
            if (oBRA == null)
            {
                return HttpNotFound();
            }

            // Verificar si existen relaciones con LOTE_INMUEBLE
            bool tieneRelacionesLotes = db.LOTE_INMUEBLE.Any(l => l.OBRA.obra_id == id);

            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna", oBRA.COMUNA_comuna_id);
            ViewBag.RegionList = new SelectList(db.REGION, "region_id", "nombre_region", oBRA.COMUNA.REGION.region_id);
            // Pasar la información a la vista
            ViewBag.TieneRelaciones = tieneRelacionesLotes;
            return View(oBRA);
        }

        // POST: OBRA/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "obra_id,nombre_obra,direccion,COMUNA_comuna_id,tipo_proyecto,total_deptos,total_viv, entidad_patrocinante")] OBRA oBRA)
        {
            // Transformar los campos para que la primera letra sea mayúscula
            oBRA.nombre_obra = CapitalizeFirstLetter(oBRA.nombre_obra);
            oBRA.direccion = CapitalizeFirstLetter(oBRA.direccion);
            oBRA.entidad_patrocinante = CapitalizeFirstLetter(oBRA.entidad_patrocinante);

            if (oBRA.tipo_proyecto == "Edificio")
            {
                oBRA.total_viv = 0;
            }
            if (oBRA.tipo_proyecto == "Vivienda")
            {
                oBRA.total_deptos = 0;
            }
            if (ModelState.IsValid)
            {
                db.Entry(oBRA).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            // Verificar si existen relaciones con LOTE_INMUEBLE
            bool tieneRelacionesLotes = db.LOTE_INMUEBLE.Any(l => l.OBRA.obra_id == oBRA.obra_id);
            ViewBag.TieneRelaciones = tieneRelacionesLotes;
            ViewBag.COMUNA_comuna_id = new SelectList(db.COMUNA, "comuna_id", "nombre_comuna", oBRA.COMUNA_comuna_id);
            ViewBag.RegionList = new SelectList(db.REGION, "region_id", "nombre_region", oBRA.COMUNA.REGION.region_id);
            return View(oBRA);
        }

        // GET: OBRA/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OBRA oBRA = await db.OBRA.FindAsync(id);
            if (oBRA == null)
            {
                return HttpNotFound();
            }
            return View(oBRA);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            OBRA oBRA = await db.OBRA.FindAsync(id);

            if (oBRA == null)
            {
                return HttpNotFound();
            }

            // Verificar si tiene registros relacionados en las tablas dependientes
            bool tieneLotesRelacionadas = db.LOTE_INMUEBLE.Any(l => l.OBRA_obra_id == id);
            bool tieneActividadesRelacionadas = db.ACTIVIDAD.Any(a => a.OBRA_obra_id == id);
            bool tieneCartillaRelacionadas = db.CARTILLA.Any(c => c.OBRA_obra_id == id);
            bool tieneResponsableRelacionadas = db.RESPONSABLE.Any(c => c.OBRA_obra_id == id);

            if (tieneLotesRelacionadas || tieneActividadesRelacionadas || tieneCartillaRelacionadas || tieneResponsableRelacionadas)
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta obra porque tiene datos relacionados.";
                return View("Delete", oBRA); // Mostrar vista de eliminación con el mensaje de error
            }

            // Eliminar todos los registros relacionados en ACCESO_OBRAS
            var accesosRelacionados = db.ACCESO_OBRAS.Where(a => a.obra_id == id).ToList();
            if (accesosRelacionados.Any())
            {
                db.ACCESO_OBRAS.RemoveRange(accesosRelacionados); // Eliminar todos los accesos relacionados a la obra
            }

            // Eliminar la obra
            db.OBRA.Remove(oBRA);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
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
