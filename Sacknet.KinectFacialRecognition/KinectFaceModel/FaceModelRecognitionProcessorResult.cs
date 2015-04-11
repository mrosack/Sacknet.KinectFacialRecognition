using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition.KinectFaceModel
{
    /// <summary>
    /// A recognition processor result for face model recognition
    /// </summary>
    public class FaceModelRecognitionProcessorResult : IRecognitionProcessorResult, IFaceModelTargetFace
    {
        /// <summary>
        /// Gets or sets the key returned when this face is found
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the normalized 3D face points
        /// </summary>
        public List<Point3D> Normalized3DFacePoints { get; set; }

        /// <summary>
        /// Gets or sets the score of the match (if Key is set)
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            // Nothing unmanaged here...
        }
    }
}
