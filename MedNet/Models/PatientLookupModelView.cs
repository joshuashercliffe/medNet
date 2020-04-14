using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class PatientLookupViewModel
    {
        [Required(ErrorMessage = "PHN is required.")]
        public string PHN { get; set; }

        [Required(ErrorMessage = "Keyword is required.")]
        public string Keyword { get; set; }
    }
}
