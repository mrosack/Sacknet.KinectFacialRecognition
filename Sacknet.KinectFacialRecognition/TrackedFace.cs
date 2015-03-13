using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Information about a single tracked face found in the frame
    /// </summary>
    public class TrackedFace : IDisposable
    {
        /// <summary>
        /// Gets or sets the results from kinect face tracking
        /// </summary>
        public KinectFaceTrackingResult TrackingResults { get; set; }

        /// <summary>
        /// Gets or sets the results from all enabled recognition processors
        /// </summary>
        public IEnumerable<IRecognitionProcessorResult> ProcessorResults { get; set; }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            if (this.ProcessorResults != null)
            {
                foreach (var result in this.ProcessorResults)
                    result.Dispose();

                this.ProcessorResults = null;
            }
        }
    }
}
