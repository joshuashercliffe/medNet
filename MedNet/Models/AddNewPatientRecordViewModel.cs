using MedNet.Data.Models.Models;
using Microsoft.AspNetCore.Http;

namespace MedNet.Models
{
    public class AddNewPatientRecordViewModel
    {
        public string DoctorFirstName { get; set; }

        public string DoctorLastName { get; set; }

        public DoctorNote DoctorsNote { get; set; }

        public Prescription Prescription { get; set; }

        public TestResult TestResult { get; set; }

        public IFormFile File { get; set; }
    }
}
