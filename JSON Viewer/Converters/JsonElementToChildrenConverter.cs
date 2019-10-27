using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace JSON_Viewer.Converters
{
    public class JsonElementToChildrenConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is JsonContainer cont))
                return null;

            return cont.Children;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new JsonElementToChildrenConverter();
    }
}
