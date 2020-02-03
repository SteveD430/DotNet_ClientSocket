using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace ClientSocket
{
    class Program
    {
        /*
         * args[0] - IP Address of Socket Server
         * args[1] - Port for socket server
         * 
         * args[2] - Full path for outputfile. Note; Contents will be over-written
         */
        static void Main(string[] args)
        {
            const int MAX_BYTES_READ = 25000;
            const int BYTES_RECV_BUF_LEN = MAX_BYTES_READ * 2;
            const int MONITOR_RECV = 10000;

            Byte[] bytesReceived = new Byte[BYTES_RECV_BUF_LEN];
            Stopwatch stopWatch = new Stopwatch();

            IPAddress iPAddress = IPAddress.Parse(args[0]);
            IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, int.Parse(args[1]));
            Socket tempSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            StreamWriter file = new StreamWriter(args[2]);
            tempSocket.Connect(ipEndPoint);
            if (tempSocket.Connected)
            {
                int retain = 0;
                int countMissedPoints = 0;
                int missedErrors = 0;
                int dataPoint = 0;
                int dataPointCt = 0;
                int monitorPointCt = 0;
                int bytes = 0;
                int bytesRead = 0;
                bool isFirstDataPoint = true;
                int firstDataPoint = 0;
                int previousDataPoint = 0;
                const int STOP_LISTENING_AT = 1000000;

                stopWatch.Start();
                do
                {
                    try
                    {
                        bytesRead = tempSocket.Receive(bytesReceived, retain, MAX_BYTES_READ, 0);
                        bytes = bytesRead + retain;
                        
                        int k = 0;
                        int[] data = new int[100];
                        int decode = ((bytes) / 5);
                        retain = (bytes) - (decode * 5);
                        file.WriteLine($"\n Bytes read: {bytesRead.ToString()}. Retained: {retain} \n");
                        if ((bytesRead % 5) != 0)
                        {
                            file.WriteLine("\n WARNING: Number of bytes received not divisible by 5 \n");
                        }

                        for (int i = 0; i < decode; i++)
                        {
                            dataPoint = 0;
                            for (int j = 4; j >= 0; j--)
                            {
                                if (bytesReceived[k + j] < 33)
                                {
                                    throw new Exception("Byte < 33 \n");
                                }
                                dataPoint = dataPoint * 85 + (bytesReceived[k + j] - 33);
                            }
                            dataPointCt++;
                            monitorPointCt++;
                            k = k + 5;
                            if (monitorPointCt >= MONITOR_RECV)
                            {
                                Console.Write(".");
                                monitorPointCt = 0;
                            }
                            file.Write($"{dataPoint.ToString()} ");
                            if (isFirstDataPoint)
                            {
                                previousDataPoint = dataPoint;
                                firstDataPoint = dataPoint;
                                isFirstDataPoint = false;
                            }
                            else
                            {
                                if (dataPoint != previousDataPoint + 1)
                                {
                                    file.WriteLine($"\nMissed datapoints. Previous: {previousDataPoint} : Latest {dataPoint} \n");
                                    countMissedPoints = countMissedPoints + (dataPoint - previousDataPoint - 1);
                                    missedErrors++;
                                }
                                previousDataPoint = dataPoint;
                            }

                        }
                        if (retain != 0)
                        {
                            for (int i = 0; i < retain; i++)
                            {
                                bytesReceived[i] = bytesReceived[bytes - retain + i];
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        file.WriteLine("\nERROR: " + e.Message);
                    }
                } while (bytes > 0 && dataPointCt < STOP_LISTENING_AT);

                stopWatch.Stop();
                file.WriteLine("\nTime Elapsed: {0}", stopWatch.Elapsed);
                file.WriteLine($"Number of missed points: {countMissedPoints.ToString()}. Number of actual errors: {missedErrors.ToString()}");
            }
            file.Close();
            //Console.WriteLine("Hit any key to close.");
            //int c = Console.Read();
        }
    }
}

