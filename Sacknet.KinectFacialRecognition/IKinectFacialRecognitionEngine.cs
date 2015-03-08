using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Defines the public interface to the Kinect Facial Recognition Engine
    /// </summary>
    public interface IKinectFacialRecognitionEngine : IDisposable
    {
        /// <summary>
        /// Raised when recognition has been completed for a frame
        /// </summary>
        event EventHandler<RecognitionResult> RecognitionComplete;

        /// <summary>
        /// Gets or sets a value indicating whether images will be processed for facial recognition.  If false, the video stream will be passed through untouched.
        /// </summary>
        bool ProcessingEnabled { get; set; }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        void SetTargetFaces(IEnumerable<TargetFace> faces);

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        /// <param name="threshold">Eigen distance threshold for a match.  1500-2000 is a reasonable value.  0 will never match.</param>
        void SetTargetFaces(IEnumerable<TargetFace> faces, double threshold);
    }
}
