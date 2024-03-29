﻿//########################################################################
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
// File name: Reading.cs
// Date of file creation: 2022-04-27
// List of autors: Lucas Drack
//########################################################################


using System.ComponentModel.DataAnnotations;

namespace binaire
{
    // Reading represents one reading taken from STM32 microcontroller, holding all relevant SRAM PUF data.
    // Data flow:
    // - STM32 Nucleo-F401RE reads SRAM PUF, sends packet data over serial interface to PC
    // - binaire (this program) reads packet over COM port, decodes it and instantiates a Reading
    // - Reading class is sent to database using Entity Framework
    public class Reading
    {
        // Entity Framework uses naming conventions: https://www.entityframeworktutorial.net/code-first/code-first-conventions.aspx
        // Properties of this class will translate to columns in the final database.
        public int ReadingId { get; set; }

        // Foreign Key to the Boards column (nullable)
        public Board? Board { get; set; }

        public int PufStart { get; set; }
        public int PufEnd { get; set; }
        public float Temperature { get; set; }
        public DateTime Timestamp { get; set; }

        [MaxLength(10000)]
        public byte[] Fingerprint { get; set; }


        public Reading(int pufStart, int pufEnd, float temperature, byte[] fingerprint)
        {
            if (fingerprint == null) { throw new ArgumentException("fp must not be null."); }
            if (pufStart < 0) { throw new ArgumentException("pufStart must be positive."); }
            if (pufEnd < 0) { throw new ArgumentException("pufEnd must be positive."); }
            if (pufEnd <= pufStart) { throw new ArgumentException("pufEnd must be a larger address than pufStart."); }
            if (temperature <= -273.0 || temperature > 250.0) { throw new ArgumentException("temperature is invalid."); }

            PufStart = pufStart;
            PufEnd = pufEnd;
            Timestamp = DateTime.Now;
            Temperature = temperature;
            Fingerprint = fingerprint;
        }

        public override string ToString()
        {
            string boardInfo = (Board != null)
                ? "Board UID: 0x" + Board.BoardId1.ToString("X8") + " 0x" + Board.BoardId2.ToString("X8") + " 0x" + Board.BoardId3.ToString("X8") + "\n" +
                  "BoardSpecifier: " + Board.BoardSpecifier + "\n"
                : "";

            return "Reading #" + ReadingId + "\n" +
                   boardInfo +
                   "PufStart: 0x" + PufStart.ToString("X8") + "\n" +
                   "PufEnd: 0x" + PufEnd.ToString("X8") + "\n" +
                   "PUF Size: " + (PufEnd - PufStart) + " Bytes\n" +
                   "Temperature: " + Temperature + "\n";
        }
    }
}
    