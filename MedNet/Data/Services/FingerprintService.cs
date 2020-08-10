using PatternRecognition.FingerprintRecognition.FeatureExtractors;
using PatternRecognition.FingerprintRecognition.Matchers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MedNet.Data.Services
{
    public static class FingerprintService
    {
        // Description: This class performs the available functions: authenticateFP and compareFP
        private static Int32 PORT = 15326; // Actual port

        private static string START = "MEDNETFP:START"; // Special MedNetFP Key
        private static string STOP = "MEDNETFP:STOP"; // Special MedNetFP STOP Key
        private static string VERIFY = "MEDNETFP:VERIFY";
        private static string VALID = "MEDNETFP:VALID";
        private static string DELIM = Convert.ToBase64String(Encoding.ASCII.GetBytes("MEDNET")); // Delimiter for FP

        public static List<Image> authenticateFP(string ipAddr, int numScans)
        {
            // Description: Carries out fingerprint authentication
            bool debug = false; 
            bool isConnected = tcpConnect(ipAddr, debug, out TcpClient tcpClient);
            int numScansLeft = numScans;
            List<Image> fpList = new List<Image>();
            while (numScansLeft > 0)
            {
                List<Image> rxList = scanMultiFP(ipAddr, numScans, tcpClient, debug);
                foreach (Image img in rxList)
                {
                    fpList.Add(img);
                }
                numScansLeft = 0; 
            }
            tcpDisconnect(tcpClient);
            return fpList;
        }

        public static Image byteToImg(byte[] fpData)
        {
            MemoryStream ms = new MemoryStream(fpData);
            Image img = Image.FromStream(ms);
            return img;
        }

        public static byte[] imgToByte(Image img)
        {
            ImageConverter conv = new ImageConverter();
            return (byte[])conv.ConvertTo(img, typeof(byte[]));
        }

        public static bool compareFP(Image inFp, List<Image> dbFp)
        {
            // Description: compares the scanned fingerprint to all of the ones in the database
            bool isMatch = false;
            double matchTol = 0.4;

            // Build feature extractor, and extract features of each fingerprint image
            MTripletsExtractor featExtract = new MTripletsExtractor() { MtiaExtractor = new Ratha1995MinutiaeExtractor() };
            var inFeat = featExtract.ExtractFeatures(new Bitmap(inFp));

            // Build matcher
            M3gl matcher = new M3gl();

            // Compare scanned image to all the ones in the database
            int numFp = dbFp.Count;
            //inFp.Save("in.bmp"); testing
            for (int i = 0; i < numFp; i++)
            {
                // Convert dbFp to Bitmap image object
                // DEBUG: save to file

                // Extract features of dbBmp
                var dbFeat = featExtract.ExtractFeatures(new Bitmap(dbFp[i]));

                // Run similarity check
                var match = matcher.Match(inFeat, dbFeat);
                //dbFp[i].Save("dbFP-"+i.ToString()+".bmp"); testing
                if (match >= matchTol)
                {
                    // Fingerprints have above 0.5 similarity
                    isMatch = true;
                    Console.WriteLine("Similarity: ", match); // Debug
                    break; // Comment for debug
                }
            }

            return isMatch;
        }

        private static bool tcpConnect(string ipAddr, bool debug, out TcpClient client)
        {
            ///Description: Create and validate connection to TCP client and stream
            // initialize variables
            bool result = false;
            client = new TcpClient();
            if (debug) { ipAddr = "localhost"; }
            byte[] rxData = new byte[0];
            try
            {
                // Setup client and stream
                client = new TcpClient(ipAddr, PORT);
                NetworkStream stream = client.GetStream();

                // Verify connection to stream
                string msg = ipAddr + "|" + VERIFY;
                byte[] wrBuf = Encoding.UTF8.GetBytes(msg);
                stream.Write(wrBuf);

                // Wait for valid response
                byte[] rdBuf = new byte[client.ReceiveBufferSize];

                if (stream.CanRead)
                {
                    int numBytesRd = stream.Read(rdBuf);
                    byte[] bytesRd = new byte[numBytesRd];
                    Array.Copy(rdBuf, bytesRd, numBytesRd);
                    if (numBytesRd > 0)
                    {
                        rxData = rxData.Concat(bytesRd).ToArray();
                        if (Encoding.ASCII.GetString(rxData) == VALID)
                        {
                            result = true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: TCP Connection failure to Client Computer");
                }

                // Parse response from Client Computer
                Console.WriteLine("here");
                string bStr = Encoding.ASCII.GetString(rxData);
            }
            catch (ArgumentException e) { Console.WriteLine("ArgumentNullException: {0}", e); }
            catch (SocketException e) { Console.WriteLine("SocketException: {0}", e); }

            return result;
        }

        private static List<Image> scanMultiFP(string ipAddr, int numScans, TcpClient client, bool debug)
        {
            ///Description: Scan fingerprints
            ///
            // Initialize variables
            List<Image> fpList = new List<Image>();
            if (debug) { ipAddr = "localhost"; }
            byte[] rxData = new byte[0];
            string msg = ipAddr + "|" + START + "|" + numScans;
            byte[] wrBuf = Encoding.UTF8.GetBytes(msg);
            int totalBytesRd = 0;
            int numBytesRd = 0;
            List<string> inList = new List<string>();
            bool readMore = true;
            try
            {
                // Get previous stream and send formatted message
                NetworkStream stream = client.GetStream();
                stream.Write(wrBuf);

                // Wait for valid response
                byte[] rdBuf = new byte[client.ReceiveBufferSize];

                if (stream.CanRead)
                {
                    do
                    {
                        numBytesRd = stream.Read(rdBuf);
                        if (numBytesRd > 0)
                        {
                            byte[] bytesRd = new byte[numBytesRd];
                            Array.Copy(rdBuf, bytesRd, numBytesRd);
                            string test = Encoding.ASCII.GetString(bytesRd);
                            if (Encoding.ASCII.GetString(bytesRd).Contains(STOP))
                            {
                                int newLen = numBytesRd - STOP.Length;
                                bytesRd = new byte[newLen];
                                Array.Copy(rdBuf, bytesRd, newLen);
                                readMore = false;
                            }
                            rxData = rxData.Concat(bytesRd).ToArray();
                            totalBytesRd += numBytesRd;
                        }
                    } while (readMore);
                }
            }
            catch (ArgumentException e) { Console.WriteLine("ArgumentNullException: {0}", e); }
            catch (SocketException e) { Console.WriteLine("SocketException: {0}", e); }

            // Parse data from Client Computer
            string bStr = Encoding.ASCII.GetString(rxData);
            inList = bStr.Split(DELIM).ToList();

            // Client IP
            var debugClientIP = Convert.FromBase64String(inList[0]);

            // Fingerprint Data
            for (int i = 1; i < inList.Count; i++)
            {
                Span<byte> buffer = new Span<byte>(new byte[inList[i].Length]);
                Convert.TryFromBase64String(inList[i], buffer, out int bytesParsed);
                if (bytesParsed > 0)
                {
                    byte[] fpByte = Convert.FromBase64String(inList[i]);
                    Image fpImg = byteToImg(fpByte);
                    fpList.Add(fpImg);
                }
            }

            return fpList;
        }

        private static void tcpDisconnect(TcpClient client)
        {
            ///Description: Close the TCP connection to the Client Computer
            NetworkStream stream = client.GetStream();
            try
            {
                stream.Close();
                client.Close();
            }
            catch (ArgumentException e) { Console.WriteLine("ArgumentNullException: {0}", e); }
            catch (SocketException e) { Console.WriteLine("SocketException: {0}", e); }

            return;
        }
    }
}