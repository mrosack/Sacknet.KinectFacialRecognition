using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sacknet.KinectFacialRecognition;
using System.Diagnostics;

namespace Sacknet.KinectFacialRecognitionTests
{
    [TestClass]
    [DeploymentItem("Images")]
    public class FacialRecognitionProcessorTest
    {
        [TestMethod]
        public void FacialRecognitionProcessorSucessfullyRecognizesMe()
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

            var processor = new FacialRecognitionProcessor(faces);

            var testFrame = new Bitmap("testframe.png");
            var recoResult = new RecognitionResult
            {
                OriginalBitmap = testFrame,
                ProcessedBitmap = (Bitmap)testFrame.Clone()
            };
            
            var trackingResults = Newtonsoft.Json.JsonConvert.DeserializeObject<TrackingResults>(File.ReadAllText("testframe.json"));

            var sw = new Stopwatch();
            sw.Start();

            processor.Process(recoResult, trackingResults);

            sw.Stop();

            Assert.AreEqual(1, recoResult.Faces.Count());

            var face = recoResult.Faces.First();
            Assert.AreEqual(1037.2743, Math.Round(face.EigenDistance, 4));
            Assert.AreEqual(@".\train_mike_2.png", face.Key);
        }
    }
}
