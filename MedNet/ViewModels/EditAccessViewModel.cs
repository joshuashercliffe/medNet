using MedNet.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedNet.ViewModels
{
    public class EditAccessViewModel
    {
        public string UserType { get; set; }

        public string UserID { get; set; }

        public string TransID { get; set; }
        
        public AssetType reportType { get; set; }
    }
}
