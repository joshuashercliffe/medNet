using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class PharmacistLoginViewModel
    {
        [Required(ErrorMessage = "Pharmacist's registration number is required.")]
        public string PharmaNum { get; set; }

        [Required(ErrorMessage = "Pharmacist's Keyword is required.")]
        public string PharmaKeyword { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string password { get; set; }
    }
}
