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
        private ObraManzanoConexion db = new ObraManzanoConexion();

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
            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol");
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre");
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
                db.USUARIO.Add(uSUARIO);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", uSUARIO.OBRA_obra_id);
            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol", uSUARIO.PERFIL_perfil_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", uSUARIO.PERSONA_rut);
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
            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol", uSUARIO.PERFIL_perfil_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", uSUARIO.PERSONA_rut);
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
            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol", uSUARIO.PERFIL_perfil_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", uSUARIO.PERSONA_rut);
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

            // Verificar si existen relaciones con claves foráneas
            if (db.USUARIO.Any(t => t.usuario_id == id))
            {
                ViewBag.ErrorMessage = "No se puede eliminar este Usuario debido a  que esta relacionado a otras Entidades.";
                return View("Delete", uSUARIO); // Mostrar vista de eliminación con el mensaje de error
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
