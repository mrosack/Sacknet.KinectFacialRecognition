using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Calculates normalized 3D face points
        /// </summary>
        public static List<Point3D> CalculateNormalized3DFacePoints(this IReadOnlyList<CameraSpacePoint> vertices, FaceAlignment alignment)
        {
            float pitch, yaw, roll;
            alignment.ExtractFaceRotationInRadians(out pitch, out yaw, out roll);

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
        /// Converts rotation quaternion to radians 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        public static void ExtractFaceRotationInRadians(this FaceAlignment faceAlignment, out float pitch, out float yaw, out float roll)
        {
            var rotQuaternion = faceAlignment.FaceOrientation;

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
    }
}
