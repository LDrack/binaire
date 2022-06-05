//////  Binaire - Utility for SRAM PUF analysis
  ////  Lucas Drack
    //  2022-05-10

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
