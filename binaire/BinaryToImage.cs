using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace binaire
{
    // Helper class for generating binary (black & white) images from byte arrays.
    // Each of the array's bytes will result in 8 pixels in the final image, where 0s in the array
    // create black pixels and 1s create white pixels.
    public static class BinaryToImage
    {
        // Generate black and white image. 1 byte = 8 pixels, written from left to right.
        // b.Length*8 must equal width*height!
        public static void Write(byte[] b, int width, int height, string fname)
        {
            if (b == null) { throw new ArgumentNullException("b must not be null."); }
            if (width <= 0 || height <= 0) { throw new ArgumentException("width and height must be positive integers."); }
            if (b.Length * 8 != width * height) { throw new ArgumentException("b.Length*8 must equal width*height."); }
            string filename = Path.HasExtension(fname) ? fname : fname + ".png";

            System.IO.FileInfo? fi = null;
            try
            {
                fi = new System.IO.FileInfo(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0 is not a valid path. {1}", filename, e.Message);
                return;
            }

            if (fi.Exists)
            {
                string? input = "";
                while (input.StartsWith("y") || input.StartsWith("n"))
                {
                    Console.WriteLine("{0} already exists. Save anyways? Enter y or n.", Path.GetFullPath(filename));
                    input = Console.ReadLine();
                }
                if (input == "n")
                {
                    Console.WriteLine("Alright. The image won't be saved.");
                    return;
                }
            }


            Bitmap bmp = GetBinaryBitmap(b, width, height);
            bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            bmp.Dispose();

            Console.WriteLine("File saved.");

        }

        private static Bitmap GetBinaryBitmap(byte[] b, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bmp);
            int bitIndex = 0;
            int byteIndex = 0;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // Bit is 0: black brush
                    // Bit is 1: white brush
                    Brush brush = (b[byteIndex] & (1 << (7 - bitIndex))) != 0 ? Brushes.White : Brushes.Black;
                    
                    g.FillRectangle(brush, j, i, 1, 1);
                    if (bitIndex == 7) { byteIndex++; }
                    bitIndex = (bitIndex + 1) % 8;
                }
            }

            g.Dispose();
            return bmp;
        }
    }
}
