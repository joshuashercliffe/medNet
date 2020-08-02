using System;
using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class PatientSignUpViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        public string PHN { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string KeyWord { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
