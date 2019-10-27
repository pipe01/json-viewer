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
            return Serialize(value as JsonContainer);
        }

        private static string Serialize(JsonContainer value)
        {
            if (value == null)
                return null;

            string str;

            if (value.Element.ValueKind == JsonValueKind.Array)
                str = $"[{value.Element.GetArrayLength()}]";
            else if (value.Element.ValueKind == JsonValueKind.Object)
                str = "{...}";
            else
                str = JsonSerializer.Serialize(value.Element);

            if (value.ArrayIndex != null)
                str = $"{value.ArrayIndex}:   {str}";
            else if (value.PropertyName != null)
                str = $"\"{value.PropertyName}\":   {str}";

            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new JsonElementToStringConverter();
    }
}
