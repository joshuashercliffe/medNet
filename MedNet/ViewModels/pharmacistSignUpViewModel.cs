using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class pharmacistSignUpViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        public string PharmaNum { get; set; }

        [Required]
        public string PharmaKeyWord { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}