using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class MLTLoginViewModel
    {
        [Required(ErrorMessage = "MLT's CSMLS ID is required.")]
        public string CSMLSID { get; set; }

        [Required(ErrorMessage = "MLT's Keyword is required.")]
        public string MLTKeyword { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string password { get; set; }
    }
}
