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

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class UsuarioController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: Usuario
        public async Task<ActionResult> Index()
        {
            var uSUARIO = db.USUARIO.Include(u => u.OBRA).Include(u => u.PERFIL).Include(u => u.PERSONA);
            return View(await uSUARIO.ToListAsync());
        }

        // GET: Usuario/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            USUARIO uSUARIO = await db.USUARIO.FindAsync(id);
            if (uSUARIO == null)
            {
                return HttpNotFound();
            }
            return View(uSUARIO);
        }

        // GET: Usuario/Create
        public ActionResult Create()
        {
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");

            var perfiles = db.PERFIL.ToList();

            // Modificar las opciones de los perfiles según tu lógica
            foreach (var perfil in perfiles)
            {
                if (perfil.rol == "Administrador")
                {
                    perfil.rol = "Administrador";
                }
                else if (perfil.rol == "Consulta")
                {
                    perfil.rol = "Consulta";
                }
                else if (perfil.rol == "OTEC")
                {
                    perfil.rol = "Autocontrol";
                }
                else if (perfil.rol == "ITO")
                {
                    perfil.rol = "F.T.O";
                }
            }

            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol");

            var personas = db.PERSONA.ToList(); // Obtén la lista de actividades desde tu base de datos

            // Crear una lista de objetos anónimos con los campos que necesitas
            var listaPersonas = personas.Select(p => new
            {
                PersonaRut = p.rut,
                rutYNombre = $"{p.rut} - {p.nombre} {p.apeliido_paterno}"
            }).ToList();

            ViewBag.PERSONA_rut = new SelectList(listaPersonas, "PersonaRut", "rutYNombre");
            return View();
        }

        // POST: Usuario/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "usuario_id,contraseña,PERFIL_perfil_id,OBRA_obra_id,PERSONA_rut")] USUARIO uSUARIO)
        {
            if (ModelState.IsValid)
            {
                // Check if the combination of foreign keys already exists
                if (db.USUARIO.Any(u => u.PERSONA_rut == uSUARIO.PERSONA_rut))
                {
                    ModelState.AddModelError("PERSONA_rut", "Esta persona ya está asociada a un usuario.");
                }
                else
                {
                    db.USUARIO.Add(uSUARIO);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", uSUARIO.OBRA_obra_id);
            var perfiles = db.PERFIL.ToList();

            // Modificar las opciones de los perfiles según tu lógica
            foreach (var perfil in perfiles)
            {
                if (perfil.rol == "Administrador")
                {
                    perfil.rol = "Administrador";
                }
                else if (perfil.rol == "Consulta")
                {
                    perfil.rol = "Consulta";
                }
                else if (perfil.rol == "OTEC")
                {
                    perfil.rol = "Autocontrol";
                }
                else if (perfil.rol == "ITO")
                {
                    perfil.rol = "F.T.O";
                }
            }

            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol");


            var personas = db.PERSONA.ToList(); // Obtén la lista de actividades desde tu base de datos

            // Crear una lista de objetos anónimos con los campos que necesitas
            var listaPersonas = personas.Select(p => new
            {
                PersonaRut = p.rut,
                rutYNombre = $"{p.rut} - {p.nombre} {p.apeliido_paterno}"
            }).ToList();

            ViewBag.PERSONA_rut = new SelectList(listaPersonas, "PersonaRut", "rutYNombre", uSUARIO.PERSONA_rut);

            return View(uSUARIO);
        }


       


        // GET: Usuario/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            USUARIO uSUARIO = await db.USUARIO.FindAsync(id);
            if (uSUARIO == null)
            {
                return HttpNotFound();
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", uSUARIO.OBRA_obra_id);
            var perfiles = db.PERFIL.ToList();

            // Modificar las opciones de los perfiles según tu lógica
            foreach (var perfil in perfiles)
            {
                if (perfil.rol == "Administrador")
                {
                    perfil.rol = "Administrador";
                }
                else if (perfil.rol == "Consulta")
                {
                    perfil.rol = "Consulta";
                }
                else if (perfil.rol == "OTEC")
                {
                    perfil.rol = "Autocontrol";
                }
                else if (perfil.rol == "ITO")
                {
                    perfil.rol = "F.T.O";
                }
            }

            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol");

            var personas = db.PERSONA.ToList(); // Obtén la lista de actividades desde tu base de datos

            // Crear una lista de objetos anónimos con los campos que necesitas
            var listaPersonas = personas.Select(p => new
            {
                PersonaRut = p.rut,
                rutYNombre = $"{p.rut} - {p.nombre} {p.apeliido_paterno}"
            }).ToList();

            ViewBag.PERSONA_rut = new SelectList(listaPersonas, "PersonaRut", "rutYNombre", uSUARIO.PERSONA_rut);

            return View(uSUARIO);
        }

        // POST: Usuario/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "usuario_id,contraseña,PERFIL_perfil_id,OBRA_obra_id,PERSONA_rut")] USUARIO uSUARIO)
        {
            if (ModelState.IsValid)
            {
                db.Entry(uSUARIO).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", uSUARIO.OBRA_obra_id);
            var perfiles = db.PERFIL.ToList();

            // Modificar las opciones de los perfiles según tu lógica
            foreach (var perfil in perfiles)
            {
                if (perfil.rol == "Administrador")
                {
                    perfil.rol = "Administrador";
                }
                else if (perfil.rol == "Consulta")
                {
                    perfil.rol = "Consulta";
                }
                else if (perfil.rol == "OTEC")
                {
                    perfil.rol = "Autocontrol";
                }
                else if (perfil.rol == "ITO")
                {
                    perfil.rol = "F.T.O";
                }
            }

            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol");
            var personas = db.PERSONA.ToList(); // Obtén la lista de actividades desde tu base de datos

            // Crear una lista de objetos anónimos con los campos que necesitas
            var listaPersonas = personas.Select(p => new
            {
                PersonaRut = p.rut,
                rutYNombre = $"{p.rut} - {p.nombre} {p.apeliido_paterno}"
            }).ToList();

            ViewBag.PERSONA_rut = new SelectList(listaPersonas, "PersonaRut", "rutYNombre", uSUARIO.PERSONA_rut);

            return View(uSUARIO);
        }

        // GET: Usuario/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            USUARIO uSUARIO = await db.USUARIO.FindAsync(id);
            if (uSUARIO == null)
            {
                return HttpNotFound();
            }
            return View(uSUARIO);
        }

        // POST: Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            USUARIO uSUARIO = await db.USUARIO.FindAsync(id);

            if (uSUARIO == null)
            {
                return HttpNotFound();
            }

            db.USUARIO.Remove(uSUARIO);
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
