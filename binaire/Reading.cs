//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-04-24

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace binaire
{
    // Reading represents one reading taken from STM32 microcontroller, holding
    // all relevant SRAM PUF data.
    // Data flow:
    // STM32 Nucleo-F401RE reads SRAM PUF, sends packet data over serial interface to PC
    // binaire (this program) reads packet over COM port, decodes it and instantiates a Reading
    // Reading class is sent to database using Entity Framework (Todo)
    internal class Reading
    {
        public const int IdLength = 3;

        // Board specifiers - these are the same as defined in the srampuf C project.
        private const int BS_NucleoF401RE = 1;

        public int ReadingID { get; set; }
        public int[] BoardID { get; set; }
        public int BoardSpecifier { get; set; }
        public int PufStart { get; set; }
        public int PufEnd { get; set; }
        public float Temperature { get; set; }
        public byte[]? Fingerprint { get; set; }


        public Reading(int[] boardId, int bs, int pufStart, int pufEnd, float temperature, byte[] fp)
        {
            if (boardId.Length != IdLength) { throw new ArgumentException("boardId must have length 3."); }
            if (bs != BS_NucleoF401RE) { throw new ArgumentException("bs is an invalid board specifier."); }
            if (pufStart < 0) { throw new ArgumentException("pufStart must be positive."); }
            if (pufEnd < 0) { throw new ArgumentException("pufEnd must be positive."); }
            if (pufEnd <= pufStart) { throw new ArgumentException("pufEnd must be a larger address than pufStart."); }
            if (temperature < -273.0 || temperature > 200.0) { throw new ArgumentException("temperature is invalid."); }
            if (fp.Length != pufEnd - pufStart) { throw new ArgumentException("Length of fp does not match pufStart and pufEnd."); }

            BoardID = new int[IdLength];
            boardId.CopyTo(BoardID, 0);
            BoardSpecifier = bs;
            PufStart = pufStart;
            PufEnd = pufEnd;
            Temperature = temperature;

            Fingerprint = new byte[PufEnd - PufStart];
            fp.CopyTo(Fingerprint, 0);
        }

        public override string ToString()
        {
            return "Reading #" + ReadingID + "\n" +
                   "BoardID: 0x" + BoardID[0].ToString("X8") + " 0x" + BoardID[1].ToString("X8") + " 0x" + BoardID[2].ToString("X8") + "\n" +
                   "BoardSpecifier: " + BoardSpecifier + "\n" +
                   "PufStart: 0x" + PufStart.ToString("X8") + "\n" +
                   "PufEnd: 0x" + PufEnd.ToString("X8") + "\n" +
                   "PUF Size: " + (PufEnd - PufStart) + " Bytes\n" +
                   "Temperature: " + Temperature + "\n";
        }
    }
}
    