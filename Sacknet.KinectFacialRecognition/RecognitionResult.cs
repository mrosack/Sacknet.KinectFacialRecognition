using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Holds the results of facial recognition
    /// </summary>
    public class RecognitionResult : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the RecognitionResult class
        /// </summary>
        public RecognitionResult()
        {
            this.Faces = new List<TrackedFace>();
        }

        /// <summary>
        /// Gets or sets the color space bitmap from the kinect
        /// </summary>
        public Bitmap ColorSpaceBitmap { get; set; }

        /// <summary>
        /// Gets or sets a list of faces detected in the image
        /// </summary>
        public IEnumerable<TrackedFace> Faces { get; set; }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            if (this.ColorSpaceBitmap != null)
            {
                this.ColorSpaceBitmap.Dispose();
                this.ColorSpaceBitmap = null;
            }

            if (this.Faces != null)
            {
                foreach (var face in this.Faces)
                    face.Dispose();

                this.Faces = null;
            }
        }
    }
}
