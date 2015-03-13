using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition.ManagedEigenObject
{
    /// <summary>
    /// An exception thrown from the Managed Eigen Object Recognition code
    /// </summary>
    public class EigenObjectException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the EigenObjectException class with a message
        /// </summary>
        public EigenObjectException(string message)
            : base(message)
        {
        }
    }
}
