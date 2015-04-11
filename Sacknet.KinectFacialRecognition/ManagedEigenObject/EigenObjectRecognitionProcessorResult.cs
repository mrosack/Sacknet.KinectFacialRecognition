using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition.ManagedEigenObject
{
    /// <summary>
    /// A recognition processor result for managed eigen object recognition
    /// </summary>
    public class EigenObjectRecognitionProcessorResult : IRecognitionProcessorResult, IEigenObjectTargetFace
    {
        /// <summary>
        /// Gets or sets the key of the detected face
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the grayscale, 100x100 image of the face to use for matching
        /// </summary>
        public Bitmap Image { get; set; }

        /// <summary>
        /// Gets or sets the distance away from a perfectly recognized face
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            if (this.Image != null)
            {
                this.Image.Dispose();
                this.Image = null;
            }
        }
    }
}
