using System;

namespace MedNet.Data.Models.Models
{
    public class Prescription
    {
        public DateTime PrescribingDate { get; set; }

        public string DrugNameStrength { get; set; }

        public double Dosage { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string DirectionForUse { get; set; }

        public string DoctorName { get; set; }

        public string DoctorMinsc { get; set; }
    }
}
