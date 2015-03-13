using System;
using System.Collections.Generic;
using System.Drawing;
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
        /// Gets or sets the original color frame
        /// </summary>
        public Bitmap OriginalBitmap { get; set; }

        /// <summary>
        /// Gets or sets the processed color frame (with a boundary drawn around the face)
        /// </summary>
        public Bitmap ProcessedBitmap { get; set; }

        /// <summary>
        /// Gets or sets a list of faces detected in the image
        /// </summary>
        public IEnumerable<TrackedFace> Faces { get; set; }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            if (this.OriginalBitmap != null)
            {
                this.OriginalBitmap.Dispose();
                this.OriginalBitmap = null;
            }

            if (this.ProcessedBitmap != null)
            {
                this.ProcessedBitmap.Dispose();
                this.ProcessedBitmap = null;
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
