using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Newtonsoft.Json;
using Sacknet.KinectFacialRecognition;
using Sacknet.KinectFacialRecognition.KinectFaceModel;
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
        private ObservableCollection<BitmapSourceTargetFace> targetFaces = new ObservableCollection<BitmapSourceTargetFace>();

        private IRecognitionProcessor activeProcessor;
        private KinectSensor kinectSensor;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.kinectSensor.Open();

            this.InitializeComponent();

            this.LoadProcessor();

            this.radioFaceModelBuilder.Checked += (sender, e) => { this.LoadProcessor(); };
            this.radioPca.Checked += (sender, e) => { this.LoadProcessor(); };

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
        /// Loads the correct procesor based on the selected radio button
        /// </summary>
        private void LoadProcessor()
        {
            if (this.radioFaceModelBuilder.IsChecked.GetValueOrDefault())
                this.activeProcessor = new FaceModelRecognitionProcessor();
            else
                this.activeProcessor = new EigenObjectRecognitionProcessor();

            this.LoadAllTargetFaces();
            this.UpdateTargetFaces();

            if (this.engine != null)
            {
                this.engine.RecognitionComplete -= this.Engine_RecognitionComplete;
                this.engine.Dispose();
            }

            this.engine = new KinectFacialRecognitionEngine(this.kinectSensor, this.activeProcessor);
            this.engine.RecognitionComplete += this.Engine_RecognitionComplete;
        }

        /// <summary>
        /// Handles recognition complete events
        /// </summary>
        private void Engine_RecognitionComplete(object sender, RecognitionResult e)
        {
            TrackedFace face = null;

            if (e.Faces != null)
                face = e.Faces.FirstOrDefault();

            using (var processedBitmap = (Bitmap)e.ColorSpaceBitmap.Clone())
            {
                if (face != null)
                {
                    using (var g = Graphics.FromImage(processedBitmap))
                    {
                        var isFmb = this.radioFaceModelBuilder.IsChecked.GetValueOrDefault();
                        var rect = face.TrackingResult.FaceRect;
                        var faceOutlineColor = Color.Green;

                        if (isFmb)
                        {
                            if (face.TrackingResult.ConstructedFaceModel == null)
                            {
                                faceOutlineColor = Color.Red;
                                
                                if (face.TrackingResult.BuilderStatus == FaceModelBuilderCollectionStatus.Complete)
                                    faceOutlineColor = Color.Orange;
                            }

                            var scale = (rect.Width + rect.Height) / 6;
                            var midX = rect.X + (rect.Width / 2);
                            var midY = rect.Y + (rect.Height / 2);

                            if ((face.TrackingResult.BuilderStatus & FaceModelBuilderCollectionStatus.LeftViewsNeeded) == FaceModelBuilderCollectionStatus.LeftViewsNeeded)
                                g.FillRectangle(new SolidBrush(Color.Red), rect.X - (scale * 2), midY, scale, scale);

                            if ((face.TrackingResult.BuilderStatus & FaceModelBuilderCollectionStatus.RightViewsNeeded) == FaceModelBuilderCollectionStatus.RightViewsNeeded)
                                g.FillRectangle(new SolidBrush(Color.Red), rect.X + rect.Width + (scale * 2), midY, scale, scale);

                            if ((face.TrackingResult.BuilderStatus & FaceModelBuilderCollectionStatus.TiltedUpViewsNeeded) == FaceModelBuilderCollectionStatus.TiltedUpViewsNeeded)
                                g.FillRectangle(new SolidBrush(Color.Red), midX, rect.Y - (scale * 2), scale, scale);

                            if ((face.TrackingResult.BuilderStatus & FaceModelBuilderCollectionStatus.FrontViewFramesNeeded) == FaceModelBuilderCollectionStatus.FrontViewFramesNeeded)
                                g.FillRectangle(new SolidBrush(Color.Red), midX, midY, scale, scale);
                        }

                        g.DrawPath(new Pen(faceOutlineColor, 5), face.TrackingResult.GetFacePath());

                        if (!string.IsNullOrEmpty(face.Key))
                        {
                            var score = Math.Round(face.ProcessorResults.First().Score, 2);

                            // Write the key on the image...
                            g.DrawString(face.Key + ": " + score, new Font("Arial", 100), Brushes.Red, new System.Drawing.Point(rect.Left, rect.Top - 25));
                        }
                    }

                    if (this.takeTrainingImage)
                    {
                        var eoResult = (EigenObjectRecognitionProcessorResult)face.ProcessorResults.SingleOrDefault(x => x is EigenObjectRecognitionProcessorResult);
                        var fmResult = (FaceModelRecognitionProcessorResult)face.ProcessorResults.SingleOrDefault(x => x is FaceModelRecognitionProcessorResult);

                        var bstf = new BitmapSourceTargetFace();
                        bstf.Key = this.NameField.Text;

                        if (eoResult != null)
                        {
                            bstf.Image = (Bitmap)eoResult.Image.Clone();
                        }
                        else
                        {
                            bstf.Image = face.TrackingResult.GetCroppedFace(e.ColorSpaceBitmap);
                        }

                        if (fmResult != null)
                        {
                            bstf.Deformations = fmResult.Deformations;
                            bstf.HairColor = fmResult.HairColor;
                            bstf.SkinColor = fmResult.SkinColor;
                        }

                        this.targetFaces.Add(bstf);

                        this.SerializeBitmapSourceTargetFace(bstf);

                        this.takeTrainingImage = false;
                        this.NameField.Text = this.NameField.Text.Replace(this.targetFaces.Count.ToString(), (this.targetFaces.Count + 1).ToString());

                        this.UpdateTargetFaces();
                    }
                }

                this.Video.Source = LoadBitmap(processedBitmap);
            }
            
            // Without an explicit call to GC.Collect here, memory runs out of control :(
            GC.Collect();
        }

        /// <summary>
        /// Saves the target face to disk
        /// </summary>
        private void SerializeBitmapSourceTargetFace(BitmapSourceTargetFace bstf)
        {
            var filenamePrefix = "TF_" + DateTime.Now.Ticks.ToString();
            var suffix = this.radioFaceModelBuilder.IsChecked.GetValueOrDefault() ? ".fmb" : ".pca";
            System.IO.File.WriteAllText(filenamePrefix + suffix, JsonConvert.SerializeObject(bstf));
            bstf.Image.Save(filenamePrefix + ".png");
        }

        /// <summary>
        /// Loads all BSTFs from the current directory
        /// </summary>
        private void LoadAllTargetFaces()
        {
            this.targetFaces.Clear();
            var result = new List<BitmapSourceTargetFace>();
            var suffix = this.radioFaceModelBuilder.IsChecked.GetValueOrDefault() ? ".fmb" : ".pca";

            foreach (var file in Directory.GetFiles(".", "TF_*" + suffix))
            {
                var bstf = JsonConvert.DeserializeObject<BitmapSourceTargetFace>(File.ReadAllText(file));
                bstf.Image = (Bitmap)Bitmap.FromFile(file.Replace(suffix, ".png"));
                this.targetFaces.Add(bstf);
            }
        }

        /// <summary>
        /// Updates the target faces
        /// </summary>
        private void UpdateTargetFaces()
        {
            if (this.targetFaces.Count > 1)
            {
                EigenObjectRecognitionProcessor pcaProcessor = this.activeProcessor as EigenObjectRecognitionProcessor;
                FaceModelRecognitionProcessor fmProcessor = this.activeProcessor as FaceModelRecognitionProcessor;

                if (pcaProcessor != null)
                    pcaProcessor.SetTargetFaces(this.targetFaces);
                else
                    fmProcessor.SetTargetFaces(this.targetFaces);
            }
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
        [JsonObject(MemberSerialization.OptIn)]
        private class BitmapSourceTargetFace : IEigenObjectTargetFace, IFaceModelTargetFace
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
            [JsonProperty]
            public string Key { get; set; }

            /// <summary>
            /// Gets or sets the grayscale, 100x100 target image
            /// </summary>
            public Bitmap Image { get; set; }

            /// <summary>
            /// Gets or sets the detected hair color of the face
            /// </summary>
            [JsonProperty]
            public Color HairColor { get; set; }

            /// <summary>
            /// Gets or sets the detected skin color of the face
            /// </summary>
            [JsonProperty]
            public Color SkinColor { get; set; }

            /// <summary>
            /// Gets or sets the detected deformations of the face
            /// </summary>
            [JsonProperty]
            public IReadOnlyDictionary<FaceShapeDeformations, float> Deformations { get; set; }
        }
    }
}
