using System;
using System.Collections.Generic;
using Microsoft.Kinect.Toolkit.FaceTracking;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Results from face tracking
    /// </summary>
    public class TrackingResults
    {
        /// <summary>
        /// Gets or sets the 3D points of the face
        /// </summary>
        public EnumIndexableCollection<FeaturePoint, PointF> FacePoints { get; set; }

        /// <summary>
        /// Gets or sets the face bounding box
        /// </summary>
        public Rect FaceRect { get; set; }

        /// <summary>
        /// Returns only the bounding points for the face (in order so you can draw a loop)
        /// </summary>
        public IEnumerable<PointF> FaceBoundaryPoints()
        {
            yield return this.FacePoints[FeaturePoint.TopSkull];
            yield return this.FacePoints[44];
            yield return this.FacePoints[45];
            yield return this.FacePoints[47];
            yield return this.FacePoints[62];
            yield return this.FacePoints[61];
            yield return this.FacePoints[FeaturePoint.LeftSideOfCheek];
            yield return this.FacePoints[FeaturePoint.LeftOfChin];
            yield return this.FacePoints[FeaturePoint.BottomOfChin];
            yield return this.FacePoints[FeaturePoint.RightOfChin];
            yield return this.FacePoints[FeaturePoint.RightSideOfChin];
            yield return this.FacePoints[28];
            yield return this.FacePoints[29];
            yield return this.FacePoints[14];
            yield return this.FacePoints[12];
            yield return this.FacePoints[11];
        }
    }
}
