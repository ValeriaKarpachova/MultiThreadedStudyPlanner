using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp2
{
    public class StringToBrushAlphaConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            try
            {
                var col = (Color)ColorConverter.ConvertFromString(v?.ToString() ?? "#534AB7");
               
                return new SolidColorBrush(Color.FromArgb(38, col.R, col.G, col.B));
            }
            catch { return new SolidColorBrush(Color.FromArgb(38, 83, 74, 183)); }
        }

        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
}