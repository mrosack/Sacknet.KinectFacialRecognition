using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sacknet.KinectFacialRecognition.ManagedEigenObject;

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
        /// Gets or sets the active facial recognition processors
        /// </summary>
        IEnumerable<IRecognitionProcessor> Processors { get; set; }
    }
}
