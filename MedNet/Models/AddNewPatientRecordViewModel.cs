using MedNet.Data.Models.Models;

namespace MedNet.Models
{
    public class AddNewPatientRecordViewModel
    {
        public DoctorNote DoctorsNote { get; set; }

        public Prescription Prescription { get; set; }

        public TestResult TestResult { get; set; }
    }
}
