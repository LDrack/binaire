//########################################################################
// (C) Embedded Systems Lab
// All rights reserved.
// ------------------------------------------------------------
// This document contains proprietary information belonging to
// Research & Development FH OÖ Forschungs und Entwicklungs GmbH.
// Using, passing on and copying of this document or parts of it
// is generally not permitted without prior written authorization.
// ------------------------------------------------------------
// info(at)embedded-lab.at
// https://www.embedded-lab.at/
//########################################################################
// File name: binaire.cs
// Date of file creation: 2022-04-24
// List of autors: Lucas Drack
//########################################################################

//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-04-24 V0.2

using System.Text;
using System.IO.Ports;
using CsvHelper;
using System.Diagnostics;

namespace binaire
{
    internal class binaire
    {
        private static SerialPort _serialPort;
        private static StringComparer _stringComparer = StringComparer.OrdinalIgnoreCase;
        private static byte[] _dataBuffer;
        private static int _readBytes;
        private static bool _localMode = false;

        private const int _minPacketSize = 40;

        private static void SetupComPort()
        {
            // Default com port can be set here, usually 12 or 7 is good. Otherwise, use the o command to set.
            //_serialPort = new SerialPort("COM12", 115200, Parity.None, 8, StopBits.One);
            _serialPort = new SerialPort("COM7", 115200, Parity.None, 8, StopBits.One);
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
            if (!OpenComPort(true)) {
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
                    OpenComPort(true);
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
                            OpenComPort(true);
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

                // COmpare hex / compare binary of two input files
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
                    commandFlush();
                }

                // Activate/Deactivate local mode. Local mode doesn't send to database, default is enabled.
                else if (_stringComparer.Equals("local", command))
                {
                    commandLocal();
                }

                // Save byte[] to image - not fully integrated, you have to adapt the source code to
                // whatever bytes you want to save.
                else if (_stringComparer.Equals("i", command))
                {
                    byte[] b = { 0, 0, 0x11, 0x23, 0xAA, 0xFF, 0xFF, 0xFF };

                    Reading fp1 = Database.GetReadingByID(22);
                    Reading fp2 = Database.GetReadingByID(26);
                    byte[] xor = new byte[fp1.Fingerprint.Length];
                    for (int i = 0; i < fp1.Fingerprint.Length; i++)
                    {
                        xor[i] = (byte)(fp1.Fingerprint[i] ^ fp2.Fingerprint[i]);
                    }

                    //BinaryToImage.Write(b, 16, 4, "imagetest.png");
                    //BinaryToImage.SaveBitImage(fp1.Fingerprint, 32, 64, "fp1.pdf");
                    //BinaryToImage.SaveBitImage(fp2.Fingerprint, 32, 64, "fp2.pdf");
                    //BinaryToImage.SaveBitImage(xor, 32, 64, "xor.pdf");

                    int[] heatmap = { 0, 0, 0, 10, 9, 5, 3, 1, 0 };
                    BinaryToImage.SaveHeatmapImage(heatmap, 10, 3, 3, "heatmap.png");
                }

                else if (_stringComparer.Equals("work", command))
                {
                    //using (var ctx = new Database.binaireDbContext())
                    //{
                    //    const int pufsPerDevice = 35;
                    //    int address = 0x20005000;
                    //    const int pufSize = 0x800;

                    //    List<List<Reading>> readingsFromB4 = multiPufQuery(4, pufsPerDevice, 0x20005000, 0x800);

                    //    int[] bitcountFirstEntryFromFirstPuf = getFpCount(readingsFromB4[0]);
                    //    BinaryToImage.SaveHeatmapImage(bitcountFirstEntryFromFirstPuf, readingsFromB4[0].Count, 256, 64, "heatmapTest.png");


                    //    const int n = 35;
                    //    double[] FHD = new double[n];
                    //    double[] interFHD = new double[n];




                    //    //ExportData.ExportCsv(m1, "messreihe1.csv");
                    //}

                    //try { evaluation1(); }
                    //catch (Exception ex) { Console.WriteLine(ex.Message); }

                    //try { evaluation2(); }
                    //catch (Exception ex) { Console.WriteLine(ex.Message); }


                    try { evaluationf446WithoutOutliers(); }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }

                    //saveFuzzyExtractorData(4, 50);

                    Console.WriteLine("Done.");
                }

                else if (_stringComparer.Equals("test", command))
                {
                    commandTest();
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

        private static bool OpenComPort(bool print)
        {
            try
            {
                _serialPort.Open();
                if (print)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Opened port " + _serialPort.PortName + ". Type QUIT to exit binaire.");
                }
            }
            catch (Exception)
            {
                if (print) {
                Console.WriteLine("");
                Console.WriteLine("Failed to open port " + _serialPort.PortName + ". Is it already in use? Use command o to retry.");
                }
            }
            return _serialPort.IsOpen;
        }

        // Public to provide access to COM port from outsite
        public static void CloseComPort()
        {
            Console.WriteLine("Closing {0}.", _serialPort.PortName);
            _serialPort.Close();
        }

        public static int Read(SerialPort sp, bool printData)
        {
            _readBytes = 0;
            try {
                _readBytes = sp.Read(_dataBuffer, 0, _dataBuffer.Length);
                if (_readBytes > 0 && printData) Console.WriteLine(Encoding.UTF8.GetString(_dataBuffer, 0, _readBytes));
            }
            catch (TimeoutException) {
                Console.WriteLine("Timeout during read. Returning to binaire.");
            }
            return _readBytes;
        }

        private static void commandRead()
        {
            Console.WriteLine("Checking on " + _serialPort.PortName + " for data...");
            Thread readThread = new Thread(() => { Read(_serialPort, true); });
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
                if (!_localMode)
                {
                    try { decodePacket(); }
                    catch (Exception e) { Console.WriteLine("Error while decoding packet: {0}", e.ToString()); };
                }
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

        private static void commandFlush()
        {
            try
            {
                _serialPort.DiscardInBuffer();
                Console.WriteLine("Serial port flushed.");
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private static void commandLocal()
        {
            if (_localMode)
            {
                _localMode = false;
                Console.WriteLine("Local mode disabled - packets will be stored in database.");
            }
            else
            {
                _localMode = true;
                Console.WriteLine("Local mode enabled - packets will not be stored in database.");
            }
        }

        // Automatic mode adds an EventHandler to the serial port. Any time data is received, autoDataReceivedAction() is 
        // called. While in auto mode, no other commands can be issued.
        private static void commandAuto()
        {
            Console.WriteLine("Automatic mode activated. binaire will listen to the COM port for srampuf data packets.");
            Console.WriteLine("Enter 'stop' to exit automatic mode. All other input is disabled.");
            commandFlush();

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

            Console.WriteLine("Data received! Let's wait 1000 ms...");
            Thread.Sleep(1000);
            Console.WriteLine("{0} bytes are now available. Reading packet...", port.BytesToRead);

            try
            {
                Read(port, false);
                Console.WriteLine("{0} bytes were read.", _readBytes);
                if (_localMode) { isPacketOk(); }
                else { decodePacket(); }
            }
            catch (Exception e) { Console.WriteLine("Error while reading packet: {0}", e.ToString()); };
        }


        // Decodes a packet sent from STM according to the protocol specified in datenbank.md
        // Subsequently, the decoded packet is used to instantiate a Reading, holding all relevant data.
        private static void decodePacket()
        {
            if (_readBytes == 0) { throw new InvalidOperationException("There are no bytes to be read."); }
            if (_readBytes < _minPacketSize) { throw new NotSupportedException("Too little bytes were received, could not decode package."); }

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

        private static bool isPacketOk()
        {
            if (_readBytes == 0) { Console.WriteLine("Packet not ok: There are no bytes to be read."); return false; }
            if (_readBytes < _minPacketSize) { Console.WriteLine("Packet not ok: Too little bytes were received, could not decode package."); return false; }

            byte[] magicNumber = { 0xE5, 0x55, 0x01, 0xEB };

            // Range notation buf[a..b] is used (C# 8.0 / .NET Core 3.0)
            var rcvMagicNumber = _dataBuffer[0..4];
            if (!magicNumber.SequenceEqual(rcvMagicNumber)) { Console.WriteLine("Packet not ok: Invalid protocol start."); return false; }
            Console.WriteLine("Packet received!");

            var rcvBoardID = _dataBuffer[4..16];
            int[] boardID = new int[Board.IdLength];
            Buffer.BlockCopy(rcvBoardID, 0, boardID, 0, rcvBoardID.Length);

            var rcvStartAddress = _dataBuffer[20..24];
            int startAddress = BitConverter.ToInt32(rcvStartAddress);

            var rcvEndAddress = _dataBuffer[24..28];
            int endAddress = BitConverter.ToInt32(rcvEndAddress);

            int pufSize = endAddress - startAddress;
            int pufOffset = 32 + pufSize;
            var fingerprint = _dataBuffer[32..pufOffset];

            var zeroBytes = _dataBuffer[pufOffset..(pufOffset + 2)];
            if (!zeroBytes.SequenceEqual(new byte[] { 0, 0 })) { Console.WriteLine("Packet not ok: Invalid protocol end."); return false; }

            Console.WriteLine("Packet seems fine!");
            return true;
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


        // Enters the automatic test mode.
        // Board behaviour: Reset -> Delay(10s) -> send packet -> nop...
        // Binaire behaviour: Open COM port while board is in delay -> wait for packet -> decode -> close port -> Delay(...)
        private static void commandTest()
        {
            Console.WriteLine($"Starting automated test procedure on {_serialPort.PortName}.");

            //_localMode = true;    // during development

            if (!tryCloseAndReopenPort()) { return; }

            commandFlush();

            // Timeout implemented with stopwatch
            const int timeoutMs = 15000;
            Stopwatch sw = new Stopwatch();

            try
            {
                int count = 1;
                while (true)
                {
                    Console.WriteLine("Board connected. Trying to read for 15 seconds...");
                    sw.Restart();
                    while (!tryTestRead())
                    {
                        if (sw.ElapsedMilliseconds > timeoutMs)
                        {
                            Console.WriteLine("Timeout during test - did not receive data. Returning to normal operation.");
                            return;
                        }
                        Thread.Sleep(500);
                    }

                    Console.WriteLine("\n=================================");
                    Console.WriteLine("Data received. This was packet #{0}", count++);
                    Console.WriteLine("=================================\n");
                    Console.WriteLine("Disconnecting COM port... Reconnect the board in the next 15 seconds.");
                    if (!tryCloseAndReopenPort()) { return; }
                    commandFlush();
                }
            }
            catch (Exception ex) { Console.WriteLine("Error while reading packet: {0}", ex.ToString()); }
        }



        private static bool tryTestRead()
        {
            if (_serialPort.IsOpen == false) { return false; }
            if (_serialPort.BytesToRead < 40) { return false; }

            Console.WriteLine("Data received! Let's wait 1000 ms...");
            Thread.Sleep(1000);
            Console.WriteLine("{0} bytes are now available. Reading packet...", _serialPort.BytesToRead);

            try
            {
                Read(_serialPort, false);
                Console.WriteLine("{0} bytes were read.", _readBytes);
                if (_localMode) { isPacketOk(); }
                else { decodePacket(); }
            }
            catch (Exception e) { Console.WriteLine("Error while reading packet: {0}", e.ToString()); }

            return true;
        }


        private static bool tryCloseAndReopenPort()
        {
            const int timeoutMs = 5000;            

            // Give tester 15 seconds to disconnect the board. After that, try to reopen
            CloseComPort();
            Console.Write("Waiting for you to reconnect the board");
            for (int i = 0; i < 15; i++)
            {
                Thread.Sleep(1000);
                Console.Write(".");
            }
            Console.WriteLine("");

            if (!OpenComPort(false))
            {
                Console.Write("Board not yet connected. Trying again for 5 seconds...");
                Stopwatch sw = new Stopwatch();
                sw.Start();

                while (!OpenComPort(false))
                {
                    if (sw.ElapsedMilliseconds > timeoutMs)
                    {
                        Console.WriteLine("Timeout during test - board was not reconnected. Returning to normal operation.");
                        return false;
                    }
                    Thread.Sleep(500);
                }
            }

            return true;
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
            Console.WriteLine("local            Turns local mode on/off. In local mode, received packets are not stored");
            Console.WriteLine("                 in database, instead they can be normally saved with the s command.");
            Console.WriteLine("auto     Automatic mode. Shifts the program to go in a loop, waiting for data to be");
            Console.WriteLine("         received. Received data packets are automatically sent to the database. This");
            Console.WriteLine("         is meant to be used with the srampuf C project and smarttex database.");
            Console.WriteLine("i        Write byte array to black & white image (adapt source code).");
            Console.WriteLine("quit, q, exit    Exit binaire.");
        }






        #region evaluationHelpers

        // Functions starting from here are for fetching data from DB - attention: rudimentary implementation.

        // Multi PUF Model: One device houses a large number of PUFs (35 in the experimental implementation)
        // This function returns a list of pufsPerDevice lists, where each sublist holds all entries for a
        // specific memory region (ex. all readings from 0x20005000 - 0x20005800 are stored in list[0])
        private static List<List<Reading>> multiPufQuery(int boardId, int pufsPerDevice, int startAddress, int pufSize)
        {
            int address = startAddress;
            List<List<Reading>> readingsFromBoard = new List<List<Reading>>();

            using (var ctx = new Database.binaireDbContext())
            {
                for (int i = 0; i < pufsPerDevice; i++)
                {
                    var rlist = ctx.Readings.Where(p => p.Board.BoardId == boardId
                                                     && p.PufStart == address
                                                     && p.PufEnd == address + pufSize).ToList();
                    address += pufSize;
                    readingsFromBoard.Add(rlist);
                }
            }
            return readingsFromBoard;
        }

        private static byte[]? calcKnownFP(List<Reading> readings)
        {
            if (readings.Count == 0) { return null; }
            int pufSize = readings[0].PufEnd - readings[0].PufStart;
            foreach(Reading r in readings) { if (r.PufEnd - r.PufStart != pufSize) return null; }   // FP of different length = faulty list

            byte[] knownFP = new byte[pufSize];
            int[] fpCount = getFpCount(readings);
            double maxCount = (double)fpCount.Max();
            if (maxCount == 0.0) { return null; }

            // Go through each byte in the known FP
            for (int k = 0; k < knownFP.Length; k++)
            { 
                byte theByte = 0;

                // Go through each bit of the current byte of the known FP
                for (int b = 0; b < 8; b++)
                {
                    int countIdx = k * 8 + b;

                    // Probability of powering up to 1
                    double p = (double)fpCount[countIdx] / maxCount;
                    if (p >= 0.5)
                    {
                        theByte += (byte)(1 << (7 - b));
                    }
                }

                knownFP[k] = theByte;
            }

            return knownFP;
        }

        // Contains 1 int for each bit in the given fingerprints.
        // array[0] holds the number of times that the first bit was 1, etc.
        private static int[] getFpCount(List<Reading> readings)
        {
            if (readings.Count == 0) { return null; }
            int pufSize = readings[0].PufEnd - readings[0].PufStart;
            int pufSizeBits = pufSize * 8;
            foreach (Reading r in readings) { if (r.PufEnd - r.PufStart != pufSize) return null; }   // FP of different length = faulty list

            int[] count = new int[pufSizeBits];

            // Go through all readings, which hold 1 fingerprint each
            for (int readingIdx = 0; readingIdx < readings.Count; readingIdx++)
            {
                // Go through the current fingerprint bytewise
                for (int i = 0; i < pufSize; i++)
                {
                    byte b = readings[readingIdx].Fingerprint[i];

                    // Go through each bit of the current byte and count
                    for (int bitIdx = 0; bitIdx < 8; bitIdx++)
                    {
                        int countIdx = i * 8 + bitIdx;
                        count[countIdx] += (b >> (7 - bitIdx)) & 1;     // MSB is saved first in the array
                    }
                }
            }
            return count;
        }

        // Calculate the average reliability from a given known fingerprint compared with a list of latent fp
        private static double calcReliability(byte[] knownFP, List<Reading> readings)
        {
            return 1.0 - calcIntraHD(knownFP, readings);
        }

        private static double calcIntraHD(byte[] knownFP, List<Reading> readings)
        {
            if (readings.Count == 0) { return 0.0; }
            double fhd = 0.0;
            foreach (Reading r in readings)
            {
                fhd += HexComp.calcFHD(knownFP, r.Fingerprint);
            }
            return fhd /= readings.Count;      // intra HD
        }

        // Averages Uniformity over a list of readings
        private static double calcUniformity(List<Reading> readings)
        {
            if (readings.Count == 0) { return 0.0; }
            double uniformity = 0.0;
            foreach (Reading r in readings)
            {
                uniformity += HexComp.calcBias(r.Fingerprint);
            }
            return uniformity /= readings.Count;
        }

        // Calculates pairwise uniqueness over the two given lists.
        // Idea: sample m fingerprints from two PUF instances. Uniqueness gives the average distance
        // between the two PUF instances, ideally 50% if they are from different devices.
        private static double calcUniqueness(List<Reading> r1, List<Reading> r2)
        {
            if (r1.Count == 0 || r2.Count == 0 || r1.Count != r2.Count) { return 0.0; }
            int m = r1.Count;
            double uniqueness = 0.0;
            for (int i = 0; i < m - 1; i++)
            {
                for (int j = i + 1; j < m; j++)
                {
                    uniqueness += HexComp.calcFHD(r1[i].Fingerprint, r2[j].Fingerprint);
                }
            }
            return uniqueness * 2 / (m * (m-1));
        }

        #endregion


        private static void listOfListToCSV<T>(List<List<T>> l, string csvname)
        {
            try
            {
                const char SEPARATOR = ';';
                using (StreamWriter writer = new StreamWriter(csvname))
                {
                    l.ForEach(line =>
                    {
                        writer.WriteLine(string.Join(SEPARATOR, line));
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }


        private static void evaluation1()
        {
            using (var ctx = new Database.binaireDbContext())
            {
                const int pufsPerDevice = 35;
                int address = 0x20005000;
                const int pufSize = 0x800;

                List<List<Reading>> readingsB4 = multiPufQuery(4, pufsPerDevice, 0x20005000, 0x800);
                List<List<Reading>> readingsB7 = multiPufQuery(7, pufsPerDevice, 0x20005000, 0x800);

                //int[] bitcountBoard4Puf0 = getFpCount(readingsB4[0]);
                //BinaryToImage.SaveHeatmapImage(bitcountBoard4Puf0, readingsB4[0].Count, 128, 128, "eval1_heatmap_board4_puf0.png");

                //int[] bitcountBoard4Puf1 = getFpCount(readingsB4[1]);
                //BinaryToImage.SaveHeatmapImage(bitcountBoard4Puf1, readingsB4[1].Count, 128, 128, "eval1_heatmap_board4_puf1.png");



                int n = readingsB4.Count;
                int m = readingsB4[0].Count;
                List<double> bias4 = new List<double>();
                List<double> bias7 = new List<double>();

                // Uniformity
                //for (int i = 0; i < n; i++)
                //{
                //    bias4.Add(calcUniformity(readingsB4[i]));
                //    bias7.Add(calcUniformity(readingsB7[i]));
                //}
                //ExportData.WriteCsv(bias4, "eval1_uniformity_board4.csv");
                //ExportData.WriteCsv(bias7, "eval1_uniformity_board7.csv");


                List<double> intraHD4 = new List<double>();
                List<double> intraHD7 = new List<double>();
                List<byte[]> knownFP4 = new List<byte[]>();
                List<byte[]> knownFP7 = new List<byte[]>();

                // Reliability
                //for (int i = 0; i < n; i++)
                //{
                //    knownFP4.Add(calcKnownFP(readingsB4[i]));
                //    knownFP7.Add(calcKnownFP(readingsB7[i]));
                //    intraHD4.Add(calcIntraHD(knownFP4[i], readingsB4[i]));
                //    intraHD7.Add(calcIntraHD(knownFP7[i], readingsB7[i]));
                //}
                //ExportData.WriteCsv(intraHD4, "eval1_intraHD_board4.csv");
                //ExportData.WriteCsv(intraHD7, "eval1_intraHD_board7.csv");


                List<double> uniqueness4 = new List<double>();
                List<double> uniqueness7 = new List<double>();
                List<double> uniqueness4vs7 = new List<double>();

                // Uniqueness
                for (int i = 0; i < m - 1; i++)
                {
                    for (int j = i + 1; j < m; j++)
                    {
                        uniqueness4.Add(calcUniqueness(readingsB4[i], readingsB4[j]));
                        uniqueness7.Add(calcUniqueness(readingsB7[i], readingsB7[j]));
                        uniqueness4vs7.Add(calcUniqueness(readingsB4[i], readingsB7[j]));
                    }
                }
                ExportData.WriteCsv(uniqueness4, "eval1_uniqueness_board4.csv");
                ExportData.WriteCsv(uniqueness7, "eval1_uniqueness_board7.csv");
                ExportData.WriteCsv(uniqueness4vs7, "eval1_uniqueness_board4vs7.csv");
            }
        }


        // Temperature data from all 14 boards
        private static void evaluation2()
        {
            using (var ctx = new Database.binaireDbContext())
            {
                const int nBoards = 14;
                const int nReadings = 50;

                List<List<Reading>> temp10 = new List<List<Reading>>();
                List<List<Reading>> temp25 = new List<List<Reading>>();
                List<List<Reading>> temp50 = new List<List<Reading>>();

                for (int i = 0; i < nBoards; i++)
                {
                    int boardNr = i + 4;    // IDs 4--17
                    temp10.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 2633
                                                     && p.ReadingId <= 3343).ToList());
                    temp25.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 3344
                                                     && p.ReadingId <= 4043).ToList());
                    temp50.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 4044
                                                     && p.ReadingId <= 4743).ToList());
                }

                List<byte[]> knownFP10 = new List<byte[]>();
                List<byte[]> knownFP25 = new List<byte[]>();
                List<byte[]> knownFP50 = new List<byte[]>();

                for (int i = 0; i < nBoards; i++)
                {
                    knownFP10.Add(calcKnownFP(temp10[i]));
                    knownFP25.Add(calcKnownFP(temp25[i]));
                    knownFP50.Add(calcKnownFP(temp50[i]));
                }

                // Graph 1: Temperatures
                // Result: 3 List of dimensions 14x50 - 50 entries for each board and temperature
                {
                    List<List<double>> temperatures10 = new List<List<double>>();
                    List<List<double>> temperatures25 = new List<List<double>>();
                    List<List<double>> temperatures50 = new List<List<double>>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        List<double> t10 = new List<double>();
                        List<double> t25 = new List<double>();
                        List<double> t50 = new List<double>();

                        for (int j = 0; j < nReadings; j++)
                        {
                            t10.Add(temp10[i][j].Temperature);
                            t25.Add(temp25[i][j].Temperature);
                            t50.Add(temp50[i][j].Temperature);
                        }

                        temperatures10.Add(t10);
                        temperatures25.Add(t25);
                        temperatures50.Add(t50);
                    }

                    listOfListToCSV(temperatures10, "eval2_temperatures10.csv");
                    listOfListToCSV(temperatures25, "eval2_temperatures25.csv");
                    listOfListToCSV(temperatures50, "eval2_temperatures50.csv");
                }


                // Graph 2: Intra HD of all measurements compared with FK25
                // Result: 3 List of dimensions 14x50 - 50 entries for each board and temperature
                {
                    List<List<double>> intraHD25_10 = new List<List<double>>();
                    List<List<double>> intraHD25_25 = new List<List<double>>();
                    List<List<double>> intraHD25_50 = new List<List<double>>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        List<double> i10 = new List<double>();
                        List<double> i25 = new List<double>();
                        List<double> i50 = new List<double>();

                        for (int j = 0; j < nReadings; j++)
                        {
                            i10.Add(HexComp.calcFHD(knownFP25[i], temp10[i][j].Fingerprint));
                            i25.Add(HexComp.calcFHD(knownFP25[i], temp25[i][j].Fingerprint));
                            i50.Add(HexComp.calcFHD(knownFP25[i], temp50[i][j].Fingerprint));
                        }

                        intraHD25_10.Add(i10);
                        intraHD25_25.Add(i25);
                        intraHD25_50.Add(i50);
                    }

                    listOfListToCSV(intraHD25_10, "eval2_intraHD25_10.csv");
                    listOfListToCSV(intraHD25_25, "eval2_intraHD25_25.csv");
                    listOfListToCSV(intraHD25_50, "eval2_intraHD25_50.csv");
                }




                // Graph 3: Uniformity per temperature
                // Result: 3 List with 14 elements - 1 entry for each board and temperature
                {
                    List<double> uniformity10 = new List<double>();
                    List<double> uniformity25 = new List<double>();
                    List<double> uniformity50 = new List<double>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        uniformity10.Add(calcUniformity(temp10[i]));
                        uniformity25.Add(calcUniformity(temp25[i]));
                        uniformity50.Add(calcUniformity(temp50[i]));
                    }

                    ExportData.WriteCsv(uniformity10, "eval2_uniformity10.csv");
                    ExportData.WriteCsv(uniformity25, "eval2_uniformity25.csv");
                    ExportData.WriteCsv(uniformity50, "eval2_uniformity50.csv");
                }

                // Graph 4: Intra HD per temperature
                {
                    List<double> intraHD10 = new List<double>();
                    List<double> intraHD25 = new List<double>();
                    List<double> intraHD50 = new List<double>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        intraHD10.Add(calcIntraHD(knownFP10[i], temp10[i]));
                        intraHD25.Add(calcIntraHD(knownFP25[i], temp10[i]));
                        intraHD50.Add(calcIntraHD(knownFP50[i], temp10[i]));
                    }

                    ExportData.WriteCsv(intraHD10, "eval2_intraHD10.csv");
                    ExportData.WriteCsv(intraHD25, "eval2_intraHD25.csv");
                    ExportData.WriteCsv(intraHD50, "eval2_intraHD50.csv");
                }

                // Graph 5: Uniqueness per temperature
                // Binomial Coefficient of (14 over 2) = 91 entries per temperature
                {
                    List<double> uniqueness10 = new List<double>();
                    List<double> uniqueness25 = new List<double>();
                    List<double> uniqueness50 = new List<double>();

                    for (int i = 0; i < nBoards - 1; i++)
                    {
                        for (int j = i + 1; j < nBoards; j++)
                        {
                            uniqueness10.Add(calcUniqueness(temp10[i], temp10[j]));
                            uniqueness25.Add(calcUniqueness(temp25[i], temp25[j]));
                            uniqueness50.Add(calcUniqueness(temp50[i], temp50[j]));
                        }
                    }

                    ExportData.WriteCsv(uniqueness10, "eval2_uniqueness10.csv");
                    ExportData.WriteCsv(uniqueness25, "eval2_uniqueness25.csv");
                    ExportData.WriteCsv(uniqueness50, "eval2_uniqueness50.csv");
                }
            }
        }






        // Same as evaluation 2 but for the F446 boards
        private static void evaluationf446()
        {
            using (var ctx = new Database.binaireDbContext())
            {
                const int nBoards = 14;
                const int nReadings = 50;

                List<List<Reading>> temp10 = new List<List<Reading>>();
                List<List<Reading>> temp25 = new List<List<Reading>>();
                List<List<Reading>> temp50 = new List<List<Reading>>();

                for (int i = 0; i < nBoards; i++)
                {
                    int boardNr = i + 18;    // IDs 18--31
                    temp10.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 6240
                                                     && p.ReadingId <= 6950).ToList());
                    temp25.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 4746
                                                     && p.ReadingId <= 5527).ToList());
                    temp50.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 5528
                                                     && p.ReadingId <= 6239).ToList());
                }

                List<byte[]> knownFP10 = new List<byte[]>();
                List<byte[]> knownFP25 = new List<byte[]>();
                List<byte[]> knownFP50 = new List<byte[]>();

                for (int i = 0; i < nBoards; i++)
                {
                    knownFP10.Add(calcKnownFP(temp10[i]));
                    knownFP25.Add(calcKnownFP(temp25[i]));
                    knownFP50.Add(calcKnownFP(temp50[i]));
                }

                // Graph 1: Temperatures
                // Result: 3 List of dimensions 14x50 - 50 entries for each board and temperature
                {
                    List<List<double>> temperatures10 = new List<List<double>>();
                    List<List<double>> temperatures25 = new List<List<double>>();
                    List<List<double>> temperatures50 = new List<List<double>>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        List<double> t10 = new List<double>();
                        List<double> t25 = new List<double>();
                        List<double> t50 = new List<double>();

                        for (int j = 0; j < nReadings; j++)
                        {
                            t10.Add(temp10[i][j].Temperature);
                            t25.Add(temp25[i][j].Temperature);
                            t50.Add(temp50[i][j].Temperature);
                        }

                        temperatures10.Add(t10);
                        temperatures25.Add(t25);
                        temperatures50.Add(t50);
                    }

                    listOfListToCSV(temperatures10, "evalf446_temperatures10.csv");
                    listOfListToCSV(temperatures25, "evalf446_temperatures25.csv");
                    listOfListToCSV(temperatures50, "evalf446_temperatures50.csv");
                }


                // Graph 2: Intra HD of all measurements compared with FK25
                // Result: 3 List of dimensions 14x50 - 50 entries for each board and temperature
                {
                    List<List<double>> intraHD25_10 = new List<List<double>>();
                    List<List<double>> intraHD25_25 = new List<List<double>>();
                    List<List<double>> intraHD25_50 = new List<List<double>>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        List<double> i10 = new List<double>();
                        List<double> i25 = new List<double>();
                        List<double> i50 = new List<double>();

                        for (int j = 0; j < nReadings; j++)
                        {
                            i10.Add(HexComp.calcFHD(knownFP25[i], temp10[i][j].Fingerprint));
                            i25.Add(HexComp.calcFHD(knownFP25[i], temp25[i][j].Fingerprint));
                            i50.Add(HexComp.calcFHD(knownFP25[i], temp50[i][j].Fingerprint));
                        }

                        intraHD25_10.Add(i10);
                        intraHD25_25.Add(i25);
                        intraHD25_50.Add(i50);
                    }

                    listOfListToCSV(intraHD25_10, "evalf446_intraHD25_10.csv");
                    listOfListToCSV(intraHD25_25, "evalf446_intraHD25_25.csv");
                    listOfListToCSV(intraHD25_50, "evalf446_intraHD25_50.csv");
                }




                // Graph 3: Uniformity per temperature
                // Result: 3 List with 14 elements - 1 entry for each board and temperature
                {
                    List<double> uniformity10 = new List<double>();
                    List<double> uniformity25 = new List<double>();
                    List<double> uniformity50 = new List<double>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        uniformity10.Add(calcUniformity(temp10[i]));
                        uniformity25.Add(calcUniformity(temp25[i]));
                        uniformity50.Add(calcUniformity(temp50[i]));
                    }

                    ExportData.WriteCsv(uniformity10, "evalf446_uniformity10.csv");
                    ExportData.WriteCsv(uniformity25, "evalf446_uniformity25.csv");
                    ExportData.WriteCsv(uniformity50, "evalf446_uniformity50.csv");
                }

                // Graph 4: Intra HD per temperature
                {
                    List<double> intraHD10 = new List<double>();
                    List<double> intraHD25 = new List<double>();
                    List<double> intraHD50 = new List<double>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        intraHD10.Add(calcIntraHD(knownFP10[i], temp10[i]));
                        intraHD25.Add(calcIntraHD(knownFP25[i], temp10[i]));
                        intraHD50.Add(calcIntraHD(knownFP50[i], temp10[i]));
                    }

                    ExportData.WriteCsv(intraHD10, "evalf446_intraHD10.csv");
                    ExportData.WriteCsv(intraHD25, "evalf446_intraHD25.csv");
                    ExportData.WriteCsv(intraHD50, "evalf446_intraHD50.csv");
                }

                // Graph 5: Uniqueness per temperature
                // Binomial Coefficient of (14 over 2) = 91 entries per temperature
                {
                    List<double> uniqueness10 = new List<double>();
                    List<double> uniqueness25 = new List<double>();
                    List<double> uniqueness50 = new List<double>();

                    for (int i = 0; i < nBoards - 1; i++)
                    {
                        for (int j = i + 1; j < nBoards; j++)
                        {
                            uniqueness10.Add(calcUniqueness(temp10[i], temp10[j]));
                            uniqueness25.Add(calcUniqueness(temp25[i], temp25[j]));
                            uniqueness50.Add(calcUniqueness(temp50[i], temp50[j]));
                        }
                    }

                    ExportData.WriteCsv(uniqueness10, "evalf446_uniqueness10.csv");
                    ExportData.WriteCsv(uniqueness25, "evalf446_uniqueness25.csv");
                    ExportData.WriteCsv(uniqueness50, "evalf446_uniqueness50.csv");
                }
            }
        }





        // Same as evaluation 2 but for the F446 boards
        private static void evaluationf446WithoutOutliers()
        {
            using (var ctx = new Database.binaireDbContext())
            {
                const int nBoards = 12;
                const int nReadings = 50;

                List<List<Reading>> temp10 = new List<List<Reading>>();
                List<List<Reading>> temp25 = new List<List<Reading>>();
                List<List<Reading>> temp50 = new List<List<Reading>>();

                for (int i = 0; i < nBoards+2; i++)
                {
                    int boardNr = i + 18;    // IDs 18--31
                    if (boardNr == 22 || boardNr == 26) { continue; }

                    temp10.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 6240
                                                     && p.ReadingId <= 6950).ToList());
                    temp25.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 4746
                                                     && p.ReadingId <= 5527).ToList());
                    temp50.Add(ctx.Readings.Where(p => p.Board.BoardId == boardNr
                                                     && p.ReadingId >= 5528
                                                     && p.ReadingId <= 6239).ToList());
                }

                List<byte[]> knownFP10 = new List<byte[]>();
                List<byte[]> knownFP25 = new List<byte[]>();
                List<byte[]> knownFP50 = new List<byte[]>();

                for (int i = 0; i < nBoards; i++)
                {
                    knownFP10.Add(calcKnownFP(temp10[i]));
                    knownFP25.Add(calcKnownFP(temp25[i]));
                    knownFP50.Add(calcKnownFP(temp50[i]));
                }

                // Graph 1: Temperatures
                // Result: 3 List of dimensions 14x50 - 50 entries for each board and temperature
                {
                    List<List<double>> temperatures10 = new List<List<double>>();
                    List<List<double>> temperatures25 = new List<List<double>>();
                    List<List<double>> temperatures50 = new List<List<double>>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        List<double> t10 = new List<double>();
                        List<double> t25 = new List<double>();
                        List<double> t50 = new List<double>();

                        for (int j = 0; j < nReadings; j++)
                        {
                            t10.Add(temp10[i][j].Temperature);
                            t25.Add(temp25[i][j].Temperature);
                            t50.Add(temp50[i][j].Temperature);
                        }

                        temperatures10.Add(t10);
                        temperatures25.Add(t25);
                        temperatures50.Add(t50);
                    }

                    listOfListToCSV(temperatures10, "evalf446_noOut_temperatures10.csv");
                    listOfListToCSV(temperatures25, "evalf446_noOut_temperatures25.csv");
                    listOfListToCSV(temperatures50, "evalf446_noOut_temperatures50.csv");
                }


                // Graph 2: Intra HD of all measurements compared with FK25
                // Result: 3 List of dimensions 14x50 - 50 entries for each board and temperature
                {
                    List<List<double>> intraHD25_10 = new List<List<double>>();
                    List<List<double>> intraHD25_25 = new List<List<double>>();
                    List<List<double>> intraHD25_50 = new List<List<double>>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        List<double> i10 = new List<double>();
                        List<double> i25 = new List<double>();
                        List<double> i50 = new List<double>();

                        for (int j = 0; j < nReadings; j++)
                        {
                            i10.Add(HexComp.calcFHD(knownFP25[i], temp10[i][j].Fingerprint));
                            i25.Add(HexComp.calcFHD(knownFP25[i], temp25[i][j].Fingerprint));
                            i50.Add(HexComp.calcFHD(knownFP25[i], temp50[i][j].Fingerprint));
                        }

                        intraHD25_10.Add(i10);
                        intraHD25_25.Add(i25);
                        intraHD25_50.Add(i50);
                    }

                    listOfListToCSV(intraHD25_10, "evalf446_noOut_intraHD25_10.csv");
                    listOfListToCSV(intraHD25_25, "evalf446_noOut_intraHD25_25.csv");
                    listOfListToCSV(intraHD25_50, "evalf446_noOut_intraHD25_50.csv");
                }




                // Graph 3: Uniformity per temperature
                // Result: 3 List with 14 elements - 1 entry for each board and temperature
                {
                    List<double> uniformity10 = new List<double>();
                    List<double> uniformity25 = new List<double>();
                    List<double> uniformity50 = new List<double>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        uniformity10.Add(calcUniformity(temp10[i]));
                        uniformity25.Add(calcUniformity(temp25[i]));
                        uniformity50.Add(calcUniformity(temp50[i]));
                    }

                    ExportData.WriteCsv(uniformity10, "evalf446_noOut_uniformity10.csv");
                    ExportData.WriteCsv(uniformity25, "evalf446_noOut_uniformity25.csv");
                    ExportData.WriteCsv(uniformity50, "evalf446_noOut_uniformity50.csv");
                }

                // Graph 4: Intra HD per temperature
                {
                    List<double> intraHD10 = new List<double>();
                    List<double> intraHD25 = new List<double>();
                    List<double> intraHD50 = new List<double>();

                    for (int i = 0; i < nBoards; i++)
                    {
                        intraHD10.Add(calcIntraHD(knownFP10[i], temp10[i]));
                        intraHD25.Add(calcIntraHD(knownFP25[i], temp10[i]));
                        intraHD50.Add(calcIntraHD(knownFP50[i], temp10[i]));
                    }

                    ExportData.WriteCsv(intraHD10, "evalf446_noOut_intraHD10.csv");
                    ExportData.WriteCsv(intraHD25, "evalf446_noOut_intraHD25.csv");
                    ExportData.WriteCsv(intraHD50, "evalf446_noOut_intraHD50.csv");
                }

                // Graph 5: Uniqueness per temperature
                // Binomial Coefficient of (14 over 2) = 91 entries per temperature
                {
                    List<double> uniqueness10 = new List<double>();
                    List<double> uniqueness25 = new List<double>();
                    List<double> uniqueness50 = new List<double>();

                    for (int i = 0; i < nBoards - 1; i++)
                    {
                        for (int j = i + 1; j < nBoards; j++)
                        {
                            uniqueness10.Add(calcUniqueness(temp10[i], temp10[j]));
                            uniqueness25.Add(calcUniqueness(temp25[i], temp25[j]));
                            uniqueness50.Add(calcUniqueness(temp50[i], temp50[j]));
                        }
                    }

                    ExportData.WriteCsv(uniqueness10, "evalf446_noOut_uniqueness10.csv");
                    ExportData.WriteCsv(uniqueness25, "evalf446_noOut_uniqueness25.csv");
                    ExportData.WriteCsv(uniqueness50, "evalf446_noOut_uniqueness50.csv");
                }
            }
        }






        //private static void evaluation3()
        //{
        //    using (var ctx = new Database.binaireDbContext())
        //    {
        //        List<Reading> b4Temp10 = ctx.Readings.Where(p => p.Board.BoardId == 4
        //                                                      && p.ReadingId >= 2221
        //                                                      && p.ReadingId <= 2420).ToList();
        //        List<Reading> b6Temp10 = ctx.Readings.Where(p => p.Board.BoardId == 6
        //                                                      && p.ReadingId >= 2221
        //                                                      && p.ReadingId <= 2420).ToList();
        //        List<Reading> b7Temp10 = ctx.Readings.Where(p => p.Board.BoardId == 7
        //                                                      && p.ReadingId >= 2221
        //                                                      && p.ReadingId <= 2420).ToList();
        //        List<Reading> b8Temp10 = ctx.Readings.Where(p => p.Board.BoardId == 8
        //                                                      && p.ReadingId >= 2221
        //                                                      && p.ReadingId <= 2420).ToList();


        //        List<Reading> b4Temp25 = ctx.Readings.Where(p => p.Board.BoardId == 4
        //                                                      && p.ReadingId >= 2429
        //                                                      && p.ReadingId <= 2628).ToList();
        //        List<Reading> b6Temp25 = ctx.Readings.Where(p => p.Board.BoardId == 6
        //                                                      && p.ReadingId >= 2429
        //                                                      && p.ReadingId <= 2628).ToList();
        //        List<Reading> b7Temp25 = ctx.Readings.Where(p => p.Board.BoardId == 7
        //                                                      && p.ReadingId >= 2429
        //                                                      && p.ReadingId <= 2628).ToList();
        //        List<Reading> b8Temp25 = ctx.Readings.Where(p => p.Board.BoardId == 8
        //                                                      && p.ReadingId >= 2429
        //                                                      && p.ReadingId <= 2628).ToList();


        //        List<Reading> b4Temp50 = ctx.Readings.Where(p => p.Board.BoardId == 4
        //                                                      && p.ReadingId >= 2021
        //                                                      && p.ReadingId <= 2220).ToList();
        //        List<Reading> b6Temp50 = ctx.Readings.Where(p => p.Board.BoardId == 6
        //                                                      && p.ReadingId >= 2021
        //                                                      && p.ReadingId <= 2220).ToList();
        //        List<Reading> b7Temp50 = ctx.Readings.Where(p => p.Board.BoardId == 7
        //                                                      && p.ReadingId >= 2021
        //                                                      && p.ReadingId <= 2220).ToList();
        //        List<Reading> b8Temp50 = ctx.Readings.Where(p => p.Board.BoardId == 8
        //                                                      && p.ReadingId >= 2021
        //                                                      && p.ReadingId <= 2220).ToList();

        //        int n = b4Temp10.Count;

        //        var knownFP_b4Temp10 = calcKnownFP(b4Temp10);
        //        var knownFP_b4Temp25 = calcKnownFP(b4Temp25);
        //        var knownFP_b4Temp50 = calcKnownFP(b4Temp50);

        //        var knownFP_b6Temp10 = calcKnownFP(b6Temp10);
        //        var knownFP_b6Temp25 = calcKnownFP(b6Temp25);
        //        var knownFP_b6Temp50 = calcKnownFP(b6Temp50);

        //        var knownFP_b7Temp10 = calcKnownFP(b7Temp10);
        //        var knownFP_b7Temp25 = calcKnownFP(b7Temp25);
        //        var knownFP_b7Temp50 = calcKnownFP(b7Temp50);

        //        var knownFP_b8Temp10 = calcKnownFP(b8Temp10);
        //        var knownFP_b8Temp25 = calcKnownFP(b8Temp25);
        //        var knownFP_b8Temp50 = calcKnownFP(b8Temp50);

        //        // Graph 1: Temperatures
        //        // Result: List of 150 entries for each board - 50 for 10°C, 50 for 25°C, 50 for 50°C
        //        {
        //            List<double> temperatures_b4 = new List<double>();
        //            List<double> temperatures_b6 = new List<double>();
        //            List<double> temperatures_b7 = new List<double>();
        //            List<double> temperatures_b8 = new List<double>();

        //            for (int i = 0; i < n; i++)
        //            {
        //                temperatures_b4.Add(b4Temp10[i].Temperature);
        //                temperatures_b6.Add(b6Temp10[i].Temperature);
        //                temperatures_b7.Add(b7Temp10[i].Temperature);
        //                temperatures_b8.Add(b8Temp10[i].Temperature);
        //            }
        //            for (int i = 0; i < n; i++)
        //            {
        //                temperatures_b4.Add(b4Temp25[i].Temperature);
        //                temperatures_b6.Add(b6Temp25[i].Temperature);
        //                temperatures_b7.Add(b7Temp25[i].Temperature);
        //                temperatures_b8.Add(b8Temp25[i].Temperature);
        //            }
        //            for (int i = 0; i < n; i++)
        //            {
        //                temperatures_b4.Add(b4Temp50[i].Temperature);
        //                temperatures_b6.Add(b6Temp50[i].Temperature);
        //                temperatures_b7.Add(b7Temp50[i].Temperature);
        //                temperatures_b8.Add(b8Temp50[i].Temperature);
        //            }
        //            ExportData.WriteCsv(temperatures_b4, "eval3_temperatures_board4.csv");
        //            ExportData.WriteCsv(temperatures_b6, "eval3_temperatures_board6.csv");
        //            ExportData.WriteCsv(temperatures_b7, "eval3_temperatures_board7.csv");
        //            ExportData.WriteCsv(temperatures_b8, "eval3_temperatures_board8.csv");
        //        }


        //        // Graph 2: Intra HD of all measurements compared with FK25
        //        // Result: List of 150 entries for each board - 50 for 10°C, 50 for 25°C, 50 for 50°C
        //        {
        //            List<double> intraHD25_b4 = new List<double>();
        //            List<double> intraHD25_b6 = new List<double>();
        //            List<double> intraHD25_b7 = new List<double>();
        //            List<double> intraHD25_b8 = new List<double>();

        //            for (int i = 0; i < n; i++)
        //            {
        //                intraHD25_b4.Add(HexComp.calcFHD(knownFP_b4Temp25, b4Temp10[i].Fingerprint));
        //                intraHD25_b6.Add(HexComp.calcFHD(knownFP_b6Temp25, b6Temp10[i].Fingerprint));
        //                intraHD25_b7.Add(HexComp.calcFHD(knownFP_b7Temp25, b7Temp10[i].Fingerprint));
        //                intraHD25_b8.Add(HexComp.calcFHD(knownFP_b8Temp25, b8Temp10[i].Fingerprint));
        //            }
        //            for (int i = 0; i < n; i++)
        //            {
        //                intraHD25_b4.Add(HexComp.calcFHD(knownFP_b4Temp25, b4Temp25[i].Fingerprint));
        //                intraHD25_b6.Add(HexComp.calcFHD(knownFP_b6Temp25, b6Temp25[i].Fingerprint));
        //                intraHD25_b7.Add(HexComp.calcFHD(knownFP_b7Temp25, b7Temp25[i].Fingerprint));
        //                intraHD25_b8.Add(HexComp.calcFHD(knownFP_b8Temp25, b8Temp25[i].Fingerprint));
        //            }
        //            for (int i = 0; i < n; i++)
        //            {
        //                intraHD25_b4.Add(HexComp.calcFHD(knownFP_b4Temp25, b4Temp50[i].Fingerprint));
        //                intraHD25_b6.Add(HexComp.calcFHD(knownFP_b6Temp25, b6Temp50[i].Fingerprint));
        //                intraHD25_b7.Add(HexComp.calcFHD(knownFP_b7Temp25, b7Temp50[i].Fingerprint));
        //                intraHD25_b8.Add(HexComp.calcFHD(knownFP_b8Temp25, b8Temp50[i].Fingerprint));
        //            }
        //            ExportData.WriteCsv(intraHD25_b4, "eval3_intraHD25_board4.csv");
        //            ExportData.WriteCsv(intraHD25_b6, "eval3_intraHD25_board6.csv");
        //            ExportData.WriteCsv(intraHD25_b7, "eval3_intraHD25_board7.csv");
        //            ExportData.WriteCsv(intraHD25_b8, "eval3_intraHD25_board8.csv");
        //        }




        //        // Graph 3: Uniformity per temperature
        //        // Result: List of 200 entries for each temperature - 50 for board 4, then 50 for board 6 etc.
        //        {
        //            List<double> uniformity_temp10 = new List<double>();
        //            List<double> uniformity_temp25 = new List<double>();
        //            List<double> uniformity_temp50 = new List<double>();

        //            uniformity_temp10.Add(calcUniformity(b4Temp10));
        //            uniformity_temp10.Add(calcUniformity(b6Temp10));
        //            uniformity_temp10.Add(calcUniformity(b7Temp10));
        //            uniformity_temp10.Add(calcUniformity(b8Temp10));

        //            uniformity_temp25.Add(calcUniformity(b4Temp25));
        //            uniformity_temp25.Add(calcUniformity(b6Temp25));
        //            uniformity_temp25.Add(calcUniformity(b7Temp25));
        //            uniformity_temp25.Add(calcUniformity(b8Temp25));

        //            uniformity_temp50.Add(calcUniformity(b4Temp50));
        //            uniformity_temp50.Add(calcUniformity(b6Temp50));
        //            uniformity_temp50.Add(calcUniformity(b7Temp50));
        //            uniformity_temp50.Add(calcUniformity(b8Temp50));

        //            ExportData.WriteCsv(uniformity_temp10, "eval3_uniformity_temp10.csv");
        //            ExportData.WriteCsv(uniformity_temp25, "eval3_uniformity_temp25.csv");
        //            ExportData.WriteCsv(uniformity_temp50, "eval3_uniformity_temp50.csv");
        //        }

        //        // Graph 4: Intra HD per temperature
        //        {
        //            List<double> intraHD_temp10 = new List<double>();
        //            List<double> intraHD_temp25 = new List<double>();
        //            List<double> intraHD_temp50 = new List<double>();

        //            intraHD_temp10.Add(calcIntraHD(knownFP_b4Temp10, b4Temp10));
        //            intraHD_temp10.Add(calcIntraHD(knownFP_b6Temp10, b6Temp10));
        //            intraHD_temp10.Add(calcIntraHD(knownFP_b7Temp10, b7Temp10));
        //            intraHD_temp10.Add(calcIntraHD(knownFP_b8Temp10, b8Temp10));

        //            intraHD_temp25.Add(calcIntraHD(knownFP_b4Temp25, b4Temp25));
        //            intraHD_temp25.Add(calcIntraHD(knownFP_b6Temp25, b6Temp25));
        //            intraHD_temp25.Add(calcIntraHD(knownFP_b7Temp25, b7Temp25));
        //            intraHD_temp25.Add(calcIntraHD(knownFP_b8Temp25, b8Temp25));

        //            intraHD_temp50.Add(calcIntraHD(knownFP_b4Temp50, b4Temp50));
        //            intraHD_temp50.Add(calcIntraHD(knownFP_b6Temp50, b6Temp50));
        //            intraHD_temp50.Add(calcIntraHD(knownFP_b7Temp50, b7Temp50));
        //            intraHD_temp50.Add(calcIntraHD(knownFP_b8Temp50, b8Temp50));

        //            ExportData.WriteCsv(intraHD_temp10, "eval3_intraHD_temp10.csv");
        //            ExportData.WriteCsv(intraHD_temp25, "eval3_intraHD_temp25.csv");
        //            ExportData.WriteCsv(intraHD_temp50, "eval3_intraHD_temp50.csv");
        //        }

        //        // Graph 5: Uniqueness per temperature
        //        {
        //            const int m = 4;
        //            List<double> uniqueness_temp10 = new List<double>();
        //            List<double> uniqueness_temp25 = new List<double>();
        //            List<double> uniqueness_temp50 = new List<double>();

        //            List<Reading>[] series_temp10 = new List<Reading>[m] { b4Temp10, b6Temp10, b7Temp10, b8Temp10 };
        //            List<Reading>[] series_temp25 = new List<Reading>[m] { b4Temp25, b6Temp25, b7Temp25, b8Temp25 };
        //            List<Reading>[] series_temp50 = new List<Reading>[m] { b4Temp50, b6Temp50, b7Temp50, b8Temp50 };

        //            for (int i = 0; i < m - 1; i++)
        //            {
        //                for (int j = i + 1; j < m; j++)
        //                {
        //                    uniqueness_temp10.Add(calcUniqueness(series_temp10[i], series_temp10[j]));
        //                    uniqueness_temp25.Add(calcUniqueness(series_temp25[i], series_temp25[j]));
        //                    uniqueness_temp50.Add(calcUniqueness(series_temp50[i], series_temp50[j]));
        //                }
        //            }

        //            ExportData.WriteCsv(uniqueness_temp10, "eval3_uniqueness_temp10.csv");
        //            ExportData.WriteCsv(uniqueness_temp25, "eval3_uniqueness_temp25.csv");
        //            ExportData.WriteCsv(uniqueness_temp50, "eval3_uniqueness_temp50.csv");
        //        }
        //    }
        //}


        private static void saveFuzzyExtractorData(uint boardid, uint temp)
        {
            uint addressStart = 0;
            uint addressEnd = 0;
            if      (temp == 10) { addressStart = 2633; addressEnd = 3343; }
            else if (temp == 25) { addressStart = 3344; addressEnd = 4043; }
            else if (temp == 50) { addressStart = 4044; addressEnd = 4743; }
            else { Console.WriteLine("saveFuzzyExtractorData(): Invalid Params."); return; }


            using (var ctx = new Database.binaireDbContext())
            {

                List<Reading> readings_b4t25 = new List<Reading>();
                readings_b4t25 = ctx.Readings.Where(r => r.Board.BoardId == boardid
                                                      && r.ReadingId >= addressStart
                                                      && r.ReadingId <= addressEnd).ToList();
                byte[] knownFP = calcKnownFP(readings_b4t25);

                const int nBytes = 16;
                List<List<byte>> fingerprints16 = new List<List<byte>>();

                foreach (var reading in readings_b4t25)
                {
                    List<byte> bytelist = new List<byte>(reading.Fingerprint[0..nBytes]);
                    fingerprints16.Add(bytelist);
                }

                List<List<byte>> knownFP16 = new List<List<byte>>();
                knownFP16.Add(new List<byte>(knownFP[0..nBytes]));




                listOfListToCSV(fingerprints16, $"readings_b{boardid}t{temp}.csv");
                listOfListToCSV(knownFP16, $"knownFP_b{boardid}t{temp}.csv");
            }
        }


    }
}
