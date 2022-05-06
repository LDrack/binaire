using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectSharp;
using VectSharp.PDF;
using VectSharp.SVG;
using VectSharp.Raster;


namespace binaire
{
    // Helper class for generating binary (black & white) images from byte arrays.
    // Each of the array's bytes will result in 8 pixels in the final image, where 0s in the array
    // create black pixels and 1s create white pixels.
    public static class BinaryToImage
    {
        private static bool IsValidPath(string path)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
            }
            catch (Exception ex) { return false; }

            return true;
        }

        private static bool IsValidExtension(string ext)
        {
            if (ext == ".pdf" || ext == ".png" || ext == ".svg") { return true; }
            else { return false; }
        }


        // Generate black and white image. 1 byte = 8 pixels, written from left to right.
        // b.Length*8 must equal width*height!
        public static void SaveBitImage(byte[] b, int width, int height, string fname)
        {
            if (b == null) { throw new ArgumentNullException("b must not be null."); }
            if (width <= 0 || height <= 0) { throw new ArgumentException("width and height must be positive integers."); }
            if (b.Length * 8 != width * height) { throw new ArgumentException("b.Length*8 must equal width*height."); }

            string ext = Path.GetExtension(fname);
            if (!IsValidExtension(ext)) { throw new ArgumentException("Invalid filename. Must end with .pdf, .svg or .png"); }
            if (!IsValidPath(fname)) { throw new ArgumentException($"{fname} is not a valid path."); }

            Document doc = new Document();
            doc.Pages.Add(new Page(width, height));
            Graphics g = doc.Pages.Last().Graphics;
            FillGraphicsBlackAndWhite(g, b, width, height);

            if      (ext == ".pdf") { doc.SaveAsPDF(fname); }
            else if (ext == ".svg") { doc.Pages.Last().SaveAsSVG(fname); }
            else if (ext == ".png") { doc.Pages.Last().SaveAsPNG(fname); }
            else { throw new NotImplementedException(); }

            Console.WriteLine($"File saved as {fname}.");

        }

        public static void SaveHeatmapImage(int[] countArray, int nReadings, int width, int height, string fname)
        {
            if (countArray == null) { throw new ArgumentNullException("countArray must not be null."); }
            if (nReadings < 1) { throw new ArgumentException("nReadings must be at least 1."); }
            if (width <= 0 || height <= 0) { throw new ArgumentException("width and height must be positive integers."); }
            if (countArray.Length != width * height) { throw new ArgumentException("countArray.Length must equal width*height."); }
            if (countArray.Max() > nReadings) { throw new ArgumentException("countArray contains a number bigger than nReadings."); }


            string ext = Path.GetExtension(fname);
            if (!IsValidExtension(ext)) { throw new ArgumentException("Invalid filename. Must end with .pdf, .svg or .png"); }
            if (!IsValidPath(fname)) { throw new ArgumentException($"{fname} is not a valid path."); }

            Document doc = new Document();
            doc.Pages.Add(new Page(width, height));
            Graphics g = doc.Pages.Last().Graphics;
            FillGraphicsHeatmap(g, countArray, nReadings, width, height);

            if      (ext == ".pdf") { doc.SaveAsPDF(fname); }
            else if (ext == ".svg") { doc.Pages.Last().SaveAsSVG(fname); }
            else if (ext == ".png") { doc.Pages.Last().SaveAsPNG(fname); }
            else { throw new NotImplementedException(); }

            Console.WriteLine($"File saved as {fname}.");
        }

        private static void FillGraphicsBlackAndWhite(Graphics g, byte[] b, int width, int height)
        {
            int bitIndex = 0;
            int byteIndex = 0;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // Bit is 0: black brush
                    // Bit is 1: white brush
                    Colour brush = (b[byteIndex] & (1 << (7 - bitIndex))) != 0 ? Colours.White : Colours.Black;

                    g.FillRectangle(j, i, 1, 1, brush);
                    if (bitIndex == 7) { byteIndex++; }
                    bitIndex = (bitIndex + 1) % 8;
                }
            }
        }

        private static void FillGraphicsHeatmap(Graphics g, int[] data, int nReadings, int width, int height)
        {
            if (nReadings < 1) { throw new ArgumentException("nReadings must be at least 1."); }

            const float maxColorValue = 255f;
            int dataIdx = 0;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // Bit is always 0: black brush
                    // Bit is always 1: white brush
                    // Bit is inbetween: shade of gray
                    float percentage = (float)data[dataIdx] / (float)nReadings;
                    int colorValue = (int)(percentage * maxColorValue);
                    Colour brush = Colour.FromRgb(colorValue, colorValue, colorValue);

                    g.FillRectangle(j, i, 1, 1, brush);
                    dataIdx++;
                }
            }
        }
    }
}
