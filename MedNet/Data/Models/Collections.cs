using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MedNet.Data.Models
{
    public class Assets<T>
    {
        [BsonId]
        public ObjectId _id { get; set; }

        [BsonElement]
        public string id { get; set; }

        [BsonElement]
        public AssetSaved<T> data { get; set; }
    }

    public class Metadatas<T>
    {
        [BsonId]
        public ObjectId _id { get; set; }

        [BsonElement]
        public string id { get; set; }

        [BsonElement]
        public Metadata<T> metadata { get; set; }
    }

    public class Metadata<T>
    {
        [BsonElement]
        public MetaDataSaved<T> metadata { get; set; }
    }

    public class AssetsMetadatas<A, M>
    {
        [BsonElement]
        public string id { get; set; }

        [BsonElement]
        public AssetSaved<A> data { get; set; }

        [BsonElement]
        public MetaDataSaved<M> metadata { get; set; }

        [BsonElement]
        public string transID { get; set; }
    }

    public class Transactions<A>
    {
        [BsonId]
        public ObjectId _id { get; set; }

        [BsonElement]
        public Assets<A> asset { get; set; }

        [BsonElement]
        public string id { get; set; }
    }
}