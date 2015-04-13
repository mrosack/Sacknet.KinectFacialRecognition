using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Recognition processor interface
    /// </summary>
    public interface IRecognitionProcessor
    {
        /// <summary>
        /// Attempt to find a trained face
        /// </summary>
        IRecognitionProcessorResult Process(Bitmap colorSpaceBitmap, KinectFaceTrackingResult trackingResults);

        /// <summary>
        /// Loads the given target faces into the processor
        /// </summary>
        void SetTargetFaces(IEnumerable<ITargetFace> faces);
    }
}
