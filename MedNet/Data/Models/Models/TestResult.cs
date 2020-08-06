using System;
using Microsoft.AspNetCore.Http;

namespace MedNet.Data.Models.Models
{
    public class FileData
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public string Type { get; set; }
        public string Extension { get; set; }
    }

    public class TestRequisitionInput
    {
        public string TestType { get; set; }
        public string ReasonForTest { get; set; }
        public IFormFile AttachedFile { get; set; }
    }

    public class TestRequisitionAsset
    {
        public string TestType { get; set; }
        public string ReasonForTest { get; set; }
        public FileData AttachedFile { get; set; }
        public DateTime DateOrdered { get; set; }
    }

    public class TestRequisitionFullData
    {
        public string transID { get; set; }

        public string assetID { get; set; }
        public TestRequisitionAsset assetData { get; set; }
        public double Metadata { get; set; }
    }
}
