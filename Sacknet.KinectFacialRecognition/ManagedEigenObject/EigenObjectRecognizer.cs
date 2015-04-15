using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Sacknet.KinectFacialRecognition.ManagedEigenObject;

namespace Sacknet.KinectFacialRecognition.ManagedEigenObject
{
    /// <summary>
    /// Based on the Emgu CV EigenObjectRecognizer, but converted to use fully managed objects.
    /// </summary>
    public class EigenObjectRecognizer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EigenObjectRecognizer"/> class.
        /// </summary>
        public EigenObjectRecognizer(IEnumerable<IEigenObjectTargetFace> targetFaces)
            : this(targetFaces, 2000)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EigenObjectRecognizer"/> class.
        /// </summary>
        public EigenObjectRecognizer(IEnumerable<IEigenObjectTargetFace> targetFaces, double eigenDistanceThreshold)
            : this(targetFaces, eigenDistanceThreshold, targetFaces.Count(), 0.001)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EigenObjectRecognizer"/> class.
        /// </summary>
        public EigenObjectRecognizer(IEnumerable<IEigenObjectTargetFace> targetFaces, double eigenDistanceThreshold, int maxIter, double eps)
        {
            Debug.Assert(eigenDistanceThreshold >= 0.0, "Eigen-distance threshold should always >= 0.0");

            Bitmap[] images = targetFaces.Select(x => x.Image).ToArray();
            DoubleImage[] eigenImages;
            DoubleImage averageImage;

            CalcEigenObjects(images, maxIter, eps, out eigenImages, out averageImage);

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
        public DoubleImage[] EigenImages { get; set; }

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
        public DoubleImage AverageImage { get; set; }

        /// <summary>
        /// Gets or sets the eigen values of each of the training image
        /// </summary>
        /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
        public double[][] EigenValues { get; set; }

        /// <summary>
        /// Caculate the eigen images for the specific traning image
        /// </summary>
        public static void CalcEigenObjects(Bitmap[] trainingImages, int maxIter, double eps, out DoubleImage[] eigenImages, out DoubleImage avg)
        {
            int width = trainingImages[0].Width;
            int height = trainingImages[0].Height;

            if (maxIter <= 0 || maxIter > trainingImages.Length)
                maxIter = trainingImages.Length;

            int maxEigenObjs = maxIter;

            eigenImages = new DoubleImage[maxEigenObjs];
            for (int i = 0; i < eigenImages.Length; i++)
                eigenImages[i] = new DoubleImage(width, height);

            avg = new DoubleImage(width, height);

            EigenObjects.CalcEigenObjects(trainingImages, maxIter, eps, eigenImages, null, avg);
        }

        /// <summary>
        /// Decompose the image as eigen values, using the specific eigen vectors
        /// </summary>
        public static double[] EigenDecomposite(Bitmap src, DoubleImage[] eigenImages, DoubleImage avg)
        {
            return EigenObjects.EigenDecomposite(src, eigenImages, avg);
        }

        /// <summary>
        /// Get the Euclidean eigen-distance between <paramref name="image"/> and every other image in the database
        /// </summary>
        public double[] GetEigenDistances(Bitmap image)
        {
            var decomp = EigenDecomposite(image, this.EigenImages, this.AverageImage);

            List<double> result = new List<double>();

            foreach (var eigenValue in this.EigenValues)
            {
                // norm = ||arr1-arr2||_L2 = sqrt( sum_I (arr1(I)-arr2(I))^2 )
                double sum = 0;

                for (var i = 0; i < eigenValue.Length; i++)
                {
                    sum += Math.Pow(decomp[i] - eigenValue[i], 2);
                }

                result.Add(Math.Sqrt(sum));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Given the <paramref name="image"/> to be examined, find in the database the most similar object, return the index and the eigen distance
        /// </summary>
        public void FindMostSimilarObject(Bitmap image, out int index, out double eigenDistance, out string label)
        {
            double[] dist = this.GetEigenDistances(image);

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
        public string Recognize(Bitmap image, out double eigenDistance)
        {
            int index;
            string label;
            this.FindMostSimilarObject(image, out index, out eigenDistance, out label);

            return (this.EigenDistanceThreshold <= 0 || eigenDistance < this.EigenDistanceThreshold) ? this.Labels[index] : string.Empty;
        }
    }
}