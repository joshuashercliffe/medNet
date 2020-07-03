using Org.BouncyCastle.Asn1.Esf;
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

        private static string START = "MEDNETFP:START"; // Special MedNetFP Key
        private static string STOP = "MEDNETFP:STOP"; // Special MedNetFP STOP Key
        private static string VERIFY = "MEDNETFP:VERIFY";
        private static string VALID = "MEDNETFP:VALID";
        private static string DELIM = Convert.ToBase64String(Encoding.ASCII.GetBytes("MEDNET")); // Delimiter for FP

        public static List<Image> authenticateFP(string server, int numScans)
        {
            // Description: Combines functions together
            // case 1: separated
            bool debug = false; // JW: DEBUGFP
            bool isConnected = tcpConnect(server, debug, out TcpClient tcpClient);
            int numScansLeft = numScans;
            List<Image> fpList = new List<Image>();
            while (numScansLeft > 0)
            {
                List<Image> rxList = scanMultiFP(server, numScans, tcpClient, debug);
                foreach (Image img in rxList)
                {
                    fpList.Add(img);
                }
                //numScansLeft -= fpList.Count;
                numScansLeft = 0; // Make this more robust
            }

            // case 2: together
            //List<Image> fpList = scanMultiFPtest(server, numScans, out _);
            return fpList;
        }

        public static bool compareFP(Image inFp, List<Image> dbFp)
        {
            // description: compares the scanned fingerprint to all of the ones in the database
            bool isMatch = false;

            // DEBUG: save to file
            //inFp.Save("inFP.jpeg");

            // Build feature extractor, and extract features of each fingerprint image
            MTripletsExtractor featExtract = new MTripletsExtractor() { MtiaExtractor = new Ratha1995MinutiaeExtractor() };
            var inFeat = featExtract.ExtractFeatures(new Bitmap(inFp));

            // Build matcher
            M3gl matcher = new M3gl();

            // Compare scanned image to all the ones in the database 
            int numFp = dbFp.Count;
            for(int i = 0; i < numFp; i++)
            {
                // Convert dbFp to Bitmap image object
                Image dbImg = dbFp[i];
                // DEBUG: save to file
                int j = i + 1;
                //dbImg.Save("dbFP" + j.ToString() + ".bmp");

                // Extract features of dbBmp 
                var dbFeat = featExtract.ExtractFeatures(new Bitmap(dbImg));

                // Run similarity check 
                var match = matcher.Match(inFeat, dbFeat);
                if(match >= 0.4)
                {
                    // Fingerprints have above 0.5 similarity
                    isMatch = true;
                    Console.WriteLine("Similarity: ", match); // Debug
                    //break; // Comment for debug
                }
            }

            return isMatch;
        }

        public static void saveFP(List<Image> fpList)
        {
            // Description: saves the fingerprints as images
            int numFp = fpList.Count;
            for (int i = 0; i < numFp; i++)
            {
                // Save to file
                int j = i + 1;
                fpList[i].Save("FP" + j.ToString() + ".bmp");
            }
            return;
        }

        public static Image scanFPtest(String server, out int bytesRead)
        {
            // Description: Only scan fingerprint once
            List<Image> fpList = scanMultiFPtest(server, 1, out bytesRead);
            return fpList[0];
        }

        public static List<Image> scanMultiFPtest(String server, int numScans, out int totalBytesRead)
        {
            // Description: Scan fingerprint multiple times using one request to the client computer
            List<Image> fpList = new List<Image>(); // the resulting fingerprint images
            List<string> inList = new List<string>();
            string bDelim = Convert.ToBase64String(Encoding.ASCII.GetBytes(DELIM));
            totalBytesRead = 0;
            int bytesRead = 0;
            byte[] rdBytes = new byte[0];
            // Connect to Specified Client IP and send message with number of scans todo
            try
            {
                // Connect to TCP Client
                // Convert message to bytearray using UTF-8 
                TcpClient client = new TcpClient("localhost", PORT); // DebugFP
                string tcpMsg = "24.84.225.22" + "|" + START + "|" + numScans.ToString(); // DebugFP

                //TcpClient client = new TcpClient(server, PORT); // Actual
                //string tcpMsg = server + "|" + START + "|" + numScans.ToString(); // Actual

                byte[] wrBuf = System.Text.Encoding.UTF8.GetBytes(tcpMsg);

                // Get a client RD/WR Stream 
                NetworkStream tcpStream = client.GetStream();

                // Send the message to the Client Computer (TCP Server) 
                tcpStream.Write(wrBuf, 0, wrBuf.Length);
                Console.WriteLine("Sent: {0}", tcpMsg);

                // Read the bytes from the buffer
                byte[] rdBuf = new byte[client.ReceiveBufferSize];
                if (tcpStream.CanRead)
                {
                    do
                    {
                        // Read the bytes from the buffer 
                        bytesRead = tcpStream.Read(rdBuf, 0, rdBuf.Length);
                        byte[] temp = new byte[bytesRead];
                        Array.Copy(rdBuf, temp, bytesRead);
                        
                        if (bytesRead > 0)
                        {
                            // Concat the bytes into a bytearray 
                            totalBytesRead += bytesRead;
                            rdBytes = rdBytes.Concat(temp).ToArray();
                        }
                        else
                        {
                            Console.WriteLine("here");
                        }
                    }
                    while (bytesRead > 0 && client.Connected);

                    // Close the stream and socket connections to client
                    tcpStream.Close();
                    client.Close();
                }
                else
                {
                    Console.WriteLine("Error: The TCP Stream has closed.");
                }

                // Encode data as base64 before sending over TCP connection
                string bStr = Encoding.ASCII.GetString(rdBytes);
                inList = bStr.Split(bDelim).ToList();

                // Client IP
                var debugClientIP = Convert.FromBase64String(inList[0]);
               
                // Fingerprint data
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

        public static bool tcpConnect(String server, bool debug, out TcpClient client)
        {
            ///Description: Create and validate connection to TCP client and stream
            // initialize variables
            bool result = false;
            client = new TcpClient();             
            if (debug) { server = "localhost"; }
            byte[] rxData = new byte[0];
            try
            {
                // Setup client and stream
                client = new TcpClient(server, PORT);
                NetworkStream stream = client.GetStream();

                // Verify connection to stream
                string msg = server + "|" + VERIFY;
                byte[] wrBuf = Encoding.UTF8.GetBytes(msg);
                stream.Write(wrBuf);

                // Wait for valid response
                byte[] rdBuf = new byte[client.ReceiveBufferSize];

                if (stream.CanRead)
                {
                    int numBytesRd = stream.Read(rdBuf);
                    byte[] bytesRd = new byte[numBytesRd];
                    Array.Copy(rdBuf, bytesRd, numBytesRd);
                    if(numBytesRd > 0) 
                    { 
                        rxData = rxData.Concat(bytesRd).ToArray();
                        if(Encoding.ASCII.GetString(rxData) == VALID)
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

        public static List<Image> scanMultiFP(string server, int numScans, TcpClient client, bool debug)
        {
            ///Description: Scan fingerprints
            ///
            // Initialize variables
            List<Image> fpList = new List<Image>();
            if (debug) { server = "localhost"; }
            byte[] rxData = new byte[0];
            string msg = server + "|" + START + "|" + numScans;
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

                if(stream.CanRead)
                {
                    do
                    {
                        numBytesRd = stream.Read(rdBuf);
                        if (numBytesRd > 0)
                        {
                            byte[] bytesRd = new byte[numBytesRd];
                            Array.Copy(rdBuf, bytesRd, numBytesRd);
                            string test = Encoding.ASCII.GetString(bytesRd);
                            if(Encoding.ASCII.GetString(bytesRd).Contains(STOP))
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

        public static void tcpDisconnect(TcpClient client)
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
    }
}
