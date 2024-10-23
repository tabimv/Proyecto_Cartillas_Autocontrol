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


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class ResponsableController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: Responsable
        public async Task<ActionResult> Index()
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

                var rESPONSABLE = db.RESPONSABLE.Include(r => r.PERSONA)
                    .Include(r => r.OBRA)
                    .Where(r => obrasAcceso.Contains(r.OBRA_obra_id));
                return View(await rESPONSABLE.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult ExportToExcel()
        {
            var responsables = db.RESPONSABLE.Include(r => r.OBRA).Include(r => r.PERSONA).ToList();

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

                var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);

                var fileName = "Responsables.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        // GET: Responsable/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            if (rESPONSABLE == null)
            {
                return HttpNotFound();
            }
            return View(rESPONSABLE);
        }

        [HttpGet]
        public JsonResult GetPersonasDisponibles(int obraId)
        {
            var asociados = db.RESPONSABLE
                .Where(r => r.OBRA_obra_id == obraId)
                .Select(r => r.PERSONA_rut)
                .ToList();

            var personasDisponibles = db.PERSONA
                .Where(p => !asociados.Contains(p.rut))
                .Select(p => new
                {
                    rut = p.rut,
                    nombreCompleto = p.nombre + " " + p.apeliido_paterno + " " + p.apellido_materno
                })
                .ToList();

            return Json(personasDisponibles, JsonRequestBehavior.AllowGet);
        }



        public ActionResult GetCargosDisponibles(int obraId)
        {
            // Suponiendo que tienes una forma de obtener los responsables asignados para una obra
            var responsables = db.RESPONSABLE.Where(r => r.OBRA_obra_id == obraId).ToList();

            var cargosAsignados = responsables.Select(r => r.cargo).ToList();

            var cargosDisponibles = new List<SelectListItem>
    {
        new SelectListItem { Text = "Administrador de Obra", Value = "Administrador de Obra" },
        new SelectListItem { Text = "Autocontrol", Value = "Autocontrol" },
        new SelectListItem { Text = "F.T.O 1", Value = "F.T.O 1" },
        new SelectListItem { Text = "F.T.O 2", Value = "F.T.O 2" },
        new SelectListItem { Text = "Supervisor Serviu", Value = "Supervisor Serviu" },
    }.Where(c => !cargosAsignados.Contains(c.Value)).ToList();

            return Json(cargosDisponibles, JsonRequestBehavior.AllowGet);
        }

        // GET: Responsable/Create
        public ActionResult Create()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso =  db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                var obras = db.OBRA.Where(a => obrasAcceso.Contains(a.obra_id)).ToList();
                ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra");

                // Obtener todas las personas
                var personasDisponibles = db.PERSONA
                    .Select(p => new
                    {
                        rut = p.rut,
                        nombreCompleto = p.nombre + " " + p.apeliido_paterno + " " + p.apellido_materno
                    })
                    .ToList();

                ViewBag.PERSONA_rut = new SelectList(personasDisponibles, "rut", "nombreCompleto");

                return View();
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }

        // POST: Responsable/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "responsable_id,cargo,OBRA_obra_id,PERSONA_rut")] RESPONSABLE rESPONSABLE)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                // Verificar si ya existe un registro con las mismas claves foráneas
                if (db.RESPONSABLE.Any(r => r.OBRA_obra_id == rESPONSABLE.OBRA_obra_id && r.PERSONA_rut == rESPONSABLE.PERSONA_rut))
                {
                    ModelState.AddModelError("PERSONA_rut", "Ya existe un responsable con este rut para la obra seleccionada.");
                }
                // Verificar si ya existe un registro con las mismas claves foráneas
                if (db.RESPONSABLE.Any(r => r.OBRA_obra_id == rESPONSABLE.OBRA_obra_id && r.cargo == rESPONSABLE.cargo))
                {
                    ModelState.AddModelError("cargo", "Ya existe un responsable con este cargo para la obra seleccionada.");
                }

                if (ModelState.IsValid)
                {
                    db.RESPONSABLE.Add(rESPONSABLE);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                var obras = db.OBRA.Where(a => obrasAcceso.Contains(a.obra_id)).ToList();
                ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", rESPONSABLE.OBRA_obra_id);

                var personasDisponibles = db.PERSONA
                    .Select(p => new
                    {
                        rut = p.rut,
                        nombreCompleto = p.nombre + " " + p.apeliido_paterno + " " + p.apellido_materno
                    })
                    .ToList();

                ViewBag.PERSONA_rut = new SelectList(personasDisponibles, "rut", "nombreCompleto", rESPONSABLE.PERSONA_rut);

                return View(rESPONSABLE);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public async Task<ActionResult> Edit(int? id)
        {
            if (Session["UsuarioAutenticado"] != null)
            {

                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                // Usa Include para cargar la propiedad PERSONA
                var rESPONSABLE = await db.RESPONSABLE
                    .Include(r => r.PERSONA) // Asegúrate de incluir la propiedad PERSONA
                    .FirstOrDefaultAsync(r => r.responsable_id == id);

                if (rESPONSABLE == null)
                {
                    return HttpNotFound();
                }

                // Obtén la lista de personas
                var personList = db.PERSONA.Select(p => new { rut = p.rut, nombreCompleto = p.nombre + " " + p.apeliido_paterno });
                ViewBag.PERSONA_rut = new SelectList(personList, "rut", "nombreCompleto", rESPONSABLE.PERSONA_rut);

                var obras = db.OBRA.Where(a => obrasAcceso.Contains(a.obra_id)).ToList();
                ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", rESPONSABLE.OBRA_obra_id);

                return View(rESPONSABLE);
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "responsable_id,cargo,OBRA_obra_id,PERSONA_rut")] RESPONSABLE rESPONSABLE)
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                // Obtén las IDs de las obras a las que el usuario autenticado tiene acceso
                var obrasAcceso = db.ACCESO_OBRAS
                    .Where(a => a.usuario_id == usuarioAutenticado.usuario_id)
                    .Select(a => a.obra_id)
                    .ToList();

                if (ModelState.IsValid)
                {
                    // Validación para verificar si el cargo ya está registrado para la obra seleccionada
                    var existingResponsable = await db.RESPONSABLE
                        .FirstOrDefaultAsync(r => r.OBRA_obra_id == rESPONSABLE.OBRA_obra_id && r.cargo == rESPONSABLE.cargo && r.responsable_id != rESPONSABLE.responsable_id);

                    if (existingResponsable != null)
                    {
                        // Agregar un error de validación al modelo si el cargo ya está registrado
                        ModelState.AddModelError("cargo", "El cargo ya está registrado para la obra seleccionada.");
                    }

                    if (ModelState.IsValid)
                    {
                        db.Entry(rESPONSABLE).State = EntityState.Modified;
                        await db.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                }

                var obras = db.OBRA.Where(a => obrasAcceso.Contains(a.obra_id)).ToList();
                ViewBag.OBRA_obra_id = new SelectList(obras, "obra_id", "nombre_obra", rESPONSABLE.OBRA_obra_id);

                var personList = db.PERSONA.Select(p => new { rut = p.rut, nombreCompleto = p.nombre + " " + p.apeliido_paterno });
                ViewBag.PERSONA_rut = new SelectList(personList, "rut", "nombreCompleto", rESPONSABLE.PERSONA_rut);

                return View(rESPONSABLE);

            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        // GET: Responsable/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            if (rESPONSABLE == null)
            {
                return HttpNotFound();
            }
            return View(rESPONSABLE);
        }

        // POST: Responsable/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            RESPONSABLE rESPONSABLE = await db.RESPONSABLE.FindAsync(id);
            db.RESPONSABLE.Remove(rESPONSABLE);
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
