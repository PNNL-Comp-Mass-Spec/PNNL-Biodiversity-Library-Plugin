using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BiodiversityPlugin
{
    public class OrgCodeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted = "";
            Color color = Colors.Black;
            formatted = value as string;
            if (String.IsNullOrEmpty(formatted))
            {
                color = Colors.Red;
            }
            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
