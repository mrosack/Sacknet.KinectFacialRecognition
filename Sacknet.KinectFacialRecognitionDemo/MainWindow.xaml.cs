using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;
using Sacknet.KinectFacialRecognition;
using Sacknet.KinectFacialRecognition.ManagedEigenObject;

namespace Sacknet.KinectFacialRecognitionDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool takeTrainingImage = false;
        private KinectFacialRecognitionEngine engine;
        private ObservableCollection<IEigenObjectTargetFace> targetFaces = new ObservableCollection<IEigenObjectTargetFace>();
        private EigenObjectRecognitionProcessor eorProcessor = new EigenObjectRecognitionProcessor();

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            KinectSensor kinectSensor = KinectSensor.GetDefault();
            kinectSensor.Open();

            this.engine = new KinectFacialRecognitionEngine(kinectSensor, this.eorProcessor);
            this.engine.RecognitionComplete += this.Engine_RecognitionComplete;

            this.InitializeComponent();

            this.TrainedFaces.ItemsSource = this.targetFaces;
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Loads a bitmap into a bitmap source
        /// </summary>
        private static BitmapSource LoadBitmap(Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }

        /// <summary>
        /// Handles recognition complete events
        /// </summary>
        private void Engine_RecognitionComplete(object sender, RecognitionResult e)
        {
            TrackedFace face = null;

            if (e.Faces != null)
                face = e.Faces.FirstOrDefault();

            if (face != null)
            {
                var eoResult = (EigenObjectRecognitionProcessorResult)face.ProcessorResults.First();

                if (!string.IsNullOrEmpty(eoResult.Key))
                {
                    // Write the key on the image...
                    using (var g = Graphics.FromImage(e.ProcessedBitmap))
                    {
                        var rect = face.TrackingResults.FaceRect;
                        g.DrawString(eoResult.Key, new Font("Arial", 100), Brushes.Red, new System.Drawing.Point(rect.Left, rect.Top - 25));
                    }
                }

                if (this.takeTrainingImage)
                {
                    this.targetFaces.Add(new BitmapSourceTargetFace
                    {
                        Image = (Bitmap)eoResult.GrayFace.Clone(),
                        Key = this.NameField.Text
                    });

                    this.takeTrainingImage = false;
                    this.NameField.Text = this.NameField.Text.Replace(this.targetFaces.Count.ToString(), (this.targetFaces.Count + 1).ToString());

                    if (this.targetFaces.Count > 1)
                        this.eorProcessor.SetTargetFaces(this.targetFaces);
                }
            }

            this.Video.Source = LoadBitmap(e.ProcessedBitmap);
            
            // Without an explicit call to GC.Collect here, memory runs out of control :(
            GC.Collect();
        }

        /// <summary>
        /// Starts the training image countdown
        /// </summary>
        private void Train(object sender, RoutedEventArgs e)
        {
            this.TrainButton.IsEnabled = false;
            this.NameField.IsEnabled = false;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s2, e2) =>
            {
                timer.Stop();
                this.NameField.IsEnabled = true;
                this.TrainButton.IsEnabled = true;
                takeTrainingImage = true;
            };
            timer.Start();
        }

        /// <summary>
        /// Target face with a BitmapSource accessor for the face
        /// </summary>
        private class BitmapSourceTargetFace : IEigenObjectTargetFace
        {
            private BitmapSource bitmapSource;

            /// <summary>
            /// Gets the BitmapSource version of the face
            /// </summary>
            public BitmapSource BitmapSource
            {
                get
                {
                    if (this.bitmapSource == null)
                        this.bitmapSource = MainWindow.LoadBitmap(this.Image);

                    return this.bitmapSource;
                }
            }

            /// <summary>
            /// Gets or sets the key returned when this face is found
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// Gets or sets the grayscale, 100x100 target image
            /// </summary>
            public Bitmap Image { get; set; }
        }
    }
}
