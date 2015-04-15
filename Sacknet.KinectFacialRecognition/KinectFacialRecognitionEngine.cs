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
        private IEnumerable<IRecognitionProcessor> processors;

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
            this.faceSource.TrackingQuality = FaceAlignmentQuality.High;
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
        /// Gets or sets the active facial recognition processors
        /// </summary>
        public IEnumerable<IRecognitionProcessor> Processors
        {
            get
            {
                return this.processors;
            }

            set
            {
                lock (this)
                {
                    this.processors = value;
                }
            }
        }

        /// <summary>
        /// Gets the active Kinect sensor
        /// </summary>
        protected KinectSensor Kinect { get; private set; }
        
        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.msReader.MultiSourceFrameArrived -= this.MultiSourceFrameArrived;
            this.msReader.Dispose();

            this.faceReader.FrameArrived -= this.FaceFrameArrived;
            this.faceReader.Dispose();

            lock (this.processFaceModelMutex)
            {
                this.DisposeFaceModelBuilder();
            }
        }

        /// <summary>
        /// Handles face frame updates
        /// </summary>
        private void FaceFrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            ulong? newTrackingId = null;

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

            if (this.Processors.Any(x => x.RequiresFaceModelBuilder) && newTrackingId.HasValue && this.currentTrackingId != newTrackingId)
            {
                lock (this.processFaceModelMutex)
                {
                    this.currentTrackingId = newTrackingId;
                    this.faceModel = null;
                    this.constructedFaceModel = null;
                    this.DisposeFaceModelBuilder();
                    this.fmb = this.faceSource.OpenModelBuilder(FaceModelBuilderAttributes.HairColor | FaceModelBuilderAttributes.SkinColor);
                    this.fmb.BeginFaceDataCollection();
                    this.fmb.CollectionCompleted += this.FaceModelBuilderCollectionCompleted;
                }
            }

            lock (this)
            {
                this.faceReady = true;
                this.StartWorkerIfReady();
            }
        }

        /// <summary>
        /// Starts the recognition worker if we have valid data
        /// </summary>
        private void StartWorkerIfReady()
        {
            if (!this.recognizerWorker.IsBusy && this.faceReady && this.multiSourceReady)
                this.recognizerWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Called when face model collection is successful
        /// </summary>
        private void FaceModelBuilderCollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            // Unfortunately, running this in a background thread seems to cause frequent crashes from
            // the kinect face library.  I wish there was a better way...
            lock (this.processFaceModelMutex)
            {
                if (this.fmb == null)
                    return;

                this.constructionInProcess = true;

                this.fmb.CollectionCompleted -= this.FaceModelBuilderCollectionCompleted;
                this.constructedFaceModel = e.ModelData.ProduceFaceModel();
                this.DisposeFaceModelBuilder();

                this.constructionInProcess = false;
            }
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
            var msFrame = e.FrameReference.AcquireFrame();

            using (var colorFrame = msFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    if (Monitor.TryEnter(this.colorImageBuffer, 0))
                    {
                        try
                        {
                            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                colorFrame.CopyRawFrameDataToArray(this.colorImageBuffer);
                            else
                                colorFrame.CopyConvertedFrameDataToArray(this.colorImageBuffer, ColorImageFormat.Bgra);
                        }
                        finally
                        {
                            Monitor.Exit(this.colorImageBuffer);
                        }
                    }
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
            this.faceReady = this.multiSourceReady = false;
            var status = FaceModelBuilderCollectionStatus.Complete;

            if (!this.constructionInProcess && this.fmb != null)
                status = this.fmb.CollectionStatus;

            var result = new RecognitionResult();
            result.ColorSpaceBitmap = this.ImageToBitmap(this.colorImageBuffer, this.imageWidth, this.imageHeight);
            e.Result = result;

            if (this.faceModel != null && this.Processors.Any() && this.ProcessingEnabled)
            {
                var faceTrackingResult = new KinectFaceTrackingResult(this.faceModel, this.constructedFaceModel, status, this.faceAlignment, this.Kinect.CoordinateMapper);

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
            
            lock (buffer)
            {
                Marshal.Copy(buffer, 0, ptr, buffer.Length);
            }
            
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
    }
}
