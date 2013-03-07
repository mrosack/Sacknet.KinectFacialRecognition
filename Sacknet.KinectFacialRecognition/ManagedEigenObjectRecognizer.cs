using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// An object recognizer using PCA (Principle Components Analysis).  Wouldn't have been possible without:
    /// http://www.codeproject.com/Articles/239849/Multiple-face-detection-and-recognition-in-real-ti?msg=4331418#xx4331418xx
    /// </summary>
    public class ManagedEigenObjectRecognizer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EigenObjectRecognizer"/> class.
        /// Create an object recognizer using the specific tranning data and parameters, it will always return the most similar object
        /// </summary>
        /// <param name="targetFaces">The images used for training, each of them should be the same size. It's recommended the images are histogram normalized</param>
        /// <param name="termCrit">The criteria for recognizer training</param>
        public ManagedEigenObjectRecognizer(IEnumerable<TargetFace> targetFaces, ref MCvTermCriteria termCrit)
            : this(targetFaces, 0, ref termCrit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EigenObjectRecognizer"/> class.
        /// Create an object recognizer using the specific tranning data and parameters
        /// </summary>
        /// <param name="targetFaces">The images used for training, each of them should be the same size. It's recommended the images are histogram normalized</param>
        /// <param name="eigenDistanceThreshold">
        /// The eigen distance threshold, (0, ~1000].
        /// The smaller the number, the more likely an examined image will be treated as unrecognized object. 
        /// If the threshold is &lt; 0, the recognizer will always treated the examined image as one of the known object. 
        /// </param>
        /// <param name="termCrit">The criteria for recognizer training</param>
        public ManagedEigenObjectRecognizer(IEnumerable<TargetFace> targetFaces, double eigenDistanceThreshold, ref MCvTermCriteria termCrit)
        {
            Debug.Assert(eigenDistanceThreshold >= 0.0, "Eigen-distance threshold should always >= 0.0");

            Bitmap[] images = targetFaces.Select(x => x.Image).ToArray();
            FloatImage[] eigenImages;
            FloatImage averageImage;

            CalcEigenObjects(images, ref termCrit, out eigenImages, out averageImage);

            /*
            _avgImage.SerializationCompressionRatio = 9;

            foreach (Image<Gray, Single> img in _eigenImages)
                //Set the compression ration to best compression. The serialized object can therefore save spaces
                img.SerializationCompressionRatio = 9;
            */

            this.EigenValues = images.Select(x => EigenDecomposite(x, eigenImages, averageImage)).ToArray();
            this.EigenImages = eigenImages;
            this.AverageImage = averageImage;
            this.Labels = targetFaces.Select(x => x.Key).ToArray();
            this.EigenDistanceThreshold = eigenDistanceThreshold;
        }

        /// <summary>
        /// Gets or sets the eigen vectors that form the eigen space
        /// </summary>
        /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
        public FloatImage[] EigenImages { get; set; }

        /// <summary>
        /// Gets or sets the labels for the corresponding training image
        /// </summary>
        public string[] Labels { get; set; }

        /// <summary>
        /// Gets or sets the eigen distance threshold.
        /// The smaller the number, the more likely an examined image will be treated as unrecognized object. 
        /// Set it to a huge number (e.g. 5000) and the recognizer will always treated the examined image as one of the known object. 
        /// </summary>
        public double EigenDistanceThreshold { get; set; }

        /// <summary>
        /// Gets or sets the average Image. 
        /// </summary>
        /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
        public FloatImage AverageImage { get; set; }

        /// <summary>
        /// Gets or sets the eigen values of each of the training image
        /// </summary>
        /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
        public float[][] EigenValues { get; set; }

        /// <summary>
        /// Caculate the eigen images for the specific traning image
        /// </summary>
        /// <param name="trainingImages">The images used for training </param>
        /// <param name="termCrit">The criteria for tranning</param>
        /// <param name="eigenImages">The resulting eigen images</param>
        /// <param name="avg">The resulting average image</param>
        public static void CalcEigenObjects(Bitmap[] trainingImages, ref MCvTermCriteria termCrit, out FloatImage[] eigenImages, out FloatImage avg)
        {
            int width = trainingImages[0].Width;
            int height = trainingImages[0].Height;

            if (termCrit.max_iter <= 0 || termCrit.max_iter > trainingImages.Length)
                termCrit.max_iter = trainingImages.Length;

            int maxEigenObjs = termCrit.max_iter;

            eigenImages = new FloatImage[maxEigenObjs];
            for (int i = 0; i < eigenImages.Length; i++)
                eigenImages[i] = new FloatImage(width, height);

            avg = new FloatImage(width, height);

            ManagedEigenObjects.CalcEigenObjects(trainingImages, termCrit.max_iter, termCrit.epsilon, eigenImages, null, avg);
        }

        /// <summary>
        /// Decompose the image as eigen values, using the specific eigen vectors
        /// </summary>
        /// <param name="src">The image to be decomposed</param>
        /// <param name="eigenImages">The eigen images</param>
        /// <param name="avg">The average images</param>
        /// <returns>Eigen values of the decomposed image</returns>
        public static float[] EigenDecomposite(Bitmap src, FloatImage[] eigenImages, FloatImage avg)
        {
            return ManagedEigenObjects.EigenDecomposite(src, eigenImages, avg);
        }

        /// <summary>
        /// Get the Euclidean eigen-distance between <paramref name="image"/> and every other image in the database
        /// </summary>
        /// <param name="image">The image to be compared from the training images</param>
        /// <returns>An array of eigen distance from every image in the training images</returns>
        public float[] GetEigenDistances(Bitmap image)
        {
            var decomp = EigenDecomposite(image, this.EigenImages, this.AverageImage);

            List<float> result = new List<float>();

            foreach (var eigenValue in this.EigenValues)
            {
                //norm = ||arr1-arr2||_L2 = sqrt( sum_I (arr1(I)-arr2(I))^2 )
                double sum = 0;

                for (var i = 0; i < eigenValue.Length; i++)
                {
                    sum += Math.Pow(decomp[i] - eigenValue[i], 2);
                }

                result.Add((float)Math.Sqrt(sum));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Given the <paramref name="image"/> to be examined, find in the database the most similar object, return the index and the eigen distance
        /// </summary>
        /// <param name="image">The image to be searched from the database</param>
        /// <param name="index">The index of the most similar object</param>
        /// <param name="eigenDistance">The eigen distance of the most similar object</param>
        /// <param name="label">The label of the specific image</param>
        public void FindMostSimilarObject(Bitmap image, out int index, out float eigenDistance, out string label)
        {
            float[] dist = this.GetEigenDistances(image);

            index = 0;
            eigenDistance = dist[0];

            for (int i = 1; i < dist.Length; i++)
            {
                if (dist[i] < eigenDistance)
                {
                    index = i;
                    eigenDistance = dist[i];
                }
            }

            label = this.Labels[index];
        }

        /// <summary>
        /// Try to recognize the image and return its label
        /// </summary>
        /// <param name="image">The image to be recognized</param>
        /// <param name="eigenDistance">The eigndistance to the best matched image</param>
        /// <returns>
        /// String.Empty, if not recognized;
        /// Label of the corresponding image, otherwise
        /// </returns>
        public string Recognize(Bitmap image, out float eigenDistance)
        {
            int index;
            string label;
            this.FindMostSimilarObject(image, out index, out eigenDistance, out label);

            return (this.EigenDistanceThreshold <= 0 || eigenDistance < this.EigenDistanceThreshold) ? this.Labels[index] : string.Empty;
        }
    }
}