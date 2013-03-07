using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sacknet.KinectFacialRecognition;

namespace Sacknet.KinectFacialRecognitionTests
{
    [TestClass]
    [DeploymentItem("Images")]
    [DeploymentItem("DLL")]
    public class EigenObjectRecognizerTest
    {
        [TestMethod]
        public void RecognizerSucessfullyRecognizesMe()
        {
            var faces = new List<TargetFace>();

            foreach(var trainingImage in Directory.GetFiles(".", "train*.*"))
            {
                faces.Add(new TargetFace
                {
                    Key = trainingImage,
                    Image = new Bitmap(trainingImage)
                });
            }

            var termCrit = new Emgu.CV.Structure.MCvTermCriteria(faces.Count, 0.001);
            var recognizer = new EigenObjectRecognizer(faces, 2000, ref termCrit);

            float eigenDistance;
            var testImage = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>("test_mike.png");
            var result = recognizer.Recognize(testImage, out eigenDistance);

            Assert.AreEqual(734.0543, Math.Round(eigenDistance, 4));
            Assert.AreEqual(@".\train_mike_2.png", result);
        }

        [TestMethod]
        public void ManagedRecognizerSucessfullyRecognizesMe()
        {
            var faces = new List<TargetFace>();

            foreach (var trainingImage in Directory.GetFiles(".", "train*.*"))
            {
                faces.Add(new TargetFace
                {
                    Key = trainingImage,
                    Image = new Bitmap(trainingImage)
                });
            }

            var termCrit = new Emgu.CV.Structure.MCvTermCriteria(faces.Count, 0.001);
            var recognizer = new ManagedEigenObjectRecognizer(faces, 2000, ref termCrit);

            float eigenDistance;
            var result = recognizer.Recognize(new Bitmap("test_mike.png"), out eigenDistance);

            Assert.AreEqual(734.0543, Math.Round(eigenDistance, 4));
            Assert.AreEqual(@".\train_mike_2.png", result);
        }

        [TestMethod]
        public void CompareEigenVals()
        {
            var faces = new List<TargetFace>();

            foreach (var trainingImage in Directory.GetFiles(".", "train*.*"))
            {
                faces.Add(new TargetFace
                {
                    Key = trainingImage,
                    Image = new Bitmap(trainingImage)
                });
            }

            var termCrit = new Emgu.CV.Structure.MCvTermCriteria(faces.Count, 0.001);
            var recognizer = new EigenObjectRecognizer(faces, 2000, ref termCrit);
            var managedRecognizer = new ManagedEigenObjectRecognizer(faces, 2000, ref termCrit);

            Assert.AreEqual(recognizer.AverageImage[0, 0].Intensity, managedRecognizer.AverageImage.Data[0]);
            Assert.AreEqual(recognizer.AverageImage[50, 0].Intensity, managedRecognizer.AverageImage.Data[5000]);
            Assert.AreEqual(recognizer.AverageImage[99, 0].Intensity, managedRecognizer.AverageImage.Data[9900]);
        }
    }
}
