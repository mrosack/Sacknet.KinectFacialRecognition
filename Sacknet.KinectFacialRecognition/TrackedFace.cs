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
        public KinectFaceTrackingResult TrackingResult { get; set; }

        /// <summary>
        /// Gets or sets the results from all enabled recognition processors
        /// </summary>
        public IEnumerable<IRecognitionProcessorResult> ProcessorResults { get; set; }

        /// <summary>
        /// Gets the key of the face if processing was successful and consistent between processors
        /// </summary>
        public string Key
        {
            get
            {
                var keyResults = this.ProcessorResults.Where(x => !string.IsNullOrEmpty(x.Key)).Select(x => x.Key).Distinct().ToList();

                if (keyResults.Count == 1)
                    return keyResults.First();

                // If we have conflicting results, or no matches, return null
                return null;
            }
        }

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
