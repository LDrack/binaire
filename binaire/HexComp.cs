//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-04-24

using System;

namespace binaire
{
    internal class HexComp
    {
        const int MAX_FILESIZE = 1048576;   // 1 MB

        private static bool checkFiles(string f1, string f2)
        {
            if (!File.Exists(f1) || !File.Exists(f2))
            {
                Console.WriteLine("One or both of the given filenames are invalid.");
                return false;
            }
            long l1 = new FileInfo(f1).Length;
            long l2 = new FileInfo(f1).Length;
            if (l1 > MAX_FILESIZE || l2 > MAX_FILESIZE)
            {
                Console.WriteLine("One or both of the given files are too large (>1 MB).");
                return false;
            }
            return true;
        }

        public static void compareHex(string f1, string f2)
        {
            if (!checkFiles(f1, f2)) return;
            compareAndPrint(f1, f2, true);
        }

        public static void compareBin(string f1, string f2)
        {
            if (!checkFiles(f1, f2)) return;
            compareAndPrint(f1, f2, false);
        }

        private static void compareAndPrint(string f1, string f2, bool hex)
        {
            byte[] b1;
            byte[] b2;
            try
            {
                b1 = File.ReadAllBytes(f1);
                b2 = File.ReadAllBytes(f2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to read file. Exception: {0}", e);
                return;
            }

            int shortestArrayLength = Math.Min(b1.Length, b2.Length);
            int hd = calcHD(b1, b2);
            double fhd = calcFHD(hd, shortestArrayLength);

            StringStyler s = new StringStyler(f1, f2);
            int lines = hex ? (int)Math.Round((double)shortestArrayLength / (double)s.hexDesiredLinewidth, MidpointRounding.ToPositiveInfinity) :
                              (int)Math.Round((double)shortestArrayLength / (double)s.binDesiredLinewidth, MidpointRounding.ToPositiveInfinity) ;
            s.setMinimumConsoleWidth(hex);
            s.printHeader(hex, shortestArrayLength, hd, fhd);
            int index = 0;
            for (int i = 0; i < lines; i++)
            {
                index += s.printLineOfBytes(b1, b2, index, i+1, hex);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("\nPrinted {0} bytes in total.", index);
        }

        public static int calcHD(byte[] b1, byte[] b2)
        {
            int hd = 0;
            for (int i = 0; i < Math.Min(b1.Length, b2.Length); i++)
            {
                hd += System.Numerics.BitOperations.PopCount((uint)(b1[i] ^ b2[i]));
            }
            return hd;
        }

        public static double calcFHD(int hd, int length)
        {
            // Length is given in bytes (filesize), so *8 is needed to count the bits
            if (hd <= 0 || length <= 0 || length*8 < hd) return 0.0;
            return (double)hd / (double)(length*8);
        }

        public static double calcBias(byte[] b)
        {
            int bitsSet = 0;
            for (int i = 0; i < b.Length; i++)
            {
                bitsSet += System.Numerics.BitOperations.PopCount((uint)(b[i]));
            }
            return (double)bitsSet / (double)(b.Length * 8);
        }
    }

    // Returns strings that build the printed output for hex and binary comparisons.
    // Strings are based on current Console width, so set it first before calling StringStyler!
    internal class StringStyler
    {
        public string fn1 { get; }
        public string fn2 { get; }

        public int hexDesiredLinewidth { get; set; }
        public int hexCharsPerByte { get; set; }
        public int hexNrGroupedBytes { get; set; }
        public int binDesiredLinewidth { get; set; }
        public int binCharsPerByte { get; set; }
        public int binNrGroupedBytes { get; set; }
        public const int fixedFill = 6;



        public StringStyler(string f1, string f2)
        {
            fn1 = f1;
            fn2 = f2;

            hexDesiredLinewidth = 16;
            hexCharsPerByte = 2;
            hexNrGroupedBytes = 2;
            binDesiredLinewidth = 8;
            binCharsPerByte = 8;
            binNrGroupedBytes = 1;
        }

        public void setMinimumConsoleWidth(bool hex)
        {
            int neededWindowWidth = calcTotalDisplayWidth(hex);
            if (neededWindowWidth >= Console.LargestWindowWidth)
            {
                throw new InvalidOperationException("Invalid line length parameters specified in StringStyler: line length surpasses maximum console width.");
            }
            if (Console.WindowWidth <= neededWindowWidth)
            {
                Console.WindowWidth = neededWindowWidth + 1;
            }
        }

        // Prints a decorative header (3 lines) including file names + binary and fractional Hamming Distance
        public void printHeader(bool hex, int bytes, int hd, double fhd)
        {
            const int nBrackets = 10;
            int leftToCenterDistance = calcLeftToCenterDistance(hex);
            int nLines1 = Console.WindowWidth - nBrackets - fn1.Length - fn2.Length - leftToCenterDistance * 2 - 1;
            string line2 = new string($"│ Bytes: {bytes}   Bits: {bytes*8}   HD: {hd}   FHD: {fhd*100:0.##}%");
            int nLines2 = Console.WindowWidth - leftToCenterDistance * 2 - line2.Length - 2;
            int nLines3 = Console.WindowWidth - leftToCenterDistance * 2 - 3;

            Console.Write($"{new String(' ', leftToCenterDistance)}");   // center the text
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("┌─[");
            Console.Write("{0}", fn1);
            Console.Write("]─{0}─[", new String('─', nLines1));
            Console.Write("{0}", fn2);
            Console.Write("]─┐\n");

            Console.Write($"{new String(' ', leftToCenterDistance)}");
            Console.Write(line2);
            Console.Write($"{new String(' ', nLines2)}│\n");

            Console.Write($"{new String(' ', leftToCenterDistance)}");
            Console.Write($"└{new String('─', nLines3)}┘\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public int printLineOfBytes(byte[] b1, byte[] b2, int index, int lineNr, bool hex)
        {
            if (index < 0 || index > b1.Length || index > b2.Length) { throw new ArgumentOutOfRangeException("Invalid index in printLineHex."); }

            int shortestArrayLength = Math.Min(b1.Length, b2.Length);
            int linewidth = hex ? Math.Min(this.hexDesiredLinewidth, shortestArrayLength - index) :
                                  Math.Min(this.binDesiredLinewidth, shortestArrayLength - index) ;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0}", new String(' ', calcLeftToCenterDistance(hex)));   // center the text

            if (hex)
            {

                for (int i = 0; i < linewidth; i++)
                {
                    string s = BitConverter.ToString(b1, index + i, 1);
                    printHexByteFormatted(b1[index + i], b2[index + i], s);
                    if (i % hexNrGroupedBytes == 1) Console.Write(" ");
                }

                printLineNr(lineNr, linewidth, hex);

                for (int i = 0; i < linewidth; i++)
                {
                    if (i % hexNrGroupedBytes == 0) Console.Write(" ");
                    string s = BitConverter.ToString(b2, index + i, 1);
                    printHexByteFormatted(b1[index + i], b2[index + i], s);
                }

            }
            else
            {

                for (int i = 0; i < linewidth; i++)
                {
                    printBinByteFormatted(b1[index + i], b2[index + i], true);
                    if (i % binNrGroupedBytes == 0) Console.Write(" ");
                }

                printLineNr(lineNr, linewidth, hex);

                for (int i = 0; i < linewidth; i++)
                {
                    if (i % binNrGroupedBytes == 0) Console.Write(" ");
                    printBinByteFormatted(b1[index + i], b2[index + i], false);
                }

            }

            Console.Write(" \n");
            return linewidth;
        }

        private void printHexByteFormatted(byte b1, byte b2, string s)
        {
            if (b1 == b2)
            {
                Console.Write(s);
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Write(s);
                Console.BackgroundColor = ConsoleColor.Black;
            }
        }

        private void printBinByteFormatted(byte b1, byte b2, bool left)
        {

            for (int i = 0; i < 8; i++)
            {
                byte mask = (byte)(0x1 << i);
                byte leftbit = (byte)((b1 & mask) >> i);
                byte rightbit = (byte)((b2 & mask) >> i);
                if (leftbit == rightbit)
                {
                    Console.Write($"{leftbit}");
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    if (left) Console.Write($"{leftbit}");
                    else      Console.Write($"{rightbit}");
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            }
        }

        private void printLineNr(int lineNr, int linewidth, bool hex)
        {
            // Middle collumn holds the line number. If a line is shorter than the desired
            // Line width, the left fill must be suited accordingly.
            // Right fill is always the same width, 5 chars.
            int leftFill = calcLeftLineNrFill(linewidth, hex);
            char fillchar = (lineNr % 2 == 1) ? '-' : ' ';

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("{0}[", new string(fillchar, leftFill));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("{0, 5}", lineNr);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]{0}", new string(fillchar, fixedFill));
            Console.ForegroundColor = ConsoleColor.White;
        }

        private int calcLeftLineNrFill(int linewidth, bool hex)
        {
            // Calculate fill example for hex display:
            //                 Hex display has 2 chars for each byte, 1 space after each second byte,
            //                 and 5 chars distance to middle (where line number is shown).

            if (hex)
            {
                if (linewidth == hexDesiredLinewidth)
                {
                    return fixedFill;
                }
                int totalLinewidth = (hexDesiredLinewidth * hexCharsPerByte) + (hexDesiredLinewidth / hexNrGroupedBytes) + fixedFill;
                return totalLinewidth - ((linewidth * hexCharsPerByte) + (linewidth / hexNrGroupedBytes));
            }
            else
            {
                if (linewidth == binDesiredLinewidth)
                {
                    return fixedFill;
                }
                int totalLinewidth = (binDesiredLinewidth * binCharsPerByte) + (binDesiredLinewidth / binNrGroupedBytes) + fixedFill;
                return totalLinewidth - ((linewidth * binCharsPerByte) + (linewidth / binNrGroupedBytes));
            }

        }

        private int calcSingleSideLineWidth(bool hex)
        {
            return hex ? hexDesiredLinewidth * hexCharsPerByte + hexDesiredLinewidth / hexNrGroupedBytes :
                         binDesiredLinewidth * binCharsPerByte + binDesiredLinewidth / binNrGroupedBytes ;
        }

        private int calcLeftToCenterDistance(bool hex)
        {
            return (Console.WindowWidth - calcTotalDisplayWidth(hex)) / 2;
        }

        private int calcTotalDisplayWidth(bool hex)
        {
            // 7 is the width of the line number column in the screen center (5 digits + 2 brackets)
            return calcSingleSideLineWidth(hex)*2 + fixedFill*2 + 7;
        }

    }

}
