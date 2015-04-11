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
        public void DataFromSamePersonMatches()
        {
            var result = this.CompareTwoFiles("mike1.txt", "mike2.txt");

            Assert.AreEqual("mike1", result.Key);
        }

        [TestMethod]
        public void DataFromDifferentPeopleFails()
        {
            var result = this.CompareTwoFiles("mike1.txt", "sara1.txt");

            Assert.IsNull(result.Key);
        }

        private FaceModelRecognitionProcessorResult CompareTwoFiles(string file1, string file2)
        {
            var result1 = this.ReadResult(file1);
            var result2 = this.ReadResult(file2);

            var processor = new FaceModelRecognitionProcessor(new List<IFaceModelTargetFace> { result1 });
            var result = new FaceModelRecognitionProcessorResult();
            result.Deformations = result2.Deformations;
            result.HairColor = result2.HairColor;
            result.SkinColor = result2.SkinColor;

            processor.Process(result);

            return result;
        }

        private FaceModelRecognitionProcessorResult ReadResult(string filename)
        {
            return JsonConvert.DeserializeObject<FaceModelRecognitionProcessorResult>(File.ReadAllText(filename));
        }
    }
}
