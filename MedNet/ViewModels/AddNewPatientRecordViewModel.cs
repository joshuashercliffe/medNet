using MedNet.Data.Models.Models;

namespace MedNet.Models
{
    public class AddNewPatientRecordViewModel
    {
        public string DoctorFirstName { get; set; }

        public string DoctorLastName { get; set; }

        public DoctorNote DoctorsNote { get; set; }

        public Prescription Prescription { get; set; }

        public TestRequisitionInput TestRequisition { get; set; }
    }
}