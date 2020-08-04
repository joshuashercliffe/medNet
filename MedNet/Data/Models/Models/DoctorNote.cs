using System;

namespace MedNet.Data.Models.Models
{
    public class DoctorNote
    {
        public string PurposeOfVisit { get; set; }

        public string Description { get; set; }

        public string FinalDiagnosis { get; set; }

        public string FurtherInstructions { get; set; }

        public DateTime DateOfRecord { get; set; }

        public string DoctorName { get; set; }

        public string DoctorMinsc { get; set; }
    }

    public class DoctorNoteFullData
    {
        public string transID { get; set; }

        public string assetID { get; set; }
        public DoctorNote assetData { get; set; }
        public double Metadata { get; set; }
    }
}