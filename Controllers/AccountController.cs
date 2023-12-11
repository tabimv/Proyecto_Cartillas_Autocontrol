using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private ObraManzanoDicEntities db = new ObraManzanoDicEntities(); // Tu contexto de base de datos

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult Login()
        {
            // Configura los encabezados para evitar el almacenamiento en caché
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            var user = db.USUARIO.FirstOrDefault(u => u.PERSONA.correo == model.correo);

            if (user == null)
            {
                ModelState.AddModelError("correo", "El correo es incorrecto.");
            }
            else if (ModelState.IsValid) // Solo si el correo es válido, verifica la contraseña
            {
                if (user.contraseña != model.contraseña)
                {
                    ModelState.AddModelError("contraseña", "La contraseña es incorrecta.");
                }

                if (ModelState.IsValid) // Verifica nuevamente después de agregar los errores
                {
                    // Autenticar al usuario
                    FormsAuthentication.SetAuthCookie(model.correo, false);

                    // Almacena el objeto de usuario completo en sesión
                    Session["UsuarioAutenticado"] = user;

                    // Obtener el nombre del tipo de perfil del usuario desde la base de datos
                    var tipoPerfil = db.PERFIL.FirstOrDefault(tp => tp.perfil_id == user.PERFIL_perfil_id)?.rol;

                    // Almacenar el tipo de perfil en una variable de sesión
                    Session["Perfil"] = user.PERFIL.rol;

                    if (Session["Perfil"].Equals("Administrador"))
                    {
                        return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
                    }
                    else if (Session["Perfil"].Equals("OTEC"))
                    {
                        return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
                    }
                    else if (Session["Perfil"].Equals("Consulta"))
                    {
                        return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
                    }
                }
            }

            return View(model);
        }


        public ActionResult Logout()
        {
            // Cerrar la sesión actual
            FormsAuthentication.SignOut();

            // Limpiar la variable de sesión relacionada con el tipo de perfil
            Session.Remove("Perfil");

            // Eliminar completamente la sesión
            Session.Clear();
            Session.RemoveAll();
            Session.Abandon();


            // Invalidar la caché del navegador para evitar el acceso a la sesión anterior
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cookies.Clear();

            // Agregar una cookie expirada para asegurar el cierre de sesión en Chrome
            HttpCookie cookie = new HttpCookie("ASP.NET_SessionId", "");
            cookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(cookie);

            // Redirigir al inicio de sesión
            return RedirectToAction("Login", "Account");
        }



    }


}