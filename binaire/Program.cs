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
// Date of file creation: 2022-04-04
// List of autors: Lucas Drack
//########################################################################



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

