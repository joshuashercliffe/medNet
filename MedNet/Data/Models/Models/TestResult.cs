namespace MedNet.Data.Models.Models
{
    public class TestResult
    {
        public int TestTypeId { get; set; }

        public string ReasonForTest { get; set; }

        public byte[] AttachedFile { get; set; }
    }
}
