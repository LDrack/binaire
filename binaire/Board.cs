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
// File name: Board.cs
// Date of file creation: 2022-04-27
// List of autors: Lucas Drack
//########################################################################

namespace binaire
{
    // Uniquely classifies one development board. The goal is to differentiate data from different
    // boards in the database.
    public class Board
    {
        // STM32 provides a 96-bit unique board identifier on each board.
        // Therefore, 3 ints are used here to save the uid.
        public const int IdLength = 3;

        // Board specifiers - these are defined in the srampuf C project.
        // Cast this enum to int to instantiate class.
        public enum BoardSpecifiers
        {
            Default = 0,
            NucleoF401RE = 1,
            NucleoF446RE = 2
        }

        public int BoardId { get; set; }
        public int BoardSpecifier { get; set; }
        public int BoardId1 { get; set; }
        public int BoardId2 { get; set; }
        public int BoardId3 { get; set; }
        public string Description { get; set; }

        public Board(int boardSpecifier, int boardId1, int boardId2, int boardId3)
        {
            BoardSpecifier = boardSpecifier;
            BoardId1 = boardId1;
            BoardId2 = boardId2;
            BoardId3 = boardId3;
            string? descString = Enum.GetName(typeof(BoardSpecifiers), BoardSpecifier);
            Description = descString == null ? "" : descString;
        }
    }
}
