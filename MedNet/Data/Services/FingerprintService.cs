using PatternRecognition.FingerprintRecognition.FeatureExtractors;
using PatternRecognition.FingerprintRecognition.Matchers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MedNet.Data.Services
{
    public static class FingerprintService
    {
        // Class variables 
        private static Int32 PORT = 15326; // Actual port
        //private static Int32 PORT = 15327; // DEBUG port

        private static string startMsg = "MEDNETFP:START"; // Special MedNetFP Key
        private static string endMsg = "MEDNETFP:STOP"; // Special MedNetFP STOP Key
        private static string delim = "MEDNET"; // Delimiter for FP
        
        public static bool compareFP2(byte[] inputFingerprint, byte[] databaseFingerprint )
        {
            // old
            bool isMatch = false;
            // Convert fingerprint byte arrays into Bitmap image objects
            var incomingImage1 = Image.FromStream(new MemoryStream(inputFingerprint));
            var bitmap1 = new Bitmap(incomingImage1);

            var incomingImage2 = Image.FromStream(new MemoryStream(databaseFingerprint));
            var bitmap2 = new Bitmap(incomingImage2);

            var fingerprintImg1 = bitmap1;
            var fingerprintImg2 = bitmap2;

            // Building feature extractor and extracting features
            var featExtractor = new MTripletsExtractor() { MtiaExtractor = new Ratha1995MinutiaeExtractor() };
            var features1 = featExtractor.ExtractFeatures(fingerprintImg1);
            var features2 = featExtractor.ExtractFeatures(fingerprintImg2);

            // Building matcher and matching
            var matcher = new M3gl();
            var similarity = matcher.Match(features1, features2);

            // Check if similarity is greater than 0.40
            if(similarity >= 0.40)
            {
                isMatch = true;   
            }

            // Return the comparison result 
            return isMatch;
        }

        public static bool compareFP(byte[] inFp, List<byte[]> dbFp)
        {
            // description: compares the scanned fingerprint to all of the ones in the database
            bool isMatch = false;

            // Convert scanned fingerprint bytearray into Bitmap image object
            Image inImg = Image.FromStream(new MemoryStream(inFp));
            Bitmap inBmp = new Bitmap(inImg);

            // DEBUG: save to file
            //inBmp.Save("inFP.bmp");

            // Build feature extractor, and extract features of each fingerprint image
            MTripletsExtractor featExtract = new MTripletsExtractor() { MtiaExtractor = new Ratha1995MinutiaeExtractor() };
            var inFeat = featExtract.ExtractFeatures(inBmp);

            // Build matcher
            M3gl matcher = new M3gl();

            // Compare scanned image to all the ones in the database 
            int numFp = dbFp.Count;
            for(int i = 0; i < numFp; i++)
            {
                // Convert dbFp to Bitmap image object
                Image dbImg = Image.FromStream(new MemoryStream(dbFp[i]));
                Bitmap dbBmp = new Bitmap(dbImg);

                // DEBUG: save to file
                int j = i + 1;
                //dbBmp.Save("dbFP" + j.ToString() + ".bmp");

                // Extract features of dbBmp 
                var dbFeat = featExtract.ExtractFeatures(dbBmp);

                // Run similarity check 
                var match = matcher.Match(inFeat, dbFeat);
                if(match >= 0.4)
                {
                    // Fingerprints have above 0.5 similarity
                    isMatch = true;
                    Console.WriteLine("Similarity: ", match); // Debug
                    break;
                }
            }

            return isMatch;
        }

        public static void saveFP(List<byte[]> fpList)
        {
            // Description: saves the fingerprints as images
            int numFp = fpList.Count;
            for (int i = 0; i < numFp; i++)
            {
                // Convert dbFp to Bitmap image object
                Image dbImg = Image.FromStream(new MemoryStream(fpList[i]));
                Bitmap dbBmp = new Bitmap(dbImg);

                // DEBUG: save to file
                int j = i + 1;
                dbBmp.Save("FP" + j.ToString() + ".bmp");
            }

            return;
        }

        public static byte[] scanFP(String server, out int bytesRead)
        {
            // Description: Only scan fingerprint once
            var fpList = scanMultiFP(server, 1, out bytesRead);
            return fpList[0];
        }

        public static List<byte[]> scanMultiFP(String server, int numScans, out int totalBytesRead)
        {
            // Description: Scan fingerprint multiple times using one request to the client computer
            List<byte[]> fpList = new List<byte[]>(); // the resulting fingerprint images
            totalBytesRead = 0;
            int bytesRead = 0;
            byte[] rdBytes = new byte[0];
            // Connect to Specified Client IP and send message with number of scans todo
            try
            {
                // Connect to TCP Client
                //TcpClient client = new TcpClient(server, PORT); // Actual
                TcpClient client = new TcpClient("localhost", PORT); // DebugFP

                // Convert message to bytearray using UTF-8 
                //string tcpMsg = server + "|" + startMsg + "|" + numScans.ToString(); // Actual
                string tcpMsg = "24.84.225.22" + "|" + startMsg + "|" + numScans.ToString(); // DebugFP
                byte[] wrBuf = System.Text.Encoding.UTF8.GetBytes(tcpMsg);

                // Get a client RD/WR Stream 
                NetworkStream tcpStream = client.GetStream();

                // Send the message to the Client Computer (TCP Server) 
                tcpStream.Write(wrBuf, 0, wrBuf.Length);
                Console.WriteLine("Sent: {0}", tcpMsg);

                // Read the bytes from the buffer 
                //byte[] rdBuf = new byte[65536]; // max TCP packet size
                byte[] rdBuf = new byte[1024];
                if (tcpStream.CanRead)
                {
                    do
                    {
                        // Read the bytes from the buffer 
                        bytesRead = tcpStream.Read(rdBuf, 0, rdBuf.Length);
                        totalBytesRead += bytesRead;

                        // Concat the bytes into a bytearray 
                        rdBytes = rdBytes.Concat(rdBuf).ToArray();

                        if (bytesRead == 0)
                        {
                            Console.WriteLine("here");
                        }
                    }
                    //while (bytesRead > 0 && client.Connected);
                    while (bytesRead > 0 && client.Connected);

                    // Close the stream and socket connections to client
                    tcpStream.Close();
                    client.Close();
                }
                else
                {
                    //Console.WriteLine("Error: The TCP Stream has closed.");
                }

                // Decode the incoming data
                // Convert from base64 to bytearray
                string b64Str = Encoding.ASCII.GetString(rdBytes);
                string b64Delim = Convert.ToBase64String(Encoding.ASCII.GetBytes(delim));
                var inList = b64Str.Split(b64Delim).ToList();

                // Client IP
                //var debugClientIP = Convert.FromBase64String(inList[0]);

                // Fingerprint data
                for (int i = 1; i < inList.Count; i++)
                {
                    byte[] fpByte = Convert.FromBase64String(inList[i]);
                    fpList.Add(fpByte);
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            return fpList;
        }

        public static string listToString(List<byte[]> fpList)
        {
            // Description: Converts a list to a string of fingerprint data
            string fpStr = "";
            for(int i = 0; i < fpList.Count; i++)
            {
                // convert fp data to base64 
                string b64fp = Convert.ToBase64String(fpList[i]);
                fpStr += b64fp; 
                if(i != fpList.Count - 1)
                {
                    fpStr += delim;
                }
            }
            return fpStr;
        }

        public static List<byte[]> stringToList(string fpStr)
        {
            // Description: Converts a string to list of fingerprint data
            List<byte[]> fpList = new List<byte[]>();
            var parsedFpData = fpStr.Split(delim);
            foreach(var parsedFp in parsedFpData)
            {
                // Convert base64 to byte array 
                byte[] fpData = Convert.FromBase64String(parsedFp);
                fpList.Add(fpData);
            }

            return fpList;
        }

        public static byte[] bmpToByte(Bitmap img)
        {
            // Description: Converts a bmp image to a byte array
            ImageConverter conv = new ImageConverter();
            return (byte[])conv.ConvertTo(img, typeof(byte[]));
        }

        public static Bitmap byteToBmp(byte[] fpData)
        {
            // Description: Converts a byte array into a Bitmap object
            MemoryStream ms = new MemoryStream(fpData);
            Image img = Image.FromStream(ms);
            return new Bitmap(img);
        }
    }
}
