using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// The frame data necessary to perform facial recognition
    /// </summary>
    public class FrameData
    {
        /// <summary>
        /// Initializes a new instance of the FrameData class
        /// </summary>
        public FrameData(ColorImageFrame colorFrame, DepthImageFrame depthFrame, Skeleton trackedSkeleton)
        {
            this.ColorFrame = colorFrame;
            this.DepthFrame = depthFrame;
            this.TrackedSkeleton = trackedSkeleton;
        }

        /// <summary>
        /// Gets or sets the color frame
        /// </summary>
        public ColorImageFrame ColorFrame { get; set; }

        /// <summary>
        /// Gets or sets the depth frame
        /// </summary>
        public DepthImageFrame DepthFrame { get; set; }

        /// <summary>
        /// Gets or sets the tracked skeleton
        /// </summary>
        public Skeleton TrackedSkeleton { get; set; }
    }
}
