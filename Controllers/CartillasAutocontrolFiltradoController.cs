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
using Rotativa;
using Rotativa.Options;
using System.Net.Mail;
using System.IO;
using System.Runtime.Remoting.Contexts;
using ClosedXML.Excel;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Web.UI;
using DocumentFormat.OpenXml.EMMA;
using iTextSharp.text.pdf;
using MoreLinq;
using System.Text.RegularExpressions;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class CartillasAutocontrolFiltradoController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();
        public async Task<ActionResult> ListaCartillasPorActividad()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtener la lista de IDs de obras a las que el usuario tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                // Obtener las cartillas para las obras a las que el usuario tiene acceso
                var detalleCartillas = db.DETALLE_CARTILLA
                    .Include(d => d.ITEM_VERIF)
                    .Include(d => d.CARTILLA.ACTIVIDAD.OBRA.RESPONSABLE)
                    .Include(d => d.CARTILLA)
                    .Where(d => obrasAcceso.Contains(d.CARTILLA.ACTIVIDAD.OBRA_obra_id))
                    .Where(d => d.CARTILLA.ACTIVIDAD_actividad_id == d.CARTILLA.ACTIVIDAD.actividad_id);

                return View(await detalleCartillas.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public async Task<ActionResult> LoteDetails(int cartillaId)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                var cartilla = await db.CARTILLA.FindAsync(cartillaId);
                if (cartilla == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var obraSeleccionada = await db.OBRA.FindAsync(cartilla.OBRA_obra_id);
                if (obraSeleccionada == null || !obrasAcceso.Contains(obraSeleccionada.obra_id))
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var items = await db.LOTE_INMUEBLE
                    .Where(a => a.OBRA_obra_id == obraSeleccionada.obra_id && obrasAcceso.Contains(a.OBRA_obra_id))
                    .ToListAsync();

                var loteStatus = new Dictionary<int, bool>();

                foreach (var item in items)
                {
                    bool tieneDetalleCartilla = LoteTieneDetalleCartilla(item.lote_id, cartillaId);
                    loteStatus[item.lote_id] = tieneDetalleCartilla;
                }

                ViewBag.ObraSeleccionado = obraSeleccionada;
                ViewBag.LoteStatus = loteStatus;
                ViewBag.CartillaId = cartillaId; // Agregar CartillaId a ViewBag
                ViewBag.NombreActividad = cartilla.ACTIVIDAD.nombre_actividad;

                return View(items);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> LoteDetailsSupervisor(int cartillaId)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                var cartilla = await db.CARTILLA.FindAsync(cartillaId);
                if (cartilla == null)
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var obraSeleccionada = await db.OBRA.FindAsync(cartilla.OBRA_obra_id);
                if (obraSeleccionada == null || !obrasAcceso.Contains(obraSeleccionada.obra_id))
                {
                    return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
                }

                var items = await db.LOTE_INMUEBLE
                    .Where(a => a.OBRA_obra_id == obraSeleccionada.obra_id && obrasAcceso.Contains(a.OBRA_obra_id))
                    .ToListAsync();

                var loteStatus = new Dictionary<int, bool>();

                foreach (var item in items)
                {
                    bool tieneDetalleCartilla = LoteTieneDetalleCartilla(item.lote_id, cartillaId);
                    loteStatus[item.lote_id] = tieneDetalleCartilla;
                }

                ViewBag.ObraSeleccionado = obraSeleccionada;
                ViewBag.LoteStatus = loteStatus;
                ViewBag.CartillaId = cartillaId; // Agregar CartillaId a ViewBag

                return View(items);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        private bool LoteTieneDetalleCartilla(int loteId, int cartillaId)
        {
            return db.DETALLE_CARTILLA
                .Any(d => d.CARTILLA_cartilla_id == cartillaId &&
                          db.INMUEBLE.Any(i => i.LOTE_INMUEBLE_lote_id == loteId && d.INMUEBLE_inmueble_id == i.inmueble_id));
        }

        public async Task<ActionResult> ListaCartillasSupervisor()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtener la lista de IDs de obras a las que el usuario tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                // Obtener la lista de IDs de cartillas a las que el usuario tiene acceso
                var cartillasAcceso = db.ACCESO_CARTILLA
                    .Where(ac => ac.USUARIO_usuario_id == usuarioAutenticado.usuario_id)
                    .Select(ac => ac.CARTILLA_cartilla_id)
                    .ToList();

                // Obtener las cartillas para las obras a las que el usuario tiene acceso
                var detalleCartillas = db.DETALLE_CARTILLA
                    .Include(d => d.ITEM_VERIF)
                    .Include(d => d.CARTILLA.ACTIVIDAD.OBRA.RESPONSABLE)
                    .Include(d => d.CARTILLA)
                    .Where(d => obrasAcceso.Contains(d.CARTILLA.ACTIVIDAD.OBRA_obra_id))
                    .Where(d => d.CARTILLA.ACTIVIDAD_actividad_id == d.CARTILLA.ACTIVIDAD.actividad_id)
                    .Where(d => cartillasAcceso.Contains(d.CARTILLA.cartilla_id)); // Filtra por las cartillas a las que tiene acceso

                return View(await detalleCartillas.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        public static (string, int) SplitLabel(string label)
        {
            string nonNumericPart = string.Concat(label.TakeWhile(char.IsLetter));
            int numericPart = int.Parse(string.Concat(label.SkipWhile(char.IsLetter)));
            return (nonNumericPart, numericPart);
        }

        // Comparador personalizado para ordenar los labels
        public class LabelComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var splitX = SplitLabel(x);
                var splitY = SplitLabel(y);

                // Primero comparar las partes no numéricas
                int nonNumericComparison = string.Compare(splitX.Item1, splitY.Item1);
                if (nonNumericComparison != 0)
                    return nonNumericComparison;

                // Luego comparar las partes numéricas
                return splitX.Item2.CompareTo(splitY.Item2);
            }
        }

        public ActionResult ExportToExcel(int cartillaId, int actividadId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var query = (from detalleCartilla in db.VistaExcel
                         where detalleCartilla.cartilla_id == cartillaId
                             && detalleCartilla.actividad_id == actividadId
                         group detalleCartilla by detalleCartilla.label into grouped
                         orderby grouped.Key, new LabelComparer() // Aquí aplicamos el comparador al grupo de elementos
                         from item in grouped
                         select item).ToList();

            // Obtener información de la cartilla y la actividad asociada
            string nombreObra = db.CARTILLA.Include(co => co.OBRA)
                                            .Where(co => co.cartilla_id == cartillaId)
                                            .Select(co => co.OBRA.nombre_obra)
                                            .FirstOrDefault();
            string ObraDireccion = db.CARTILLA.Include(co => co.OBRA)
                                              .Where(co => co.cartilla_id == cartillaId)
                                              .Select(co => co.OBRA.direccion)
                                              .FirstOrDefault();
            string ObraComuna = db.CARTILLA.Include(co => co.OBRA)
                                           .Where(co => co.cartilla_id == cartillaId)
                                           .Select(co => co.OBRA.COMUNA.nombre_comuna)
                                           .FirstOrDefault();
            string ObraEP = db.CARTILLA.Include(co => co.OBRA)
                                       .Where(co => co.cartilla_id == cartillaId)
                                       .Select(co => co.OBRA.entidad_patrocinante)
                                       .FirstOrDefault();
            string observacionesPublicas = db.CARTILLA.Include(co => co.OBRA)
                                                      .Where(co => co.cartilla_id == cartillaId)
                                                      .Select(co => co.observaciones)
                                                      .FirstOrDefault();
            string nombreActividad = db.CARTILLA.Include(ca => ca.ACTIVIDAD)
                                                .Where(ca => ca.cartilla_id == cartillaId && ca.ACTIVIDAD_actividad_id == actividadId)
                                                .Select(ca => ca.ACTIVIDAD.nombre_actividad)
                                                .FirstOrDefault();

            string codigoActividad = db.CARTILLA.Include(ca => ca.ACTIVIDAD)
                                        .Where(ca => ca.cartilla_id == cartillaId && ca.ACTIVIDAD_actividad_id == actividadId)
                                        .Select(ca => ca.ACTIVIDAD.codigo_actividad)
                                        .FirstOrDefault();

            string estadoCartilla = db.CARTILLA
                                      .Where(ca => ca.cartilla_id == cartillaId && ca.ACTIVIDAD_actividad_id == actividadId)
                                      .Select(ca => ca.ESTADO_FINAL.descripcion)
                                      .FirstOrDefault();


            string observacionesPrivadas = db.CARTILLA.Include(co => co.OBRA)
                                                   .Where(co => co.cartilla_id == cartillaId)
                                                   .Select(co => co.observaciones_priv)
                                                   .FirstOrDefault();
            var cartilla = db.CARTILLA
              .Where(ca => ca.cartilla_id == cartillaId && ca.ACTIVIDAD_actividad_id == actividadId)
              .FirstOrDefault();

            string fechainicio = cartilla != null ? cartilla.fecha.ToString("yyyy-MM-dd") : null;


            string fechamodif = cartilla != null && cartilla.fecha_modificacion.HasValue
                                 ? cartilla.fecha_modificacion.Value.ToString("yyyy-MM-dd")
                                 : null;


            var cargoAdminObra = db.RESPONSABLE
                        .Where(r => r.OBRA_obra_id == cartilla.OBRA_obra_id && r.cargo == "Administrador de Obra")
                        .Select(r => new { r.PERSONA.nombre, r.PERSONA.apeliido_paterno })
                        .FirstOrDefault();

            var cargoAdminObraFullName = cargoAdminObra != null
                                         ? $"{cargoAdminObra.nombre} {cargoAdminObra.apeliido_paterno}"
                                         : string.Empty;



            var cargoAutocontrol = db.RESPONSABLE
                                    .Where(r => r.OBRA_obra_id == cartilla.OBRA_obra_id && r.cargo == "Autocontrol")
                                     .Select(r => new { r.PERSONA.nombre, r.PERSONA.apeliido_paterno })
                                    .FirstOrDefault();

            var cargoAutocontrolFullName = cargoAutocontrol != null
                                        ? $"{cargoAutocontrol.nombre} {cargoAutocontrol.apeliido_paterno}"
                                        : string.Empty;


            var cargoftop = db.RESPONSABLE
                                   .Where(r => r.OBRA_obra_id == cartilla.OBRA_obra_id && r.cargo == "F.T.O 1")
                                  .Select(r => new { r.PERSONA.nombre, r.PERSONA.apeliido_paterno })
                                   .FirstOrDefault();

            var cargofto1FullName = cargoftop != null
                                   ? $"{cargoftop.nombre} {cargoftop.apeliido_paterno}"
                                   : string.Empty;

            var cargoftos = db.RESPONSABLE
                                 .Where(r => r.OBRA_obra_id == cartilla.OBRA_obra_id && r.cargo == "F.T.O 2")
                             .Select(r => new { r.PERSONA.nombre, r.PERSONA.apeliido_paterno })
                                 .FirstOrDefault();

            var cargofto2FullName = cargoftos != null
                                      ? $"{cargoftos.nombre} {cargoftos.apeliido_paterno}"
                                      : string.Empty;

            var cargoserviusupervisor = db.RESPONSABLE
                               .Where(r => r.OBRA_obra_id == cartilla.OBRA_obra_id && r.cargo == "Supervisor Serviu")
                           .Select(r => new { r.PERSONA.nombre, r.PERSONA.apeliido_paterno })
                               .FirstOrDefault();

            var cargoserviu = cargoserviusupervisor != null
                                     ? $"{cargoserviusupervisor.nombre} {cargoserviusupervisor.apeliido_paterno}"
                                     : string.Empty;



            // Obtener la lista de códigos de inmueble distintos y ordenarlos
            var codigosInmueble = query
                 .Select(x => x.codigo_inmueble)
                 .Distinct()
                 .OrderBy(codigo =>
                 {
                     // Extraer el prefijo alfabético (ej. "MA-VIV")
                     var prefix = Regex.Match(codigo, @"^[A-Za-z-]+").Value;

                     // Extraer la parte numérica (ej. "1" en "MA-VIV1")
                     var numericPart = int.Parse(Regex.Match(codigo, @"\d+").Value);

                     // Devolver tupla (prefijo, parte numérica) para ordenamiento
                     return (prefix, numericPart);
                 })
                 .ToList();


            byte[] fileContents;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("VistaExcel");

                // Agregar título
                worksheet.Cells["A1"].Value = "Cartilla de Autocontrol";
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1:M1"].Merge = true;

                // Agregar la imagen en la celda A2
                string imagePath = HttpContext.Server.MapPath("~/Content/img/logo2.png");
                FileInfo imageFile = new FileInfo(imagePath);
                var picture = worksheet.Drawings.AddPicture("Logo", imageFile);
                picture.SetPosition(1, 0, 0, 0); // Row 2, Column A (A2)
                picture.SetSize(80, 80);

                worksheet.Cells["A7"].Value = "OBRA: " + nombreObra;
                worksheet.Cells["A7"].Style.Font.Bold = true;
                worksheet.Cells["A8"].Value = "DIRECCIÓN: " + ObraDireccion;
                worksheet.Cells["A8"].Style.Font.Bold = true;
                worksheet.Cells["A9"].Value = "COMUNA: " + ObraComuna;
                worksheet.Cells["A9"].Style.Font.Bold = true;
                worksheet.Cells["A10"].Value = "CONSTRUCTORA: Constructora Manzano y Asociados Ltda";
                worksheet.Cells["A10"].Style.Font.Bold = true;

                worksheet.Cells["E7"].Value = "ACTIVIDAD: " + codigoActividad + "-" + nombreActividad;
                worksheet.Cells["E7"].Style.Font.Bold = true;
                worksheet.Cells["E8"].Value = "ESTADO: " + estadoCartilla;
                worksheet.Cells["E8"].Style.Font.Bold = true;
                worksheet.Cells["E9"].Value = "FECHA INICIO: " + fechainicio;
                worksheet.Cells["E9"].Style.Font.Bold = true;
                worksheet.Cells["E10"].Value = "FECHA MODIFICACIÓN: " + fechamodif;
                worksheet.Cells["E10"].Style.Font.Bold = true;

                // Definir el rango de celdas para aplicar los bordes
                var borderStyle = ExcelBorderStyle.Thin;
                var rangeForBorders = worksheet.Cells["E7:E10"];

                // Aplicar bordes a todas las celdas dentro del rango
                rangeForBorders.Style.Border.Top.Style = borderStyle;
                rangeForBorders.Style.Border.Bottom.Style = borderStyle;
                rangeForBorders.Style.Border.Left.Style = borderStyle;
                rangeForBorders.Style.Border.Right.Style = borderStyle;

                // También puedes aplicar un borde exterior al rango completo
                var border = rangeForBorders.Style.Border;
                border.BorderAround(borderStyle);
                if (!string.IsNullOrEmpty(ObraEP))
                {
                    worksheet.Cells["A11"].Value = "ENTIDAD PATROCINANTE: " + ObraEP;
                    worksheet.Cells["A11"].Style.Font.Bold = true;
                }

                if (!string.IsNullOrEmpty(cargoAdminObraFullName))
                {
                    worksheet.Cells["A12"].Value = "ADMINISTRADOR DE OBRA: " + cargoAdminObraFullName;
                    worksheet.Cells["A12"].Style.Font.Bold = true;
                }

                if (!string.IsNullOrEmpty(cargoAutocontrolFullName))
                {
                    worksheet.Cells["A13"].Value = "AUTOCONTROL: " + cargoAutocontrolFullName;
                    worksheet.Cells["A13"].Style.Font.Bold = true;
                }

                if (!string.IsNullOrEmpty(cargofto1FullName))
                {
                    worksheet.Cells["A14"].Value = "F.T.O 1: " + cargofto1FullName;
                    worksheet.Cells["A14"].Style.Font.Bold = true;
                }

                // Inicializa el texto que se mostrará en la celda
                string cellText = string.Empty;

                // Verifica si cargofto2FullName tiene un valor
                if (!string.IsNullOrEmpty(cargofto2FullName))
                {
                    cellText = "F.T.O 2: " + cargofto2FullName;
                }
                // Verifica si cargoserviu tiene un valor
                else if (!string.IsNullOrEmpty(cargoserviu))
                {
                    cellText = "SUPERVISOR SERVIU: " + cargoserviu;
                }

                // Asigna el texto a la celda A15 y establece el estilo de la fuente en negrita si se ha definido texto
                if (!string.IsNullOrEmpty(cellText))
                {
                    worksheet.Cells["A15"].Value = cellText;
                    worksheet.Cells["A15"].Style.Font.Bold = true;
                }


                // Ajustar ancho de columnas
                worksheet.Column(1).Width = 5; // Número de fila
                worksheet.Column(2).Width = 70; // Elemento de Verificación (ancho ajustado)
                int colOffset = 3; // Offset para la primera columna de inmueble
                for (int i = 0; i < codigosInmueble.Count; i++)
                {
                    worksheet.Column(colOffset + i * 3).Width = 15; // Fecha Revisión ahora es SUPV
                    worksheet.Column(colOffset + i * 3 + 1).Width = 15; // SUPV AHORA ES AC
                    worksheet.Column(colOffset + i * 3 + 2).Width = 15; // AC AHORA ES F.T.O
                }

                // Aquí ajustamos para que la tabla comience en la celda A17
                worksheet.Cells["A17"].Value = "N°";
                worksheet.Cells["B17"].Value = "Elemento de Verificación";

                for (int i = 0; i < codigosInmueble.Count; i++)
                {
                    worksheet.Cells[17, colOffset + i * 3, 17, colOffset + i * 3 + 2].Merge = true;
                    worksheet.Cells[17, colOffset + i * 3].Value = "Codigo Inmueble: " + codigosInmueble[i];
                    worksheet.Cells[18, colOffset + i * 3].Value = "SUPV";
                    worksheet.Cells[18, colOffset + i * 3 + 1].Value = "AC";
                    worksheet.Cells[18, colOffset + i * 3 + 2].Value = "F.T.O";
                }

                using (var range = worksheet.Cells[17, 1, 18, colOffset + codigosInmueble.Count * 3 - 1])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                int row = 19; // Ajuste para que la tabla de datos comience en la fila 19

                var agrupados = query
               .GroupBy(x => new { x.label, x.elemento_verificacion })
               .OrderBy(g => g.Key.label)
               .ThenBy(g => g.Key.elemento_verificacion);

                foreach (var detalle in agrupados)
                {
                    worksheet.Cells[row, 1].Value = detalle.Key.label;
                    worksheet.Cells[row, 2].Value = detalle.Key.elemento_verificacion;

                    for (int i = 0; i < codigosInmueble.Count; i++)
                    {
                        var codigoInmueble = codigosInmueble[i];
                        var detalleInmueble = detalle.FirstOrDefault(x => x.codigo_inmueble == codigoInmueble);
                        var tipoItem = detalleInmueble.tipo_item;

                        if (detalleInmueble != null)
                        {
                            // fechas
                            var fechaSupv = detalleInmueble.fecha_supv.HasValue ? detalleInmueble.fecha_supv.Value.ToString("dd-MM-yyyy") : string.Empty;
                            var fechaAutocontrol = detalleInmueble.fecha_autocontrol.HasValue ? detalleInmueble.fecha_autocontrol.Value.ToString("dd-MM-yyyy") : string.Empty;
                            var fechaFto = detalleInmueble.fecha_fto.HasValue ? detalleInmueble.fecha_fto.Value.ToString("dd-MM-yyyy") : string.Empty;
                            // Condición para Estado de Supervisor
                            if (detalleInmueble.estado_supv == null && tipoItem == true)
                            {
                                // No se debe mostrar nada
                            }
                            else if (detalleInmueble.estado_supv == true && detalleInmueble.fecha_supv != null && tipoItem == true)
                            {
                                worksheet.Cells[row, colOffset + i * 3].Value = fechaSupv;
                            }
                            else if (detalleInmueble.estado_supv == true)
                            {
                                worksheet.Cells[row, colOffset + i * 3].Value = "Aprobado";
                            }
                            else if (detalleInmueble.estado_supv == false && detalleInmueble.fecha_supv == null && tipoItem == true)
                            {
                                // No se debe mostrar nada
                            }
                            else if (detalleInmueble.estado_supv == false)
                            {
                                worksheet.Cells[row, colOffset + i * 3].Value = "Rechazado";
                            }
                            else
                            {
                                worksheet.Cells[row, colOffset + i * 3].Value = "Sin Revisión";
                            }
                            // Condición para Estado de Autocontrol (estado_autocontrol)
                            if (detalleInmueble.estado_autocontrol == null && tipoItem == true)
                            {
                                // No se debe mostrar nada
                            }
                            else if (detalleInmueble.estado_autocontrol == true && detalleInmueble.fecha_autocontrol != null && tipoItem == true)
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 1].Value = fechaAutocontrol;
                            }
                            else if (detalleInmueble.estado_autocontrol == true)
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 1].Value = "Aprobado";
                            }
                            else if (detalleInmueble.estado_autocontrol == false && detalleInmueble.fecha_autocontrol == null && tipoItem == true)
                            {
                                // No se debe mostrar nada
                            }
                            else if (detalleInmueble.estado_autocontrol == false)
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 1].Value = "Rechazado";
                            }
                            else
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 1].Value = "Sin Revisión";
                            }
                            // Condición para Estado de F.T.O
                            if (detalleInmueble.estado_ito == null && tipoItem == true)
                            {
                                // No se debe mostrar nada
                            }
                            else if (detalleInmueble.estado_ito == true && detalleInmueble.fecha_fto != null && tipoItem == true)
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 2].Value = fechaFto;
                            }
                            else if (detalleInmueble.estado_ito == true)
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 2].Value = "Aprobado";
                            }
                            else if (detalleInmueble.estado_ito == false && detalleInmueble.fecha_fto == null && tipoItem == true)
                            {
                                // No se debe mostrar nada
                            }
                            else if (detalleInmueble.estado_ito == false)
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 2].Value = "Rechazado";
                            }
                            else
                            {
                                worksheet.Cells[row, colOffset + i * 3 + 2].Value = "Sin Revisión";
                            }
                        }
                    }
                    row++;
                }

                using (var range = worksheet.Cells[17, 1, row - 1, colOffset + codigosInmueble.Count * 3 - 1])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                // Agregar observaciones públicas después de la tabla
                worksheet.Cells[row, 1].Value = "Observaciones Públicas: " + observacionesPublicas;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Merge = true; // Fusionar celdas para cubrir toda la fila
                worksheet.Cells[row, 1].Style.WrapText = true;

                // Aplicar bordes a la fila completa
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                row++; // Avanzar a la siguiente fila para las observaciones privadas

                // Agregar observaciones privadas después de observaciones públicas
                worksheet.Cells[row, 1].Value = "Observaciones Privadas: " + observacionesPrivadas;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Merge = true; // Fusionar celdas para cubrir toda la fila
                worksheet.Cells[row, 1].Style.WrapText = true;

                // Aplicar bordes a la fila completa
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1, row, colOffset + codigosInmueble.Count * 3 - 1].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                row++; // Avanzar a la siguiente fila después de las observaciones privadas



                fileContents = package.GetAsByteArray();
            }

            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CartillaAutocontrol.xlsx");
        }


     

        public ActionResult GenerarPdf(int cartilla_id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;
                // Obtener los detalles de la cartilla
                var detalles = db.vw_DetalleCartilla
                             .Where(d => d.CARTILLA_cartilla_id == cartilla_id)
                             .OrderBy(dc => dc.label)
                             .ToList();
                // Obtener la información de la obra
                var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == cartilla_id);
                var obra = cartilla != null ? db.OBRA.FirstOrDefault(o => o.obra_id == cartilla.OBRA_obra_id) : null;
                var personas = cartilla != null ? db.RESPONSABLE.Include(r => r.PERSONA).FirstOrDefault(r => r.OBRA_obra_id == cartilla.OBRA_obra_id) : null;
                // Establecer el ViewBag con la información de la obra
                ViewBag.ObraNombre = obra != null ? obra.nombre_obra : "No disponible";
                ViewBag.ObraDireccion = obra != null ? obra.direccion : "No disponible";
                ViewBag.ObraComuna = obra != null ? obra.COMUNA.nombre_comuna : "No disponible";
                ViewBag.ActividadNombre = cartilla != null ? cartilla.ACTIVIDAD.nombre_actividad : "No disponible";
                ViewBag.Entidad = obra != null ? obra.entidad_patrocinante : "No disponible";
                ViewBag.ActividadNotas = cartilla != null ? cartilla.ACTIVIDAD.notas : "No disponible";
                ViewBag.ActividadCodigo = cartilla != null ? cartilla.ACTIVIDAD.codigo_actividad : "No disponible";
                ViewBag.ObservacionesPublic = cartilla != null ? cartilla.observaciones : "No disponible";
                ViewBag.CartillaFecha = cartilla != null && cartilla.fecha != null ? cartilla.fecha.ToString("dd/MM/yyyy") : "";
                ViewBag.CartillaFechaModif = cartilla != null && cartilla.fecha_modificacion.HasValue
                    ? cartilla.fecha_modificacion.Value.ToString("dd/MM/yyyy")
                    : "";
                @ViewBag.Estado = cartilla != null ? cartilla.ESTADO_FINAL.descripcion : "No disponible";
                var ReponsablesObra = db.RESPONSABLE.Include(r => r.PERSONA)
                 .Where(r => r.OBRA.CARTILLA.Any(c => c.OBRA_obra_id == cartilla.OBRA_obra_id))
                 .ToList();
                ViewBag.Responsables = ReponsablesObra;



                var orderedFirmas = ReponsablesObra.OrderBy(firma =>
                {
                    switch (firma.cargo)
                    {
                        case "Administrador de Obra":
                            return 1;
                        case "Autocontrol":
                            return 2;
                        case "F.T.O 1":
                            return 3;
                        case "F.T.O 2":
                            return 4;
                        case "Supervisor Serviu":
                            return 5;
                        default:
                            return 6;
                    }
                }).ToList();

                ViewBag.Firmas = orderedFirmas;

                // Agrupar por inmueble
                var groupedByInmueble = detalles
                    .GroupBy(d => d.inmueble_id)
                    .OrderBy(g =>
                    {
                        var codigoInmueble = g.First().codigo_inmueble;
                        var prefix = Regex.Match(codigoInmueble, @"^[A-Za-z-]+").Value;
                        var numericPart = int.Parse(Regex.Match(codigoInmueble, @"\d+").Value);
                        return (prefix, numericPart);
                    })
                    .ToList();
                // Dividir en páginas con un máximo de 8 inmuebles por página
                int pageSize = 8;
                var pagedDetails = groupedByInmueble
                    .Select((group, index) => new { group, index })
                    .GroupBy(x => x.index / pageSize)
                    .Select(g => g.Select(x => x.group).ToList()) // Asegúrate de que sea una lista dentro del grupo
                    .ToList();
                // Pasar los detalles paginados y la información de la obra a la vista
                var pdfResult = new Rotativa.ViewAsPdf("GenerarPdf", pagedDetails)
                {
                    PageWidth = 400,
                    PageHeight = 800,
                    PageOrientation = Rotativa.Options.Orientation.Landscape,
                };
                return pdfResult;
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult GenerarPdf2(int cartilla_id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;
                // Obtener los detalles de la cartilla
                var detalles = db.vw_DetalleCartilla
                             .Where(d => d.CARTILLA_cartilla_id == cartilla_id)
                             .OrderBy(dc => dc.label)
                             .ToList();
                // Obtener la información de la obra
                var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == cartilla_id);
                var obra = cartilla != null ? db.OBRA.FirstOrDefault(o => o.obra_id == cartilla.OBRA_obra_id) : null;
                var personas = cartilla != null ? db.RESPONSABLE.Include(r => r.PERSONA).FirstOrDefault(r => r.OBRA_obra_id == cartilla.OBRA_obra_id) : null;
                // Establecer el ViewBag con la información de la obra
                ViewBag.ObraNombre = obra != null ? obra.nombre_obra : "No disponible";
                ViewBag.ObraDireccion = obra != null ? obra.direccion : "No disponible";
                ViewBag.ObraComuna = obra != null ? obra.COMUNA.nombre_comuna : "No disponible";
                ViewBag.ActividadNombre = cartilla != null ? cartilla.ACTIVIDAD.nombre_actividad : "No disponible";
                ViewBag.Entidad = obra != null ? obra.entidad_patrocinante : "No disponible";
                ViewBag.ActividadNotas = cartilla != null ? cartilla.ACTIVIDAD.notas : "No disponible";
                ViewBag.ActividadCodigo = cartilla != null ? cartilla.ACTIVIDAD.codigo_actividad : "No disponible";
                ViewBag.ObservacionesPublic = cartilla != null ? cartilla.observaciones : "No disponible";
                ViewBag.CartillaFecha = cartilla != null && cartilla.fecha != null ? cartilla.fecha.ToString("dd/MM/yyyy") : "";
                ViewBag.CartillaFechaModif = cartilla != null && cartilla.fecha_modificacion.HasValue
                    ? cartilla.fecha_modificacion.Value.ToString("dd/MM/yyyy")
                    : "";
                @ViewBag.Estado = cartilla != null ? cartilla.ESTADO_FINAL.descripcion : "No disponible";
                var ReponsablesObra = db.RESPONSABLE.Include(r => r.PERSONA)
                 .Where(r => r.OBRA.CARTILLA.Any(c => c.OBRA_obra_id == cartilla.OBRA_obra_id))
                 .ToList();
                ViewBag.Responsables = ReponsablesObra;



                var orderedFirmas = ReponsablesObra.OrderBy(firma =>
                {
                    switch (firma.cargo)
                    {
                        case "Administrador de Obra":
                            return 1;
                        case "Autocontrol":
                            return 2;
                        case "F.T.O 1":
                            return 3;
                        case "F.T.O 2":
                            return 4;
                        case "Supervisor Serviu":
                            return 5;
                        default:
                            return 6;
                    }
                }).ToList();

                ViewBag.Firmas = orderedFirmas;

                // Agrupar por inmueble
                var groupedByInmueble = detalles
                    .GroupBy(d => d.inmueble_id)
                    .OrderBy(g =>
                    {
                        var codigoInmueble = g.First().codigo_inmueble;
                        var prefix = Regex.Match(codigoInmueble, @"^[A-Za-z-]+").Value;
                        var numericPart = int.Parse(Regex.Match(codigoInmueble, @"\d+").Value);
                        return (prefix, numericPart);
                    })
                    .ToList();
                // Dividir en páginas con un máximo de 8 inmuebles por página
                int pageSize = 8;
                var pagedDetails = groupedByInmueble
                    .Select((group, index) => new { group, index })
                    .GroupBy(x => x.index / pageSize)
                    .Select(g => g.Select(x => x.group).ToList()) // Asegúrate de que sea una lista dentro del grupo
                    .ToList();
                // Pasar los detalles paginados y la información de la obra a la vista
                var pdfResult = new Rotativa.ViewAsPdf("GenerarPdf2", pagedDetails)
                {
                    PageWidth = 400,
                    PageHeight = 800,
                    PageOrientation = Rotativa.Options.Orientation.Landscape,
                };
                return pdfResult;
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult GenerarPdfPorLote(int cartilla_id, int? lote_id)
        {
            // Obtener los detalles de la cartilla filtrando por lote_id si se proporciona
            var detallesQuery = db.vw_DetalleCartilla
                .Where(d => d.CARTILLA_cartilla_id == cartilla_id);

            if (lote_id.HasValue)
            {
                detallesQuery = detallesQuery.Where(d => d.lote_id == lote_id.Value);
            }

            var detalles = detallesQuery
                .OrderBy(dc => dc.label)
                .ToList();

            // Obtener la información de la obra
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == cartilla_id);
            var obra = cartilla != null ? db.OBRA.FirstOrDefault(o => o.obra_id == cartilla.OBRA_obra_id) : null;
            var personas = cartilla != null ? db.RESPONSABLE.Include(r => r.PERSONA).FirstOrDefault(r => r.OBRA_obra_id == cartilla.OBRA_obra_id) : null;

            // Establecer el ViewBag con la información de la obra
            ViewBag.ObraNombre = obra != null ? obra.nombre_obra : "No disponible";
            ViewBag.ObraDireccion = obra != null ? obra.direccion : "No disponible";
            ViewBag.ObraComuna = obra != null ? obra.COMUNA.nombre_comuna : "No disponible";
            ViewBag.ActividadNombre = cartilla != null ? cartilla.ACTIVIDAD.nombre_actividad : "No disponible";
            ViewBag.Entidad = obra != null ? obra.entidad_patrocinante : "No disponible";
            ViewBag.ActividadNotas = cartilla != null ? cartilla.ACTIVIDAD.notas : "No disponible";
            ViewBag.ActividadCodigo = cartilla != null ? cartilla.ACTIVIDAD.codigo_actividad : "No disponible";
            ViewBag.ObservacionesPublic = cartilla != null ? cartilla.observaciones : "No disponible";
            ViewBag.CartillaFecha = cartilla != null && cartilla.fecha != null ? cartilla.fecha.ToString("dd/MM/yyyy") : "";
            ViewBag.CartillaFechaModif = cartilla != null && cartilla.fecha_modificacion.HasValue
                ? cartilla.fecha_modificacion.Value.ToString("dd/MM/yyyy")
                : "";
            ViewBag.Estado = cartilla != null ? cartilla.ESTADO_FINAL.descripcion : "No disponible";

            var ReponsablesObra = db.RESPONSABLE.Include(r => r.PERSONA)
                .Where(r => r.OBRA.CARTILLA.Any(c => c.OBRA_obra_id == cartilla.OBRA_obra_id))
                .ToList();

            ViewBag.Responsables = ReponsablesObra;

            var orderedFirmas = ReponsablesObra.OrderBy(firma =>
            {
                switch (firma.cargo)
                {
                    case "Administrador de Obra":
                        return 1;
                    case "Autocontrol":
                        return 2;
                    case "F.T.O 1":
                        return 3;
                    case "F.T.O 2":
                        return 4;
                    case "Supervisor Serviu":
                        return 5;
                    default:
                        return 6;
                }
            }).ToList();

            ViewBag.Firmas = orderedFirmas;

            // Agrupar por inmueble
            var groupedByInmueble = detalles
                .GroupBy(d => d.inmueble_id)
                .OrderBy(g =>
                {
                    var codigoInmueble = g.First().codigo_inmueble;
                    var prefix = Regex.Match(codigoInmueble, @"^[A-Za-z-]+").Value;
                    var numericPart = int.Parse(Regex.Match(codigoInmueble, @"\d+").Value);
                    return (prefix, numericPart);
                })
                .ToList();

            // Dividir en páginas con un máximo de 8 inmuebles por página
            int pageSize = 8;
            var pagedDetails = groupedByInmueble
                .Select((group, index) => new { group, index })
                .GroupBy(x => x.index / pageSize)
                .Select(g => g.Select(x => x.group).ToList()) // Asegúrate de que sea una lista dentro del grupo
                .ToList();

            // Pasar los detalles paginados y la información de la obra a la vista
            var pdfResult = new Rotativa.ViewAsPdf("GenerarPdf", pagedDetails)
            {
                PageWidth = 400,
                PageHeight = 800,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
            };
            return pdfResult;
        }

        public ActionResult GenerarPdfPorLote2(int cartilla_id, int? lote_id)
        {
            // Obtener los detalles de la cartilla filtrando por lote_id si se proporciona
            var detallesQuery = db.vw_DetalleCartilla
                .Where(d => d.CARTILLA_cartilla_id == cartilla_id);

            if (lote_id.HasValue)
            {
                detallesQuery = detallesQuery.Where(d => d.lote_id == lote_id.Value);
            }

            var detalles = detallesQuery
                .OrderBy(dc => dc.label)
                .ToList();

            // Obtener la información de la obra
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == cartilla_id);
            var obra = cartilla != null ? db.OBRA.FirstOrDefault(o => o.obra_id == cartilla.OBRA_obra_id) : null;
            var personas = cartilla != null ? db.RESPONSABLE.Include(r => r.PERSONA).FirstOrDefault(r => r.OBRA_obra_id == cartilla.OBRA_obra_id) : null;

            // Establecer el ViewBag con la información de la obra
            ViewBag.ObraNombre = obra != null ? obra.nombre_obra : "No disponible";
            ViewBag.ObraDireccion = obra != null ? obra.direccion : "No disponible";
            ViewBag.ObraComuna = obra != null ? obra.COMUNA.nombre_comuna : "No disponible";
            ViewBag.ActividadNombre = cartilla != null ? cartilla.ACTIVIDAD.nombre_actividad : "No disponible";
            ViewBag.Entidad = obra != null ? obra.entidad_patrocinante : "No disponible";
            ViewBag.ActividadNotas = cartilla != null ? cartilla.ACTIVIDAD.notas : "No disponible";
            ViewBag.ActividadCodigo = cartilla != null ? cartilla.ACTIVIDAD.codigo_actividad : "No disponible";
            ViewBag.ObservacionesPublic = cartilla != null ? cartilla.observaciones : "No disponible";
            ViewBag.CartillaFecha = cartilla != null && cartilla.fecha != null ? cartilla.fecha.ToString("dd/MM/yyyy") : "";
            ViewBag.CartillaFechaModif = cartilla != null && cartilla.fecha_modificacion.HasValue
                ? cartilla.fecha_modificacion.Value.ToString("dd/MM/yyyy")
                : "";
            ViewBag.Estado = cartilla != null ? cartilla.ESTADO_FINAL.descripcion : "No disponible";

            var ReponsablesObra = db.RESPONSABLE.Include(r => r.PERSONA)
                .Where(r => r.OBRA.CARTILLA.Any(c => c.OBRA_obra_id == cartilla.OBRA_obra_id))
                .ToList();

            ViewBag.Responsables = ReponsablesObra;

            var orderedFirmas = ReponsablesObra.OrderBy(firma =>
            {
                switch (firma.cargo)
                {
                    case "Administrador de Obra":
                        return 1;
                    case "Autocontrol":
                        return 2;
                    case "F.T.O 1":
                        return 3;
                    case "F.T.O 2":
                        return 4;
                    case "Supervisor Serviu":
                        return 5;
                    default:
                        return 6;
                }
            }).ToList();

            ViewBag.Firmas = orderedFirmas;

            // Agrupar por inmueble
            var groupedByInmueble = detalles
                .GroupBy(d => d.inmueble_id)
                .OrderBy(g =>
                {
                    var codigoInmueble = g.First().codigo_inmueble;
                    var prefix = Regex.Match(codigoInmueble, @"^[A-Za-z-]+").Value;
                    var numericPart = int.Parse(Regex.Match(codigoInmueble, @"\d+").Value);
                    return (prefix, numericPart);
                })
                .ToList();

            // Dividir en páginas con un máximo de 8 inmuebles por página
            int pageSize = 8;
            var pagedDetails = groupedByInmueble
                .Select((group, index) => new { group, index })
                .GroupBy(x => x.index / pageSize)
                .Select(g => g.Select(x => x.group).ToList()) // Asegúrate de que sea una lista dentro del grupo
                .ToList();

            // Pasar los detalles paginados y la información de la obra a la vista
            var pdfResult = new Rotativa.ViewAsPdf("GenerarPdf2", pagedDetails)
            {
                PageWidth = 400,
                PageHeight = 800,
                PageOrientation = Rotativa.Options.Orientation.Landscape,
            };
            return pdfResult;
        }





    }
}