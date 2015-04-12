using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sacknet.KinectFacialRecognitionDemo
{
    /// <summary>
    /// Command implementation that calls an action
    /// </summary>
    public class ActionCommand : ICommand
    {
        private Action toExecute;

        /// <summary>
        /// Initializes a new instance of the ActionCommand class
        /// </summary>
        public ActionCommand(Action toExecute)
        {
            this.toExecute = toExecute;
        }

#pragma warning disable 67
        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        /// <summary>
        /// Determines whether the command can execute in its current state
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// The action to be called when the command is invoked.
        /// </summary>
        public void Execute(object parameter)
        {
            this.toExecute();
        }
    }
}
