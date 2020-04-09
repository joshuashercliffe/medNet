using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MedNet.Data.Models
{
    public class Assets
    {
        [BsonId]
        public ObjectId _id { get; set; }
        [BsonElement]
        public string id { get; set; }
        [BsonElement]
        public Asset data { get; set; }
    }
    public class Metadatas
    {
        [BsonId]
        public ObjectId _id { get; set; }
        [BsonElement]
        public string id { get; set; }
        [BsonElement]
        public Metadata metadata { get; set; }
    }
    public class Metadata
    {
        [BsonElement]
        public MetaDataSaved metadata { get; set; }
    }
    public class AssetsMetadatas
    {
        [BsonElement]
        public string id { get; set; }
        [BsonElement]
        public Asset data { get; set; }
        [BsonElement]
        public MetaDataSaved metadata { get; set; }
    }

    public class ULAsset
    {
        [BsonId]
        public ObjectId _id { get; set; }
        [BsonElement]
        public string id { get; set; }
        [BsonElement]
        public UserListAsset data { get; set; }
    }
    public class ULMetadata
    {
        [BsonId]
        public ObjectId _id { get; set; }
        [BsonElement]
        public string id { get; set; }
        [BsonElement]
        public _ULMetadata metadata { get; set; }
    }
    public class _ULMetadata
    {
        [BsonElement]
        public UserListMetadata metadata { get; set; }
    }
    public class ULAssetMetadata
    {
        [BsonElement]
        public string id { get; set; }
        [BsonElement]
        public UserListAsset data { get; set; }
        [BsonElement]
        public UserListMetadata metadata { get; set; }
    }
}
