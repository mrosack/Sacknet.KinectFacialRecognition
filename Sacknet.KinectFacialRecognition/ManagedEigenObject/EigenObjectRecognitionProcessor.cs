using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition.ManagedEigenObject
{
    /// <summary>
    /// Performs facial recognition
    /// </summary>
    public class EigenObjectRecognitionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the EigenObjectRecognitionProcessor class without any trained faces
        /// </summary>
        public EigenObjectRecognitionProcessor()
        {
        }

        /// <summary>
        /// Initializes a new instance of the EigenObjectRecognitionProcessor class
        /// </summary>
        public EigenObjectRecognitionProcessor(IEnumerable<EigenObjectTargetFace> faces)
        {
            this.Recognizer = new EigenObjectRecognizer(faces);
        }

        /// <summary>
        /// Initializes a new instance of the EigenObjectRecognitionProcessor class
        /// </summary>
        public EigenObjectRecognitionProcessor(IEnumerable<EigenObjectTargetFace> faces, double threshold)
        {
            this.Recognizer = new EigenObjectRecognizer(faces, threshold);
        }

        /// <summary>
        /// Gets the facial recognition engine
        /// </summary>
        protected EigenObjectRecognizer Recognizer { get; private set; }

        /// <summary>
        /// Attempt to find a trained face in the original bitmap
        /// </summary>
        public void Process(RecognitionResult result, KinectFaceTrackingResult trackingResults)
        {
            GraphicsPath origPath;

            using (var g = Graphics.FromImage(result.ProcessedBitmap))
            {
                // Create a path tracing the face and draw on the processed image
                origPath = new GraphicsPath();

                foreach (var point in trackingResults.FacePoints)
                {
                    origPath.AddLine(point, point);
                }

                origPath.CloseFigure();
                g.DrawPath(new Pen(Color.Red, 2), origPath);
            }

            var minX = (int)origPath.PathPoints.Min(x => x.X);
            var maxX = (int)origPath.PathPoints.Max(x => x.X);
            var minY = (int)origPath.PathPoints.Min(x => x.Y);
            var maxY = (int)origPath.PathPoints.Max(x => x.Y);
            var width = maxX - minX;
            var height = maxY - minY;

            // Create a cropped path tracing the face...
            var croppedPath = new GraphicsPath();

            foreach (var point in trackingResults.FacePoints)
            {
                var croppedPoint = new System.Drawing.Point(point.X - minX, point.Y - minY);
                croppedPath.AddLine(croppedPoint, croppedPoint);
            }

            croppedPath.CloseFigure();

            // ...and create a cropped image to use for facial recognition
            using (var croppedBmp = new Bitmap(width, height))
            {
                using (var croppedG = Graphics.FromImage(croppedBmp))
                {
                    croppedG.FillRectangle(Brushes.Gray, 0, 0, width, height);
                    croppedG.SetClip(croppedPath);
                    croppedG.DrawImage(result.OriginalBitmap, minX * -1, minY * -1);
                }

                using (var grayBmp = croppedBmp.MakeGrayscale(100, 100))
                {
                    grayBmp.HistogramEqualize();

                    string key = null;
                    float eigenDistance = -1;

                    if (this.Recognizer != null)
                        key = this.Recognizer.Recognize(grayBmp, out eigenDistance);

                    // Save detection info
                    result.Faces = new List<TrackedFace>()
                    {
                        new TrackedFace()
                        {
                            TrackingResults = trackingResults,
                            ProcessorResults = new List<IRecognitionProcessorResult>
                            {
                                new EigenObjectRecognitionProcessorResult
                                {
                                    EigenDistance = eigenDistance,
                                    GrayFace = (Bitmap)grayBmp.Clone(),
                                    Key = key
                                }
                            }
                        }
                    };
                }
            }
        }
    }
}
