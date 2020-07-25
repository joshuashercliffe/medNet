using System.Collections.Generic;

namespace MedNet.Data.Models
{
    public class MetaDataSaved<T>
    {
        public Dictionary<string,string> AccessList { get; set; }
        public T data { get; set; }
    }
}
