using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Sacknet.KinectFacialRecognition.ManagedEigenObject;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// A facial recognition engine using the Kinect facial tracking system and principal component analysis for recognition
    /// </summary>
    public class KinectFacialRecognitionEngine : IKinectFacialRecognitionEngine, IDisposable
    {
        private BackgroundWorker recognizerWorker;

        private int imageWidth, imageHeight;
        
        private byte[] colorImageBuffer;
        private FaceModel faceModel;
        private FaceAlignment faceAlignment = new FaceAlignment();

        private Body[] bodies;
        private MultiSourceFrameReader msReader;

        private HighDefinitionFaceFrameSource faceSource;
        private HighDefinitionFaceFrameReader faceReader;

        /// <summary>
        /// Initializes a new instance of the KinectFacialRecognitionEngine class
        /// </summary>
        public KinectFacialRecognitionEngine(KinectSensor kinect)
        {
            this.Kinect = kinect;

            this.bodies = new Body[kinect.BodyFrameSource.BodyCount];
            this.colorImageBuffer = new byte[4 * kinect.ColorFrameSource.FrameDescription.LengthInPixels];
            this.imageWidth = kinect.ColorFrameSource.FrameDescription.Width;
            this.imageHeight = kinect.ColorFrameSource.FrameDescription.Height;

            this.msReader = this.Kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
            this.msReader.MultiSourceFrameArrived += this.MultiSourceFrameArrived;

            this.faceSource = new HighDefinitionFaceFrameSource(kinect);
            this.faceReader = this.faceSource.OpenReader();
            this.faceReader.FrameArrived += this.FaceFrameArrived;

            this.ProcessingMutex = new object();
            this.ProcessingEnabled = true;
            this.Processor = new EigenObjectRecognitionProcessor();

            this.recognizerWorker = new BackgroundWorker();
            this.recognizerWorker.DoWork += this.RecognizerWorker_DoWork;
            this.recognizerWorker.RunWorkerCompleted += this.RecognizerWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Raised when recognition has been completed for a frame
        /// </summary>
        public event EventHandler<RecognitionResult> RecognitionComplete;

        /// <summary>
        /// Gets or sets a value indicating whether images will be processed for facial recognition.  If false, the video stream will be passed through untouched.
        /// </summary>
        public bool ProcessingEnabled { get; set; }

        /// <summary>
        /// Gets a mutex that prevents the target faces from being updated during processing and vice-versa
        /// </summary>
        protected object ProcessingMutex { get; private set; }

        /// <summary>
        /// Gets the active facial recognition processor
        /// </summary>
        protected EigenObjectRecognitionProcessor Processor { get; private set; }

        /// <summary>
        /// Gets the active Kinect sensor
        /// </summary>
        protected KinectSensor Kinect { get; private set; }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        public virtual void SetTargetFaces(IEnumerable<EigenObjectTargetFace> faces)
        {
            this.SetTargetFaces(faces, 1750);
        }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        /// <param name="threshold">Eigen distance threshold for a match.  1500-2000 is a reasonable value.  0 will never match.</param>
        public virtual void SetTargetFaces(IEnumerable<EigenObjectTargetFace> faces, double threshold)
        {
            lock (this.ProcessingMutex)
            {
                if (faces != null && faces.Any())
                {
                    this.Processor = new EigenObjectRecognitionProcessor(faces, threshold);
                }
            }
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.msReader.MultiSourceFrameArrived -= this.MultiSourceFrameArrived;
            this.msReader.Dispose();

            this.faceReader.FrameArrived -= this.FaceFrameArrived;
            this.faceReader.Dispose();
        }

        /// <summary>
        /// Handles face frame updates
        /// </summary>
        private void FaceFrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            if (this.recognizerWorker.IsBusy)
                return;

            lock (this)
            {
                this.faceModel = null;

                using (var frame = e.FrameReference.AcquireFrame())
                {
                    if (frame != null && frame.IsTrackingIdValid && frame.IsFaceTracked)
                    {
                        frame.GetAndRefreshFaceAlignmentResult(this.faceAlignment);
                        this.faceModel = frame.FaceModel;
                    }
                }
            }

            this.recognizerWorker.RunWorkerAsync(e);
        }

        /// <summary>
        /// Handles body/color frame updates
        /// </summary>
        private void MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (this.recognizerWorker.IsBusy)
                return;

            lock (this)
            {
                var msFrame = e.FrameReference.AcquireFrame();

                using (var colorFrame = msFrame.ColorFrameReference.AcquireFrame())
                {
                    if (colorFrame != null)
                    {
                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                            colorFrame.CopyRawFrameDataToArray(this.colorImageBuffer);
                        else
                            colorFrame.CopyConvertedFrameDataToArray(this.colorImageBuffer, ColorImageFormat.Bgra);
                    }
                }

                using (var bodyFrame = msFrame.BodyFrameReference.AcquireFrame())
                {
                    if (bodyFrame != null)
                    {
                        bodyFrame.GetAndRefreshBodyData(this.bodies);

                        var trackedBody = this.bodies.Where(b => b.IsTracked).FirstOrDefault();

                        if (!this.faceSource.IsTrackingIdValid && trackedBody != null)
                            this.faceSource.TrackingId = trackedBody.TrackingId;
                    }
                }
            }
        }

        /// <summary>
        /// Worker thread for recognition processing
        /// </summary>
        private void RecognizerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var result = new RecognitionResult();
            result.OriginalBitmap = this.ImageToBitmap(this.colorImageBuffer, this.imageWidth, this.imageHeight);
            result.ProcessedBitmap = (Bitmap)result.OriginalBitmap.Clone();
            e.Result = result;

            if (this.faceModel != null)
            {
                var vertices = this.faceModel.CalculateVerticesForAlignment(this.faceAlignment);
                var trackingResults = new KinectFaceTrackingResult(vertices, this.Kinect.CoordinateMapper);

                lock (this.ProcessingMutex)
                {
                    if (this.Processor != null && this.ProcessingEnabled)
                    {
                        this.Processor.Process(result, trackingResults);
                    }
                }
            }
        }

        /// <summary>
        /// Work complete - brings the results back to the UI thread and raises the complete event
        /// </summary>
        private void RecognizerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.RecognitionComplete != null)
            {
                var recoResult = (RecognitionResult)e.Result;

                try
                {
                    this.RecognitionComplete(this, recoResult);
                }
                finally
                {
                    recoResult.Dispose();
                }
            }
        }

        /// <summary>
        /// Transforms a Kinect ColorImageFrame to a bitmap (why is this so hard?)
        /// </summary>
        private Bitmap ImageToBitmap(byte[] buffer, int width, int height)
        {
            Bitmap bmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
    }
}
