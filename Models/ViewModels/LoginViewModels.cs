using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Proyecto_Cartilla_Autocontrol.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El campo Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo proporcionado no es válido.")]
        public String correo { get; set; }

        [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
        [DataType(DataType.Password)]
        public String contraseña { get; set; }

        [Display(Name = "Token de Restablecimiento")]
        public string ResetPasswordToken { get; set; }

        [Display(Name = "Fecha de Vencimiento del Token")]
        public DateTime? ResetPasswordTokenExpiration { get; set; }
    }



}