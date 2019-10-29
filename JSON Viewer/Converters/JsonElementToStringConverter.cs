using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace JSON_Viewer.Converters
{
    public class JsonElementToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is JsonContainer cont))
                return null;

            string str;

            if (cont.Element.ValueKind == JsonValueKind.Array)
                str = $"[{cont.Element.GetArrayLength()}]";
            else if (cont.Element.ValueKind == JsonValueKind.Object)
                str = "{...}";
            else
                str = JsonSerializer.Serialize(cont.Element);

            if (cont.ArrayIndex != null)
                str = $"{cont.ArrayIndex}:   {str}";
            else if (cont.PropertyName != null)
                str = $"\"{cont.PropertyName}\":   {str}";

            return str;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new JsonElementToStringConverter();
    }
}
