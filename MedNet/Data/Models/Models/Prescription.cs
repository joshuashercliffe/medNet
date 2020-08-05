using System;
using System.Collections.Generic;

namespace MedNet.Data.Models.Models
{
    public class Prescription
    {
        public DateTime PrescribingDate { get; set; }

        public string Superscription { get; set; }

        public string DrugName { get; set; }

        public double Concentration { get; set; }

        //public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double Refill { get; set; }

        public bool Substitution { get; set; }

        public string DirectionForUse { get; set; }

        public string DoctorName { get; set; }

        public string DoctorMinsc { get; set; }
    }

    public class PrescriptionMetadata
    {
        public double RefillRemaining { get; set; }
        public DateTime LastIssueDate { get; set; }
        public double LastIssueQty { get; set; }
    }

    public class PrescriptionFullData
    {
        public string transID { get; set; }

        public string assetID { get; set; }
        public Prescription assetData { get; set; }
        public PrescriptionMetadata Metadata { get; set; }
    }
}
