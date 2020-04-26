using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class doctorSignUpViewModel
    {
        [Required]
        public string FirstName  { get; set; }

        [Required]
        public string LastName { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        public string DoctorMINC { get; set; }

        [Required]
        public string DoctorKeyWord { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
