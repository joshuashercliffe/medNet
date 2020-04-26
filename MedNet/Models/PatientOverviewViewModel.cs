using MedNet.Data.Models.Models;
using System;
using System.Collections.Generic;

namespace MedNet.Models
{
    public class PatientOverviewViewModel
    {
        public string PatientName { get; set; }

        public string PatientPHN { get; set; }

        public DateTime PatientDOB { get; set; }

        public int PatientAge { get; set; }

        public List<DoctorNote> DoctorNotes { get; set; }

        public List<Prescription> Prescriptions { get; set; }
    }
}
