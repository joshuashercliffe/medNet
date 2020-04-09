namespace MedNet.Data.Models
{
    public class AssetSaved<T>
    {
        public AssetType Type { get; set; }

        public T Data { get; set; }

        public int RandomId { get; set; }
    }
}
