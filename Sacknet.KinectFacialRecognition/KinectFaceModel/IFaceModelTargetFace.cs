using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition.KinectFaceModel
{
    /// <summary>
    /// A target face for face model recognition
    /// </summary>
    public interface IFaceModelTargetFace
    {
        /// <summary>
        /// Gets or sets the key returned when this face is found
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Gets or sets the normalized 3D face points
        /// </summary>
        List<Point3D> Normalized3DFacePoints { get; set; }
    }
}
