//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-04-24

using System;
using System.IO.Ports;
using System.Threading;
//using binaire;

namespace binaire
{
    internal class Program
    {

        static void Main(string[] args)
        {
            binaire.Run();
            binaire.CloseComPort();
        }
    }
}

