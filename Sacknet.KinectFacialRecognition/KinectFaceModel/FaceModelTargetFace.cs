using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition.KinectFaceModel
{
    /// <summary>
    /// A target face for face model recognition
    /// </summary>
    public class FaceModelTargetFace : IFaceModelTargetFace
    {
        /// <summary>
        /// Gets or sets the key returned when this face is found
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the detected hair color of the face
        /// </summary>
        public Color HairColor { get; set; }

        /// <summary>
        /// Gets or sets the detected skin color of the face
        /// </summary>
        public Color SkinColor { get; set; }

        /// <summary>
        /// Gets or sets the detected face deformations
        /// </summary>
        public IReadOnlyDictionary<FaceShapeDeformations, float> Deformations { get; set; }
    }
}
