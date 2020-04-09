using System.Collections.Generic;

namespace MedNet.Data.Models
{
    public class MetaDataSaved
    {
        public Dictionary<string, string> AccessList { get; set; }

        public MetaDataSaved()
        {
            AccessList = new Dictionary<string, string>();
        }
    }
}
