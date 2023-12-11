﻿using System;
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
using Aspose.Pdf;
using Aspose.Pdf.Facades;
using Ionic.Zip;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class EnviarPorCorreoController : Controller
    {
        private ObraManzanoDicEntities db = new ObraManzanoDicEntities();

        public ActionResult GeneratePDF(int id)
        {
            var actividad = db.ACTIVIDAD.SingleOrDefault(a => a.actividad_id == id);

            if (actividad == null)
            {
                return HttpNotFound();
            }

            var elementosVerificacion = db.DETALLE_CARTILLA
                .Include(dc => dc.ITEM_VERIF)
                .Where(dc => dc.CARTILLA.ACTIVIDAD_actividad_id == actividad.actividad_id)
                .ToList();

            var ReponsablesObra = db.RESPONSABLE.Include(r => r.PERSONA).ToList();
            ViewBag.Responsables = ReponsablesObra;
            ViewBag.Actividad = actividad;

            var pdfStream = new MemoryStream();
            var pdf = new ViewAsPdf("GeneratePDF", elementosVerificacion)
            {
                FileName = "CartillaDeControl.pdf",
                PageSize = Size.B4,
                PageOrientation = Orientation.Landscape,
                CustomSwitches = "--disable-smart-shrinking",
            };

            var pdfBytes = pdf.BuildFile(ControllerContext);
            pdfStream.Write(pdfBytes, 0, pdfBytes.Length);
            pdfStream.Position = 0;

            return File(pdfStream, "application/pdf");
        }


    

        public ActionResult EnviarPDFPorCorreo(int id)
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            string correoDestinatario = usuarioAutenticado.PERSONA.correo;

            var pdfResult = GeneratePDF(id) as FileStreamResult;

            if (pdfResult != null)
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "yalt vlic kpmy nlvv");
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com");
                        mailMessage.Subject = "Cartilla de Control Vivienda";
                        mailMessage.Body = "Cartilla de Control Vivienda";

                        mailMessage.To.Add(correoDestinatario);

                        // Crear un nuevo MemoryStream y copiar el contenido del FileStream original
                        using (var pdfMemoryStream = new MemoryStream())
                        {
                            pdfResult.FileStream.CopyTo(pdfMemoryStream);
                            pdfMemoryStream.Position = 0;

                            // Agregar el archivo adjunto al correo
                            mailMessage.Attachments.Add(new Attachment(pdfMemoryStream, "CartillaDeControl.pdf"));

                            smtpClient.Send(mailMessage);
                        }
                    }
                }
            }
            else
            {
                // Manejar el caso en el que el resultado no es FileStreamResult
                // Puedes agregar aquí el código para manejar este caso según tus necesidades.
            }

            return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
        }



        
    }
}