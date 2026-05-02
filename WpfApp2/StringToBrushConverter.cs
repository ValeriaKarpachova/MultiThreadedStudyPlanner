using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp2
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            try
            {
                return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(v?.ToString() ?? "#FFFDE7"));
            }
            catch { return Brushes.LightYellow; }
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
}