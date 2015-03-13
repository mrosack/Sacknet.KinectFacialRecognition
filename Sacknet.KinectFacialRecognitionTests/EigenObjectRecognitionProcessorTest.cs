#if FALSE
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sacknet.KinectFacialRecognition;
using System.Diagnostics;
using Sacknet.KinectFacialRecognition.ManagedEigenObject;

namespace Sacknet.KinectFacialRecognitionTests
{
    [TestClass]
    [DeploymentItem("Images")]
    public class EigenObjectRecognitionProcessorTest
    {
        [TestMethod]
        public void EigenObjectRecognitionProcessorStillReturnsInfoIfNoTrainingImages()
        {
            var processor = new EigenObjectRecognitionProcessor();

            var recoResult = RunFacialRecognitionProcessor(processor);

            Assert.AreEqual(1, recoResult.Faces.Count());

            var face = recoResult.Faces.First();
            var eoResult = (EigenObjectRecognitionProcessorResult)face.ProcessorResults.First();

            Assert.AreEqual(-1, eoResult.EigenDistance);
            Assert.IsNull(eoResult.Key);
            Assert.IsNotNull(eoResult.GrayFace);
        }

        [TestMethod]
        [ExpectedException(typeof(EigenObjectException))]
        public void EigenObjectRecognitionProcessorThrowsExceptionIfOnlyOneTrainingImage()
        {
            var faces = new List<EigenObjectTargetFace>();

            var trainingImage = Directory.GetFiles(".", "train*.*").First();

            faces.Add(new EigenObjectTargetFace
            {
                Key = trainingImage,
                Image = new Bitmap(trainingImage)
            });

            var processor = new EigenObjectRecognitionProcessor(faces);

            RunFacialRecognitionProcessor(processor);
        }

        [TestMethod]
        public void EigenObjectRecognitionProcessorSucessfullyRecognizesMe()
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

            var processor = new EigenObjectRecognitionProcessor(faces);

            var recoResult = RunFacialRecognitionProcessor(processor);

            Assert.AreEqual(1, recoResult.Faces.Count());

            var face = recoResult.Faces.First();
            var eoResult = (EigenObjectRecognitionProcessorResult)face.ProcessorResults.First();
            Assert.AreEqual(789.3967, Math.Round(eoResult.EigenDistance, 4));
            Assert.AreEqual(@".\train_mike_2.png", eoResult.Key);
            Assert.IsNotNull(eoResult.GrayFace);
        }

        private RecognitionResult RunFacialRecognitionProcessor(EigenObjectRecognitionProcessor processor)
        {
            var testFrame = new Bitmap("testframe.png");
            var recoResult = new RecognitionResult
            {
                OriginalBitmap = testFrame,
                ProcessedBitmap = (Bitmap)testFrame.Clone()
            };

            var trackingResults = Newtonsoft.Json.JsonConvert.DeserializeObject<KinectFaceTrackingResult>(File.ReadAllText("testframe.json"));

            var sw = new Stopwatch();
            sw.Start();

            processor.Process(recoResult, trackingResults);

            sw.Stop();

            return recoResult;
        }
    }
}
#endif