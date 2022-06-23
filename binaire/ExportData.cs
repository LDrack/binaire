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
// File name: ExportData.cs
// Date of file creation: 2022-05-10
// List of autors: Lucas Drack
//########################################################################



using CsvHelper;
using System.Text;

namespace binaire
{
    public static class ExportData
    {
        // Replaced own implementation with CsvHelper library
        public static void WriteCsv<T>(List<T> genericList, string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(genericList);
            }
        }
    }
}
