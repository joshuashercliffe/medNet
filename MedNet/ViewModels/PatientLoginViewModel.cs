using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class PatientLoginViewModel
    {
        [Required(ErrorMessage = "Patient's PHN is required.")]
        public string PatientPHN { get; set; }

        [Required(ErrorMessage = "Patients's Keyword is required.")]
        public string PatientKeyword { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string password { get; set; }
    }
}
