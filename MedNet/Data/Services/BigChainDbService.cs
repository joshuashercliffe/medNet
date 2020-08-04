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
            var client = new MongoClient("mongodb://dbreader:dbreaderpassword@cnode.lifeblocks.site:27017/?authSource=bigchain");
            bigchainDatabase = client.GetDatabase("bigchain");
        }

        public BigChainDbService(string[] urls)
        {
            // Create a list of connections
            List<BlockchainConnection> connections = new List<BlockchainConnection>();
            Console.WriteLine("NodeURLs: ");
            Random rnd = new Random();
            string[] MyRandomArray = urls.OrderBy(x => rnd.Next()).ToArray();
            foreach (var url in MyRandomArray)
            {
                // print urls that we're connecting to
                Console.WriteLine(url);

                var conn_conf = new Dictionary<string, object>();
                conn_conf.Add("baseUrl", "https://"+url);
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
            var baseURL = builder.BaseUrl.Split("/")[2];;
            Console.WriteLine("Finished connecting to multiple nodes");
            var client = new MongoClient($"mongodb://dbreader:dbreaderpassword@{baseURL}:27017/?authSource=bigchain");
            bigchainDatabase = client.GetDatabase("bigchain");
            return;
        }

        public AssetsMetadatas<A,M> GetMetaDataAndAssetFromTransactionId<A,M>(string transactionId)
        {
            var assets = bigchainDatabase.GetCollection<Assets<object>>("assets").AsQueryable().ToList();

            var metadata = bigchainDatabase.GetCollection<Metadatas<object>>("metadata").AsQueryable();
            var metadataReduced = metadata.Where(x => x.id == transactionId).ToList();

            var transaction = (bigchainDatabase.GetCollection<Transactions<object>>("transactions").AsQueryable());
            var transactionsReduced = transaction.Where(x => x.id == transactionId);
            transactionsReduced = transactionsReduced.Select(r => new Transactions<object> { id = r.id, asset = r.asset, _id = r._id });

            var r = from t1 in metadataReduced
                    join t2 in transactionsReduced on t1.id equals t2.id
                    join t3 in assets on 1 equals 1
                    where (t2.id == t3.id || (t2.asset != null && t2.asset.id == t3.id))
                    select new AssetsMetadatas<A, M>()
                    {
                        id = t3.id,
                        data = JsonConvert.DeserializeObject<AssetSaved<A>>(JsonConvert.SerializeObject(t3.data)),
                        metadata = new MetaDataSaved<M>
                        {
                            AccessList = new Dictionary<string, string>(t1.metadata.metadata.AccessList),
                            data = JsonConvert.DeserializeObject<M>(JsonConvert.SerializeObject(t1.metadata.metadata.data))
                        },
                        transID = t2.id
                    };
            var result = r.FirstOrDefault();
            return result;
        }

        // This function is similar to 

        public string SendCreateTransactionToDataBase<A,M>(AssetSaved<A> asset, MetaDataSaved<M> metaData, string privateSignKey)
        {
            var signPrivateKey = EncryptionService.getSignKeyFromPrivate(privateSignKey);

            var transaction = BigchainDbTransactionBuilder<AssetSaved<A>, MetaDataSaved<M>>
                .init()
                .addAssets(asset)
                .addMetaData(metaData)
                .operation(Operations.CREATE)
                .buildAndSignOnly(signPrivateKey.PublicKey, signPrivateKey);

            var createTransaction = TransactionsApi<AssetSaved<A>, MetaDataSaved<M>>.sendTransactionAsync(transaction).GetAwaiter().GetResult();
            return createTransaction.Data.Id;
        }

        public string SendCreateTransferTransactionToDataBase<A, M>(AssetSaved<A> asset, MetaDataSaved<M> metaData, string senderPrivateSignKey, string recieverPublicSignKey)
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
            return createTransaction.Data.Id;
        }

        public string SendTransferTransactionToDataBase<M>(string assetID, MetaDataSaved<M> metaData, 
            string senderPrivateSignKey, string recieverPublicSignKey, string inputTransID)
        {
            var senderSignPrivateKey = EncryptionService.getSignKeyFromPrivate(senderPrivateSignKey);
            var recieverSignPublicKey = EncryptionService.getSignPublicKeyFromString(recieverPublicSignKey);

            // Using TRANSFER: START
            // add input
            var input = new Omnibasis.BigchainCSharp.Model.FulFill();
            input.TransactionId = inputTransID;
            input.OutputIndex = 0;
            Details details = null;

            // transfer transaction
            // assetId is the transactionId to the asset we want to change
            var transaction = BigchainDbTransactionBuilder<Asset<string>, MetaDataSaved<M>>
                .init()
                .addAssets(assetID)
                .addMetaData(metaData)
                .addInput(details, input, senderSignPrivateKey.PublicKey)
                .addOutput("1", recieverSignPublicKey)
                .operation(Operations.TRANSFER)
                .buildAndSignOnly(senderSignPrivateKey.PublicKey, senderSignPrivateKey);

            var createTransaction = TransactionsApi<Asset<string>, MetaDataSaved<M>>.sendTransactionAsync(transaction).GetAwaiter().GetResult();
            return createTransaction.Data.Id;
        }

        public List<AssetsMetadatas<A,M>> GetAllTypeRecordsFromDPublicPPublicKey<A,M>
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
            var metadata = bigchainDatabase.GetCollection<Metadatas<object>>("metadata").AsQueryable();
            var metadataReduced = metadata.Where(x => transactionIDs.Contains(x.id));
            List<Metadatas<object>> metadataList = new List<Metadatas<object>>();
            foreach (var a in metadataReduced)
            {
                if (a.metadata.metadata.AccessList != null 
                    && a.metadata.metadata.AccessList.Keys.ToList().Contains(doctorSignPublicKey)
                    && a.metadata.metadata.AccessList.Keys.ToList().Contains(patientSignPublicKey))
                {
                    metadataList.Add(a);
                }
            }

            var transaction = (bigchainDatabase.GetCollection<Transactions<string>>("transactions").AsQueryable());
            var transactionsReduced = transaction.Where(x => transactionIDs.Contains(x.id));
            transactionsReduced = transactionsReduced.Select(r => new Transactions<string> { id = r.id, asset = r.asset, _id = r._id});
            var r = from t1 in metadataList
                    join t2 in transactionsReduced on t1.id equals t2.id
                    join t3 in TypeAssets on 1 equals 1
                    where (t2.id == t3.id || (t2.asset != null && t2.asset.id == t3.id))
                    select new AssetsMetadatas<A,M>()
                    {
                        id = t3.id,
                        data = t3.data,
                        metadata = new MetaDataSaved<M> { 
                            AccessList = new Dictionary<string, string>(t1.metadata.metadata.AccessList),
                            data = JsonConvert.DeserializeObject<M>(JsonConvert.SerializeObject(t1.metadata.metadata.data))
                        },
                        transID = t2.id
                    };
            return r.ToList();
        }

        public List<AssetsMetadatas<A, M>> GetAllTypeRecordsFromPPublicKey<A,M>
    (AssetType type, string patientSignPublicKey)
        {
            // get unspent outputs of the patient, this means get all outputs that he owns
            var rawPublicKey = EncryptionService.getRawBase58PublicKey(patientSignPublicKey);
            var unspentOutsList = OutputsApi.getUnspentOutputsAsync(rawPublicKey).GetAwaiter().GetResult();
            List<string> transactionIDs = new List<string>();
            foreach (var unspentOutput in unspentOutsList)
            {
                transactionIDs.Add(unspentOutput.TransactionId);
            }
            var assets = bigchainDatabase.GetCollection<Assets<A>>("assets").AsQueryable();
            var TypeAssets = assets.Where(x => x.data.Type == type);
            // reduce to get only metadata where both public keys are present and is in the unspent list
            var metadata = bigchainDatabase.GetCollection<Metadatas<object>>("metadata").AsQueryable();
            var metadataReduced = metadata.Where(x => transactionIDs.Contains(x.id));
            List<Metadatas<object>> metadataList = new List<Metadatas<object>>();
            foreach (var a in metadataReduced)
            {
                if (a.metadata.metadata.AccessList != null
                    && a.metadata.metadata.AccessList.Keys.ToList().Contains(patientSignPublicKey))
                {
                    metadataList.Add(a);
                }
            }

            var transaction = (bigchainDatabase.GetCollection<Transactions<string>>("transactions").AsQueryable());
            var transactionsReduced = transaction.Where(x => transactionIDs.Contains(x.id));
            transactionsReduced = transactionsReduced.Select(r => new Transactions<string> { id = r.id, asset = r.asset, _id = r._id });
            var r = from t1 in metadataList
                    join t2 in transactionsReduced on t1.id equals t2.id
                    join t3 in TypeAssets on 1 equals 1
                    where (t2.id == t3.id || (t2.asset != null && t2.asset.id == t3.id))
                    select new AssetsMetadatas<A, M>()
                    {
                        id = t3.id,
                        data = t3.data,
                        metadata = new MetaDataSaved<M>
                        {
                            AccessList = new Dictionary<string, string>(t1.metadata.metadata.AccessList),
                            data = JsonConvert.DeserializeObject<M>(JsonConvert.SerializeObject(t1.metadata.metadata.data))
                        },
                        transID = t2.id
                    };
            return r.ToList();
        }

        public Models.Assets<UserCredAssetData> GetUserAssetFromTypeID(AssetType assetType, string id)
        {
            var assets = bigchainDatabase.GetCollection<Models.Assets<UserCredAssetData>>("assets").AsQueryable();
            var asset = from a in assets where a.data.Type == assetType && a.data.Data.ID == id select a;
            if (asset.Any())
                return asset.FirstOrDefault();
            else
                return null;
        }

        public Models.Assets<PatientCredAssetData> GetPatientAssetFromID( string id)
        {
            var assets = bigchainDatabase.GetCollection<Models.Assets<PatientCredAssetData>>("assets").AsQueryable();
            var asset = from a in assets where a.data.Type == AssetType.Patient && a.data.Data.ID == id select a;
            if (asset.Any())
                return asset.FirstOrDefault();
            else
                return null;
        }

        public List<string> GetAllTypeIDs(AssetType assetType)
        {
            // Search for all patients in database
            // get all assets
            var assets = bigchainDatabase.GetCollection<Models.Assets<UserCredAssetData>>("assets").AsQueryable();
            var patients = assets.Where(a => a.data.Type == assetType).Select(a => a.data.Data.ID);
            return patients.ToList();    
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

        public MetaData<MetaDataSaved<M>> GetMetadataIDFromAssetPublicKey<M>(string assetID, string signPublicKey)
        {
            var rawPublicKey = EncryptionService.getRawBase58PublicKey(signPublicKey);
            var unspentOutsList = OutputsApi.getUnspentOutputsAsync(rawPublicKey).GetAwaiter().GetResult();
            var assetTransactions = TransactionsApi<object, MetaDataSaved<M>>.getTransactionsByAssetIdAsync(assetID).GetAwaiter().GetResult();
            var neededTransaction = from a in unspentOutsList.AsQueryable()
                                    join b in assetTransactions.AsQueryable()
                                    on a.TransactionId equals b.Id
                                    select new MetaData<MetaDataSaved<M>> { 
                                    Id = b.Id,
                                    Metadata = b.MetaData.Metadata
                                    };
            var result = neededTransaction.FirstOrDefault();
            return result;
        }

        public List<UserInfo> GetUserInfoList(string[] userSignPublicKeyList)
        {
            // Search for all users in database
            // get all assets
            var assets = bigchainDatabase.GetCollection<Models.Assets<UserCredAssetData>>("assets").AsQueryable();
            var patients = assets.Where(a => userSignPublicKeyList.Contains(a.data.Data.SignPublicKey));
            List<UserInfo> userInfoList = new List<UserInfo>();
            foreach ( var patient in patients)
            {
                UserInfo user = new UserInfo()
                {
                    UserID = patient.data.Data.ID,
                    UserName = patient.data.Data.FirstName + " " + patient.data.Data.LastName
                };
                user.UserType = patient.data.Type == AssetType.Doctor ? "Doctor" : patient.data.Type == AssetType.Pharmacist ? "Pharmacist" : "MLT";
                userInfoList.Add(user);
            }
            return userInfoList;
        }

        public List<AssetsMetadatas<A, object>> GetAllTypeRecordsFromPPublicKey<A>
            (AssetType[] recordTypes, string patientSignPublicKey)
        {
            // get unspent outputs of the patient, this means get all outputs that he owns
            var rawPublicKey = EncryptionService.getRawBase58PublicKey(patientSignPublicKey);
            var unspentOutsList = OutputsApi.getUnspentOutputsAsync(rawPublicKey).GetAwaiter().GetResult();
            List<string> transactionIDs = new List<string>();
            foreach (var unspentOutput in unspentOutsList)
            {
                transactionIDs.Add(unspentOutput.TransactionId);
            }
            var assets = bigchainDatabase.GetCollection<Assets<A>>("assets").AsQueryable();
            var TypeAssets = assets.Where(x => recordTypes.Contains(x.data.Type));
            // reduce to get only metadata where both public keys are present and is in the unspent list
            var metadata = bigchainDatabase.GetCollection<Metadatas<object>>("metadata").AsQueryable();
            var metadataReduced = metadata.Where(x => transactionIDs.Contains(x.id));
            List<Metadatas<object>> metadataList = new List<Metadatas<object>>();
            foreach (var a in metadataReduced)
            {
                if (a.metadata.metadata.AccessList != null
                    && a.metadata.metadata.AccessList.Keys.ToList().Contains(patientSignPublicKey))
                {
                    metadataList.Add(a);
                }
            }

            var transaction = (bigchainDatabase.GetCollection<Transactions<string>>("transactions").AsQueryable());
            var transactionsReduced = transaction.AsQueryable().Where(x => transactionIDs.Contains(x.id));
            transactionsReduced = transactionsReduced.Select(r => new Transactions<string> { id = r.id, asset = r.asset, _id = r._id });
            var r = from t1 in metadataList
                    join t2 in transactionsReduced on t1.id equals t2.id
                    join t3 in TypeAssets on 1 equals 1
                    where (t2.id == t3.id || (t2.asset != null && t2.asset.id == t3.id))
                    select new AssetsMetadatas<A, object>()
                    {
                        id = t3.id,
                        data = t3.data,
                        metadata = t1.metadata.metadata,
                        transID = t2.id
                    };
            return r.ToList();
        }
    }
}
