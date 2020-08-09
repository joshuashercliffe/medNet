using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class mltSignUpViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        public string CSMLSID { get; set; }

        [Required]
        public string MLTKeyWord { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}