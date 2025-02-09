﻿using System;
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
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class InmuebleController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();

        // GET: Inmueble
        public async Task<ActionResult> Index()
        {
            var iNMUEBLE = db.INMUEBLE.Include(i => i.LOTE_INMUEBLE);
            return View(await iNMUEBLE.ToListAsync());
        }

        public ActionResult ExportToExcel()
        {
            var inmuebles = db.INMUEBLE.Include(o => o.LOTE_INMUEBLE).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Inmuebles");

                worksheet.Cell(1, 1).Value = "Código Inmueble";
                worksheet.Cell(1, 2).Value = "Tipo de Inmueble";
                worksheet.Cell(1, 3).Value = "Obra Asociada";

                int row = 2;
                foreach (var inmueble in inmuebles)
                {
                    worksheet.Cell(row, 1).Value = inmueble.codigo_inmueble;
                    worksheet.Cell(row, 2).Value = inmueble.tipo_inmueble;
                    worksheet.Cell(row, 3).Value = inmueble.LOTE_INMUEBLE.OBRA.nombre_obra;
                    row++;
                }

                var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);

                var fileName = "Inmuebles.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        public async Task<ActionResult> InmuebleLista()
        {
            var inmuebleGroupedByObra = await db.INMUEBLE
                .OrderBy(e => e.inmueble_id)
                .GroupBy(e => e.LOTE_INMUEBLE.lote_id)
                .Select(g => g.FirstOrDefault())
                .ToListAsync();


            return View(inmuebleGroupedByObra);
        }


        public async Task<ActionResult> InmuebleDetails(int loteId)
        {
            var loteSeleccionado = await db.LOTE_INMUEBLE.FindAsync(loteId);
            if (loteSeleccionado == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }

            var items = await db.INMUEBLE
                .Include(a => a.LOTE_INMUEBLE)
                .Where(a => a.LOTE_INMUEBLE.lote_id == loteId)
                .ToListAsync();

            items = items.OrderBy(a => int.Parse(Regex.Match(a.codigo_inmueble, @"\d+").Value)).ToList();


            ViewBag.LoteSeleccionado = loteSeleccionado;

            return View(items);

            
        }

        public async Task<ActionResult> InmuebleDetailss(int obraId)
        {
            var obraSeleccionado = await db.OBRA.FindAsync(obraId);
            if (obraSeleccionado == null)
            {
                return HttpNotFound(); // O maneja la situación de evento no encontrado de la forma que prefieras
            }

            var items = await db.INMUEBLE
                .Where(a => a.LOTE_INMUEBLE.OBRA_obra_id == obraId)
                .ToListAsync();

            items = items.OrderBy(a => int.Parse(Regex.Match(a.codigo_inmueble, @"\d+").Value)).ToList();


            ViewBag.ObraSeleccionado = obraSeleccionado;

            return View(items);


        }

        private int TryParseInt(string input)
        {
            int result;
            int.TryParse(Regex.Match(input, @"\d+").Value, out result);
            return result;
        }


        public ActionResult Crear()
        {
            // Obtén la lista completa de obras desde la base de datos
            var todasLasObras = db.OBRA.ToList();

            // Filtra las obras que no tienen el nombre "Oficina Central"
            var obrasFiltradas = todasLasObras.Where(o => o.nombre_obra != "Oficina Central").ToList();

            // Asigna la lista filtrada a ViewBag.Obras
            ViewBag.Obras = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarRegistros(FormCollection form)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int obraId = int.Parse(form["ObraId"]);
                   


                    // Obtener la cantidad de registros que se están guardando
                    int registrosAGuardar = (form.Count - 1) / 2;  

                    for (int i = 0; i < registrosAGuardar; i++)
                    {
                        var nuevoItem = new INMUEBLE
                        {
                            codigo_inmueble = form[$"Inmueble[{i}].Codigo_inmueble"],
                            tipo_inmueble = form[$"Inmueble[{i}].tipo_inmueble"],
                            LOTE_INMUEBLE_lote_id = obraId
                        };

                        db.INMUEBLE.Add(nuevoItem);
                    }

                    db.SaveChanges();
                    return RedirectToAction("InmuebleLista");
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores aquí
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los registros: " + ex.Message);
            }

            // Obtén la lista completa de obras desde la base de datos
            var todasLasObras = db.OBRA.ToList();

            // Filtra las obras que no tienen el nombre "Oficina Central"
            var obrasFiltradas = todasLasObras.Where(o => o.nombre_obra != "Oficina Central").ToList();

            // Asigna la lista filtrada a ViewBag.Obras
            ViewBag.Obras = new SelectList(obrasFiltradas, "obra_id", "nombre_obra");
            return View("Crear");
        }


        public async Task<ActionResult> ConfirmarEliminarInmuebles(int obraId)
        {
            var obra = await db.OBRA.FindAsync(obraId); // Obtén la actividad por su ID
            if (obra == null)
            {
                return HttpNotFound(); // Manejar el caso donde la actividad no existe
            }

            return View(obra);
        }

        [HttpPost]
        public async Task<ActionResult> EliminarInmueblesPorObra(int obraId)
        {
            // Encuentra y elimina todos los registros de ITEM_VERIF asociados a la actividadId
            var inmueblesAEliminar = await db.INMUEBLE
                .Where(e => e.LOTE_INMUEBLE_lote_id == obraId)
                .ToListAsync();

            if (inmueblesAEliminar == null || !inmueblesAEliminar.Any())
            {
                return HttpNotFound(); // O el código de error que desees si no se encuentran elementos para eliminar
            }

            // Elimina los registros asociados a la actividadId
            db.INMUEBLE.RemoveRange(inmueblesAEliminar);
            await db.SaveChangesAsync();

            return RedirectToAction("InmuebleLista"); // Redirige a la vista de la lista de ítems
        }


        public async Task<ActionResult> EditarPorObra(int? obraId)
        {
            if (obraId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Obtener todos los registros asociados a la actividad
            var registros = await db.INMUEBLE.Where(item => item.LOTE_INMUEBLE_lote_id == obraId).ToListAsync();

            registros = registros.OrderBy(a => int.Parse(Regex.Match(a.codigo_inmueble, @"\d+").Value)).ToList();

            if (registros == null || registros.Count == 0)
            {
                return HttpNotFound();
            }

            ViewBag.Obra_obra_id = obraId;
            return View("EditarPorObra", registros);
        }

        // POST: ItemVerif/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarPorObra(List<INMUEBLE> registros)
        {
            if (registros == null)
            {
                // Manejar el caso en que la lista de registros es null, por ejemplo, redirigir a una página de error
                return RedirectToAction("Error");
            }

            if (ModelState.IsValid)
            {
                foreach (var item in registros)
                {
                    // Adjunta el objeto al contexto para que EF pueda rastrear los cambios
                    db.Entry(item).State = EntityState.Modified;
                }

                await db.SaveChangesAsync();
                return RedirectToAction("InmuebleLista");
            }

            // Si hay errores de validación, es posible que necesites volver a cargar la lista desde la base de datos y volver a mostrar la vista
            // Esto es para asegurarse de que los cambios no confirmados no se pierdan
            ViewBag.Obra_obra_id = registros.FirstOrDefault()?.LOTE_INMUEBLE;
            return View("EditarPorObra", registros);
        }


        // GET: Inmueble/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            if (iNMUEBLE == null)
            {
                return HttpNotFound();
            }
            return View(iNMUEBLE);
        }

        // GET: Inmueble/Create
        public ActionResult Create()
        {
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra");
            return View();
        }

        // POST: Inmueble/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "inmueble_id,tipo_inmueble,OBRA_obra_id")] INMUEBLE iNMUEBLE)
        {
            if (ModelState.IsValid)
            {
                db.INMUEBLE.Add(iNMUEBLE);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", iNMUEBLE.LOTE_INMUEBLE.OBRA_obra_id);
            return View(iNMUEBLE);
        }

        // GET: Inmueble/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            if (iNMUEBLE == null)
            {
                return HttpNotFound();
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", iNMUEBLE.LOTE_INMUEBLE.OBRA_obra_id);
            return View(iNMUEBLE);
        }

        // POST: Inmueble/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "inmueble_id,codigo_inmueble,tipo_inmueble,OBRA_obra_id")] INMUEBLE iNMUEBLE)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iNMUEBLE).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("InmuebleLista");
            }
            ViewBag.OBRA_obra_id = new SelectList(db.OBRA, "obra_id", "nombre_obra", iNMUEBLE.LOTE_INMUEBLE.OBRA_obra_id);
            return View(iNMUEBLE);
        }


        // GET: Inmueble/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            if (iNMUEBLE == null)
            {
                return HttpNotFound();
            }
            return View(iNMUEBLE);
        }

        // POST: Inmueble/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            INMUEBLE iNMUEBLE = await db.INMUEBLE.FindAsync(id);
            db.INMUEBLE.Remove(iNMUEBLE);
            await db.SaveChangesAsync();
            return RedirectToAction("InmuebleLista");
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
