using MedNet.Data.Models.Models;
using Microsoft.AspNetCore.Http;

namespace MedNet.Models
{
    public class UploadResultViewModel
    {
        public PatientCredAssetData PatientAsset { get; set; }
        public PatientCredMetadata PatientMetadata { get; set; }
        public IFormFile ResultFile { get; set; }
        public TestRequisitionFullData TestData { get; set; }
    }
}