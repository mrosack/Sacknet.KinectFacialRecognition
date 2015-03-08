using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Results from face tracking
    /// </summary>
    public class TrackingResults
    {
        private static readonly List<int> BOUNDING_HIGH_DETAIL_FACE_POINTS = new List<int>
        {
            1245, 976, 977, 978, 1064, 1065, 1060, 1059, 1055, 1058, 1027, 988, 878, 879, 886, 889, 890,
            891, 295, 445, 250, 247, 245, 232, 335, 214, 462, 460, 537, 141, 548, 477, 478, 280, 36, 1254,
            1297, 37, 1073, 65, 429, 427, 57, (int)HighDetailFacePoints.LowerjawLeftend, 1309, 48, 41, 528,
            40, 533, 532, 530, 529, 531, 1039, 1038, 998, 1036, 1037, 1043, 1046, 1042, 1003, 1328,
            (int)HighDetailFacePoints.LowerjawRightend, 595, 581, 596, 1025, 1074, 1024, 1292
        };

        /// <summary>
        /// Initializes a new instance of the TrackingResults class
        /// </summary>
        public TrackingResults()
        {
        }

        /// <summary>
        /// Initializes a new instance of the TrackingResults class from a set of Kinect face points
        /// </summary>
        public TrackingResults(IReadOnlyList<CameraSpacePoint> vertices, CoordinateMapper mapper)
        {
            this.FacePoints = this.FaceBoundaryPoints(vertices, mapper);

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
        /// Since the HighDetailFacePoints aren't well documented, this is some code I used to figure out which ones to use to get the face outline
        /// </summary>
        private static List<int> CalculateBoundingHighDefinitionFacePoints(IReadOnlyList<CameraSpacePoint> vertices)
        {
            var vertexMaps = new List<VertexMap>();

            for (int i = 0; i < vertices.Count; i++)
            {
                vertexMaps.Add(new VertexMap
                {
                    Index = i,
                    Vertex = vertices[i]
                });
            }

            var avgXValue = vertexMaps.Average(x => x.Vertex.X);
            var avgYValue = vertexMaps.Average(x => x.Vertex.Y);

            foreach (var vertexMap in vertexMaps)
            {
                var xDistance = vertexMap.Vertex.X - avgXValue;
                var yDistance = vertexMap.Vertex.Y - avgYValue;

                vertexMap.Distance = Math.Sqrt(Math.Pow(xDistance, 2) + Math.Pow(yDistance, 2));
                var angle = Math.Atan2(yDistance, xDistance);

                vertexMap.Degree = (int)((angle > 0 ? angle : ((2 * Math.PI) + angle)) * 360 / (2 * Math.PI));
            }

            return vertexMaps.GroupBy(x => x.Degree / 5).OrderBy(x => x.Key).Select(vertexMapGroup =>
            {
                var maxDistance = vertexMapGroup.Max(x => x.Distance);
                return vertexMapGroup.First(x => x.Distance == maxDistance).Index;
            }).ToList();
        }

        /// <summary>
        /// Returns only the bounding points for the face (in order so you can draw a loop)
        /// </summary>
        private List<System.Drawing.Point> FaceBoundaryPoints(IReadOnlyList<CameraSpacePoint> vertices, CoordinateMapper mapper)
        {
            /*if (BOUNDING_HIGH_DETAIL_FACE_POINTS == null)
                BOUNDING_HIGH_DETAIL_FACE_POINTS = CalculateBoundingHighDefinitionFacePoints(vertices);*/

            return BOUNDING_HIGH_DETAIL_FACE_POINTS.Select(x => this.TranslatePoint(vertices[(int)x], mapper)).ToList();
        }

        /// <summary>
        /// Translates between kinect and drawing points
        /// </summary>
        private System.Drawing.Point TranslatePoint(CameraSpacePoint point, CoordinateMapper mapper)
        {
            var colorPoint = mapper.MapCameraPointToColorSpace(point);
            return new System.Drawing.Point((int)colorPoint.X, (int)colorPoint.Y);
        }

        /// <summary>
        /// Helps with calculating the appropriate high definition face points to use to bound the face
        /// </summary>
        private class VertexMap
        {
            /// <summary>
            /// Gets or sets the index of the vertex
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// Gets or sets the raw vertex data
            /// </summary>
            public CameraSpacePoint Vertex { get; set; }

            /// <summary>
            /// Gets or sets the degree from the center of the face this point lies on
            /// </summary>
            public int Degree { get; set; }

            /// <summary>
            /// Gets or sets the distance from the center of the face this point lies on
            /// </summary>
            public double Distance { get; set; }
        }
    }
}
