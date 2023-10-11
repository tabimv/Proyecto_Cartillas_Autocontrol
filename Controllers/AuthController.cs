//using Microsoft.AspNet.Identity;
//using Proyecto_Cartilla_Autocontrol.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;
//using Proyecto_Cartilla_Autocontrol.Models.ViewModels;
//using System.Web.UI.WebControls;
//using static System.Collections.Specialized.BitVector32;
//using System.Security.Claims;

//namespace Proyecto_Cartilla_Autocontrol.Controllers
//{
//    public class AuthController : Controller
//    {
//        // GET: Auth
//        private TestConexion db = new TestConexion();


//        [HttpGet]
//        public ActionResult Login()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<ActionResult> Login(LoginViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                var user = await db.USUARIO.FirstOrDefaultAsync(x => x.correo == model.correo);
//                if (user == null)
//                {
//                    TempData["ErrorMesagge"] = "Usuario no encontrado";
//                    return RedirectToAction("Login");
//                }

//                // Aquí puedes realizar la autenticación del usuario si las credenciales son válidas
//                // Puedes usar Identity o cualquier otro sistema de autenticación que estés utilizando
//                // Autenticar al usuario antes de redirigirlo
//                await InitOwin(user);

//                // Luego, redirige al usuario a la página de Index
//                return RedirectToAction("Index", "Usuario");
//            }
//            TempData["SucessMessage"] = "Conectado correctamente";
//            return View();
//        }


//        private async Task InitOwin(USUARIO user)
//        {
//            var claims = new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, user.PERSONA_rut.ToString()),
//                new Claim(ClaimTypes.Email, user.correo.ToString()),
//            };

//            var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
//            var role = await db.PERFIL.FindAsync(user.PERFIL_perfil_id);
//            if (role != null)
//                identity.AddClaim(new Claim(ClaimTypes.Role, role.rol));

//            var context = Request.GetOwinContext();
//            var authManager = context.Authentication;

//            authManager.SignIn(identity);
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//                db.Dispose();
//            base.Dispose(disposing);
//        }

//        public ActionResult Logout()
//        {
//            // Sign out al usuario
//            var context = Request.GetOwinContext();
//            var authManager = context.Authentication;
//            authManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

//            // Redirige al usuario a la página de inicio o a donde desees después de cerrar sesión
//            return RedirectToAction("Login", "Auth");
//        }

//    }
//}