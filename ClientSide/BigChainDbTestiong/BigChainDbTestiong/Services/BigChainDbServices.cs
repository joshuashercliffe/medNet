using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BigChainDbTestiong.Models;
using Omnibasis.BigchainCSharp.Api;
using Omnibasis.BigchainCSharp.Builders;
using Omnibasis.BigchainCSharp.Constants;
using Omnibasis.BigchainCSharp.Model;
using Omnibasis.BigchainCSharp.Util;
using MongoDB.Driver;
using MongoDB.Bson;
using Nito.AsyncEx;
using NSec.Cryptography;


namespace BigChainDbTestiong.Services
{
    public class BigChainDbServices
    {
        private IMongoDatabase bigchainDatabase;

        public void GetUserList(string publicKey, string privateKey)
        {
            UserListAsset userListAsset = new UserListAsset();

            UserListMetadata userListMetadata = new UserListMetadata();

            // Checks if Asset is already created 
            var userList = GetUserListAsset(publicKey);

            if (userList.Count == 0)
            {
                // create a new asset
                SendTransactionToDataBase(userListAsset, userListMetadata, publicKey, privateKey);
            }
            var test = GetUserListAsset(publicKey);

            return;
        }
        
        public BigChainDbServices(string url)
        {
            Console.Write("Connecting to NodeURL: ");
            Console.WriteLine(url);

            var builder = BigchainDbConfigBuilder
                .baseUrl(url)
                .addToken("header1", "header1_value")
                .addToken("header2", "header2_value");

            if (!AsyncContext.Run(() => builder.setup()))
            {
                Console.WriteLine("Failed to setup");
            };

            // Use MongoDB for READ-ONLY access
            //connect directly to MongoDB service running on the node to enable better query options
            var client = new MongoClient("mongodb://dbreader:dbreaderpassword@anode.lifeblocks.site:27017/?authSource=bigchain");
            bigchainDatabase = client.GetDatabase("bigchain");
        }

        public void MultiNodeSetup(List<string> urls)
        {
            // Create a list of connections
            List<BlockchainConnection> connections = new List<BlockchainConnection>();
            Console.WriteLine("NodeURLs: ");
            foreach (var url in urls)
            {
                // print urls that we're connecting to
                Console.WriteLine(url);

                var conn_conf = new Dictionary<string, object>();
                conn_conf.Add("baseUrl", url);

                BlockchainConnection bc_conn = new BlockchainConnection(conn_conf);
                connections.Add(bc_conn);
            }

            var builder = BigchainDbConfigBuilder
                .addConnections(connections)
                .setTimeout(60000);

            if (!AsyncContext.Run(() => builder.setup()))
            {
                Console.WriteLine("Failed Multi-Node Setup");
            }
            Console.WriteLine("Finished connecting to multiple nodes");
            return;
        }

        public TransactionInformation GetMetaDataAndAssetFromTransactionId(string transactionId)
        {
            var transactionInformation = new TransactionInformation();
            var result = TransactionsApi<object, object>.getTransactionByIdAsync(transactionId).Result;

            transactionInformation.Asset = result.Asset.Data.ToString();
            transactionInformation.MetaData = result.MetaData.Metadata.ToString();

            return transactionInformation;
        }

        // This function is similar to 
        public void SendTransactionToDataBase(DoctorNoteAsset asset, DoctorNoteMetadata metaData, string publicKey, string privateKey)
        {
            var algorithm = SignatureAlgorithm.Ed25519;
            var privateKeySigned = Key.Import(algorithm, Utils.StringToByteArray(privateKey), KeyBlobFormat.PkixPrivateKey);
            var publicKeySigned = PublicKey.Import(algorithm, Utils.StringToByteArray(publicKey), KeyBlobFormat.PkixPublicKey);

            var transaction = BigchainDbTransactionBuilder<DoctorNoteAsset, DoctorNoteMetadata>
                .init()
                .addAssets(asset)
                .addMetaData(metaData)
                .operation(Operations.CREATE)
                .buildAndSignOnly(publicKeySigned, privateKeySigned);

            var createTransaction = TransactionsApi<DoctorNoteAsset, DoctorNoteMetadata>.sendTransactionAsync(transaction);
        }

        // general form
        public void SendTransactionToDataBase(UserListAsset asset, UserListMetadata metaData, string publicKey, string privateKey)
        {
            var algorithm = SignatureAlgorithm.Ed25519;
            var privateKeySigned = Key.Import(algorithm, Utils.StringToByteArray(privateKey), KeyBlobFormat.PkixPrivateKey);
            var publicKeySigned = PublicKey.Import(algorithm, Utils.StringToByteArray(publicKey), KeyBlobFormat.PkixPublicKey);

            var transaction = BigchainDbTransactionBuilder<UserListAsset, UserListMetadata>
                .init()
                .addAssets(asset)
                .addMetaData(metaData)
                .operation(Operations.CREATE)
                .buildAndSignOnly(publicKeySigned, privateKeySigned);

            var createTransaction = TransactionsApi<UserListAsset, UserListMetadata>.sendTransactionAsync(transaction);
        }

        public List<string> GetAllDoctorNotesFromPublicKey(string publicKey)
        {
            Console.WriteLine("Looking for Doctor's Notes");
            var doctorNotes = new List<string>();
            var assets = bigchainDatabase.GetCollection<Assets>("assets");
            var docNotesAssets = assets.AsQueryable().Where(x => x.data.type == "Doctor's Note");
            var metadata = bigchainDatabase.GetCollection<Metadatas>("metadata");
            var r = from t1 in docNotesAssets
                    join t2 in metadata.AsQueryable() on t1.id equals t2.id
                    where t2.metadata.metadata.accessList.ContainsKey(publicKey)
                    select new AssetsMetadatas()
                    {
                        id = t1.id,
                        data = t1.data,
                        metadata = t2.metadata.metadata
                    };
            foreach (var a in r)
            {
                Console.WriteLine(a.id);
            }
            return doctorNotes;
        }

        // Gets the list of users
        public List<string> GetUserListAsset(string publicKey, string assetType = "User List")
        {
            //Console.WriteLine("Looking for assets of type: \"" + assetType + "\"");
            var assetData = new List<string>();
            var assetCollection = bigchainDatabase.GetCollection<ULAsset>("assets");
            var filter = Builders<ULAsset>.Filter.Empty;
            var docCount = assetCollection.EstimatedDocumentCount();

            //Console.WriteLine("Document count: " + docCount);
            
            //Console.WriteLine("Filter: " + filter);
            var result = assetCollection.Find(filter);
            //Console.WriteLine("Result: " + result);
            
            var assetFound = assetCollection.AsQueryable().Where(x => x.data.type == assetType);
            //var metadataFound = bigchainDatabase.GetCollection<ULMetadata>("metadata"); 


            var assetInst = from currAsset in assetFound
                    //join currMetadata in metadataFound.AsQueryable() on currAsset.id equals currMetadata.id
                    //where currMetadata.metadata.metadata.accessList.ContainsKey(publicKey)
                    select new ULAssetMetadata()
                    {
                        id = currAsset.id,
                        data = currAsset.data,
                        //metadata = currMetadata.metadata.metadata,
                    };
            foreach(var inst in assetInst)
            {
               assetData.Add(inst.id);
               //Console.WriteLine(inst.id);
            }

            return assetData;
        }
        public List<string> GetAllTransactionsFromPublicKey(string publicKey, string privateKey)
        {
            var algorithm = SignatureAlgorithm.Ed25519;
            var signedPublicKey = PublicKey.Import(algorithm, Utils.StringToByteArray(publicKey), KeyBlobFormat.PkixPublicKey);
            var signedPrivateKey = Key.Import(algorithm, Utils.StringToByteArray(privateKey), KeyBlobFormat.PkixPrivateKey);

            //Need to get the signed string of the public key in here, instead of having to create a transaction using public and private key
            var transaction = BigchainDbTransactionBuilder<TestAsset, TestMetaData>
                .init()
                .addAssets(new TestAsset())
                .addMetaData(new TestMetaData())
                .operation(Operations.CREATE)
                .buildAndSignOnly(signedPublicKey, signedPrivateKey);

            var test = transaction.Outputs[0].PublicKeys[0].ToString();

            var transactionIds = new List<string>();
            var outputs = OutputsApi.getOutputsAsync(test).Result;
            foreach(var output in outputs)
            {
                transactionIds.Add(output.TransactionId);
            }

            return transactionIds;
        }
    }
}
