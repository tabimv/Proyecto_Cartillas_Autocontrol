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


namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class PerfilController : Controller
    {
        private ObraManzanoFinal db = new ObraManzanoFinal();
        // GET: Perfil
        public ActionResult PerfilAdmin()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                // Recupera la información del usuario autenticado desde la sesión
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var informacionUsuarios = db.USUARIO
                  .Include(u => u.PERFIL)
                  .Include(u => u.PERSONA)
                  .Include(u => u.OBRA)
                  .Where(u => u.PERSONA_rut == usuarioAutenticado.PERSONA_rut) // Filtra la información solo para el usuario autenticado
                  .ToList();

                ViewBag.InformacionUsuarios = informacionUsuarios;
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
            return View();
        }

        public ActionResult PerfilOtec()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                // Recupera la información del usuario autenticado desde la sesión
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var informacionUsuarios = db.USUARIO
                  .Include(u => u.PERFIL)
                  .Include(u => u.PERSONA)
                  .Include(u => u.OBRA)
                  .Where(u => u.PERSONA_rut == usuarioAutenticado.PERSONA_rut) // Filtra la información solo para el usuario autenticado
                  .ToList();

                ViewBag.InformacionUsuarios = informacionUsuarios;
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
            return View();
        }

        public ActionResult PerfilIto()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                // Recupera la información del usuario autenticado desde la sesión
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var informacionUsuarios = db.USUARIO
                  .Include(u => u.PERFIL)
                  .Include(u => u.PERSONA)
                  .Include(u => u.OBRA)
                  .Where(u => u.PERSONA_rut == usuarioAutenticado.PERSONA_rut) // Filtra la información solo para el usuario autenticado
                  .ToList();

                ViewBag.InformacionUsuarios = informacionUsuarios;
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
            return View();
        }

        public ActionResult PerfilConsulta()
        {
            if (Session["UsuarioAutenticado"] != null)
            {
                // Recupera la información del usuario autenticado desde la sesión
                var usuarioAutenticado = (USUARIO)Session["UsuarioAutenticado"];
                ViewBag.UsuarioAutenticado = usuarioAutenticado;

                var informacionUsuarios = db.USUARIO
                  .Include(u => u.PERFIL)
                  .Include(u => u.PERSONA)
                  .Include(u => u.OBRA)
                  .Where(u => u.PERSONA_rut == usuarioAutenticado.PERSONA_rut) // Filtra la información solo para el usuario autenticado
                  .ToList();

                ViewBag.InformacionUsuarios = informacionUsuarios;
            }
            else
            {
                // Maneja el caso en el que el usuario no esté autenticado correctamente
                return RedirectToAction("Login", "Account"); // Redirige a la página de inicio de sesión u otra página adecuada
            }
            return View();
        }

    }
}