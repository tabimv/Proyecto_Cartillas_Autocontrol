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
using System.Data.Entity.Infrastructure;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class PersonaController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: Persona
        public async Task<ActionResult> Index()
        {
            return View(await db.PERSONA.ToListAsync());
        }

        // GET: Persona/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PERSONA pERSONA = await db.PERSONA.FindAsync(id);
            if (pERSONA == null)
            {
                return HttpNotFound();
            }
            return View(pERSONA);
        }

        // GET: Persona/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Persona/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "rut,nombre,apeliido_paterno,apellido_materno,correo")] PERSONA pERSONA)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Transformar los campos para que la primera letra sea mayúscula
                    pERSONA.nombre = CapitalizeFirstLetter(pERSONA.nombre);
                    pERSONA.apeliido_paterno = CapitalizeFirstLetter(pERSONA.apeliido_paterno);
                    pERSONA.apellido_materno = CapitalizeFirstLetter(pERSONA.apellido_materno);
             

                    // Verificar si el RUT ya existe en la base de datos
                    if (db.PERSONA.Any(p => p.rut == pERSONA.rut))
                    {
                        ModelState.AddModelError("rut", "Este rut ya existe."); // Agregar error al modelo de validación
                    }
                    // Verificar si el correo ya existe en la base de datos
                    else if (db.PERSONA.Any(p => p.correo == pERSONA.correo))
                    {
                        ModelState.AddModelError("correo", "Este correo ya está asignado a otra persona"); // Agregar error al modelo de validación
                    }
                    else
                    {
                        db.PERSONA.Add(pERSONA);
                        await db.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                }
                catch (DbUpdateException)
                {
                    // Manejar excepción de clave primaria duplicada aquí si es necesario
                    ModelState.AddModelError("", "No se pudo crear la persona. Intente nuevamente.");
                }
            }

            return View(pERSONA);
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            input = input.Trim();
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        // GET: Persona/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PERSONA pERSONA = await db.PERSONA.FindAsync(id);
            if (pERSONA == null)
            {
                return HttpNotFound();
            }
            return View(pERSONA);
        }


        // POST: Persona/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "rut,nombre,apeliido_paterno,apellido_materno,correo")] PERSONA persona)
        {
            if (ModelState.IsValid)
            {
                // Transformar los campos para que la primera letra sea mayúscula
                persona.nombre = CapitalizeFirstLetter(persona.nombre);
                persona.apeliido_paterno = CapitalizeFirstLetter(persona.apeliido_paterno);
                persona.apellido_materno = CapitalizeFirstLetter(persona.apellido_materno);

                db.Entry(persona).State = EntityState.Modified; // Indica que la entidad ha sido modificada

                try
                {
                    db.SaveChanges(); // Guarda los cambios en la base de datos
                    return RedirectToAction("Index"); // Redirige a la vista de lista
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Maneja excepciones de concurrencia aquí si es necesario
                    ModelState.AddModelError("", "No se pudo guardar los cambios. Intente nuevamente.");
                }
            }
            return View(persona);
        }

        // GET: Persona/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PERSONA pERSONA = await db.PERSONA.FindAsync(id);
            if (pERSONA == null)
            {
                return HttpNotFound();
            }
            return View(pERSONA);
        }

        // POST: Persona/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            PERSONA pERSONA = await db.PERSONA.FindAsync(id);

            if (pERSONA == null)
            {
                return HttpNotFound();
            }

            bool UserRelacionadas = db.USUARIO.Any(u => u.PERSONA_rut == id);
            bool ResponsableRelacionadas = db.RESPONSABLE.Any(u => u.PERSONA_rut == id);

            if (UserRelacionadas && ResponsableRelacionadas)
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Persona porque está relacionado con un Usuario y Responsable de Obra.";
                return View("Delete", pERSONA); // Mostrar vista de eliminación con el mensaje de error
            }
            else if (UserRelacionadas)
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Persona porque está relacionado con un Usuario.";
                return View("Delete", pERSONA); // Mostrar vista de eliminación con el mensaje de error
            }
            else if (ResponsableRelacionadas)
            {
                ViewBag.ErrorMessage = "No se puede eliminar esta Persona porque está relacionado con un Responsable de Obra.";
                return View("Delete", pERSONA); // Mostrar vista de eliminación con el mensaje de error
            }
            else
            {
                db.PERSONA.Remove(pERSONA);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
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
