using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class MLTHomeViewModel
    {
        [Required(ErrorMessage = "MLT's CSMLS ID is required.")]
        public string CSMLSID { get; set; }
    }
}
