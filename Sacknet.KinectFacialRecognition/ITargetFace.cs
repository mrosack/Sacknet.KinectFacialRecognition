using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Common inteface for all target face types
    /// </summary>
    public interface ITargetFace
    {
        /// <summary>
        /// Gets or sets the key returned when this face is found
        /// </summary>
        string Key { get; set; }
    }
}
