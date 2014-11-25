using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BiodiversityPlugin
{
    public class OrgCodeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted = "";
            FontWeight weight = FontWeights.Normal;
            formatted = value as string;
            if (!String.IsNullOrEmpty(formatted))
            {
                weight = FontWeights.Bold;
            }
            return weight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
