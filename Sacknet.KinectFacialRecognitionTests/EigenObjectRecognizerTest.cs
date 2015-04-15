using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sacknet.KinectFacialRecognition;
using Sacknet.KinectFacialRecognition.ManagedEigenObject;

namespace Sacknet.KinectFacialRecognitionTests
{
    [TestClass]
    [DeploymentItem("Images")]
    public class EigenObjectRecognizerTest
    {
        [TestMethod]
        public void ManagedRecognizerSucessfullyRecognizesMe()
        {
            var faces = new List<IEigenObjectTargetFace>();

            foreach (var trainingImage in Directory.GetFiles(".", "train*.*"))
            {
                var mockFace = new Mock<IEigenObjectTargetFace>()
                    .SetupProperty(x => x.Image, new Bitmap(trainingImage))
                    .SetupProperty(x => x.Key, trainingImage)
                    .Object;
                faces.Add(mockFace);
            }

            var recognizer = new EigenObjectRecognizer(faces);

            double eigenDistance;
            var result = recognizer.Recognize(new Bitmap("test_mike.png"), out eigenDistance);

            Assert.AreEqual(734.0479, Math.Round(eigenDistance, 4));
            Assert.AreEqual(@".\train_mike_2.png", result);
        }
    }
}
