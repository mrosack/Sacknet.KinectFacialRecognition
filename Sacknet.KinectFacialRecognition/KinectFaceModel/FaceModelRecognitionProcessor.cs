using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Face;

namespace Sacknet.KinectFacialRecognition.KinectFaceModel
{
    /// <summary>
    /// Analyzes the Kinect Face Model to recognize a face
    /// </summary>
    public class FaceModelRecognitionProcessor : IRecognitionProcessor
    {
        private object processingMutex = new object();
        private IEnumerable<IFaceModelTargetFace> faces = new List<IFaceModelTargetFace>();
        
        /// <summary>
        /// Initializes a new instance of the FaceModelRecognitionProcessor class
        /// </summary>
        public FaceModelRecognitionProcessor()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FaceModelRecognitionProcessor class
        /// </summary>
        public FaceModelRecognitionProcessor(IEnumerable<IFaceModelTargetFace> faces)
        {
            this.SetTargetFaces(faces);
        }

        /// <summary>
        /// Loads the given target faces
        /// </summary>
        public virtual void SetTargetFaces(IEnumerable<IFaceModelTargetFace> faces)
        {
            lock (this.processingMutex)
            {
                this.faces = faces;
            }
        }

        /// <summary>
        /// Attempt to find a trained face
        /// </summary>
        public IRecognitionProcessorResult Process(Bitmap croppedBmp, KinectFaceTrackingResult trackingResults)
        {
            lock (this.processingMutex)
            {
                var result = new FaceModelRecognitionProcessorResult
                {
                    Normalized3DFacePoints = trackingResults.Normalized3DFacePoints
                };

                foreach (var targetFace in this.faces)
                {
                    var score = this.ScoreFaceDifferences(result, targetFace);

                    if (score > 20 && score > result.Score)
                    {
                        result.Score = score;
                        result.Key = targetFace.Key;
                    }   
                }

                return result;
            }
        }

        /// <summary>
        /// Calculates a score for how similar the subject is to the target
        /// </summary>
        public float ScoreFaceDifferences(IFaceModelTargetFace subject, IFaceModelTargetFace target)
        {
            if (subject.Normalized3DFacePoints.Count != target.Normalized3DFacePoints.Count)
                return 0;

            float score = 0;

            for (int i = 0; i < subject.Normalized3DFacePoints.Count; i++)
            {
                var s = subject.Normalized3DFacePoints[i];
                var t = target.Normalized3DFacePoints[i];
                var distance = (float)Math.Sqrt(Math.Pow(s.X - t.X, 2) + Math.Pow(s.Y - t.Y, 2) + Math.Pow(s.Z - t.Z, 2));

                // good distance is < 0.003
                if (distance < 0.003)
                    score += 1;
            }

            return score;
        }
    }
}
