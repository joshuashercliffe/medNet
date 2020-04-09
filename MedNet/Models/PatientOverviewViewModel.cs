using MedNet.Data.Models.Models;
using System.Collections.Generic;

namespace MedNet.Models
{
    public class PatientOverviewViewModel
    {
        public List<DoctorNote> DoctorNotes { get; set; }

        public List<Prescription> Prescriptions { get; set; }
    }
}
