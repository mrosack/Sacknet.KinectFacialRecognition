using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.FaceTracking;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// A facial recognition engine using the Kinect facial tracking system and principal component analysis for recognition
    /// </summary>
    public class KinectFacialRecoEngine : IDisposable
    {
        private IFrameSource frameSource;

        private byte[] colorImageBuffer;
        private short[] depthImageBuffer;
        private int previousTrackedSkeletonId = -1;

        private FaceTracker faceTracker;

        /// <summary>
        /// Initializes a new instance of the KinectFacialRecoEngine class
        /// </summary>
        public KinectFacialRecoEngine(KinectSensor kinect, IFrameSource frameSource)
        {
            this.Kinect = kinect;
            this.frameSource = frameSource;
            this.frameSource.FrameDataUpdated += this.FrameSource_FrameDataUpdated;
        }

        /// <summary>
        /// Gets or sets the active Kinect sensor
        /// </summary>
        protected KinectSensor Kinect { get; set; }

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
            if (this.colorImageBuffer == null || this.colorImageBuffer.Length != e.ColorFrame.PixelDataLength)
                this.colorImageBuffer = new byte[e.ColorFrame.PixelDataLength];

            e.ColorFrame.CopyPixelDataTo(this.colorImageBuffer);

            if (this.depthImageBuffer == null || this.depthImageBuffer.Length != e.DepthFrame.PixelDataLength)
                this.depthImageBuffer = new short[e.DepthFrame.PixelDataLength];

            e.DepthFrame.CopyPixelDataTo(this.depthImageBuffer);

            if (e.TrackedSkeleton != null && e.TrackedSkeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                // Reset the face tracker if we lost our old skeleton...
                if (e.TrackedSkeleton.TrackingId != this.previousTrackedSkeletonId && this.faceTracker != null)
                    this.faceTracker.ResetTracking();

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
                    // DON'T DISPOSE THE FACE TRACK FRAME - even though it's marked as disposable,
                    // it's kept around and used multiple times by the facetracker!
                    var faceTrackFrame = this.faceTracker.Track(
                        e.ColorFrame.Format,
                        this.colorImageBuffer,
                        e.DepthFrame.Format,
                        this.depthImageBuffer,
                        e.TrackedSkeleton);

                    if (faceTrackFrame.TrackSuccessful)
                    {
                        var colorBitmap = this.ImageToBitmap(this.colorImageBuffer, e.ColorFrame.Width, e.ColorFrame.Height);
                        var trackingResults = new TrackingResults
                        {
                            FacePoints = faceTrackFrame.GetProjected3DShape(),
                            FaceRect = new Rectangle(
                                faceTrackFrame.FaceRect.Left,
                                faceTrackFrame.FaceRect.Top,
                                faceTrackFrame.FaceRect.Width,
                                faceTrackFrame.FaceRect.Height)
                        };
                    }
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

        /// <summary>
        /// Results from face tracking
        /// </summary>
        public class TrackingResults
        {
            /// <summary>
            /// Gets or sets the 3D points of the face
            /// </summary>
            public EnumIndexableCollection<FeaturePoint, Microsoft.Kinect.Toolkit.FaceTracking.PointF> FacePoints { get; set; }

            /// <summary>
            /// Gets or sets the face bounding box
            /// </summary>
            public Rectangle FaceRect { get; set; }
        }
    }
}
