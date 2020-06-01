using System;
using System.Net.Sockets;
using System.Text;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace TCPSender
{
    class Program
    {
        static void Main(string[] args)
        {
            // Using code from: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=netframework-4.8
            //String server = "3.23.5.132";
            String server = "127.0.0.1";
            Int32 port = 15326;
            String message = "MEDNETFP:START";
            Connect(server, port, message);
        }

        static void Connect(String server, Int32 port, String message)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                String tcpMsg = "24.84.225.22" + "|" + message;
                Byte[] wrBuf = System.Text.Encoding.UTF8.GetBytes(tcpMsg);

                // Get a client stream for reading and writing.

                NetworkStream tcpStream = client.GetStream();

                // Send the message to the connected TcpServer. 
                tcpStream.Write(wrBuf, 0, wrBuf.Length);

                Console.WriteLine("Sent: {0}", tcpMsg);

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.

                // Read the bytes from the buffer 
                byte[] rdBuf = new byte[512];
                StringBuilder completeMsg = new StringBuilder();
                int numBytesRead = 0;
                byte[] fpBytes = new byte[0];
                if (tcpStream.CanRead)
                {
                    // Read the whole message 
                    do
                    {
                        // Read bytes from buffer
                        numBytesRead = tcpStream.Read(rdBuf, 0, rdBuf.Length);

                        // Concat the bytes into a bytearray 
                        fpBytes = fpBytes.Concat(rdBuf).ToArray();
                    }
                    while (tcpStream.DataAvailable);
                }
                else
                {
                    Console.WriteLine("Error: You cannot read from this NetworkStream");
                }

                // parse the reply from the client computer
                byte[] delim = Encoding.Default.GetBytes("|");
                int idx = Array.IndexOf(fpBytes, delim[0]);

                byte[] ip = new byte[idx];
                byte[] fpByte = new byte[59707];
                Array.Copy(fpBytes, ip, idx);
                Array.Copy(fpBytes, ip.Length+1, fpByte, 0, fpByte.Length-1);
                
                // debug variables
                var debugIp = Encoding.Default.GetString(ip);
                var debugFp = Encoding.Default.GetString(fpByte);



                var img = Image.FromStream(new MemoryStream(fpByte));
                var bmp = new Bitmap(img);

                bmp.Save("cccc1.bmp");

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

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }
    }
}
