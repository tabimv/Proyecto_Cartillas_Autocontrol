using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Rotativa;
using Proyecto_Cartilla_Autocontrol.Models;
using System.Data.Entity;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using Rotativa.Options;
using System.IO.Compression;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Ionic.Zip;
using DocumentFormat.OpenXml.EMMA;
using System.Text.RegularExpressions;



namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class EnviarPorCorreoController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        public async Task<FileStreamResult> GenerarPdfCorreoAsync(int cartilla_id)
        {
            // Obtener los detalles de la cartilla
            var detalles = await db.vw_DetalleCartilla
                                    .Where(d => d.CARTILLA_cartilla_id == cartilla_id)
                                    .OrderBy(dc => dc.label)
                                    .ToListAsync();

            // Obtener la información de la cartilla y la obra asociada
            var cartilla = await db.CARTILLA
                                   .Include(c => c.ACTIVIDAD)
                                   .Include(c => c.OBRA)
                                   .FirstOrDefaultAsync(c => c.cartilla_id == cartilla_id);

            var obra = cartilla?.OBRA;
            var actividad = cartilla?.ACTIVIDAD;

            // Establecer información de la obra y cartilla en el ViewBag
            ViewBag.ObraNombre = obra?.nombre_obra ?? "No disponible";
            ViewBag.ObraDireccion = obra?.direccion ?? "No disponible";
            ViewBag.ObraComuna = obra?.COMUNA?.nombre_comuna ?? "No disponible";
            ViewBag.ActividadNombre = actividad?.nombre_actividad ?? "No disponible";
            ViewBag.ActividadCodigo = actividad?.codigo_actividad ?? "No disponible";
            ViewBag.Entidad = obra?.entidad_patrocinante ?? "No disponible";
            ViewBag.ActividadNotas = actividad?.notas ?? "No disponible";
            ViewBag.ObservacionesPublic = cartilla?.observaciones ?? "No disponible";
            ViewBag.CartillaFecha = cartilla?.fecha.ToString("dd/MM/yyyy") ?? "";
            ViewBag.CartillaFechaModif = cartilla?.fecha_modificacion.HasValue ?? false
                ? cartilla.fecha_modificacion.Value.ToString("dd/MM/yyyy")
                : "";
            ViewBag.Estado = cartilla?.ESTADO_FINAL?.descripcion ?? "No disponible";

            // Obtener los responsables de la obra
            var responsablesObra = await db.RESPONSABLE
                                          .Include(r => r.PERSONA)
                                          .Where(r => r.OBRA.CARTILLA.Any(c => c.OBRA_obra_id == cartilla.OBRA_obra_id))
                                          .ToListAsync();

            ViewBag.Responsables = responsablesObra;

            // Ordenar las firmas de los responsables por su cargo
            var orderedFirmas = responsablesObra.OrderBy(firma =>
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

            // Agrupar los detalles por inmueble y ordenar
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

            // Dividir los detalles en páginas, con un máximo de 8 inmuebles por página
            int pageSize = 8;
            var pagedDetails = groupedByInmueble
                .Select((group, index) => new { group, index })
                .GroupBy(x => x.index / pageSize)
                .Select(g => g.Select(x => x.group).ToList())
                .ToList();

            // Generar el PDF
            var pdf = new Rotativa.ViewAsPdf("GenerarPdfCorreoAsync", pagedDetails)
            {
                PageWidth = 400,
                PageHeight = 800,
                CustomSwitches = "--zoom 0.95", // Ajusta el zoom para escalar el contenido a una sola página
                PageOrientation = Rotativa.Options.Orientation.Landscape,
            };

            // Obtener el MemoryStream del PDF
            var pdfStream = new MemoryStream(pdf.BuildFile(ControllerContext));

            // Devolver el FileStreamResult
            return new FileStreamResult(pdfStream, "application/pdf")
            {
                FileDownloadName = "Cartilla_Autocontrol.pdf"
            };
        }


        public async Task<ActionResult> EnviarPDFPorCorreo(int cartillaId)
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            string correoDestinatario = usuarioAutenticado.PERSONA.correo;

            // Generar el PDF y obtener el stream
            var pdfResult = await GenerarPdfCorreoAsync(cartillaId);
            var pdfStream = new MemoryStream();
            await pdfResult.FileStream.CopyToAsync(pdfStream);
            pdfStream.Position = 0;

            // Obtener los nombres de la actividad y la obra utilizando el cartillaId
            string nombreActividad, nombreObra;
            ObtenerNombresPorIDCartilla(cartillaId, out nombreActividad, out nombreObra);

            // Envía el correo con el PDF adjunto
            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "yalt vlic kpmy nlvv");
                smtpClient.EnableSsl = true;

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com");
                    mailMessage.Subject = "Cartilla de Control Vivienda";
                    mailMessage.Body = $"Cartilla de Control Vivienda para la Obra Asociada: {nombreObra} y Actividad: {nombreActividad}";
                    mailMessage.To.Add(correoDestinatario);
                    mailMessage.Attachments.Add(new Attachment(pdfStream, "CartillaDeControl.pdf"));
                    smtpClient.Send(mailMessage);
                }
            }

            // Redirigir según el rol del usuario
            if (usuarioAutenticado.PERFIL.rol.Equals("Administrador"))
            {
                return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
            }
            else if (usuarioAutenticado.PERFIL.rol.Equals("Supervisor"))
            {
                return RedirectToAction("ListaCartillasSupervisor", "CartillasAutocontrolFiltrado");
            }
            else if (usuarioAutenticado.PERFIL.rol.Equals("Autocontrol") || usuarioAutenticado.PERFIL.rol.Equals("Consulta"))
            {
                return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrolFiltrado");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }


        // Método para obtener el nombre de la actividad y de la obra por Cartilla_ID
        private void ObtenerNombresPorIDCartilla(int cartillaId, out string nombreActividad, out string nombreObra)
        {
            // Utiliza cartillaId para obtener los nombres de la actividad y obra correspondientes.
            using (var dbContext = new ObraManzanoFinal())
            {
                // Obtener la cartilla según cartillaId
                var cartilla = dbContext.CARTILLA.Include(c => c.ACTIVIDAD)
                                                 .Include(c => c.OBRA)
                                                 .FirstOrDefault(c => c.cartilla_id == cartillaId);
                if (cartilla != null)
                {
                    // Asignar los nombres de actividad y obra
                    nombreActividad = cartilla.ACTIVIDAD?.nombre_actividad ?? "Actividad Desconocida";
                    nombreObra = cartilla.OBRA?.nombre_obra ?? "Obra Desconocida";
                    return;
                }
            }

            // Si no se encuentra la cartilla, devolver valores por defecto o manejar de otra manera según tus necesidades.
            nombreActividad = "Actividad Desconocida";
            nombreObra = "Obra Desconocida";
        }


        public async Task<FileStreamResult> GenerarPdfCorreoAsync2(int cartilla_id)
        {
            // Obtener los detalles de la cartilla
            var detalles = await db.vw_DetalleCartilla
                                    .Where(d => d.CARTILLA_cartilla_id == cartilla_id)
                                    .OrderBy(dc => dc.label)
                                    .ToListAsync();

            // Obtener la información de la cartilla y la obra asociada
            var cartilla = await db.CARTILLA
                                   .Include(c => c.ACTIVIDAD)
                                   .Include(c => c.OBRA)
                                   .FirstOrDefaultAsync(c => c.cartilla_id == cartilla_id);

            var obra = cartilla?.OBRA;
            var actividad = cartilla?.ACTIVIDAD;

            // Establecer información de la obra y cartilla en el ViewBag
            ViewBag.ObraNombre = obra?.nombre_obra ?? "No disponible";
            ViewBag.ObraDireccion = obra?.direccion ?? "No disponible";
            ViewBag.ObraComuna = obra?.COMUNA?.nombre_comuna ?? "No disponible";
            ViewBag.ActividadNombre = actividad?.nombre_actividad ?? "No disponible";
            ViewBag.ActividadCodigo = actividad?.codigo_actividad ?? "No disponible";
            ViewBag.Entidad = obra?.entidad_patrocinante ?? "No disponible";
            ViewBag.ActividadNotas = actividad?.notas ?? "No disponible";
            ViewBag.ObservacionesPublic = cartilla?.observaciones ?? "No disponible";
            ViewBag.CartillaFecha = cartilla?.fecha.ToString("dd/MM/yyyy") ?? "";
            ViewBag.CartillaFechaModif = cartilla?.fecha_modificacion.HasValue ?? false
                ? cartilla.fecha_modificacion.Value.ToString("dd/MM/yyyy")
                : "";
            ViewBag.Estado = cartilla?.ESTADO_FINAL?.descripcion ?? "No disponible";

            // Obtener los responsables de la obra
            var responsablesObra = await db.RESPONSABLE
                                          .Include(r => r.PERSONA)
                                          .Where(r => r.OBRA.CARTILLA.Any(c => c.OBRA_obra_id == cartilla.OBRA_obra_id))
                                          .ToListAsync();

            ViewBag.Responsables = responsablesObra;

            // Ordenar las firmas de los responsables por su cargo
            var orderedFirmas = responsablesObra.OrderBy(firma =>
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

            // Agrupar los detalles por inmueble y ordenar
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

            // Dividir los detalles en páginas, con un máximo de 8 inmuebles por página
            int pageSize = 8;
            var pagedDetails = groupedByInmueble
                .Select((group, index) => new { group, index })
                .GroupBy(x => x.index / pageSize)
                .Select(g => g.Select(x => x.group).ToList())
                .ToList();

            // Generar el PDF
            var pdf = new Rotativa.ViewAsPdf("GenerarPdfCorreoAsync2", pagedDetails)
            {
                PageWidth = 400,
                PageHeight = 800,
                CustomSwitches = "--zoom 0.95", // Ajusta el zoom para escalar el contenido a una sola página
                PageOrientation = Rotativa.Options.Orientation.Landscape,
            };

            // Obtener el MemoryStream del PDF
            var pdfStream = new MemoryStream(pdf.BuildFile(ControllerContext));

            // Devolver el FileStreamResult
            return new FileStreamResult(pdfStream, "application/pdf")
            {
                FileDownloadName = "Cartilla_Autocontrol.pdf"
            };
        }

        public async Task<ActionResult> EnviarPDFPorCorreo2(int cartillaId)
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            string correoDestinatario = usuarioAutenticado.PERSONA.correo;

            // Generar el PDF y obtener el stream
            var pdfResult = await GenerarPdfCorreoAsync2(cartillaId);
            var pdfStream = new MemoryStream();
            await pdfResult.FileStream.CopyToAsync(pdfStream);
            pdfStream.Position = 0;

            // Obtener los nombres de la actividad y la obra utilizando el cartillaId
            string nombreActividad, nombreObra;
            ObtenerNombresPorIDCartilla(cartillaId, out nombreActividad, out nombreObra);

            // Envía el correo con el PDF adjunto
            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "yalt vlic kpmy nlvv");
                smtpClient.EnableSsl = true;

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com");
                    mailMessage.Subject = "Cartilla de Control Vivienda";
                    mailMessage.Body = $"Cartilla de Control Vivienda para la Obra Asociada: {nombreObra} y Actividad: {nombreActividad}";
                    mailMessage.To.Add(correoDestinatario);
                    mailMessage.Attachments.Add(new Attachment(pdfStream, "CartillaDeControl.pdf"));
                    smtpClient.Send(mailMessage);
                }
            }

            // Redirigir según el rol del usuario
            if (usuarioAutenticado.PERFIL.rol.Equals("Administrador"))
            {
                return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
            }
            else if (usuarioAutenticado.PERFIL.rol.Equals("Supervisor"))
            {
                return RedirectToAction("ListaCartillasSupervisor", "CartillasAutocontrolFiltrado");
            }
            else if (usuarioAutenticado.PERFIL.rol.Equals("Autocontrol") || usuarioAutenticado.PERFIL.rol.Equals("Consulta"))
            {
                return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrolFiltrado");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }



    }
}
