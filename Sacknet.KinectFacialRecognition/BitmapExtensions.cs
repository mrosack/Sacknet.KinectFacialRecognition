using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
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
        /// Little-endian Format32bppArgb is stored as BGRA
        /// </summary>
        private enum RGB
        {
            B = 0,
            G = 1,
            R = 2
        }

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

                // Fixes "ringing" around the borders...
                attributes.SetWrapMode(WrapMode.TileFlipXY);

                // Draw the original image on the new image using the grayscale color matrix
                g.CompositingMode = CompositingMode.SourceCopy;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, newWidth, newHeight), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return newBitmap;
        }

        /// <summary>
        /// Copies a bitmap to a byte array
        /// </summary>
        public static byte[] CopyBitmapToByteArray(this Bitmap bitmap, out int step)
        {
            var bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            step = bits.Stride;

            byte[] result = new byte[step * bitmap.Height];
            Marshal.Copy(bits.Scan0, result, 0, result.Length);
            bitmap.UnlockBits(bits);

            return result;
        }

        /// <summary>
        /// Copies a grayscale bitmap to a byte array
        /// </summary>
        public static byte[] CopyGrayscaleBitmapToByteArray(this Bitmap bitmap, out int step)
        {
            var baseResult = bitmap.CopyBitmapToByteArray(out step);

            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                step /= 4;
                byte[] result = new byte[step * bitmap.Height];

                for (int i = 0; i < result.Length; i++)
                    result[i] = baseResult[i * 4];

                return result;
            }

            return baseResult;
        }

        /// <summary>
        /// Histogram equalizes the input bitmap
        /// </summary>
        public static void HistogramEqualize(this Bitmap bitmap)
        {
            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                throw new ArgumentException("Input bitmap must be 32bppargb!");

            int step;
            var rawData = bitmap.CopyBitmapToByteArray(out step);

            // Get the Lookup table for histogram equalization
            var histLut = HistogramEqualizationLut(rawData);

            for (int i = 0; i < rawData.Length; i += 4)
            {
                // Update pixels according to LUT
                rawData[i + (int)RGB.R] = (byte)histLut[(int)RGB.R, rawData[i + (int)RGB.R]];
                rawData[i + (int)RGB.G] = (byte)histLut[(int)RGB.G, rawData[i + (int)RGB.G]];
                rawData[i + (int)RGB.B] = (byte)histLut[(int)RGB.B, rawData[i + (int)RGB.B]];
            }

            // Copy bits back into the bitmap...
            var bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            Marshal.Copy(rawData, 0, bits.Scan0, rawData.Length);
            bitmap.UnlockBits(bits);
        }

        /// <summary>
        /// Gets the histogram equalization lookup table for separate R, G, B channels
        /// </summary>
        private static int[,] HistogramEqualizationLut(byte[] rawData)
        {
            // Get an image histogram - calculated values by R, G, B channels
            int[,] imageHist = ImageHistogram(rawData);

            // Create the lookup table
            int[,] imageLut = new int[3, 256];

            long sumr = 0;
            long sumg = 0;
            long sumb = 0;

            // Calculate the scale factor
            float scaleFactor = (float)(255.0 / (rawData.Length / 4));

            for (int i = 0; i < 256; i++)
            {
                sumr += imageHist[(int)RGB.R, i];
                int valr = (int)(sumr * scaleFactor);
                imageLut[(int)RGB.R, i] = valr > 255 ? 255 : valr;

                sumg += imageHist[(int)RGB.G, i];
                int valg = (int)(sumg * scaleFactor);
                imageLut[(int)RGB.G, i] = valg > 255 ? 255 : valg;

                sumb += imageHist[(int)RGB.B, i];
                int valb = (int)(sumb * scaleFactor);
                imageLut[(int)RGB.B, i] = valb > 255 ? 255 : valb;
            }

            return imageLut;
        }

        /// <summary>
        /// Returns an array containing histogram values for separate R, G, B channels
        /// </summary>
        private static int[,] ImageHistogram(byte[] rawData)
        {
            var result = new int[3, 256];

            for (int i = 0; i < rawData.Length; i += 4)
            {
                result[(int)RGB.R, rawData[i + (int)RGB.R]]++;
                result[(int)RGB.G, rawData[i + (int)RGB.G]]++;
                result[(int)RGB.B, rawData[i + (int)RGB.B]]++;
            }

            return result;
        }
    }
}
