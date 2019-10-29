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

            if (parameter is string foo && foo == "name")
            {
                if (cont.ArrayIndex != null)
                    return $"{cont.ArrayIndex}:   ";
                else if (cont.PropertyName != null)
                    return $"\"{cont.PropertyName}\":   ";

                return null;
            }

            if (cont.Element.ValueKind == JsonValueKind.Array)
                return $"[{cont.Element.GetArrayLength()}]";
            else if (cont.Element.ValueKind == JsonValueKind.Object)
                return "{...}";
            else
                return JsonSerializer.Serialize(cont.Element);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new JsonElementToStringConverter();
    }
}
