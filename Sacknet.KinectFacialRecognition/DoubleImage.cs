using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// An "Image" consisting of double precision floating point pixels
    /// </summary>
    public class DoubleImage
    {
        /// <summary>
        /// Initializes a new instance of the DoubleImage class
        /// </summary>
        public DoubleImage(int width, int height)
        {
            this.Step = width;
            this.Size = new Size(width, height);
            this.Data = new double[width * height];
        }

        /// <summary>
        /// Gets the step of the image (width of the row)
        /// </summary>
        public int Step { get; private set; }

        /// <summary>
        /// Gets the size of the image
        /// </summary>
        public Size Size { get; private set; }

        /// <summary>
        /// Gets the raw image data
        /// </summary>
        public double[] Data { get; private set; }
    }
}
