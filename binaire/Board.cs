//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-04-27

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
        // For now, only Nucleo F401RE is used, so this is kept simple.
        public const int BS_NucleoF401RE = 1;

        public int BoardId { get; set; }
        public int BoardSpecifier { get; set; }
        public int BoardId1 { get; set; }
        public int BoardId2 { get; set; }
        public int BoardId3 { get; set; }
        public string Description { get; set; }

        public Board(int boardSpecifier, int boardId1, int boardId2, int boardId3, string description)
        {
            BoardSpecifier = boardSpecifier;
            BoardId1 = boardId1;
            BoardId2 = boardId2;
            BoardId3 = boardId3;
            Description = description;
        }
    }
}
