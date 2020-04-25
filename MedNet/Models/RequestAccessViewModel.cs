using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class RequestAccessViewModel
    {
        [Required(ErrorMessage = "Keyword is required.")]
        public string keyword { set; get; }
    }
}
