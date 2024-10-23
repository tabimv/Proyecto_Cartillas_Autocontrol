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
            var uSUARIO = db.USUARIO.Include(u => u.PERFIL).Include(u => u.PERSONA);
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
            // Filtra las obras excluyendo aquella cuyo nombre sea "Oficina Central"
            var obrasExcluida = db.OBRA.ToList();

            // Crea el SelectList con las obras filtradas
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");

          

            var perfiles = db.PERFIL.ToList();

            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol");

            var asociados = db.USUARIO.Select(r => r.PERSONA_rut).ToList();
            var personasDisponibles = db.PERSONA
                .Where(p => !asociados.Contains(p.rut))
                .Select(p => new
                {
                    rut = p.rut,
                    nombreCompleto = p.nombre + " " + p.apeliido_paterno + " " + p.apellido_materno
                })
                .ToList();

            ViewBag.PERSONA_rut = new SelectList(personasDisponibles, "rut", "nombreCompleto");
            ViewBag.Obras = obrasExcluida;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "usuario_id,contraseña,PERFIL_perfil_id,PERSONA_rut")] USUARIO uSUARIO, int[] obraIds)
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
                    uSUARIO.estado_usuario = true;
                    db.USUARIO.Add(uSUARIO);
                    await db.SaveChangesAsync();

                    // Agrega las obras seleccionadas en la tabla ACCESO_OBRAS
                    if (obraIds != null)
                    {
                        foreach (var obraId in obraIds)
                        {
                            db.ACCESO_OBRAS.Add(new ACCESO_OBRAS { usuario_id = uSUARIO.usuario_id, obra_id = obraId });
                        }
                        await db.SaveChangesAsync();
                    }

                    return RedirectToAction("Index");
                }
            }


            var perfiles = db.PERFIL.ToList();

            ViewBag.PERFIL_perfil_id = new SelectList(db.PERFIL, "perfil_id", "rol");

            var asociados = db.USUARIO.Select(r => r.PERSONA_rut).ToList();
            var personasDisponibles = db.PERSONA
                .Where(p => !asociados.Contains(p.rut))
                .Select(p => new
                {
                    rut = p.rut,
                    nombreCompleto = p.nombre + " " + p.apeliido_paterno + " " + p.apellido_materno
                })
                .ToList();

            ViewBag.PERSONA_rut = new SelectList(personasDisponibles, "rut", "nombreCompleto", uSUARIO.PERSONA_rut);

            return View(uSUARIO);
        }


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

            var perfiles = db.PERFIL.ToList();
            var selectedPerfilId = uSUARIO.PERFIL_perfil_id;
            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol", selectedPerfilId);

            var personas = db.PERSONA.ToList();
            var listaPersonas = personas.Select(p => new
            {
                PersonaRut = p.rut,
                rutYNombre = $"{p.rut} - {p.nombre} {p.apeliido_paterno} {p.apellido_materno}"
            }).ToList();
            ViewBag.PERSONA_rut = new SelectList(listaPersonas, "PersonaRut", "rutYNombre", uSUARIO.PERSONA_rut);

            // Obtener las obras disponibles para mostrar en la vista
            ViewBag.Obras = db.OBRA.ToList();

            // Obtener las obras a las que el usuario ya tiene acceso
            var accesosObras = db.ACCESO_OBRAS.Where(a => a.usuario_id == id).Select(a => a.obra_id).ToList();
            ViewBag.ObrasSeleccionadas = accesosObras;

            return View(uSUARIO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "usuario_id,contraseña,PERFIL_perfil_id,PERSONA_rut")] USUARIO uSUARIO, int[] obraIds)
        {
            if (ModelState.IsValid)
            {
                // Obtener el estado actual del usuario antes de modificarlo
                var usuarioOriginal = await db.USUARIO.AsNoTracking().FirstOrDefaultAsync(u => u.usuario_id == uSUARIO.usuario_id);
                if (usuarioOriginal == null)
                {
                    return HttpNotFound();
                }

                // Mantener el estado_usuario original
                uSUARIO.estado_usuario = usuarioOriginal.estado_usuario;

                // Actualizar la información del usuario
                db.Entry(uSUARIO).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Obtener las obras a las que el usuario tenía acceso antes de la actualización
                var accesosExistentes = db.ACCESO_OBRAS.Where(a => a.usuario_id == uSUARIO.usuario_id).ToList();

                // Obtener los IDs de las obras a las que el usuario tenía acceso previamente
                var obraIdsExistentes = accesosExistentes.Select(a => a.obra_id).ToList();

                // Convertir los obraIds en una lista de int, manejando posibles valores nulos
                var obraIdsNueva = obraIds?.ToList() ?? new List<int>();

                // Convertir los IDs existentes a una lista de int para comparación
                var obraIdsExistentesInt = obraIdsExistentes.Where(id => id.HasValue).Select(id => id.Value).ToList();

                // Determinar las obras a las que se les ha quitado el permiso
                var obrasQuitadas = obraIdsExistentesInt.Except(obraIdsNueva).ToList();

                // Determinar los accesos que se deben eliminar
                var accesosParaEliminar = accesosExistentes
                    .Where(a => a.obra_id.HasValue && !obraIdsNueva.Contains(a.obra_id.Value))
                    .ToList();

                // Eliminar registros existentes para las obras quitadas
                db.ACCESO_OBRAS.RemoveRange(accesosParaEliminar);

                // Agregar los accesos seleccionados solo si hay obras seleccionadas
                if (obraIdsNueva.Any())
                {
                    foreach (var obraId in obraIdsNueva)
                    {
                        if (!db.ACCESO_OBRAS.Any(a => a.usuario_id == uSUARIO.usuario_id && a.obra_id == obraId))
                        {
                            db.ACCESO_OBRAS.Add(new ACCESO_OBRAS { usuario_id = uSUARIO.usuario_id, obra_id = obraId });
                        }
                    }
                }
                else
                {
                    // Si no se seleccionó ninguna obra, eliminar todos los accesos en ACCESO_OBRAS
                    db.ACCESO_OBRAS.RemoveRange(accesosExistentes);
                }

                // Guardar cambios en ACCESO_OBRAS
                await db.SaveChangesAsync();

                // Eliminar los registros de ACCESO_CARTILLA solo para las cartillas asociadas a las obras de las que se ha quitado el permiso
                foreach (var obraId in obrasQuitadas)
                {
                    var cartillasParaEliminar = db.CARTILLA.Where(c => c.OBRA_obra_id == obraId).Select(c => c.cartilla_id).ToList();
                    var accesosCartillaExistentes = db.ACCESO_CARTILLA
                        .Where(ac => ac.USUARIO_usuario_id == uSUARIO.usuario_id && cartillasParaEliminar.Contains(ac.CARTILLA_cartilla_id)).ToList();

                    // Eliminar solo los accesos de cartillas asociadas a las obras quitadas
                    db.ACCESO_CARTILLA.RemoveRange(accesosCartillaExistentes);
                }

                // Guardar cambios finales en ACCESO_CARTILLA
                await db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            // Cargar datos para la vista en caso de que ModelState no sea válido
            var perfiles = db.PERFIL.ToList();
            ViewBag.PERFIL_perfil_id = new SelectList(perfiles, "perfil_id", "rol", uSUARIO.PERFIL_perfil_id);

            var personas = db.PERSONA.ToList();
            var listaPersonas = personas.Select(p => new
            {
                PersonaRut = p.rut,
                rutYNombre = $"{p.rut} - {p.nombre} {p.apeliido_paterno} {p.apellido_materno}"
            }).ToList();
            ViewBag.PERSONA_rut = new SelectList(listaPersonas, "PersonaRut", "rutYNombre", uSUARIO.PERSONA_rut);

            var accesosObras = db.ACCESO_OBRAS.Where(a => a.usuario_id == uSUARIO.usuario_id).Select(a => a.obra_id).ToList();
            ViewBag.ObrasSeleccionadas = accesosObras;

            return View(uSUARIO);
        }








        [HttpPost]
        public JsonResult UpdateEstadoUsuario(int usuarioId, int estado_usuario)  // Cambié bool a int
        {
            try
            {
                // Buscar el usuario por ID
                var usuario = db.USUARIO.Find(usuarioId);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                // Actualizar el estado del usuario
                usuario.estado_usuario = estado_usuario == 1;  // Convierte a bool si es 1
                db.SaveChanges();

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar el estado: " + ex.Message });
            }
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

            // Obtener las obras asociadas al usuario
            var obrasSeleccionadas = db.ACCESO_OBRAS
                .Where(a => a.usuario_id == id)
                .Select(a => a.OBRA)
                .ToList();

            // Pasar el usuario y la lista de obras a la vista
            ViewBag.ObrasSeleccionadas = obrasSeleccionadas;

            return View(uSUARIO);
        }

        // POST: Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            // Buscar el usuario a eliminar
            USUARIO uSUARIO = await db.USUARIO.FindAsync(id);

            if (uSUARIO == null)
            {
                return HttpNotFound();
            }

            // Eliminar los registros asociados en ACCESO_OBRAS
            var accesosObras = db.ACCESO_OBRAS.Where(a => a.usuario_id == id).ToList();
            if (accesosObras.Any())
            {
                db.ACCESO_OBRAS.RemoveRange(accesosObras);
            }

            // Eliminar los registros asociados en ACCESO_CARTILLA
            var accesosCartilla = db.ACCESO_CARTILLA.Where(a => a.USUARIO_usuario_id == id).ToList();
            if (accesosCartilla.Any())
            {
                db.ACCESO_CARTILLA.RemoveRange(accesosCartilla);
            }

            // Eliminar el usuario
            db.USUARIO.Remove(uSUARIO);

            // Guardar cambios en la base de datos
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
