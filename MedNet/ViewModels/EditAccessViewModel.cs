using MedNet.Data.Models;

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