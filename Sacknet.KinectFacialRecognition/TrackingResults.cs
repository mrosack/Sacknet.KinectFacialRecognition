using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kinect.Toolkit.FaceTracking;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Results from face tracking
    /// </summary>
    public class TrackingResults
    {
        /// <summary>
        /// Initializes a new instance of the TrackingResults class
        /// </summary>
        public TrackingResults()
        {
        }

        /// <summary>
        /// Initializes a new instance of the TrackingResults class from a set of Kinect face points
        /// </summary>
        public TrackingResults(EnumIndexableCollection<FeaturePoint, PointF> facePoints)
        {
            this.FacePoints = this.FaceBoundaryPoints(facePoints);

            // Calculate facerect manually from facepoints
            var rectX = this.FacePoints.Min(x => x.X);
            var rectWidth = this.FacePoints.Max(x => x.X) - rectX;
            var rectY = this.FacePoints.Min(x => x.Y);
            var rectHeight = this.FacePoints.Max(x => x.Y) - rectY;

            this.FaceRect = new System.Drawing.Rectangle(rectX, rectY, rectWidth, rectHeight);
        }

        /// <summary>
        /// Gets or sets the 3D points of the face
        /// </summary>
        public List<System.Drawing.Point> FacePoints { get; set; }

        /// <summary>
        /// Gets or sets the face bounding box
        /// </summary>
        public System.Drawing.Rectangle FaceRect { get; set; }

        /// <summary>
        /// Returns only the bounding points for the face (in order so you can draw a loop)
        /// </summary>
        private List<System.Drawing.Point> FaceBoundaryPoints(EnumIndexableCollection<FeaturePoint, PointF> facePoints)
        {
            var result = new List<System.Drawing.Point>();

            result.Add(this.TranslatePoint(facePoints[FeaturePoint.TopSkull]));
            result.Add(this.TranslatePoint(facePoints[44]));
            result.Add(this.TranslatePoint(facePoints[45]));
            result.Add(this.TranslatePoint(facePoints[47]));
            result.Add(this.TranslatePoint(facePoints[62]));
            result.Add(this.TranslatePoint(facePoints[61]));
            result.Add(this.TranslatePoint(facePoints[FeaturePoint.LeftSideOfCheek]));
            result.Add(this.TranslatePoint(facePoints[FeaturePoint.LeftOfChin]));
            result.Add(this.TranslatePoint(facePoints[FeaturePoint.BottomOfChin]));
            result.Add(this.TranslatePoint(facePoints[FeaturePoint.RightOfChin]));
            result.Add(this.TranslatePoint(facePoints[FeaturePoint.RightSideOfChin]));
            result.Add(this.TranslatePoint(facePoints[28]));
            result.Add(this.TranslatePoint(facePoints[29]));
            result.Add(this.TranslatePoint(facePoints[14]));
            result.Add(this.TranslatePoint(facePoints[12]));
            result.Add(this.TranslatePoint(facePoints[11]));

            return result;
        }

        /// <summary>
        /// Translates between kinect and drawing points
        /// </summary>
        private System.Drawing.Point TranslatePoint(Microsoft.Kinect.Toolkit.FaceTracking.PointF point)
        {
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }
    }
}
