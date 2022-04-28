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
        private static StringComparer _stringComparer = StringComparer.OrdinalIgnoreCase;
        private static byte[] _dataBuffer;
        private static int _readBytes;

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

            _dataBuffer = new byte[_serialPort.ReadBufferSize];
        }

        private static bool portReady()
        {
            if (_serialPort.IsOpen) { return true; }
            else 
            { 
                Console.WriteLine("COM port is closed. Use the o command to open it. Use help for more info.");
                return false;
            }
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

                if (_stringComparer.Equals("quit", command) ||
                    _stringComparer.Equals("exit", command) ||
                    _stringComparer.Equals("q", command))
                {
                    _continue = false;
                }

                else if (_stringComparer.Equals("help", command) || _stringComparer.Equals("h", command))
                {
                    printHelp();
                }

                else if (_stringComparer.Equals("c", command))
                {
                    CloseComPort();
                }

                else if (_stringComparer.Equals("o", command))
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
                        Console.WriteLine("Error: COM port is already open and cannot be set. Use c to close COM port." + e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Please check your syntax! <COMx> must be a valid COM port name like 'COM7'. Exception thrown:" + e.Message);
                    }
                }

                else if (_stringComparer.Equals("r", command))
                {
                    if (portReady()) { commandRead(); }
                }

                else if (_stringComparer.Equals("auto", command))
                {
                    if (portReady()) { commandAuto(); }
                    
                }

                else if (_stringComparer.Equals("s", command))
                {
                    if (portReady()) { commandStore(); }
                }

                else if (command.StartsWith("s ") && command.Length > 2)
                {
                    if (portReady()) { commandStore(command.Remove(0, 2)); }
                }

                else if (command.StartsWith("cb ") || command.StartsWith("ch "))
                {
                    string[] split = command.Split(' ');
                    if(split.Length != 3)
                    {
                        Console.WriteLine($"Invalid command. Usage: {split[0]} <file1> <file2>");
                        continue;
                    }
                    if (_stringComparer.Equals("cb", split[0]))
                    {
                        commandCompareBinary(split[1], split[2]);
                    }
                    else if (_stringComparer.Equals("ch", split[0]))
                    {
                        commandCompareHex(split[1], split[2]);
                    }
                }
                
                else if (_stringComparer.Equals("flush", command))
                {
                    try
                    {
                        _serialPort.DiscardInBuffer();
                        Console.WriteLine("Serial port flushed.");
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
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
            }
            catch (Exception)
            {
                Console.WriteLine("");
                Console.WriteLine("Failed to open port " + _serialPort.PortName + ". Is it already in use? Use command o to retry.");
            }
            return _serialPort.IsOpen;
        }

        // Public to provide access to COM port from outsite
        public static void CloseComPort()
        {
            Console.WriteLine("Closing {0}.", _serialPort.PortName);
            _serialPort.Close();
        }

        public static void Read(SerialPort sp)
        {
            _readBytes = 0;
            try {
                _readBytes = sp.Read(_dataBuffer, 0, _dataBuffer.Length);
                if (_readBytes > 0) Console.WriteLine(Encoding.UTF8.GetString(_dataBuffer, 0, _readBytes));
            }
            catch (TimeoutException) {
                Console.WriteLine("Timeout during read. Returning to binaire.");
            }
        }

        private static void commandRead()
        {
            Console.WriteLine("Checking on " + _serialPort.PortName + " for data...");
            Thread readThread = new Thread(() => { Read(_serialPort); });
            readThread.Start();
            readThread.Join();

            if (_readBytes == 0)
            {
                Console.WriteLine("There is no data available. Send data again and retry.");
            }
            else
            {
                Console.WriteLine("==== end of data ====");
                Console.WriteLine("{0} bytes were read in total.", _readBytes);
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
            if (_readBytes == 0)
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
                    fs.Write(_dataBuffer, 0, _readBytes);
                }
            }
            catch( Exception e)
            {
                Console.WriteLine("Store command failed. Exception: {0}", e);
            }
        }


        // Automatic mode adds an EventHandler to the serial port. Any time data is received, autoDataReceivedAction() is 
        // called. While in auto mode, no other commands can be issued.
        private static void commandAuto()
        {
            Console.WriteLine("Automatic mode activated. binaire will listen to the COM port for srampuf data packets.");
            Console.WriteLine("Enter 'stop' to exit automatic mode. All other input is disabled.");

            _serialPort.DataReceived += autoDataReceivedAction;

            bool _continueAuto = true;
            while (_continueAuto)
            {
                string? s = Console.ReadLine();
                if (s == "stop") { _continueAuto = false; }
            }

            Console.WriteLine("Stopping auto mode.");
            _serialPort.DataReceived -= autoDataReceivedAction;
        }


        // Lazy method to wait for complete packets. Since only one COM port can be open at one time, no thought is
        // spent on making this safe for multi-channel communication. Instead, the thread sleeps for a second to let
        // all bytes of a packet arrive before attempting decoding.
        private static void autoDataReceivedAction(object sender, EventArgs args)
        {
            SerialPort port = (SerialPort)sender;
            if (port == null) { return; }
            if (port.BytesToRead < 40) { return; }

            Console.WriteLine("Data received! {0} bytes are available.", port.BytesToRead);
            Console.WriteLine("Let's wait 1000 ms...");
            Console.WriteLine("{0} bytes are now available. Reading packet...", port.BytesToRead);

            Thread.Sleep(1000);

            try
            {
                Read(port);
                decodePacket();
            }
            catch (Exception e) { Console.WriteLine("Error while reading packet: {0}", e.ToString()); };
        }


        // Decodes a packet sent from STM according to the protocol specified in datenbank.md
        // Subsequently, the decoded packet is used to instantiate a Reading, holding all relevant data.
        private static void decodePacket()
        {
            const int minPacketSize = 40;
            if (_readBytes == 0) { throw new InvalidOperationException("There are no bytes to be read."); }
            if (_readBytes < minPacketSize) { throw new NotSupportedException("Too little bytes were received, could not decode package."); }

            byte[] magicNumber = { 0xE5, 0x55, 0x01, 0xEB };

            // Range notation buf[a..b] is used (C# 8.0 / .NET Core 3.0)
            var rcvMagicNumber = _dataBuffer[0..4];
            if (!magicNumber.SequenceEqual(rcvMagicNumber)) { throw new NotSupportedException("Invalid protocol start."); }
            Console.WriteLine("Packet received!");

            var rcvBoardID = _dataBuffer[4..16];
            int[] boardID = new int[Board.IdLength];
            Buffer.BlockCopy(rcvBoardID, 0, boardID, 0, rcvBoardID.Length);

            var rcvBoardSpecifier = _dataBuffer[16..20];
            int boardSpecifier = BitConverter.ToInt32(rcvBoardSpecifier);

            var rcvStartAddress = _dataBuffer[20..24];
            int startAddress = BitConverter.ToInt32(rcvStartAddress);

            var rcvEndAddress = _dataBuffer[24..28];
            int endAddress = BitConverter.ToInt32(rcvEndAddress);

            var rcvTemperature = _dataBuffer[28..32];
            int temperature = BitConverter.ToInt32(rcvTemperature);
            float tempF = ((float)temperature) / 1000;

            int pufSize = endAddress - startAddress;
            int pufOffset = 32 + pufSize;
            var fingerprint = _dataBuffer[32..pufOffset];

            var zeroBytes = _dataBuffer[pufOffset..(pufOffset+2)];
            if (!zeroBytes.SequenceEqual(new byte[] { 0, 0 })) { throw new NotSupportedException("Invalid protocol end."); }

            Console.WriteLine("Packet seems fine!");

            try
            {
                Board.BoardSpecifiers bs = (Board.BoardSpecifiers)boardSpecifier;
                packetToDatabase(bs, boardID, startAddress, endAddress, tempF, fingerprint);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

        }


        private static void packetToDatabase(Board.BoardSpecifiers bs, int[] id, int pufStart, int pufEnd, float temp, byte[] fp)
        {
            if (fp == null) { throw new ArgumentException("fp must not be null."); }
            if (id.Length != Board.IdLength) { throw new ArgumentException($"id must consist of {Board.IdLength} integers."); }
            if (pufStart < 0) { throw new ArgumentException("pufStart must be positive."); }
            if (pufEnd < 0) { throw new ArgumentException("pufEnd must be positive."); }
            if (pufEnd <= pufStart) { throw new ArgumentException("pufEnd must be a larger address than pufStart."); }
            if (temp <= -273.0 || temp > 250.0) { throw new ArgumentException("temperature is invalid."); }
            Board b = Database.AddBoard(bs, id[0], id[1], id[2]);
            Reading? r = Database.AddReading(b, pufStart, pufEnd, temp, fp);
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
            Console.WriteLine("flush            Flush the serial port's input buffer and discard all received data.");
            Console.WriteLine("auto     Automatic mode. Shifts the program to go in a loop, waiting for data to be");
            Console.WriteLine("         received. Received data packets are automatically sent to the database. This");
            Console.WriteLine("         is meant to be used with the srampuf C project and smarttex database. EXPERIMENTAL.");
            Console.WriteLine("quit, q, exit    Exit binaire.");
        }
    }
}
