using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedNet.Models
{
    public class PatientEditProfileViewModel
    {
        public string address { get; set; }
        public string phone { get; set; }
        public double weight { get; set; }
        public List<string> allergies { get; set; }
        public List<string> daily_meds { get; set; }
        public List<string> med_hist { get; set; }
        public string emer_name { get; set; }
        public string emer_phone { get; set; }
        public string emer_rel { get; set; }
    }
}
