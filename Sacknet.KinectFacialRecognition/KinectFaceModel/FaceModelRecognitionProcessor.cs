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
        private IEnumerable<IFaceModelTargetFace> faces;
        
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
                    Deformations = trackingResults.FaceModel.FaceShapeDeformations,
                    HairColor = trackingResults.FaceModel.HairColor,
                    SkinColor = trackingResults.FaceModel.SkinColor
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
            float score = 0;

            if (subject.HairColor == target.HairColor)
                score += 5;

            if (subject.SkinColor == target.SkinColor)
                score += 5;

            foreach (FaceShapeDeformations deformation in Enum.GetValues(typeof(FaceShapeDeformations)))
            {
                var deformationDifference = Math.Abs(subject.Deformations[deformation] - target.Deformations[deformation]);
                if (deformationDifference > 1)
                    deformationDifference = 1;

                // Maximum score of 1 per deformation point
                score += 1 - deformationDifference;
            }

            return score;
        }
    }
}
