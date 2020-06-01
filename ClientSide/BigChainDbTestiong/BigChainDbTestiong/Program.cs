using System;
using BigChainDbTestiong.Models;
using BigChainDbTestiong.Services;
using System.Linq;
using System.Collections.Generic;

namespace BigChainDbTestiong
{
    class Program
    {
        private static string userListId;
        private static string _publicKeyString = "302a300506032b657003210033c43dc2180936a2a9138a05f06c892d2fb1cfda4562cbc35373bf13cd8ed373";
        private static string _privateKeyString = "302e020100300506032b6570042204206f6b0cd095f1e83fc5f08bffb79c7c8a30e77a3ab65f4bc659026b76394fcea8";
        
        public static void Main()
        {
            Console.WriteLine("Starting MedNet Database test");
            var random = new Random();
            var bigChainDbServices = new BigChainDbServices("https://anode.lifeblocks.site");
            var fingerprintServices = new FingerprintServices(); // just creates the object

            userListId = bigChainDbServices.GetUserListAsset(_publicKeyString)[0]; // only use the first one

            // create new docotr note
            var asset = new DoctorNoteAsset
            {
                data = "encrypted data, the AES symmetric key should be in the metadata",
                randomNoteID = random.Next(60, 80)
            };

            var metaData = new DoctorNoteMetadata();
            metaData.accessList[_publicKeyString] = "AES symmetric Key for encrypted data stored encrypted by asymmetric public key of user";

            //var transactions = bigChainDbServices.GetAllDoctorNotesFromPublicKey(_publicKeyString);

            //bigChainDbServices.SendTransactionToDataBase(asset, metaData, _publicKeyString, _privateKeyString);

            bool done = false;
            while(!done)
            {
                Console.Write("1. Add user\n" +
                    "2. Get print user list\n" +
                    "3. Verify user\n");
                Console.WriteLine("0. Exit");
                Console.Write(">> ");
                string select = Console.ReadLine();

                if (select == "1")
                {
                    Console.WriteLine("Adding User");
                    fingerprintServices.AddUser(userListId);
                }
                else
                {
                    return;
                }

            }

/*            // Connect to database
            string test_node = "https://test.ipdb.io"; // use with single-node setup
            string anode = "https://anode.lifeblocks.site";
            string bnode = "https://bnode.lifeblocks.site";
            string cnode = "https://cnode.lifeblocks.site";
            List<string> Nodes = new List<string>();
            Nodes.Add(anode);
            Nodes.Add(bnode);
            Nodes.Add(cnode);

            //bigChainDbServices.SingleNodeSetup(anode);
            bigChainDbServices.SingleNodeSetup(anode);*/

            //bigChainDbServices.MultiNodeSetup(Nodes);

            

            //var transactions = bigChainDbServices.GetAllDoctorNotesFromPublicKey(_publicKeyString);

/*            var transactions = bigChainDbServices.GetAllTransactionsFromPublicKey(_publicKeyString, _privateKeyString);
            foreach(var transaction in transactions)
            {
                var transactionInformation = bigChainDbServices.GetMetaDataAndAssetFromTransactionId(transaction);
                Console.WriteLine("done");
            }*/



        }
    }
}
