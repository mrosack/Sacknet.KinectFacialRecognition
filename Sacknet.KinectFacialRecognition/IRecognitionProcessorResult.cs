using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// A recognition result from a recognition processor
    /// </summary>
    public interface IRecognitionProcessorResult : IDisposable
    {
        /// <summary>
        /// Gets the key of the detected face
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the score (0 is perfect match)
        /// </summary>
        double Score { get; }
    }
}
