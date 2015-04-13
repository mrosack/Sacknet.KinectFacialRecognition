using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sacknet.KinectFacialRecognition.KinectFaceModel;

namespace Sacknet.KinectFacialRecognitionTests
{
    [TestClass]
    [DeploymentItem("FaceData")]
    public class FaceModelRecognizerTest
    {
        [TestMethod]
        public void DataMatchesCorrectly()
        {
            var brady = this.ReadResult("brady.fmb");
            var matt = this.ReadResult("matt.fmb");
            var rosack = this.ReadResult("rosack.fmb");
            var rosackTest = this.ReadResult("rosack_test.fmb");

            var processor = new FaceModelRecognitionProcessor(new List<IFaceModelTargetFace> { brady, matt, rosack });
            var result = new FaceModelRecognitionProcessorResult();
            result.Deformations = rosackTest.Deformations;
            result.HairColor = rosackTest.HairColor;
            result.SkinColor = rosackTest.SkinColor;

            processor.Process(result);

            Assert.AreEqual("Rosack", result.Key);
        }

        private FaceModelRecognitionProcessorResult ReadResult(string filename)
        {
            return JsonConvert.DeserializeObject<FaceModelRecognitionProcessorResult>(File.ReadAllText(filename));
        }
    }
}
