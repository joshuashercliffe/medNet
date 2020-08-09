using MedNet.Data.Models.Models;

namespace MedNet.Models
{
    public class FillPrescriptionViewModel
    {
        public PatientCredAssetData PatientAsset { get; set; }
        public PatientCredMetadata PatientMetadata { get; set; }
        public double QtyFilled { get; set; }

        public string PatientKeyword { get; set; }

        public PrescriptionFullData PrescriptionData { get; set; }
    }
}