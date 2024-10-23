using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Mail;
using System.Web.Configuration;
using System.Threading.Tasks;
using System.Runtime.Caching;


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class VistaPerfilSupervisorController : Controller
    {
        public ObraManzanoFinal db { get; set; }

        public VistaPerfilSupervisorController()
        {
            db = new ObraManzanoFinal();
        }

        // GET: VistaPerfilITO
        public static bool actualizacionEditCartilla = false;

        public ActionResult Index()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            ViewBag.UsuarioAutenticado = usuarioAutenticado;

            if (usuarioAutenticado != null)
            {
                // Obtiene la lista de IDs de obras a las que el usuario tiene acceso
                var obrasIds = db.ACCESO_OBRAS
                                 .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                 .Select(a => a.obra_id)
                                 .ToList();

                // Obtiene la lista de IDs de cartillas a las que el usuario tiene acceso
                var cartillasIds = db.ACCESO_CARTILLA
                                     .Where(ac => ac.USUARIO_usuario_id == usuarioAutenticado.usuario_id)
                                     .Select(ac => ac.CARTILLA_cartilla_id)
                                     .ToList();

                // Filtra las cartillas basándose en las obras a las que el usuario tiene acceso
                var cartillas = db.CARTILLA
                    .Include(c => c.ACTIVIDAD)
                    .Include(c => c.ESTADO_FINAL)
                    .Include(c => c.OBRA)
                    .Where(c => obrasIds.Contains(c.OBRA_obra_id) && cartillasIds.Contains(c.cartilla_id));

                return View(cartillas.ToList());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult ConsultaRevision(int id)
        {
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);
            if (cartilla != null)
            {
                var viewModel = new CartillasViewModel
                {
                    Cartilla = cartilla,
                    DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList(),
                    ActividadesList = db.ACTIVIDAD.ToList(),
                    ElementosVerificacion = db.ITEM_VERIF.ToList(),
                    InmuebleList = db.INMUEBLE.ToList(),
                    EstadoFinalList = db.ESTADO_FINAL.ToList()
                };
                int obraId = cartilla.OBRA_obra_id;
                ViewBag.LoteList = new SelectList(
                    db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id)))
                                    .Select(l => new { l.lote_id, l.abreviatura }).ToList(),
                    "lote_id", "abreviatura"
                );

                ViewBag.InmueblePorLote = new SelectList(
                    db.INMUEBLE.Where(i => i.LOTE_INMUEBLE.OBRA_obra_id == obraId && i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id))
                               .Select(i => new { i.inmueble_id, i.codigo_inmueble }).ToList(),
                    "inmueble_id", "codigo_inmueble"
                );
                return View(viewModel);
            }
            return RedirectToAction("Index", "Cartilla");
        }


        [HttpPost]
        public ActionResult ConsultaRevision(CartillasViewModel viewModel, List<DETALLE_CARTILLA> DetalleCartillas)
        {

            // Si llegamos aquí, hay un error, retornamos a la vista con el ViewModel
            ViewBag.LoteList = new SelectList(db.LOTE_INMUEBLE.ToList(), "lote_id", "abreviatura");
            ViewBag.InmueblePorLote = new SelectList(db.INMUEBLE.ToList(), "inmueble_id", "codigo_inmueble");
            viewModel.ActividadesList = db.ACTIVIDAD.ToList();
            viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
            viewModel.InmuebleList = db.INMUEBLE.ToList();
            viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();
            return View(viewModel);
        }

       
        public ActionResult EditarCartilla(int id)
        {
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);

            if (cartilla != null)
            {
                var viewModel = new CartillasViewModel
                {
                    Cartilla = cartilla,
                    DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList(),
                    ActividadesList = db.ACTIVIDAD.ToList(),
                    ElementosVerificacion = db.ITEM_VERIF.ToList(),
                    InmuebleList = db.INMUEBLE.ToList(),
                    EstadoFinalList = db.ESTADO_FINAL.ToList()
                };

                int obraId = cartilla.OBRA_obra_id;
                ViewBag.LoteList = new SelectList(
                    db.LOTE_INMUEBLE.Where(l => l.OBRA_obra_id == obraId && l.INMUEBLE.Any(i => i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id)))
                                    .Select(l => new { l.lote_id, l.abreviatura }).ToList(),
                    "lote_id", "abreviatura"
                );
                ViewBag.InmueblePorLote = new SelectList(
                    db.INMUEBLE.Where(i => i.LOTE_INMUEBLE.OBRA_obra_id == obraId && i.DETALLE_CARTILLA.Any(dc => dc.CARTILLA_cartilla_id == id))
                               .Select(i => new { i.inmueble_id, i.codigo_inmueble }).ToList(),
                    "inmueble_id", "codigo_inmueble"
                );

                ViewBag.SupervisorNames = db.USUARIO.ToDictionary(
            u => u.PERSONA_rut,
            u => $"{u.PERSONA.nombre} {u.PERSONA.apeliido_paterno} {u.PERSONA.apellido_materno}"
        );

                var rutSPV = ObtenerRutUsuarioAutenticado();
                ViewBag.RutUsuarioAutenticado = rutSPV;

                // Establecer la bandera global para indicar que se ha realizado una actualización
                actualizacionEditCartilla = true;

                return View(viewModel);
            }
            return RedirectToAction("Index");
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

                        // Obtener el rut del usuario autenticado
                        var rutSPV = ObtenerRutUsuarioAutenticado();

                        // Obtener los detalles existentes en la base de datos
                        var existingDetalles = dbContext.DETALLE_CARTILLA
                                                        .Where(d => d.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id)
                                                        .ToList();

                        // Recorrer los detalles recibidos en el viewModel
                        foreach (var detalleCartilla in DetalleCartillas)
                        {
                           

                            var existingDetalle = existingDetalles.FirstOrDefault(d => d.detalle_cartilla_id == detalleCartilla.detalle_cartilla_id);
                            if (existingDetalle != null)
                            {
                                // Verificar si estado_supv ha cambiado y fecha_supv aún no está establecida
                                if (existingDetalle.estado_supv != detalleCartilla.estado_supv && existingDetalle.fecha_supv == null)
                                {
                                    existingDetalle.fecha_supv = DateTime.Now;
                                }

                                // Verificar cambios y actualizar solo si hay modificaciones
                                bool hasChanged = false;

                                if (existingDetalle.estado_supv != detalleCartilla.estado_supv)
                                {
                                    // Si estado_supv ya tiene un valor no nulo, no permitir cambios
                                    if (existingDetalle.estado_supv != null)
                                    {
                                        detalleCartilla.estado_supv = existingDetalle.estado_supv;
                                    }
                                    else
                                    {
                                        existingDetalle.estado_supv = detalleCartilla.estado_supv;
                                        hasChanged = true;

                                        // Si estado_supv cambia a true o false, asignar rut_spv solo si es null
                                        if ((detalleCartilla.estado_supv == true || detalleCartilla.estado_supv == false) && existingDetalle.rut_spv == null && !string.IsNullOrEmpty(rutSPV))
                                        {
                                            existingDetalle.rut_spv = rutSPV; // Almacenar el rut del usuario autenticado
                                        }
                                    }
                                }
                                // Verificar si hay otros cambios relevantes y actualizar fecha_rev si es necesario
                                if (hasChanged)
                                {
                                    existingDetalle.fecha_rev = DateTime.Now; // Actualizar la fecha de revisión                                                                           
                                    dbContext.Entry(existingDetalle).State = EntityState.Modified;                                
                                }                            
                            }
                        }

                        // Eliminar detalles que ya no están presentes en la edición
                        foreach (var detalle in existingDetalles)
                        {
                            if (!DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                            {
                                // No eliminar detalles si estado_supv o rut_spv están establecidos
                                if (detalle.estado_supv != null || detalle.rut_spv != null)
                                {
                                    continue;
                                }
                                dbContext.DETALLE_CARTILLA.Remove(detalle);
                            }
                        }
                     
                        dbContext.SaveChanges();
                        // Verificar si enviar_correo es true antes de enviar correos
                        var cartilla = dbContext.CARTILLA
                                                .FirstOrDefault(c => c.cartilla_id == viewModel.Cartilla.cartilla_id);

                        if (cartilla != null && cartilla.enviar_correo == true)
                        {
                            EnviarCorreosAutomatizados();
                        }

                        return RedirectToAction("Index", "VistaPerfilSupervisor");
                    }

                
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            // Pasar el rut al modelo de vista en caso de errores
            ViewBag.RutUsuarioAutenticado = ObtenerRutUsuarioAutenticado();

            // Si hay un error de validación, regresar a la vista con el viewModel
            return View(viewModel);
        }


        // Ejemplo en el controlador (Controlador.cs)
        public JsonResult ObtenerEstadoInmuebles(int loteId)
        {
            // Lógica para obtener los inmuebles y sus estados.
            var inmuebles = db.DETALLE_CARTILLA
                .Where(dc => dc.INMUEBLE.LOTE_INMUEBLE.lote_id == loteId)
                .GroupBy(dc => dc.INMUEBLE_inmueble_id)
                .Select(g => new
                {
                    inmuebleId = g.Key,
                    allSupvTrue = g.All(dc => dc.estado_supv == true) // Verificar si todos son true
                })
                .ToList();

            return Json(inmuebles, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult VerificarEstadoSupv(int lote_id, int inmueble_id)
        {
            // Aquí deberías implementar la lógica para verificar los estados en la base de datos.
            bool todosEstadosTrue = VerificarEstadosSupvEnBaseDeDatos(lote_id, inmueble_id);

            return Json(new { todosEstadosTrue });
        }

        private bool VerificarEstadosSupvEnBaseDeDatos(int loteId, int inmuebleId)
        {
            // Reemplaza esto con tu lógica para consultar la base de datos.
            // Por ejemplo, puedes usar Entity Framework para verificar los estados.

            using (var context = new ObraManzanoFinal())
            {
                var todosTrue = context.DETALLE_CARTILLA
                    .Where(d => d.INMUEBLE.LOTE_INMUEBLE_lote_id == loteId && d.INMUEBLE_inmueble_id == inmuebleId)
                    .All(d => d.estado_supv == true);

                return todosTrue;
            }
        }


        // Método para obtener el correo del usuario autenticado
        private string ObtenerCorreoUsuarioAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            if (usuarioAutenticado != null)
            {
                return usuarioAutenticado.PERSONA.correo;
            }
            return null;
        }

        //esto es para pruebas
        private void EnviarCorreoExito()
        {
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "uraa qkpw rnyd asvb");
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com", "Tu Nombre o Empresa");
                    mailMessage.To.Add("ta.melo@duocuc.cl");
                    mailMessage.Subject = "Cambios guardados exitosamente";
                    mailMessage.Body = "Hola, los cambios han sido guardados exitosamente.";

                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores al enviar el correo electrónico
                // Puedes registrar el error o manejarlo de otra forma según tus necesidades
                Console.WriteLine("Error al enviar el correo: " + ex.Message);
            }
        }


        // Método para obtener el rut del usuario autenticado
        private string ObtenerRutUsuarioAutenticado()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            if (usuarioAutenticado != null)
            {
                return usuarioAutenticado.PERSONA_rut;
            }
            return null;
        }

    


        // Método para enviar correos automatizados
        public void EnviarCorreosAutomatizados()
        {
            if (actualizacionEditCartilla)
            {
                var detallesConEstadoSupv = db.DETALLE_CARTILLA
                   .Where(dc => dc.estado_supv == true && dc.ITEM_VERIF.tipo_item == true && (dc.correo_enviado_ac == false || dc.correo_enviado_ac == null)
                   && (dc.correo_enviado_supv == false || dc.correo_enviado_supv == null))
                   .Include(dc => dc.ITEM_VERIF)
                   .Include(dc => dc.CARTILLA)
                   .Include(dc => dc.INMUEBLE.LOTE_INMUEBLE)
                   .ToList();

                var groupedDetalles = detallesConEstadoSupv
                    .GroupBy(dc => new { dc.INMUEBLE.LOTE_INMUEBLE.lote_id, dc.INMUEBLE.inmueble_id, dc.CARTILLA_cartilla_id });

                foreach (var grupo in groupedDetalles)
                {
                    var primerDetalle = grupo.First();
                    var nombreActividad = primerDetalle.CARTILLA.ACTIVIDAD.nombre_actividad;
                    var codigoActividad = primerDetalle.CARTILLA.ACTIVIDAD.codigo_actividad;
                    var nombreObra = primerDetalle.CARTILLA.OBRA.nombre_obra;
                    var cartillaId = primerDetalle.CARTILLA.cartilla_id;
                    var fechaSupv = primerDetalle.fecha_supv;
                    var loteInmueble = primerDetalle.INMUEBLE.LOTE_INMUEBLE.abreviatura;
                    var inmueble = primerDetalle.INMUEBLE.codigo_inmueble;
                    var obspublicas = primerDetalle.CARTILLA.observaciones;
                    var obspriv = primerDetalle.CARTILLA.observaciones_priv;

                    var correoAutocontrol = ObtenerCorreoPorPerfilYObra(primerDetalle.CARTILLA.OBRA_obra_id, 3);
                    if (!string.IsNullOrEmpty(correoAutocontrol))
                    {
                        var correoSupervisor = ObtenerCorreoSupervisorAutenticado(primerDetalle.CARTILLA.OBRA_obra_id);

                        if (!string.IsNullOrEmpty(correoSupervisor))
                        {
                            var nombreCompletoSupervisor = ObtenerNombreCompletoSupervisorAutenticado();

                            EnviarCorreo(nombreActividad, codigoActividad, nombreObra, loteInmueble, inmueble, cartillaId, fechaSupv, correoAutocontrol, correoSupervisor, nombreCompletoSupervisor, obspublicas, obspriv);
                        }

                        foreach (var detalle in grupo)
                        {
                            detalle.correo_enviado_ac = true;
                            detalle.correo_enviado_supv = true;
                        }
                    }
                }

                db.SaveChanges();

                actualizacionEditCartilla = false;
            }
        }


        private string ObtenerNombreCompletoSupervisorAutenticado()
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

        private string ObtenerCorreoSupervisorAutenticado(int obraId)
        {
            if (Session != null && Session["UsuarioAutenticado"] is USUARIO usuarioAutenticado)
            {
                // Obtener las IDs de obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                // Verifica si la obra solicitada está en la lista de obras a las que el usuario tiene acceso
                if (obrasAcceso.Contains(obraId))
                {
                    // Buscar el supervisor asociado a la obra con el perfil específico
                    var correoSupervisor = (from u in db.USUARIO
                                            join a in db.ACCESO_OBRAS on u.usuario_id equals a.usuario_id
                                            join p in db.PERSONA on u.PERSONA_rut equals p.rut
                                            where a.obra_id == obraId
                                              && u.PERFIL_perfil_id == 4
                                              && a.usuario_id == usuarioAutenticado.usuario_id
                                            select p.correo).FirstOrDefault();

                    return correoSupervisor;
                }
            }

            return null;
        }



        private string ObtenerCorreoPorPerfilYObra(int obraId, int perfilId)
        {
            // Buscar el correo del usuario con el perfil dado y la obra especificada
            var correo = (from a in db.ACCESO_OBRAS
                          join u in db.USUARIO on a.usuario_id equals u.usuario_id
                          join p in db.PERSONA on u.PERSONA_rut equals p.rut
                          where a.obra_id == obraId
                            && u.PERFIL_perfil_id == perfilId
                          select p.correo).FirstOrDefault();

            return correo;
        }



        private void EnviarCorreo(string nombreActividad, string codigoActividad, string nombreobra, string loteInmueble, string inmueble, int cartillaid, DateTime? fechasupv, string destinatarioAutocontrol, string destinatarioSupervisor, string nombreSupervisor, string observacionesPublicas = null, string observacionesPrivadas = null)
        {
            string asunto = $"V°B° SUPV {codigoActividad} - {nombreActividad} {nombreobra}";
            string body = $"Estimad@,\n\nInformo a usted la aprobación de lote de inmueble {loteInmueble} del inmueble {inmueble} para la cartilla de actividad {codigoActividad} {nombreActividad}." +
                         $"\n\nCon fecha {fechasupv?.ToString("dd-MM-yyyy") ?? "N/A"} para obra {nombreobra}.";

            if (!string.IsNullOrEmpty(observacionesPublicas) || !string.IsNullOrEmpty(observacionesPrivadas))
            {
                body += $"\n\nObservaciones:";
                if (!string.IsNullOrEmpty(observacionesPublicas))
                {
                    body += $"\nPúblicas: {observacionesPublicas}";
                }
                if (!string.IsNullOrEmpty(observacionesPrivadas))
                {
                    body += $"\nPrivadas: {observacionesPrivadas}";
                }
            }

            body += $"\n\nSaludos cordiales,\n\n {nombreSupervisor}.";

            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential("cartillas.obra.manzano@gmail.com", "uraa qkpw rnyd asvb");
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress("cartillas.obra.manzano@gmail.com");
                        mailMessage.Subject = asunto;
                        mailMessage.Body = body;
                        mailMessage.To.Add(destinatarioAutocontrol);
                        mailMessage.CC.Add(destinatarioSupervisor);

                        // Agregar mensaje de consola para saber cuándo se está intentando enviar el correo
                        Console.WriteLine($"Intentando enviar correo a {destinatarioAutocontrol} y CC a {destinatarioSupervisor}...");

                        smtpClient.Send(mailMessage);

                        // Agregar mensaje de consola para confirmar que el correo se envió correctamente
                        Console.WriteLine($"Correo enviado exitosamente a {destinatarioAutocontrol} y CC a {destinatarioSupervisor}.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Capturar excepciones y mostrar mensaje de error en consola
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
            }
        }

      

        [HttpGet]
        public ActionResult EditarCartillaMovil()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtrar las obras a las que el usuario autenticado tiene acceso a través de ACCESO_OBRAS
                var obrasAcceso = db.ACCESO_OBRAS
                                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                    .Select(a => a.obra_id)
                                    .ToList();

                // Cargar datos para el dropdown de Obras
                var obraIdsConCartillas = db.CARTILLA
                                            .Select(c => c.OBRA_obra_id)
                                            .Distinct()
                                            .ToList();

                // Obtener las obras que el usuario tiene acceso y que están relacionadas con los IDs filtrados
                var obrasFiltradas = db.OBRA
                                        .Where(o => obrasAcceso.Contains(o.obra_id) && obraIdsConCartillas.Contains(o.obra_id))
                                        .ToList();

                // Crear el SelectList para el dropdown de Obras
                ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra", null);
                ViewBag.LOTE_id = new SelectList(db.LOTE_INMUEBLE, "lote_id", "abreviatura", null);
                ViewBag.INMUEBLE_id = new SelectList(db.INMUEBLE, "inmueble_id", "codigo_inmueble", null);


                // Cargar datos para el dropdown de Actividades
                var actividadIdsConCartillas = db.CARTILLA
                                    .Select(c => c.ACTIVIDAD_actividad_id)
                                    .Distinct()
                                    .ToList();

                var actividadFiltradas = db.ACTIVIDAD
                                            .Where(o => actividadIdsConCartillas.Contains(o.actividad_id))
                                            .ToList();

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(actividadFiltradas, "actividad_id", "nombre_actividad", null);
                ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "descripcion", null);

                // Obtener los registros de detalle de cartillas
                var detalleCartillas = db.DETALLE_CARTILLA.ToList();

                // Crear el modelo para la vista
                var model = new CartillaMovilViewModel
                {
                    DetalleCartillas = detalleCartillas
                };

                ViewBag.SupervisorNames = db.USUARIO.ToDictionary(
                    u => u.PERSONA_rut,
                    u => $"{u.PERSONA.nombre} {u.PERSONA.apeliido_paterno} {u.PERSONA.apellido_materno}"
                );

                var rutSPV = ObtenerRutUsuarioAutenticado();
                ViewBag.RutUsuarioAutenticado = rutSPV;

                actualizacionEditCartilla = true;

                return View(model);
            }
            else
            {
                // Manejar el caso en el que el usuario no esté autenticado
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarCartillaMovil(CartillaMovilViewModel model)
        {
            if (Session["UsuarioAutenticado"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                LoadViewData();
                return View(model);
            }

            var rutSPV = ObtenerRutUsuarioAutenticado();
            bool cambiosRealizados = false;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var cartilla = db.CARTILLA.Find(model.Cartilla.cartilla_id);
                    if (cartilla != null)
                    {
                        // Solo actualizar observaciones si han cambiado
                        if (cartilla.observaciones != model.Cartilla.observaciones ||
                            cartilla.observaciones_priv != model.Cartilla.observaciones_priv)
                        {
                            cartilla.observaciones = model.Cartilla.observaciones ?? string.Empty;
                            cartilla.observaciones_priv = model.Cartilla.observaciones_priv ?? string.Empty;
                            db.Entry(cartilla).State = EntityState.Modified;
                            cambiosRealizados = true;
                        }
                    }

                    // Obtener los detalles existentes en la base de datos
                    var existingDetalles = db.DETALLE_CARTILLA
                                            .Where(d => d.CARTILLA_cartilla_id == model.Cartilla.cartilla_id)
                                            .ToList();

                    // Recorrer los detalles del modelo
                    foreach (var detalleModel in model.DetalleCartillas)
                    {
                        var existingDetalle = existingDetalles.FirstOrDefault(d => d.detalle_cartilla_id == detalleModel.detalle_cartilla_id);
                        if (existingDetalle != null)
                        {
                            bool hasChanged = false;

                            // Verificar si estado_supv ha cambiado
                            if (existingDetalle.estado_supv != detalleModel.estado_supv)
                            {
                                if (existingDetalle.estado_supv != null)
                                {
                                    detalleModel.estado_supv = existingDetalle.estado_supv;
                                }
                                else
                                {
                                    existingDetalle.estado_supv = detalleModel.estado_supv;
                                    hasChanged = true;

                                    if ((detalleModel.estado_supv == true || detalleModel.estado_supv == false) &&
                                        existingDetalle.rut_spv == null && !string.IsNullOrEmpty(rutSPV))
                                    {
                                        existingDetalle.rut_spv = rutSPV;
                                    }

                                    // Verificar si estado_supv ha cambiado y fecha_supv aún no está establecida
                                    if (existingDetalle.fecha_supv == null)
                                    {
                                        existingDetalle.fecha_supv = DateTime.Now;
                                    }
                                }
                            }

                            if (hasChanged)
                            {
                                existingDetalle.fecha_rev = DateTime.Now;
                                db.Entry(existingDetalle).State = EntityState.Modified;
                                cambiosRealizados = true;
                            }
                        }
                    }

                    // Eliminar detalles que ya no están presentes
                    foreach (var detalle in existingDetalles)
                    {
                        if (!model.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                        {
                            if (detalle.estado_supv != null || detalle.rut_spv != null)
                            {
                                continue;
                            }
                            db.DETALLE_CARTILLA.Remove(detalle);
                            cambiosRealizados = true;
                        }
                    }

                    // Guardar cambios si hubo modificaciones
                    if (cambiosRealizados)
                    {
                        db.SaveChanges();
                        transaction.Commit();

                        // Verificar si enviar_correo es true antes de enviar correos
                        if (cartilla != null && cartilla.enviar_correo == true)
                        {
                            EnviarCorreosAutomatizados();
                        }
                    }

                    return RedirectToAction("EditarCartillaMovil", "VistaPerfilSupervisor");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Ocurrió un error al guardar los cambios: " + ex.Message);
                    LoadViewData();
                    return View(model);
                }
            }
        }


        // Método auxiliar para cargar datos para la vista
        private void LoadViewData()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Filtrar las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                                    .Select(a => a.obra_id)
                                    .ToList();

                var obraIdsConCartillas = db.CARTILLA
                                            .Select(c => c.OBRA_obra_id)
                                            .Distinct()
                                            .ToList();

                var obrasFiltradas = db.OBRA
                                        .Where(o => obrasAcceso.Contains(o.obra_id) && obraIdsConCartillas.Contains(o.obra_id))
                                        .ToList();

                ViewBag.OBRA_obra_id = new SelectList(obrasFiltradas, "obra_id", "nombre_obra", null);
                ViewBag.LOTE_id = new SelectList(db.LOTE_INMUEBLE, "lote_id", "abreviatura", null);
                ViewBag.INMUEBLE_id = new SelectList(db.INMUEBLE, "inmueble_id", "codigo_inmueble", null);

                var actividadIdsConCartillas = db.CARTILLA
                                        .Select(c => c.ACTIVIDAD_actividad_id)
                                        .Distinct()
                                        .ToList();

                var actividadFiltradas = db.ACTIVIDAD
                                            .Where(o => actividadIdsConCartillas.Contains(o.actividad_id))
                                            .ToList();

                ViewBag.ACTIVIDAD_actividad_id = new SelectList(actividadFiltradas, "actividad_id", "nombre_actividad", null);
                ViewBag.ESTADO_FINAL_estado_final_id = new SelectList(db.ESTADO_FINAL, "estado_final_id", "descripcion", null);

                ViewBag.SupervisorNames = db.USUARIO.ToDictionary(
                    u => u.PERSONA_rut,
                    u => $"{u.PERSONA.nombre} {u.PERSONA.apeliido_paterno} {u.PERSONA.apellido_materno}"
                );

                var rutSPV = ObtenerRutUsuarioAutenticado();
                ViewBag.RutUsuarioAutenticado = rutSPV;
            }
        }





        // Método para cargar la cartilla basándose en la actividad seleccionada
        // Método para obtener la cartilla según obra y actividad seleccionada
        public async Task<JsonResult> ObtenerCartillaPorObraYActividad(int obraId, int actividadId, int loteId, int inmuebleId)
        {
            var cartilla = await db.CARTILLA
                                   .Where(c => c.OBRA_obra_id == obraId && c.ACTIVIDAD_actividad_id == actividadId)
                                   .Select(c => new
                                   {
                                       Detalles = c.DETALLE_CARTILLA
                                                    .Where(d => d.INMUEBLE.LOTE_INMUEBLE_lote_id == loteId && d.INMUEBLE.inmueble_id == inmuebleId)
                                                    .Select(d => new
                                                    {
                                                        d.ITEM_VERIF.label,
                                                        d.ITEM_VERIF.elemento_verificacion,
                                                        d.estado_supv,
                                                        d.estado_autocontrol,
                                                        d.estado_ito,
                                                        d.fecha_fto,
                                                        d.fecha_supv,
                                                        d.fecha_rev,
                                                        d.fecha_autocontrol,
                                                        d.correo_enviado_supv,
                                                        d.correo_enviado_ac,
                                                        d.INMUEBLE_inmueble_id,
                                                        d.CARTILLA_cartilla_id,
                                                        d.ITEM_VERIF_item_verif_id,
                                                        d.revision_dos
                                                    }).ToList()
                                   }).FirstOrDefaultAsync();

            if (cartilla == null)
            {
                return Json(new { success = false, message = "No se encontró ninguna cartilla para esta combinación de obra y actividad." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = cartilla.Detalles }, JsonRequestBehavior.AllowGet);
        }



        public JsonResult ObtenerActividadesPorObra(int obraId)
        {
            // Obtener el RUT del usuario autenticado
            var rutUsuario = ObtenerRutUsuarioAutenticado();

            // Si el usuario no está autenticado, devolver un error o vacío
            if (string.IsNullOrEmpty(rutUsuario))
            {
                return Json(new { mensaje = "Usuario no autenticado" }, JsonRequestBehavior.AllowGet);
            }

            // Obtener las actividades de la obra que tienen cartilla y a las que el usuario tiene acceso
            var actividades = db.ACTIVIDAD
                                .Where(a => a.OBRA_obra_id == obraId &&
                                            db.CARTILLA.Any(c => c.ACTIVIDAD_actividad_id == a.actividad_id) &&
                                            db.ACCESO_CARTILLA.Any(ac => ac.CARTILLA.ACTIVIDAD_actividad_id == a.actividad_id && ac.USUARIO.PERSONA_rut == rutUsuario))
                                .Select(a => new
                                {
                                    actividadId = a.actividad_id,
                                    nombreActividad = a.nombre_actividad
                                }).ToList();

            return Json(actividades, JsonRequestBehavior.AllowGet);
        }




        public JsonResult ObtenerLotesPorObra(int obraId)
        {
            var cacheKey = $"lotes_obra_{obraId}";
            var lotes = MemoryCache.Default.Get(cacheKey) as List<LoteDto>;

            if (lotes == null)
            {
                lotes = db.LOTE_INMUEBLE
                          .Where(l => l.OBRA_obra_id == obraId)
                          .Select(l => new LoteDto
                          {
                              LoteId = l.lote_id,
                              NombreLote = l.abreviatura
                          })
                          .Distinct()
                          .ToList();
                MemoryCache.Default.Add(cacheKey, lotes, DateTimeOffset.Now.AddMinutes(30)); // Cachea por 30 minutos
            }

            return Json(lotes, JsonRequestBehavior.AllowGet);
        }


        public JsonResult ObtenerInmueblesPorLote(int loteId)
        {
            var inmuebles = db.INMUEBLE
                .Where(i => i.LOTE_INMUEBLE.lote_id == loteId) // Filtra los inmuebles por el lote
                .Select(i => new
                {
                    inmuebleId = i.inmueble_id,       // El identificador del inmueble
                    nombreInmueble = i.codigo_inmueble // La propiedad que quieres mostrar en el dropdown
                })
                .Distinct()
                .ToList();

            return Json(inmuebles, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCartillaDataByActividad(int actividadId)
        {
            // Asumiendo que tienes un contexto de Entity Framework llamado _context
            var cartilla = db.CARTILLA
                .Where(c => c.ACTIVIDAD_actividad_id == actividadId)
                .Select(c => new
                {
                    FechaCartilla = c.fecha,       // Campo de fecha de la cartilla
                    EstadoFinal = c.ESTADO_FINAL.descripcion    // Campo de estado final de la cartilla
                })
                .FirstOrDefault();

            if (cartilla == null)
            {
                return Json(new { success = false, message = "No se encontró una cartilla para la actividad seleccionada." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, fecha = cartilla.FechaCartilla, estado = cartilla.EstadoFinal }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerCartillaPorActividad(int actividadId)
        {
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.ACTIVIDAD_actividad_id == actividadId);

            if (cartilla != null)
            {
                return Json(new
                {
                    success = true,
                    cartillaId = cartilla.cartilla_id,
                    observaciones = cartilla.observaciones,
                    observaciones_priv = cartilla.observaciones_priv,
                    fecha = cartilla.fecha.ToString("dd/MM/yyyy"),
                    estado = cartilla.ESTADO_FINAL.descripcion,
                    fecha_modificacion = cartilla.fecha_modificacion // Asegúrate de incluir esto
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "No se encontró una cartilla para la actividad seleccionada." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerEstadoRevisionDos(int cartillaId)
        {
            try
            {
                // Obtener el estado de revisión_dos para la cartilla especificada
                bool revisionDos = db.DETALLE_CARTILLA
                    .Any(d => d.CARTILLA_cartilla_id == cartillaId && d.revision_dos);

                return Json(new { revisionDos = revisionDos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


    }
}
