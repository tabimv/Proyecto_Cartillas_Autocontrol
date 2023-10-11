using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Helpers
{
    public class PasswordHelper
    {
        public static void CreatePassword(string contraseña, out byte[] passwordMash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordMash = hmac.ComputeHash(Encoding.UTF8.GetBytes(contraseña));
            }
        }

        public static bool CheckPassword(string contraseña, byte[] passwordMash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                byte[] computePassword = hmac.ComputeHash(Encoding.UTF8.GetBytes(contraseña));
                for (int i = 0; i < computePassword.Length; i++)
                    if (passwordMash[i] != computePassword[i])
                        return false;
            }

            return true;
        }
    }
}



