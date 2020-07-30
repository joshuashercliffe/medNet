namespace MedNet.Data.Models
{
    public class TransactionInformation<A,M>
    {
        public A Asset { get; set; }

        public M MetaData { get; set; }

        public string transID { get; set; }
        
        public string assetID { get; set; }
    }
}
