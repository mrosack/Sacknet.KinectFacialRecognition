using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
        private FaceModel constructedFaceModel;
        private FaceAlignment faceAlignment = new FaceAlignment();
        private ulong? currentTrackingId;
        private FaceModelBuilder fmb = null;
        private object processFaceModelMutex = new object();
        private bool constructionInProcess = false;

        private Body[] bodies;
        private MultiSourceFrameReader msReader;

        private HighDefinitionFaceFrameSource faceSource;
        private HighDefinitionFaceFrameReader faceReader;

        private bool faceReady = false;
        private bool multiSourceReady = false;

        /// <summary>
        /// Initializes a new instance of the KinectFacialRecognitionEngine class
        /// </summary>
        public KinectFacialRecognitionEngine(KinectSensor kinect, params IRecognitionProcessor[] processors)
        {
            this.Kinect = kinect;

            this.ProcessingEnabled = true;
            this.Processors = processors;

            if (this.Processors == null || !this.Processors.Any())
                throw new ArgumentException("Please pass in at least one recognition processor!");

            this.bodies = new Body[kinect.BodyFrameSource.BodyCount];
            this.colorImageBuffer = new byte[4 * kinect.ColorFrameSource.FrameDescription.LengthInPixels];
            this.imageWidth = kinect.ColorFrameSource.FrameDescription.Width;
            this.imageHeight = kinect.ColorFrameSource.FrameDescription.Height;

            this.msReader = this.Kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
            this.msReader.MultiSourceFrameArrived += this.MultiSourceFrameArrived;

            this.faceSource = new HighDefinitionFaceFrameSource(kinect);
            this.faceReader = this.faceSource.OpenReader();
            this.faceReader.FrameArrived += this.FaceFrameArrived;

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
        /// Gets the active facial recognition processors
        /// </summary>
        protected IEnumerable<IRecognitionProcessor> Processors { get; private set; }

        /// <summary>
        /// Gets the active Kinect sensor
        /// </summary>
        protected KinectSensor Kinect { get; private set; }
        
        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            lock (this.processFaceModelMutex)
            {
                this.DisposeFaceModelBuilder();
            }

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

            ulong? newTrackingId = null;
            this.faceModel = null;

            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (frame.IsTrackingIdValid && frame.IsFaceTracked)
                    {
                        frame.GetAndRefreshFaceAlignmentResult(this.faceAlignment);
                        this.faceModel = frame.FaceModel;
                        newTrackingId = frame.TrackingId;
                    }
                }
            }

            if (newTrackingId.HasValue && this.currentTrackingId != newTrackingId)
            {
                if (Monitor.TryEnter(this.processFaceModelMutex, 100))
                {
                    try
                    {
                        Console.WriteLine("Creating FaceModelBuilder");
                        this.currentTrackingId = newTrackingId;
                        this.constructedFaceModel = null;
                        this.DisposeFaceModelBuilder();
                        this.fmb = this.faceSource.OpenModelBuilder(FaceModelBuilderAttributes.HairColor | FaceModelBuilderAttributes.SkinColor);
                        this.fmb.BeginFaceDataCollection();
                        this.fmb.CollectionCompleted += this.FaceModelBuilderCollectionCompleted;
                    }
                    finally
                    {
                        Monitor.Exit(this.processFaceModelMutex);
                    }
                }
            }

            lock (this)
            {
                this.faceReady = this.faceModel != null;
                this.StartWorkerIfReady();
            }
        }

        /// <summary>
        /// Starts the recognition worker if we have valid data
        /// </summary>
        private void StartWorkerIfReady()
        {
            if (this.faceReady && this.multiSourceReady)
                this.recognizerWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Called when face model collection is successful
        /// </summary>
        private void FaceModelBuilderCollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((wc) =>
            {
                if (Monitor.TryEnter(this.processFaceModelMutex, 100))
                {
                    try
                    {
                        if (this.fmb == null)
                            return;

                        this.constructionInProcess = true;

                        this.fmb.CollectionCompleted -= this.FaceModelBuilderCollectionCompleted;
                        this.constructedFaceModel = e.ModelData.ProduceFaceModel();
                        this.DisposeFaceModelBuilder();

                        this.constructionInProcess = false;
                    }
                    finally
                    {
                        Monitor.Exit(this.processFaceModelMutex);
                    }
                }
            });
        }

        /// <summary>
        /// Disposes the face model builder
        /// </summary>
        private void DisposeFaceModelBuilder()
        {
            var localFmb = this.fmb;
            this.fmb = null;

            if (localFmb != null)
            {
                localFmb.CollectionCompleted -= this.FaceModelBuilderCollectionCompleted;
                localFmb.Dispose();
            }
        }

        /// <summary>
        /// Handles body/color frame updates
        /// </summary>
        private void MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (this.recognizerWorker.IsBusy)
                return;

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

            lock (this)
            {
                this.multiSourceReady = true;
                this.StartWorkerIfReady();
            }
        }

        /// <summary>
        /// Worker thread for recognition processing
        /// </summary>
        private void RecognizerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var status = FaceModelBuilderCollectionStatus.Complete;

            if (!this.constructionInProcess && this.fmb != null)
                status = this.fmb.CollectionStatus;

            var faceTrackingResult = new KinectFaceTrackingResult(this.faceModel, this.constructedFaceModel, status, this.faceAlignment, this.Kinect.CoordinateMapper);

            var result = new RecognitionResult();
            result.ColorSpaceBitmap = this.ImageToBitmap(this.colorImageBuffer, this.imageWidth, this.imageHeight);
            e.Result = result;

            if (this.Processors.Any() && this.ProcessingEnabled)
            {
                var rpResults = new List<IRecognitionProcessorResult>();

                foreach (var processor in this.Processors)
                    rpResults.Add(processor.Process(result.ColorSpaceBitmap, faceTrackingResult));

                result.Faces = new List<TrackedFace>
                {
                    new TrackedFace
                    {
                        ProcessorResults = rpResults,
                        TrackingResult = faceTrackingResult
                    }
                };
            }

            this.faceReady = false;
            this.multiSourceReady = false;
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
