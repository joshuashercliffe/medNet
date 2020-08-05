using Microsoft.AspNetCore.Http;

namespace MedNet.Data.Models.Models
{
    public class TestResult
    {
        public int TestTypeId { get; set; }

        public string ReasonForTest { get; set; }

        public string File { get; set; }

        public string FileType { get; set; }

        public string FileExtension { get; set; }
    }
}
