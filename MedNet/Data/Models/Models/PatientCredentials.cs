using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedNet.Data.Models.Models
{
    public class PatientCredAssetData
    {
        public DateTime DateOfBirth { get; set; }

        public string ID { get; set; }

        public string PrivateKeys { get; set; }

        public string SignPublicKey { get; set; }

        public string AgreePublicKey { get; set; }

        public DateTime DateOfRecord { get; set; }

        public List<string> FingerprintData { get; set; }
        //public string FingerprintData { get; set; }
    }

    public class PatientCredMetadata
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Postal { get; set; }
        public string Allergies { get; set; }
        public string MedHist { get; set; }
        public string Meds { get; set; }
        // Emergency contact info
        public string emerFirstName { get; set; }
        public string emerLastName { get; set; }
        public string emerPhone { get; set; }
        public string emerAddress { get; set; }
        public string emerCity { get; set; }
        public string emerProvince { get; set; }
        public string emerPostal { get; set; }
        public string Relationship { get; set; }

        public string hashedPassword { get; set;}
    }
}
