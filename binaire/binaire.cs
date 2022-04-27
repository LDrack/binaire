//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-04-24

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace binaire
{
    internal class binaire
    {
        private static SerialPort _serialPort;
        private static StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        private static byte[] dataBuffer;
        private static int readBytes;
        private static bool comConnected = false;

        private static void SetupComPort()
        {
            // Default com port can be set here, usually 12 or 7 is good. Otherwise, use the o command to set.
            _serialPort = new SerialPort("COM12", 115200, Parity.None, 8, StopBits.One);
            //_serialPort = new SerialPort("COM7", 115200, Parity.None, 8, StopBits.One);
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadBufferSize = 5120; // reserve 5kB max Buffer for incoming serial data

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 10000;
            _serialPort.WriteTimeout = 500;

            dataBuffer = new byte[_serialPort.ReadBufferSize];
        }

        public static void Run() {
            printProgramHeader();
            SetupComPort();
            if (!OpenComPort()) {
                //return;     // Exit if COM port is blocked
            }

            string? command;
            bool _continue = true;
            while (_continue)
            {
                command = Console.ReadLine()?.Trim();
                if (command == null) continue;

                if (stringComparer.Equals("quit", command) ||
                    stringComparer.Equals("exit", command) ||
                    stringComparer.Equals("q", command))
                {
                    _continue = false;
                }
                else if (stringComparer.Equals("help", command) || stringComparer.Equals("h", command))
                {
                    printHelp();
                }
                else if (stringComparer.Equals("c", command))
                {
                    CloseComPort();
                }
                else if (stringComparer.Equals("o", command))
                {
                    OpenComPort();
                }
                else if (command.StartsWith("o "))
                {
                    string[] split = command.Split(' ');
                    if (split.Length != 2)
                    {
                        Console.WriteLine($"Invalid command. Usage: {split[0]} <COMx>");
                        continue;
                    }
                    try 
                    {
                        if (SerialPort.GetPortNames().ToList().Contains(split[1]))
                        {
                            _serialPort.PortName = split[1];
                            OpenComPort();
                        }
                        else
                        {
                            Console.WriteLine($"{split[1]} is an invalid port name. The following ports are available:");
                            foreach (var p in SerialPort.GetPortNames())
                            {
                                Console.WriteLine(p);
                            }
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine("Error: COM port is already open and cannot be set. Use c to close COM port.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Please check your syntax! <COMx> must be a valid COM port name like 'COM7'. Exception thrown:" + e.Message);
                    }
                }
                else if (stringComparer.Equals("r", command))
                {
                    if (comConnected) { commandRead(); }
                    else { Console.WriteLine("COM port is closed. Use the o command to open it. Use help for more info."); }
                }
                else if (stringComparer.Equals("s", command))
                        {
                    commandStore();
                }
                else if (command.StartsWith("s ") && command.Length > 2)
                {
                    commandStore(command.Remove(0, 2));
                }
                else if (command.StartsWith("cb ") || command.StartsWith("ch "))
                {
                    string[] split = command.Split(' ');
                    if(split.Length != 3)
                    {
                        Console.WriteLine($"Invalid command. Usage: {split[0]} <file1> <file2>");
                        continue;
                    }
                    if (stringComparer.Equals("cb", split[0]))
                    {
                        commandCompareBinary(split[1], split[2]);
                    }
                    else if (stringComparer.Equals("ch", split[0]))
                    {
                        commandCompareHex(split[1], split[2]);
                    }
                }
                else
                {
                    Console.WriteLine("Unknown command. Enter 'help' for usage information.");
                }
            }

            _serialPort.Close();
        }

        private static void commandCompareHex(string f1, string f2)
        {
            HexComp.compareHex(f1, f2);
        }

        private static void commandCompareBinary(string f1, string f2)
        {
            HexComp.compareBin(f1, f2);
        }

        private static bool OpenComPort()
        {
            try
            {
                _serialPort.Open();
                Console.WriteLine("");
                Console.WriteLine("Opened port " + _serialPort.PortName + ". Type QUIT to exit binaire.");
                comConnected = true;
            }
            catch (Exception)
            {
                Console.WriteLine("");
                Console.WriteLine("Failed to open port " + _serialPort.PortName + ". Is it already in use? Use command o to retry.");
                comConnected = false;
            }
            return comConnected;
        }

        // Public to provide access to COM port from outsite
        public static void CloseComPort()
        {
            Console.WriteLine("Closing {0}.", _serialPort.PortName);
            _serialPort.Close();
            comConnected = false;
        }

        public static void Read()
        {
            readBytes = 0;
            try {
                readBytes = _serialPort.Read(dataBuffer, 0, dataBuffer.Length);
                if (readBytes > 0) Console.WriteLine(Encoding.UTF8.GetString(dataBuffer, 0, readBytes));
            }
            catch (TimeoutException) {
                Console.WriteLine("Timeout during read. Returning to binaire.");
            }
        }

        private static void commandRead()
        {
            Console.WriteLine("Checking on " + _serialPort.PortName + " for data...");
            Thread readThread = new Thread(() => { Read(); });
            readThread.Start();
            readThread.Join();

            if (readBytes == 0)
            {
                Console.WriteLine("There is no data available. Send data again and retry.");
            }
            else
            {
                Console.WriteLine("==== end of data ====");
                Console.WriteLine("{0} bytes were read in total.", readBytes);
                try { decodePacket(); }
                catch (Exception e) { Console.WriteLine("Error while decoding packet: {0}", e.ToString()); };
            }
        }

        private static void commandStore()
        {
            commandStore(Directory.GetCurrentDirectory());
        }

        private static void commandStore(string s)
        {
            if (readBytes == 0)
            {
                Console.WriteLine("No data is available. Use the r command first.");
                return;
            }
            try
            {
                string fname = s + "\\binaire-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bin";
                Console.WriteLine("Storing file at " + fname);
                using (var fs = new FileStream(fname, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(dataBuffer, 0, readBytes);
                }
            }
            catch( Exception e)
            {
                Console.WriteLine("Store command failed. Exception: {0}", e);
            }
        }


        // Decodes a packet sent from STM according to the protocol specified in datenbank.md
        // Subsequently, the decoded packet is used to instantiate a Reading, holding all relevant data.
        private static void decodePacket()
        {
            const int minPacketSize = 40;
            if (readBytes == 0) { throw new InvalidOperationException("There are no bytes to be read."); }
            if (readBytes < minPacketSize) { throw new NotSupportedException("Too little bytes were received, could not decode package."); }

            byte[] magicNumber = { 0xE5, 0x55, 0x01, 0xEB };

            // Range notation buf[a..b] is used (C# 8.0 / .NET Core 3.0)
            var rcvMagicNumber = dataBuffer[0..4];
            if (!magicNumber.SequenceEqual(rcvMagicNumber)) { throw new NotSupportedException("Invalid protocol start."); }
            Console.WriteLine("Packet received!");

            var rcvBoardID = dataBuffer[4..16];
            int[] boardID = new int[Reading.IdLength];
            Buffer.BlockCopy(rcvBoardID, 0, boardID, 0, rcvBoardID.Length);

            var rcvBoardSpecifier = dataBuffer[16..20];
            int boardSpecifier = BitConverter.ToInt32(rcvBoardSpecifier);

            var rcvStartAddress = dataBuffer[20..24];
            int startAddress = BitConverter.ToInt32(rcvStartAddress);

            var rcvEndAddress = dataBuffer[24..28];
            int endAddress = BitConverter.ToInt32(rcvEndAddress);

            var rcvTemperature = dataBuffer[28..32];
            int temperature = BitConverter.ToInt32(rcvTemperature);
            float tempF = ((float)temperature) / 1000;

            int pufSize = endAddress - startAddress;
            int pufOffset = 32 + pufSize;
            var fingerprint = dataBuffer[32..pufOffset];

            var zeroBytes = dataBuffer[pufOffset..(pufOffset+2)];
            if (!zeroBytes.SequenceEqual(new byte[] { 0, 0 })) { throw new NotSupportedException("Invalid protocol end."); }

            Console.WriteLine("Packet seems fine!");
            Reading r = new Reading(boardID, boardSpecifier, startAddress, endAddress, tempF, fingerprint);
            Console.WriteLine(r);
        }


        private static void printProgramHeader()
        {
            Console.WriteLine("***** binaire v0.2 - Utility for SRAM PUF analysis and binary comparison");
            Console.WriteLine("  *** Author: Lucas Drack");
            Console.WriteLine("    * 2022-04-13");
        }

        private static void printHelp()
        {
            printProgramHeader();
            Console.WriteLine("");
            Console.WriteLine("binaire can read binary data over a COM port and store it as a binary file.");
            Console.WriteLine(_serialPort.PortName + " is opened at startup and listens for data. All received data is buffered");
            Console.WriteLine("in background. Use the r command to read, which prints the received data to console and loads it into");
            Console.WriteLine("the internal file buffer. Once loaded, use the s command to save the last read data as file.");
            Console.WriteLine("COM port settings are documented in the source code.");
            Console.WriteLine("");
            Console.WriteLine("binaire supports the following commands:");
            Console.WriteLine("help, h   Print this help text.");
            Console.WriteLine("c         Closes the used COM port.");
            Console.WriteLine("o         Opens the previously selected COM port.");
            Console.WriteLine("o <COMx>  Tells binaire to use the specified COM port and tries to open it. <COMx> must");
            Console.WriteLine("          be a valid string usable with the SerialPort class, for example: COM7");
            Console.WriteLine("r         Receive data over COM port. Any data that was streamed to the port since");
            Console.WriteLine("          it was opened is printed, stored internally and can subsequently be stored");
            Console.WriteLine("          to disk with the s command.");
            Console.WriteLine("s            Store received data to current directory. Filename is the current timestamp.");
            Console.WriteLine("s <name>     Store received data to disk. <name> specifies the desired filename, which ");
            Console.WriteLine("             can be a relative path starting from current directory.");
            Console.WriteLine("cb <f1> <f2>     Compare two files with binary formatting. Differences will be");
            Console.WriteLine("                 highlighted side by side.");
            Console.WriteLine("ch <f1> <f2>     Compare two files with hexadecimal formatting. Differences will be");
            Console.WriteLine("                 highlighted side by side.");
            Console.WriteLine("quit, q, exit    Exit binaire.");
        }
    }
}
