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
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.IO;


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class VistaConsultaController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();
        // GET: VistsConsulta
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

        public async Task<ActionResult> DescargarExcel(int obraId)
        {
            var items = await db.LOTE_INMUEBLE
                .Where(a => a.OBRA_obra_id == obraId)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Lotes de Inmueble");

                // Agregar encabezados
                worksheet.Cell(1, 1).Value = "Obra Asociada";
                worksheet.Cell(1, 2).Value = "Tipo de Bloque";
                worksheet.Cell(1, 3).Value = "Abreviatura";
                worksheet.Cell(1, 4).Value = "Rango Inicial";
                worksheet.Cell(1, 5).Value = "Rango Final";
                worksheet.Cell(1, 6).Value = "Cantidad de Pisos";
                worksheet.Cell(1, 7).Value = "Cantidad de Inmuebles";

                // Agregar datos
                var row = 2;
                foreach (var item in items)
                {
                    worksheet.Cell(row, 1).Value = item.OBRA.nombre_obra;
                    worksheet.Cell(row, 2).Value = item.tipo_bloque;
                    worksheet.Cell(row, 3).Value = item.abreviatura;
                    worksheet.Cell(row, 4).Value = item.rango_inicial;
                    worksheet.Cell(row, 5).Value = item.rango_final;
                    worksheet.Cell(row, 6).Value = item.cantidad_pisos;
                    worksheet.Cell(row, 7).Value = item.cantidad_inmuebles;
                    row++;
                }

                // Ajustar el ancho de las columnas según el contenido
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LotesInmueble.xlsx");
                }
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



        public async Task<ActionResult> LoteLista()
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

                // Filtra los LOTE_INMUEBLE que corresponden a las obras a las que el usuario tiene acceso
                var inmuebleGroupedByObra = await db.LOTE_INMUEBLE
                    .Include(e => e.OBRA)
                    .Where(e => obrasAcceso.Contains(e.OBRA_obra_id)) // Filtra solo las obras a las que el usuario tiene acceso
                    .OrderBy(e => e.lote_id)
                    .GroupBy(e => e.OBRA_obra_id)
                    .Select(g => g.FirstOrDefault())
                    .ToListAsync();

                return View(inmuebleGroupedByObra);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> LoteDetails(int ObraId)
        {
            var obraSeleccionado = await db.OBRA.FindAsync(ObraId);
            if (obraSeleccionado == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }

            var items = await db.LOTE_INMUEBLE
                .Where(a => a.OBRA_obra_id == ObraId)
                .ToListAsync();

            ViewBag.ObraSeleccionado = obraSeleccionado;

            return View(items);


        }


        public async Task<ActionResult> InmuebleDetails(int loteId)
        {
            var loteSeleccionado = await db.LOTE_INMUEBLE.FindAsync(loteId);
            if (loteSeleccionado == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }

            var items = await db.INMUEBLE
                .Include(a => a.LOTE_INMUEBLE)
                .Where(a => a.LOTE_INMUEBLE.lote_id == loteId)
                .ToListAsync();

            items = items.OrderBy(a => int.Parse(Regex.Match(a.codigo_inmueble, @"\d+").Value)).ToList();


            ViewBag.LoteSeleccionado = loteSeleccionado;

            return View(items);


        }


        public ActionResult ExportToExcel()
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

                var obras = db.OBRA.Include(o => o.COMUNA)
                  .Where(a => obrasUsuarioIds.Contains(a.obra_id))
                    .ToList();

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
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }




        public async Task<ActionResult> Responsable()
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

                // Busca los responsables de las obras a las que el usuario tiene acceso
                var rESPONSABLE = await db.RESPONSABLE
                    .Include(r => r.OBRA)
                    .Where(r => obrasAcceso.Contains(r.OBRA_obra_id))
                    .ToListAsync();

                return View(rESPONSABLE);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult ExportToExcelResponsable()
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

                var responsables = db.RESPONSABLE.Include(r => r.OBRA).Include(r => r.PERSONA)
                    .Where(i => obrasAcceso.Contains(i.OBRA_obra_id))
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Responsables");
                    worksheet.Cell(1, 1).Value = "Rut Responsable de Obra";
                    worksheet.Cell(1, 2).Value = "Nombre Responsable";
                    worksheet.Cell(1, 3).Value = "Cargo";
                    worksheet.Cell(1, 4).Value = "Obra Asociada";


                    int row = 2;
                    foreach (var responsable in responsables)
                    {
                        worksheet.Cell(row, 1).Value = responsable.PERSONA_rut;
                        worksheet.Cell(row, 2).Value = $"{responsable.PERSONA.nombre} {responsable.PERSONA.apeliido_paterno} {responsable.PERSONA.apellido_materno}";
                        worksheet.Cell(row, 3).Value = responsable.cargo;
                        worksheet.Cell(row, 4).Value = responsable.OBRA.nombre_obra;


                        row++;
                    }

                    // Ajustar el ancho de las columnas según el contenido
                    worksheet.Columns().AdjustToContents();

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Responsables.xlsx";
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





        public ActionResult ExportToExcelInmueble()
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
                { }
                var inmuebles = db.INMUEBLE.Include(o => o.LOTE_INMUEBLE.OBRA)
                      .Where(r => obrasAcceso.Contains(r.LOTE_INMUEBLE.OBRA_obra_id))
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Inmuebles");

                    worksheet.Cell(1, 1).Value = "Código Inmueble";
                    worksheet.Cell(1, 2).Value = "Tipo de Inmueble";
                    worksheet.Cell(1, 3).Value = "Obra Asociada";

                    int row = 2;
                    foreach (var inmueble in inmuebles)
                    {
                        worksheet.Cell(row, 1).Value = inmueble.codigo_inmueble;
                        worksheet.Cell(row, 2).Value = inmueble.tipo_inmueble;
                        worksheet.Cell(row, 3).Value = inmueble.LOTE_INMUEBLE.OBRA.nombre_obra;
                        row++;
                    }

                    // Ajustar el ancho de las columnas según el contenido
                    worksheet.Columns().AdjustToContents();

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Inmuebles.xlsx";
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

        public async Task<ActionResult> Actividad()
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

                var aCTIVIDAD = db.ACTIVIDAD.Include(a => a.OBRA).Where(r => obrasAcceso.Contains(r.OBRA_obra_id));
                return View(await aCTIVIDAD.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult ExportToExcelActividad()
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

                var actividades = db.ACTIVIDAD.Include(r => r.OBRA)
                      .Where(r => obrasAcceso.Contains(r.OBRA_obra_id));

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Actividad");
                    worksheet.Cell(1, 1).Value = "Obra Asociada";
                    worksheet.Cell(1, 2).Value = "Codigo Actividad";
                    worksheet.Cell(1, 3).Value = "Nombre Actividad";
                    worksheet.Cell(1, 4).Value = "Estado (Activo/Bloqueado)";
                    worksheet.Cell(1, 5).Value = "Tipo de Actividad (Proyecto/Inmueble)";


                    int row = 2;
                    foreach (var actividad in actividades)
                    {
                        worksheet.Cell(row, 1).Value = actividad.OBRA.nombre_obra;
                        worksheet.Cell(row, 2).Value = actividad.codigo_actividad;
                        worksheet.Cell(row, 3).Value = actividad.nombre_actividad;
                        worksheet.Cell(row, 4).Value = actividad.estado;
                        worksheet.Cell(row, 5).Value = actividad.tipo_actividad;


                        row++;
                    }

                    // Ajustar el ancho de las columnas según el contenido
                    worksheet.Columns().AdjustToContents();

                    var stream = new System.IO.MemoryStream();
                    workbook.SaveAs(stream);

                    var fileName = "Actividades.xlsx";
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


        public async Task<ActionResult> ItemVerificacion()
        {
            var iTEM_VERIF = db.ITEM_VERIF.Include(i => i.ACTIVIDAD).OrderBy(i => i.label).ThenBy(i => i.ACTIVIDAD_actividad_id);
            return View(await iTEM_VERIF.ToListAsync());
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

                var actividadSeleccionado = await db.ACTIVIDAD.FindAsync(actividadId);
                if (actividadSeleccionado == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var items = await db.ITEM_VERIF
                    .Where(a => a.ACTIVIDAD_actividad_id == actividadId)
                    .Where(a => obrasAcceso.Contains(a.ACTIVIDAD.OBRA_obra_id))
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


        public ActionResult ExportToExcelItems()
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
                   .Where(r => obrasAcceso.Contains(r.ACTIVIDAD.OBRA_obra_id))
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

    }
}