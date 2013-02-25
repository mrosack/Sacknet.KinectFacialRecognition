using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// An object recognizer using PCA (Principle Components Analysis).  Wouldn't have been possible without:
    /// http://www.codeproject.com/Articles/239849/Multiple-face-detection-and-recognition-in-real-ti?msg=4331418#xx4331418xx
    /// </summary>
    public class EigenObjectRecognizer : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EigenObjectRecognizer"/> class.
        /// Create an object recognizer using the specific tranning data and parameters, it will always return the most similar object
        /// </summary>
        /// <param name="targetFaces">The images used for training, each of them should be the same size. It's recommended the images are histogram normalized</param>
        /// <param name="termCrit">The criteria for recognizer training</param>
        public EigenObjectRecognizer(IEnumerable<TargetFace> targetFaces, ref MCvTermCriteria termCrit)
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
        public EigenObjectRecognizer(IEnumerable<TargetFace> targetFaces, double eigenDistanceThreshold, ref MCvTermCriteria termCrit)
        {
            Debug.Assert(eigenDistanceThreshold >= 0.0, "Eigen-distance threshold should always >= 0.0");

            Image<Gray, byte>[] images = targetFaces.Select(x => new Image<Gray, byte>(x.Image)).ToArray();
            Image<Gray, float>[] eigenImages;
            Image<Gray, float> averageImage;

            CalcEigenObjects(images, ref termCrit, out eigenImages, out averageImage);

            /*
            _avgImage.SerializationCompressionRatio = 9;

            foreach (Image<Gray, Single> img in _eigenImages)
                //Set the compression ration to best compression. The serialized object can therefore save spaces
                img.SerializationCompressionRatio = 9;
            */

            this.EigenValues = Array.ConvertAll<Image<Gray, byte>, Matrix<float>>(images,
                delegate(Image<Gray, byte> img)
                {
                    return new Matrix<float>(EigenDecomposite(img, eigenImages, averageImage));
                });

            this.EigenImages = eigenImages;
            this.AverageImage = averageImage;
            this.Labels = targetFaces.Select(x => x.Key).ToArray();
            this.EigenDistanceThreshold = eigenDistanceThreshold;
        }

        /// <summary>
        /// Gets or sets the eigen vectors that form the eigen space
        /// </summary>
        /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
        public Image<Gray, float>[] EigenImages { get; set; }

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
        public Image<Gray, float> AverageImage { get; set; }

        /// <summary>
        /// Gets or sets the eigen values of each of the training image
        /// </summary>
        /// <remarks>The set method is primary used for deserialization, do not attemps to set it unless you know what you are doing</remarks>
        public Matrix<float>[] EigenValues { get; set; }

        /// <summary>
        /// Caculate the eigen images for the specific traning image
        /// </summary>
        /// <param name="trainingImages">The images used for training </param>
        /// <param name="termCrit">The criteria for tranning</param>
        /// <param name="eigenImages">The resulting eigen images</param>
        /// <param name="avg">The resulting average image</param>
        public static void CalcEigenObjects(Image<Gray, byte>[] trainingImages, ref MCvTermCriteria termCrit, out Image<Gray, float>[] eigenImages, out Image<Gray, float> avg)
        {
            int width = trainingImages[0].Width;
            int height = trainingImages[0].Height;

            IntPtr[] inObjs = Array.ConvertAll<Image<Gray, byte>, IntPtr>(trainingImages, delegate(Image<Gray, byte> img) { return img.Ptr; });

            if (termCrit.max_iter <= 0 || termCrit.max_iter > trainingImages.Length)
                termCrit.max_iter = trainingImages.Length;

            int maxEigenObjs = termCrit.max_iter;

            eigenImages = new Image<Gray, float>[maxEigenObjs];
            for (int i = 0; i < eigenImages.Length; i++)
                eigenImages[i] = new Image<Gray, float>(width, height);
            IntPtr[] eigObjs = Array.ConvertAll<Image<Gray, float>, IntPtr>(eigenImages, delegate(Image<Gray, float> img) { return img.Ptr; });

            avg = new Image<Gray, float>(width, height);

            CvInvoke.cvCalcEigenObjects(
                inObjs,
                ref termCrit,
                eigObjs,
                null,
                avg.Ptr);
        }

        /// <summary>
        /// Decompose the image as eigen values, using the specific eigen vectors
        /// </summary>
        /// <param name="src">The image to be decomposed</param>
        /// <param name="eigenImages">The eigen images</param>
        /// <param name="avg">The average images</param>
        /// <returns>Eigen values of the decomposed image</returns>
        public static float[] EigenDecomposite(Image<Gray, byte> src, Image<Gray, float>[] eigenImages, Image<Gray, float> avg)
        {
            return CvInvoke.cvEigenDecomposite(
                src.Ptr,
                Array.ConvertAll<Image<Gray, float>, IntPtr>(eigenImages, delegate(Image<Gray, float> img) { return img.Ptr; }),
                avg.Ptr);
        }

        /// <summary>
        /// Given the eigen value, reconstruct the projected image
        /// </summary>
        /// <param name="eigenValue">The eigen values</param>
        /// <returns>The projected image</returns>
        public Image<Gray, byte> EigenProjection(float[] eigenValue)
        {
            Image<Gray, byte> res = new Image<Gray, byte>(this.AverageImage.Width, this.AverageImage.Height);
            CvInvoke.cvEigenProjection(
                Array.ConvertAll<Image<Gray, float>, IntPtr>(this.EigenImages, delegate(Image<Gray, float> img) { return img.Ptr; }),
                eigenValue,
                this.AverageImage.Ptr,
                res.Ptr);
            return res;
        }

        /// <summary>
        /// Get the Euclidean eigen-distance between <paramref name="image"/> and every other image in the database
        /// </summary>
        /// <param name="image">The image to be compared from the training images</param>
        /// <returns>An array of eigen distance from every image in the training images</returns>
        public float[] GetEigenDistances(Image<Gray, byte> image)
        {
            using (Matrix<float> eigenValue = new Matrix<float>(EigenDecomposite(image, this.EigenImages, this.AverageImage)))
                return Array.ConvertAll<Matrix<float>, float>(this.EigenValues,
                    delegate(Matrix<float> eigenValueI)
                    {
                        return (float)CvInvoke.cvNorm(eigenValue.Ptr, eigenValueI.Ptr, Emgu.CV.CvEnum.NORM_TYPE.CV_L2, IntPtr.Zero);
                    });
        }

        /// <summary>
        /// Given the <paramref name="image"/> to be examined, find in the database the most similar object, return the index and the eigen distance
        /// </summary>
        /// <param name="image">The image to be searched from the database</param>
        /// <param name="index">The index of the most similar object</param>
        /// <param name="eigenDistance">The eigen distance of the most similar object</param>
        /// <param name="label">The label of the specific image</param>
        public void FindMostSimilarObject(Image<Gray, byte> image, out int index, out float eigenDistance, out string label)
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
        /// <returns>
        /// String.Empty, if not recognized;
        /// Label of the corresponding image, otherwise
        /// </returns>
        public string Recognize(Image<Gray, byte> image, out float eigenDistance)
        {
            int index;
            string label;
            this.FindMostSimilarObject(image, out index, out eigenDistance, out label);

            return (this.EigenDistanceThreshold <= 0 || eigenDistance < this.EigenDistanceThreshold) ? this.Labels[index] : string.Empty;
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            this.AverageImage.Dispose();

            foreach (var image in this.EigenImages)
                image.Dispose();
        }
    }
}