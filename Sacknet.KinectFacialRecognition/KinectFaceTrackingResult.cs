using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Results from kinect face tracking
    /// </summary>
    public class KinectFaceTrackingResult
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
        /// Initializes a new instance of the KinectFaceTrackingResult class
        /// </summary>
        public KinectFaceTrackingResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the KinectFaceTrackingResult class from a set of Kinect face points
        /// </summary>
        public KinectFaceTrackingResult(FaceModel faceModel, FaceModel constructedFaceModel, FaceModelBuilderCollectionStatus builderStatus, FaceAlignment faceAlignment, CoordinateMapper mapper)
        {
            this.FaceModel = faceModel;
            this.ConstructedFaceModel = constructedFaceModel;
            this.BuilderStatus = builderStatus;
            this.FaceAlignment = faceAlignment;

            var vertices = faceModel.CalculateVerticesForAlignment(faceAlignment);
            this.ColorSpaceFacePoints = this.FaceBoundaryPoints(vertices, mapper);

            // Calculate facerect manually from facepoints
            var rectX = this.ColorSpaceFacePoints.Min(x => x.X);
            var rectWidth = this.ColorSpaceFacePoints.Max(x => x.X) - rectX;
            var rectY = this.ColorSpaceFacePoints.Min(x => x.Y);
            var rectHeight = this.ColorSpaceFacePoints.Max(x => x.Y) - rectY;

            this.FaceRect = new System.Drawing.Rectangle(rectX, rectY, rectWidth, rectHeight);
        }

        /// <summary>
        /// Gets the face model
        /// </summary>
        public FaceModel FaceModel { get; private set; }

        /// <summary>
        /// Gets a face model constructed from the HD face builder
        /// </summary>
        public FaceModel ConstructedFaceModel { get; private set; }

        /// <summary>
        /// Gets the status of the face builder
        /// </summary>
        public FaceModelBuilderCollectionStatus BuilderStatus { get; private set; }

        /// <summary>
        /// Gets the face alignment
        /// </summary>
        public FaceAlignment FaceAlignment { get; private set; }

        /// <summary>
        /// Gets the outline of the face
        /// </summary>
        public List<System.Drawing.Point> ColorSpaceFacePoints { get; private set; }

        /// <summary>
        /// Gets the face bounding box
        /// </summary>
        public System.Drawing.Rectangle FaceRect { get; private set; }

        /// <summary>
        /// Gets a path tracing the face in the color space
        /// </summary>
        public GraphicsPath GetFacePath()
        {
            // Create a path tracing the face and draw on the processed image
            var path = new GraphicsPath();

            foreach (var point in this.ColorSpaceFacePoints)
            {
                path.AddLine(point, point);
            }

            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// Returns a cropped image of the face from the color space bitmap
        /// </summary>
        public Bitmap GetCroppedFace(Bitmap colorSpaceBitmap)
        {
            GraphicsPath origPath = this.GetFacePath();

            var minX = (int)origPath.PathPoints.Min(x => x.X);
            var maxX = (int)origPath.PathPoints.Max(x => x.X);
            var minY = (int)origPath.PathPoints.Min(x => x.Y);
            var maxY = (int)origPath.PathPoints.Max(x => x.Y);
            var width = maxX - minX;
            var height = maxY - minY;

            // Create a cropped path tracing the face...
            var croppedPath = new GraphicsPath();

            foreach (var point in this.ColorSpaceFacePoints)
            {
                var croppedPoint = new System.Drawing.Point(point.X - minX, point.Y - minY);
                croppedPath.AddLine(croppedPoint, croppedPoint);
            }

            croppedPath.CloseFigure();

            // ...and create a cropped image to use for facial recognition
            var croppedBmp = new Bitmap(width, height);

            using (var croppedG = Graphics.FromImage(croppedBmp))
            {
                croppedG.FillRectangle(Brushes.Gray, 0, 0, width, height);
                croppedG.SetClip(croppedPath);
                croppedG.DrawImage(colorSpaceBitmap, minX * -1, minY * -1);
            }

            return croppedBmp;
        }

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
