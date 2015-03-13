using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var faces = new List<EigenObjectTargetFace>();

            foreach (var trainingImage in Directory.GetFiles(".", "train*.*"))
            {
                faces.Add(new EigenObjectTargetFace
                {
                    Key = trainingImage,
                    Image = new Bitmap(trainingImage)
                });
            }

            var recognizer = new EigenObjectRecognizer(faces);

            float eigenDistance;
            var result = recognizer.Recognize(new Bitmap("test_mike.png"), out eigenDistance);

            Assert.AreEqual(734.0547, Math.Round(eigenDistance, 4));
            Assert.AreEqual(@".\train_mike_2.png", result);
        }
    }
}
