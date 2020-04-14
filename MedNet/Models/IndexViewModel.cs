using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class IndexViewModel
    {
        [Required(ErrorMessage = "Doctor's MINC is required.")]
        public string DoctorMINC { get; set; }

        [Required(ErrorMessage = "Doctor's Keyword is required.")]
        public string DoctorKeyword { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string password { get; set; }
    }
}
