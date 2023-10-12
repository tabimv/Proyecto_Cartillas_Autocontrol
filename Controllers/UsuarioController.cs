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
using Microsoft.AspNet.Identity;
using System.Text.RegularExpressions;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    
    public class UsuarioController : Controller
    {
        private TestConexion db = new TestConexion();

        // GET: USUARIOs

       
        public async Task<ActionResult> Index()
        {
            var uSUARIO = db.USUARIO.Include(u => u.OBRA).Include(u => u.PERFIL).Include(u => u.PERSONA);
            return View(await uSUARIO.ToListAsync());
        }

        // GET: USUARIOs/Details/5
  
        public async Task<ActionResult> Details(long? id)
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


       
        // GET: USUARIOs/Create


        public ActionResult Create()
        {
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");
            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol");
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre");
            return View();
        }

        // POST: USUARIOs/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "usuario_id,correo,contraseña,PERFIL_perfil_id,OBRA_obra_id,PERSONA_rut")] USUARIO uSUARIO)
        {
            if (ModelState.IsValid)
            {
                // Validación de dominio de correo permitido
                string[] dominiosPermitidos = { "gmail.com", "hotmail.com", "paumar.cl" };
                string[] partesCorreo = uSUARIO.correo.Split('@');

                if (partesCorreo.Length != 2 || !dominiosPermitidos.Contains(partesCorreo[1]))
                {
                    ModelState.AddModelError("correo", "El dominio del correo no está permitido.");
                }
                else
                {
                    // Verificar si ya existe un usuario con la misma combinación de OBRA_obra_id y PERSONA_rut
                    bool obraRutDuplicate = db.USUARIO.Any(u => u.OBRA_obra_id == uSUARIO.OBRA_obra_id && u.PERSONA_rut == uSUARIO.PERSONA_rut);

                    // Verificar si ya existe un usuario con el mismo PERSONA_rut independientemente de OBRA_obra_id
                    bool rutDuplicate = db.USUARIO.Any(u => u.PERSONA_rut == uSUARIO.PERSONA_rut);

                    if (obraRutDuplicate)
                    {
                        ModelState.AddModelError("", "Ya existe un usuario con la misma combinación de Obra y Nombre de Usuario.");
                    }
                    else if (rutDuplicate)
                    {
                        ModelState.AddModelError("", "Ya existe un usuario con el mismo rut.");
                    }
                    else
                    {
                        db.USUARIO.Add(uSUARIO);
                        await db.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                }
            }

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", uSUARIO.OBRA_obra_id);
            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol", uSUARIO.PERFIL_perfil_id);
            ViewBag.PERSONA_rut = new SelectList(db.PERSONA, "rut", "nombre", uSUARIO.PERSONA_rut);
            return View(uSUARIO);
        }

        // GET: USUARIOs/Edit/5
        public async Task<ActionResult> Edit(long? id)
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

        // POST: USUARIOs/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "usuario_id,correo,contraseña,PERFIL_perfil_id,OBRA_obra_id,PERSONA_rut")] USUARIO uSUARIO)
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

        // GET: USUARIOs/Delete/5
     
        public async Task<ActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            USUARIO uSUARIO = db.USUARIO.Find(id);
            if (uSUARIO == null)
            {
                return HttpNotFound();
            }
            return View(uSUARIO);
        }

        // POST: USUARIOs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
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
