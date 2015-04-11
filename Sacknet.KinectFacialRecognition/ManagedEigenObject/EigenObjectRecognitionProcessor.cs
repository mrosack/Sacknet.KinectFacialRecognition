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
    public class EigenObjectRecognitionProcessor : IRecognitionProcessor
    {
        private object processingMutex = new object();

        /// <summary>
        /// Initializes a new instance of the EigenObjectRecognitionProcessor class without any trained faces
        /// </summary>
        public EigenObjectRecognitionProcessor()
        {
        }

        /// <summary>
        /// Initializes a new instance of the EigenObjectRecognitionProcessor class
        /// </summary>
        public EigenObjectRecognitionProcessor(IEnumerable<IEigenObjectTargetFace> faces)
        {
            this.SetTargetFaces(faces);
        }

        /// <summary>
        /// Initializes a new instance of the EigenObjectRecognitionProcessor class
        /// </summary>
        public EigenObjectRecognitionProcessor(IEnumerable<IEigenObjectTargetFace> faces, double threshold)
        {
            this.SetTargetFaces(faces, threshold);
        }

        /// <summary>
        /// Gets the facial recognition engine
        /// </summary>
        protected EigenObjectRecognizer Recognizer { get; private set; }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        public virtual void SetTargetFaces(IEnumerable<IEigenObjectTargetFace> faces)
        {
            this.SetTargetFaces(faces, 1750);
        }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        /// <param name="threshold">Eigen distance threshold for a match.  1500-2000 is a reasonable value.  0 will never match.</param>
        public virtual void SetTargetFaces(IEnumerable<IEigenObjectTargetFace> faces, double threshold)
        {
            lock (this.processingMutex)
            {
                if (faces != null && faces.Any())
                {
                    this.Recognizer = new EigenObjectRecognizer(faces, threshold);
                }
            }
        }

        /// <summary>
        /// Attempt to find a trained face in the original bitmap
        /// </summary>
        public IRecognitionProcessorResult Process(Bitmap colorSpaceBitmap, KinectFaceTrackingResult trackingResults)
        {
            lock (this.processingMutex)
            {
                using (var croppedBmp = trackingResults.GetCroppedFace(colorSpaceBitmap))
                {
                    using (var grayBmp = croppedBmp.MakeGrayscale(100, 100))
                    {
                        grayBmp.HistogramEqualize();

                        string key = null;
                        float eigenDistance = -1;

                        if (this.Recognizer != null)
                            key = this.Recognizer.Recognize(grayBmp, out eigenDistance);

                        // Save detection info
                        return new EigenObjectRecognitionProcessorResult
                        {
                            EigenDistance = eigenDistance,
                            Image = (Bitmap)grayBmp.Clone(),
                            Key = key
                        };
                    }
                }
            }
        }
    }
}
