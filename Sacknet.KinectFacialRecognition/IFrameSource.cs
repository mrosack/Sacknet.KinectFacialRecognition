using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// An object capable of providing frames to the facial recognition engine
    /// </summary>
    public interface IFrameSource
    {
        /// <summary>
        /// Raised when a new frame of data is available
        /// </summary>
        event EventHandler<FrameData> FrameDataUpdated;
    }
}
