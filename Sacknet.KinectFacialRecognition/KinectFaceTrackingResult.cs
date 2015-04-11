using System;
using System.Collections.Generic;
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
        public KinectFaceTrackingResult(FaceModel faceModel, FaceAlignment faceAlignment, CoordinateMapper mapper)
        {
            this.FaceModel = faceModel;
            this.FaceAlignment = faceAlignment;

            var vertices = faceModel.CalculateVerticesForAlignment(faceAlignment);
            this.ColorSpaceFacePoints = this.FaceBoundaryPoints(vertices, mapper);
            this.Normalized3DFacePoints = CalculateNormalized3DFacePoints(vertices, this.FaceAlignment);

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
        /// Gets the face alignment
        /// </summary>
        public FaceAlignment FaceAlignment { get; private set; }

        /// <summary>
        /// Gets the outline of the face
        /// </summary>
        public List<System.Drawing.Point> ColorSpaceFacePoints { get; private set; }

        /// <summary>
        /// Gets the normalized 3D face points
        /// </summary>
        public List<Point3D> Normalized3DFacePoints { get; private set; }

        /// <summary>
        /// Gets the face bounding box
        /// </summary>
        public System.Drawing.Rectangle FaceRect { get; private set; }

        /// <summary>
        /// Calculates normalized 3D face points
        /// </summary>
        private static List<Point3D> CalculateNormalized3DFacePoints(IReadOnlyList<CameraSpacePoint> vertices, FaceAlignment alignment)
        {
            float pitch, yaw, roll;
            ExtractFaceRotationInDegrees(alignment.FaceOrientation, out pitch, out yaw, out roll);
            Console.WriteLine("X: {0}, Y: {1}, Z: {2}", pitch, yaw, roll);

            var result = new List<Point3D>();
            float maxValue = 0;

            foreach (var vertex in vertices)
            {
                var x = vertex.X - alignment.HeadPivotPoint.X;
                var y = vertex.Y - alignment.HeadPivotPoint.Y;
                var z = vertex.Z - alignment.HeadPivotPoint.Z;

                RotateX3D(pitch * -1, ref y, ref z);
                RotateY3D(yaw * -1, ref x, ref z);
                RotateZ3D(roll * -1, ref x, ref y);

                result.Add(new Point3D { X = x, Y = y, Z = z });

                maxValue = Math.Max(maxValue, Math.Abs(x));
                maxValue = Math.Max(maxValue, Math.Abs(y));
                maxValue = Math.Max(maxValue, Math.Abs(z));
            }

            var ratio = 1 / maxValue;

            foreach (var point in result)
            {
                point.X *= ratio;
                point.Y *= ratio;
                point.Z *= ratio;
            }

            return result;
        }

        /// <summary>
        /// Rotates delta radians around the Z axis
        /// </summary>
        private static void RotateZ3D(float delta, ref float x, ref float y)
        {
            var sin = (float)Math.Sin(delta);
            var cos = (float)Math.Cos(delta);
            var origX = x;
            var origY = y;

            x = (origX * cos) - (origY * sin);
            y = (origY * cos) + (origX * sin);
        }

        /// <summary>
        /// Rotates delta radians around the Y axis
        /// </summary>
        private static void RotateY3D(float delta, ref float x, ref float z)
        {
            RotateZ3D(delta, ref x, ref z);
        }

        /// <summary>
        /// Rotates delta radians around the X axis
        /// </summary>
        private static void RotateX3D(float delta, ref float y, ref float z)
        {
            RotateZ3D(delta, ref y, ref z);
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
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out float pitch, out float yaw, out float roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            pitch = (float)Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z));
            yaw = (float)Math.Asin(2 * ((w * y) - (x * z))) * -1;
            roll = (float)Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z));
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
