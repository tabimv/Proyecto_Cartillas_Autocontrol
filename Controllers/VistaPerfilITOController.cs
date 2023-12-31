﻿using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class VistaPerfilITOController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();
        // GET: VistaPerfilITO
       
        public async Task<ActionResult> Index()
        {
            var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
            ViewBag.UsuarioAutenticado = usuarioAutenticado;

            if (Session["UsuarioAutenticado"] != null)
            {
                var cARTILLA = db.CARTILLA.Include(c => c.ACTIVIDAD).Include(c => c.ESTADO_FINAL).Include(c => c.OBRA)
                               .Include(c => c.OBRA.USUARIO)
                               .Where(c => c.OBRA.USUARIO.Any(r => r.OBRA_obra_id == usuarioAutenticado.OBRA_obra_id));
                return View(await cARTILLA.ToListAsync());
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
        }


        public ActionResult EditarCartilla(int id)
        {
            // Obtener la Cartilla que se quiere editar por su ID
            var cartilla = db.CARTILLA.FirstOrDefault(c => c.cartilla_id == id);

            if (cartilla != null)
            {
                CartillasViewModel viewModel = new CartillasViewModel();
                viewModel.Cartilla = cartilla;

                // Obtener los detalles de la Cartilla que se quiere editar
                viewModel.DetalleCartillas = db.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == id).ToList();

                // Obtener otras listas necesarias para el formulario (actividades, elementos de verificación, inmuebles, etc.)
                viewModel.ActividadesList = db.ACTIVIDAD.ToList();
                viewModel.ElementosVerificacion = db.ITEM_VERIF.ToList();
                viewModel.InmuebleList = db.INMUEBLE.ToList();
                viewModel.EstadoFinalList = db.ESTADO_FINAL.ToList();

                return View(viewModel);
            }

            // Manejar si no se encuentra la Cartilla con el ID proporcionado
            return RedirectToAction("Index"); // O alguna otra acción adecuada
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

                        // Actualizar o agregar los detalles de la Cartilla en la base de datos
                        foreach (var detalleCartilla in viewModel.DetalleCartillas)
                        {
                            // Obtener el detalle de la base de datos para poder modificar solo estado_ito
                            var existingDetalle = dbContext.DETALLE_CARTILLA.FirstOrDefault(d => d.detalle_cartilla_id == detalleCartilla.detalle_cartilla_id);

                            if (existingDetalle != null)
                            {
                                // Modificar solo el campo estado_ito, sin afectar estado_otec
                                existingDetalle.estado_ito = detalleCartilla.estado_ito;
                                dbContext.Entry(existingDetalle).State = EntityState.Modified;


                            }
                        }

                        // Eliminar detalles de la Cartilla que se hayan quitado en la edición
                        foreach (var detalle in dbContext.DETALLE_CARTILLA.Where(d => d.CARTILLA_cartilla_id == viewModel.Cartilla.cartilla_id))
                        {
                            if (!viewModel.DetalleCartillas.Any(d => d.detalle_cartilla_id == detalle.detalle_cartilla_id))
                            {
                                dbContext.DETALLE_CARTILLA.Remove(detalle);
                            }
                        }

                        dbContext.Entry(viewModel.Cartilla).State = EntityState.Modified;
                        // Guardar los cambios en la base de datos
                        dbContext.SaveChanges();

                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                }
            }

            return View(viewModel);
        }
    }
}