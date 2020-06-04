using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedNet.Data.Models.Models
{
    public class UserCredAssetData
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string ID { get; set; }

        public string PrivateKeys { get; set; }

        public string SignPublicKey { get; set; }

        public string AgreePublicKey { get; set; }

        public DateTime DateOfRecord { get; set; }

        public List<string> FingerPrintData { get; set; }
    }

    public class UserCredMetadata
    {
        public string hashedPassword { get; set;}
    }
}
