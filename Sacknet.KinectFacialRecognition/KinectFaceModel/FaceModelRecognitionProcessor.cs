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
            this.Threshold = 25;
        }

        /// <summary>
        /// Initializes a new instance of the FaceModelRecognitionProcessor class
        /// </summary>
        public FaceModelRecognitionProcessor(IEnumerable<IFaceModelTargetFace> faces)
            : this()
        {
            this.SetTargetFaces(faces);
        }

        /// <summary>
        /// Gets a value indicating whether this processor requires a face model to be constructed
        /// </summary>
        public bool RequiresFaceModelBuilder
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets the score threshold that denotes a match
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Loads the given target faces
        /// </summary>
        public virtual void SetTargetFaces(IEnumerable<ITargetFace> faces)
        {
            if (!faces.All(x => x is IFaceModelTargetFace))
                throw new ArgumentException("All target faces must implement IFaceModelTargetFace!");

            this.SetTargetFaces(faces.Cast<IFaceModelTargetFace>());
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
        public IRecognitionProcessorResult Process(Bitmap colorSpaceBitmap, KinectFaceTrackingResult trackingResults)
        {
            lock (this.processingMutex)
            {
                var result = new FaceModelRecognitionProcessorResult();

                if (trackingResults.ConstructedFaceModel != null)
                {
                    result.Deformations = trackingResults.ConstructedFaceModel.FaceShapeDeformations;
                    result.HairColor = this.UIntToColor(trackingResults.ConstructedFaceModel.HairColor);
                    result.SkinColor = this.UIntToColor(trackingResults.ConstructedFaceModel.SkinColor);

                    this.Process(result);
                }

                return result;
            }
        }

        /// <summary>
        /// Processes the subject data (contained in result) against the target faces
        /// </summary>
        public void Process(FaceModelRecognitionProcessorResult result)
        {
            lock (this.processingMutex)
            {
                result.Score = double.MaxValue;

                foreach (var targetFace in this.faces)
                {
                    var score = this.ScoreFaceDifferences(result, targetFace);

                    if (score < this.Threshold && score < result.Score)
                    {
                        result.Score = score;
                        result.Key = targetFace.Key;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a score for how similar the subject is to the target
        /// </summary>
        public double ScoreFaceDifferences(IFaceModelTargetFace subject, IFaceModelTargetFace target)
        {
            double score = 0;
            var shc = subject.HairColor;
            var ssc = subject.SkinColor;
            var thc = target.HairColor;
            var tsc = target.SkinColor;

            var hairColorDistance = Math.Sqrt(Math.Pow(shc.R - thc.R, 2) + Math.Pow(shc.G - thc.G, 2) + Math.Pow(shc.B - thc.B, 2));
            var skinColorDistance = Math.Sqrt(Math.Pow(ssc.R - tsc.R, 2) + Math.Pow(ssc.G - tsc.G, 2) + Math.Pow(ssc.B - tsc.B, 2));

            // add up to 5 points for hair/skin color differences
            score += Math.Min(5, hairColorDistance / 10);
            score += Math.Min(5, skinColorDistance / 10);

            foreach (FaceShapeDeformations deformation in Enum.GetValues(typeof(FaceShapeDeformations)))
            {
                if (!subject.Deformations.ContainsKey(deformation) || !target.Deformations.ContainsKey(deformation))
                    continue;

                var deformationDifference = Math.Abs(subject.Deformations[deformation] - target.Deformations[deformation]);
                score += deformationDifference;
            }

            return score;
        }

        /// <summary>
        /// Converts an unsigned int into a color
        /// </summary>
        private Color UIntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
