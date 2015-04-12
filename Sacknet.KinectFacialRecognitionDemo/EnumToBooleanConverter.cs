using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Sacknet.KinectFacialRecognitionDemo
{
    /// <summary>
    /// Returns true when the enum matches
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts from enum to boolean
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        /// <summary>
        /// Converts from boolean to enum
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
