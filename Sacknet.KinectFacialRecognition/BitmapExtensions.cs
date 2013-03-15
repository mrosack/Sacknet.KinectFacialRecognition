using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Extension methods for performing operations on bitmaps
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        /// Converts a bitmap to grayscale.  Based on:
        /// http://tech.pro/tutorial/660/csharp-tutorial-convert-a-color-image-to-grayscale
        /// </summary>
        public static Bitmap MakeGrayscale(this Bitmap original, int newWidth, int newHeight)
        {
            // Create a blank bitmap the desired size
            Bitmap newBitmap = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                // Create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(new float[][] 
                {
                    new float[] { .3f, .3f, .3f, 0, 0 },
                    new float[] { .59f, .59f, .59f, 0, 0 },
                    new float[] { .11f, .11f, .11f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });

                ImageAttributes attributes = new ImageAttributes();

                // Set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                // Draw the original image on the new image using the grayscale color matrix
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, new Rectangle(0, 0, newWidth, newHeight), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return newBitmap;
        }

        /// <summary>
        /// Histogram equalizes the input bitmap
        /// </summary>
        public static void HistogramEqualize(this Bitmap bitmap)
        {
            // Get the Lookup table for histogram equalization
            var histLut = HistogramEqualizationLut(bitmap);

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    // Get pixels by R, G, B
                    var origPixel = bitmap.GetPixel(x, y);
                    bitmap.SetPixel(x, y, Color.FromArgb(histLut[0, origPixel.R], histLut[0, origPixel.G], histLut[0, origPixel.B]));
                }
            }
        }

        /// <summary>
        /// Gets the histogram equalization lookup table for separate R, G, B channels
        /// </summary>
        private static int[,] HistogramEqualizationLut(Bitmap input)
        {
            // Get an image histogram - calculated values by R, G, B channels
            int[,] imageHist = ImageHistogram(input);

            // Create the lookup table
            int[,] imageLut = new int[3, 256];

            long sumr = 0;
            long sumg = 0;
            long sumb = 0;

            // Calculate the scale factor
            float scaleFactor = (float)(255.0 / (input.Width * input.Height));

            for (int i = 0; i < 256; i++)
            {
                sumr += imageHist[0, i];
                int valr = (int)(sumr * scaleFactor);
                imageLut[0, i] = valr > 255 ? 255 : valr;

                sumg += imageHist[1, i];
                int valg = (int)(sumg * scaleFactor);
                imageLut[1, i] = valg > 255 ? 255 : valg;

                sumb += imageHist[2, i];
                int valb = (int)(sumb * scaleFactor);
                imageLut[2, i] = valb > 255 ? 255 : valb;
            }

            return imageLut;
        }

        /// <summary>
        /// Returns an array containing histogram values for separate R, G, B channels
        /// </summary>
        private static int[,] ImageHistogram(Bitmap input)
        {
            var result = new int[3, 256];

            for (int x = 0; x < input.Width; x++)
            {
                for (int y = 0; y < input.Height; y++)
                {
                    var pixel = input.GetPixel(x, y);

                    // Increase the values of colors
                    result[0, pixel.R]++;
                    result[1, pixel.G]++;
                    result[2, pixel.B]++;
                }
            }

            return result;
        }
    }
}
