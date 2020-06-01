using System;
using System.Collections.Generic;

namespace BigChainDbTestiong.Models
{
    public class DoctorNoteAsset
    {
        public int randomNoteID { get; set;}
        public string data { get; set; }

        public string type { get; set; }

        public DoctorNoteAsset()
        {
            type = "Doctor's Note";
        }
    }

    public class DoctorNoteMetadata
    {
        public Dictionary<string, string> accessList { get; set; }

        public DoctorNoteMetadata() 
        {
            accessList = new Dictionary<string, string>();
        }
    }
}
