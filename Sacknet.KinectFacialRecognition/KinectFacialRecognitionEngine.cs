using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.FaceTracking;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// A facial recognition engine using the Kinect facial tracking system and principal component analysis for recognition
    /// </summary>
    public class KinectFacialRecognitionEngine : IDisposable
    {
        private IFrameSource frameSource;
        private BackgroundWorker recognizerWorker;

        private int imageWidth, imageHeight;
        
        private byte[] colorImageBuffer;
        private ColorImageFormat colorImageFormat;

        private short[] depthImageBuffer;
        private DepthImageFormat depthImageFormat;

        private Skeleton trackedSkeleton;
        private int previousTrackedSkeletonId = -1;

        private FaceTracker faceTracker;

        /// <summary>
        /// Initializes a new instance of the KinectFacialRecognitionEngine class
        /// </summary>
        public KinectFacialRecognitionEngine(KinectSensor kinect, IFrameSource frameSource)
        {
            this.Kinect = kinect;
            this.ProcessingMutex = new object();
            this.ProcessingEnabled = true;
            this.Processor = new FacialRecognitionProcessor();
            this.frameSource = frameSource;
            this.frameSource.FrameDataUpdated += this.FrameSource_FrameDataUpdated;

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
        protected FacialRecognitionProcessor Processor { get; private set; }

        /// <summary>
        /// Gets the active Kinect sensor
        /// </summary>
        protected KinectSensor Kinect { get; private set; }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        public virtual void SetTargetFaces(IEnumerable<TargetFace> faces)
        {
            this.SetTargetFaces(faces, 1750);
        }

        /// <summary>
        /// Loads the given target faces into the eigen object recognizer
        /// </summary>
        /// <param name="faces">The target faces to use for training.  Faces should be 100x100 and grayscale.</param>
        /// <param name="threshold">Eigen distance threshold for a match.  1500-2000 is a reasonable value.  0 will never match.</param>
        public virtual void SetTargetFaces(IEnumerable<TargetFace> faces, double threshold)
        {
            lock (this.ProcessingMutex)
            {
                if (faces != null && faces.Any())
                {
                    this.Processor = new FacialRecognitionProcessor(faces, threshold);
                }
            }
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.frameSource.FrameDataUpdated -= this.FrameSource_FrameDataUpdated;
        }

        /// <summary>
        /// Performs recognition on a new frame of data
        /// </summary>
        private void FrameSource_FrameDataUpdated(object sender, FrameData e)
        {
            if (!this.recognizerWorker.IsBusy)
            {
                if (this.colorImageBuffer == null || this.colorImageBuffer.Length != e.ColorFrame.PixelDataLength)
                    this.colorImageBuffer = new byte[e.ColorFrame.PixelDataLength];

                e.ColorFrame.CopyPixelDataTo(this.colorImageBuffer);
                this.colorImageFormat = e.ColorFrame.Format;
                this.imageWidth = e.ColorFrame.Width;
                this.imageHeight = e.ColorFrame.Height;

                if (this.depthImageBuffer == null || this.depthImageBuffer.Length != e.DepthFrame.PixelDataLength)
                    this.depthImageBuffer = new short[e.DepthFrame.PixelDataLength];

                e.DepthFrame.CopyPixelDataTo(this.depthImageBuffer);
                this.depthImageFormat = e.DepthFrame.Format;

                this.trackedSkeleton = e.TrackedSkeleton;

                this.recognizerWorker.RunWorkerAsync(e);
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

            if (this.trackedSkeleton != null && this.trackedSkeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                // Reset the face tracker if we lost our old skeleton...
                if (this.trackedSkeleton.TrackingId != this.previousTrackedSkeletonId && this.faceTracker != null)
                    this.faceTracker.ResetTracking();

                this.previousTrackedSkeletonId = this.trackedSkeleton.TrackingId;

                if (this.faceTracker == null)
                {
                    try
                    {
                        this.faceTracker = new FaceTracker(this.Kinect);
                    }
                    catch (InvalidOperationException)
                    {
                        // During some shutdown scenarios the FaceTracker
                        // is unable to be instantiated.  Catch that exception
                        // and don't track a face.
                        this.faceTracker = null;
                    }
                }

                if (this.faceTracker != null)
                {
                    var faceTrackFrame = this.faceTracker.Track(
                        this.colorImageFormat,
                        this.colorImageBuffer,
                        this.depthImageFormat,
                        this.depthImageBuffer,
                        this.trackedSkeleton);

                    if (faceTrackFrame.TrackSuccessful)
                    {
                        var trackingResults = new TrackingResults(faceTrackFrame.GetProjected3DShape());

                        lock (this.ProcessingMutex)
                        {
                            if (this.Processor != null && this.ProcessingEnabled)
                            {
                                this.Processor.Process(result, trackingResults);
                            }
                        }
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
                this.RecognitionComplete(this, (RecognitionResult)e.Result);
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
