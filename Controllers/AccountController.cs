using Proyecto_Cartilla_Autocontrol.Models;
using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace Proyecto_Cartilla_Autocontrol.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private ObraManzanoFinal db = new ObraManzanoFinal(); // Tu contexto de base de datos

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
            else if (!user.estado_usuario)
            {
                // Si el usuario está bloqueado (estado_usuario = false)
                ModelState.AddModelError("", "El Usuario se encuentra en estado bloqueado.");
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
                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                        1,                             // Ticket version
                        model.correo,                  // Username to be associated with this ticket
                        DateTime.Now,                  // Date/time issued
                        DateTime.Now.AddHours(12),     // Date/time to expire
                        true,                          // "true" for a persistent user cookie
                        user.PERFIL.rol,               // User-data, in this case the role
                        FormsAuthentication.FormsCookiePath // Cookie path specified in the web.config file
                    );

                    // Encrypt the ticket
                    string encTicket = FormsAuthentication.Encrypt(ticket);

                    // Create the cookie.
                    HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket)
                    {
                        HttpOnly = true,
                        Expires = DateTime.Now.AddHours(12)
                    };

                    // Add the cookie to the response.
                    Response.Cookies.Add(cookie);

                    // Almacena el objeto de usuario completo en sesión
                    Session["UsuarioAutenticado"] = user;

                    // Almacenar el tipo de perfil en una variable de sesión
                    Session["Perfil"] = user.PERFIL.rol;

                    if (Session["Perfil"].Equals("Administrador"))
                    {
                        if (EsDispositivoMovil())
                        {
                            return RedirectToAction("EditarCartillaMovilAdmin", "RevisionMovil"); // Redirige a la vista para dispositivos móviles
                        }
                        else
                        {
                            return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrol");
                        }
                       
                    }
                    else if (Session["Perfil"].Equals("Supervisor"))
                    {
                        // Detecta si el usuario está en un dispositivo móvil o no
                        if (EsDispositivoMovil())
                        {
                            return RedirectToAction("EditarCartillaMovilTest", "SupervisorMovil"); // Redirige a la vista para dispositivos móviles
                        }
                        else
                        {
                            return RedirectToAction("ListaCartillasSupervisor", "CartillasAutocontrolFiltrado"); // Redirige a la vista de computadoras
                        }
                    }
                    else if (Session["Perfil"].Equals("Autocontrol"))
                    {
                        // Detecta si el usuario está en un dispositivo móvil o no
                        if (EsDispositivoMovil())
                        {
                            return RedirectToAction("EditarCartillaMovilAutocontrol", "RevisionMovil"); // Redirige a la vista para dispositivos móviles
                        }
                        else
                        {
                            return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrolFiltrado"); // Redirige a la vista de computadoras
                        }
                    }
                    else if (Session["Perfil"].Equals("Consulta"))
                    {
                        return RedirectToAction("ListaCartillasPorActividad", "CartillasAutocontrolFiltrado");
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


        public bool EsDispositivoMovil()
        {
            string userAgent = Request.UserAgent.ToLower();

            // Detecta algunos User-Agents comunes para dispositivos móviles
            return userAgent.Contains("mobi") || userAgent.Contains("android") || userAgent.Contains("iphone") || userAgent.Contains("ipad");
        }


    }
}