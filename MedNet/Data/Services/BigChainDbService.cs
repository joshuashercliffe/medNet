using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Omnibasis.BigchainCSharp.Api;
using Omnibasis.BigchainCSharp.Builders;
using Omnibasis.BigchainCSharp.Constants;
using Omnibasis.BigchainCSharp.Model;
using Omnibasis.BigchainCSharp.Util;
using MongoDB.Driver;
using MongoDB.Bson;
using Nito.AsyncEx;
using NSec.Cryptography;
using MedNet.Data.Models;
using MedNet.Data.Models.Models;
using Newtonsoft.Json;
using BigchainDB.Objects;

namespace MedNet.Data.Services
{
    public class BigChainDbService
    {
        private static IMongoDatabase bigchainDatabase;

/*        public void GetUserList(string publicKey, string privateKey)
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
        }*/

        public BigChainDbService(string url)
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

        public void SendCreateTransactionToDataBase<A,M>(AssetSaved<A> asset, MetaDataSaved<M> metaData, string privateSignKey)
        {
            var signPrivateKey = EncryptionService.getSignKeyFromPrivate(privateSignKey);

            var transaction = BigchainDbTransactionBuilder<AssetSaved<A>, MetaDataSaved<M>>
                .init()
                .addAssets(asset)
                .addMetaData(metaData)
                .operation(Operations.CREATE)
                .buildAndSignOnly(signPrivateKey.PublicKey, signPrivateKey);

            var createTransaction = TransactionsApi<AssetSaved<A>, MetaDataSaved<M>>.sendTransactionAsync(transaction).GetAwaiter().GetResult();
        }

        public void SendCreateTransferTransactionToDataBase<A, M>(AssetSaved<A> asset, MetaDataSaved<M> metaData, string senderPrivateSignKey, string recieverPublicSignKey)
        {
            var senderSignPrivateKey = EncryptionService.getSignKeyFromPrivate(senderPrivateSignKey);
            var recieverSignPublicKey = EncryptionService.getSignPublicKeyFromString(recieverPublicSignKey);

            var transaction = BigchainDbTransactionBuilder<AssetSaved<A>, MetaDataSaved<M>>
                .init()
                .addAssets(asset)
                .addMetaData(metaData)
                .operation(Operations.CREATE)
                .addOutput("1", recieverSignPublicKey)
                .buildAndSignOnly(senderSignPrivateKey.PublicKey, senderSignPrivateKey);

            var createTransaction = TransactionsApi<AssetSaved<A>, MetaDataSaved<M>>.sendTransactionAsync(transaction).GetAwaiter().GetResult();
        }

        public List<AssetsMetadatas<A,Dictionary<string,string>>> GetAllTypeRecordsFromDPublicPPublicKey<A>
            (AssetType type, string doctorSignPublicKey, string patientSignPublicKey)
        {
            // get unspent outputs of the patient, this means get all outputs that he owns
            var rawPublicKey = EncryptionService.getRawBase58PublicKey(patientSignPublicKey);
            var unspentOutsList = OutputsApi.getUnspentOutputsAsync(rawPublicKey).GetAwaiter().GetResult();
            List<string> transactionIDs = new List<string>();
            foreach(var unspentOutput in unspentOutsList)
            {
                transactionIDs.Add(unspentOutput.TransactionId);
            }
            var assets = bigchainDatabase.GetCollection<Assets<A>>("assets").AsQueryable();
            var TypeAssets = assets.Where(x => x.data.Type == type);
            // reduce to get only metadata where both public keys are present and is in the unspent list
            var metadata = bigchainDatabase.GetCollection<Metadatas<Dictionary<string, string>>>("metadata").AsQueryable();
            var metadataReduced = metadata.Where(x => transactionIDs.Contains(x.id));/*
            metadataReduced = metadataReduced.Where(x => x.metadata.metadata.data.Keys.ToList().Contains(doctorSignPublicKey) && 
            x.metadata.metadata.data.Keys.ToList().Contains(patientSignPublicKey));*/
            List<Metadatas<Dictionary<string, string>>> metadataList = new List<Metadatas<Dictionary<string, string>>>();
            foreach (var a in metadataReduced)
            {
                if (a.metadata.metadata.data.Keys.ToList().Contains(doctorSignPublicKey)
                    && a.metadata.metadata.data.Keys.ToList().Contains(patientSignPublicKey))
                {
                    metadataList.Add(a);
                }
            }

            var transaction = (bigchainDatabase.GetCollection<Transactions<string>>("transactions").AsQueryable());
            var transactionsReduced = transaction.AsQueryable().Where(x => transactionIDs.Contains(x.id));
            transactionsReduced = transactionsReduced.Select(r => new Transactions<string> { id = r.id, assets = r.assets, _id = r._id});
            var r = from t1 in metadataList
                    join t2 in transactionsReduced on t1.id equals t2.id
                    join t3 in TypeAssets on 1 equals 1
                    where (t2.id == t3.id || (t2.assets != null && t2.assets.id == t3.id))
                    select new AssetsMetadatas<A,Dictionary<string,string>>()
                    {
                        id = t3.id,
                        data = t3.data,
                        metadata = t1.metadata.metadata
                    };
            return r.ToList();
        }

        public List<string> GetAlPrescriptionsFromPublicKey(string publicKey)
        {
            var prescriptions = new List<string>();
            var assets = bigchainDatabase.GetCollection<Models.Assets<string>>("assets");
            var prescriptionAssets = assets.AsQueryable().Where(x => x.data.Type == AssetType.Prescription);
            var metadata = bigchainDatabase.GetCollection<Metadatas<Dictionary<string,string>>>("metadata");
            var r = from t1 in prescriptionAssets
                    join t2 in metadata.AsQueryable() on t1.id equals t2.id
                    where t2.metadata.metadata.data.ContainsKey(publicKey)
                    select new AssetsMetadatas<string,Dictionary<string,string>>()
                    {
                        id = t1.id,
                        data = t1.data,
                        metadata = t2.metadata.metadata
                    };
            foreach (var a in r)
            {
                prescriptions.Add(a.data.Data);
                Console.WriteLine(a.id);
            }
            return prescriptions;
        }

        // Gets the list of users
/*        public List<string> GetUserListAsset(string publicKey, string assetType = "User List")
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
            foreach (var inst in assetInst)
            {
                assetData.Add(inst.id);
                //Console.WriteLine(inst.id);
            }

            return assetData;
        }*/

        public Models.Assets<UserCredAssetData> GetUserAssetFromTypeID(AssetType assetType, string id)
        {
            var assets = bigchainDatabase.GetCollection<Models.Assets<UserCredAssetData>>("assets");
            var asset = from a in assets.AsQueryable() where a.data.Type == assetType && a.data.Data.ID == id select a;
            if (asset.Any())
                return asset.FirstOrDefault();
            else
                return null;
        }

        public M GetMetadataFromAssetPublicKey<M>(string assetID, string signPublicKey)
        {
            var rawPublicKey = EncryptionService.getRawBase58PublicKey(signPublicKey);
            var unspentOutsList = OutputsApi.getUnspentOutputsAsync(rawPublicKey).GetAwaiter().GetResult();
            var assetTransactions = TransactionsApi<object, object>.getTransactionsByAssetIdAsync(assetID).GetAwaiter().GetResult();
            var neededTransaction = from a in unspentOutsList.AsQueryable()
                                     join b in assetTransactions.AsQueryable()
                                     on a.TransactionId equals b.Id
                                     select b.MetaData.Metadata;
            MetaDataSaved<M> result = JsonConvert.DeserializeObject<MetaDataSaved<M>>(neededTransaction.FirstOrDefault().ToString());
            return result.data;
        }

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
    }
}
