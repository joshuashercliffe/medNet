using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Drawing;

namespace MedNet.Data.Services
{
    public class SocketService
    {
        public SocketService()
        {
            // Empty Constructor
            return;
        }

        public static byte[] tcpConnect(String server, String message, out int numBytesRead)
        {
            // link: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=netframework-4.8
            // Connect to Specified ClientIP (server), and send message to start FP Service 
            Int32 port = 15326; // Use a specific port
            Byte[] tcpData = new byte[1024];
            byte[] fpByte = new byte[59707]; // for 227*257 img size incl header
            numBytesRead = 0;
            try
            {
                // Connect to the TCP Client 
                TcpClient client = new TcpClient(server, port);

                // Convert the message to a bytearray using UTF-8 
                String tcpMsg = server + "|" + message;
                Byte[] wrBuf = System.Text.Encoding.UTF8.GetBytes(tcpMsg);

                // Get a client stream for reading and writing.
                NetworkStream tcpStream = client.GetStream();

                // Send the message to the connected TcpServer. 
                tcpStream.Write(wrBuf, 0, wrBuf.Length);
                Console.WriteLine("Sent: {0}", tcpMsg);

                // Read the bytes from the buffer 
                byte[] rdBuf = new byte[65000];
                byte[] incBytes = new byte[0];
                if (tcpStream.CanRead)
                {
                    // Read the whole TCP Packet 
                    do
                    {
                        // Read bytes from buffer
                        numBytesRead += tcpStream.Read(rdBuf, 0, rdBuf.Length);

                        // Concat the bytes into a bytearray 
                        incBytes = incBytes.Concat(rdBuf).ToArray();
                    }
                    while (tcpStream.DataAvailable);
                }
                else
                {
                    Console.WriteLine("Error: You cannot read from this NetworkStream");
                }

                // Parse the reply from the client computer, CLIENT_IP | FP_DATA
                byte[] delim = Encoding.Default.GetBytes("|");
                int idx = Array.IndexOf(incBytes, delim[0]);                

                byte[] ip = new byte[idx];
                Array.Copy(incBytes, ip, idx); // CLIENT_FP
                Array.Copy(incBytes, ip.Length + 1, fpByte, 0, fpByte.Length - 1); // FP_DATA
                
                // Variables used for debugging
                var debugIp = Encoding.Default.GetString(ip);
                var debugFp = Encoding.Default.GetString(fpByte);

                // DEBUG: should check if ip is the same as server (input)

                // Close the stream and socket connections to client
                tcpStream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            // Return fingerprint data as a byte array
            return fpByte;
        }
    }
}
