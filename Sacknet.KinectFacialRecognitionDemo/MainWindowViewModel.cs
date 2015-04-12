using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Sacknet.KinectFacialRecognitionDemo
{
    /// <summary>
    /// View model for the main window
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private ImageSource currentVideoFrame;
        private ProcessorTypes processorType;
        private string trainName;
        private bool trainNameEnabled, readyForTraining, trainingInProcess;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class
        /// </summary>
        public MainWindowViewModel()
        {
            this.TargetFaces = new ObservableCollection<MainWindow.BitmapSourceTargetFace>();
        }

        /// <summary>
        /// Raised when a property is changed on the view model
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the target faces for matching
        /// </summary>
        public ObservableCollection<MainWindow.BitmapSourceTargetFace> TargetFaces { get; private set; }

        /// <summary>
        /// Gets or sets a command that's executed when the train button is clicked
        /// </summary>
        public ICommand TrainButtonClicked { get; set; }

        /// <summary>
        /// Gets or sets the current video frame
        /// </summary>
        public ImageSource CurrentVideoFrame
        {
            get
            {
                return this.currentVideoFrame;
            }

            set
            {
                this.currentVideoFrame = value;
                if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentVideoFrame"));
            }
        }

        /// <summary>
        /// Gets or sets the current processor type
        /// </summary>
        public ProcessorTypes ProcessorType
        {
            get
            {
                return this.processorType;
            }

            set
            {
                this.processorType = value;
                if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("ProcessorType"));
            }
        }

        /// <summary>
        /// Gets or sets the name of the training image
        /// </summary>
        public string TrainName
        {
            get
            {
                return this.trainName;
            }

            set
            {
                this.trainName = value;
                if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("TrainName"));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the train button should be enabled
        /// </summary>
        public bool TrainButtonEnabled
        {
            get { return this.ReadyForTraining && !this.trainingInProcess; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we're ready to train the system
        /// </summary>
        public bool ReadyForTraining
        {
            get
            {
                return this.readyForTraining;
            }

            set
            {
                this.readyForTraining = value;
                if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("TrainButtonEnabled"));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether training is in process
        /// </summary>
        public bool TrainingInProcess
        {
            get
            {
                return this.trainingInProcess;
            }

            set
            {
                this.trainingInProcess = value;
                if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("TrainButtonEnabled"));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the training name field should be enabled
        /// </summary>
        public bool TrainNameEnabled
        {
            get
            {
                return this.trainNameEnabled;
            }

            set
            {
                this.trainNameEnabled = value;
                if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("TrainNameEnabled"));
            }
        }
    }
}
