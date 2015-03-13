using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacknet.KinectFacialRecognition.ManagedEigenObject
{
    /// <summary>
    /// Describes a target face for eigen object recognition
    /// </summary>
    public interface IEigenObjectTargetFace
    {
        /// <summary>
        /// Gets or sets the key returned when this face is found
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Gets or sets the grayscale, 100x100 target image
        /// </summary>
        Bitmap Image { get; set; }
    }
}
