using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace JSON_Viewer.Converters
{
    public class JsonElementToValueColor : DependencyObject, IValueConverter
    {
        public Brush StringBrush
        {
            get { return (Brush)GetValue(StringBrushProperty); }
            set { SetValue(StringBrushProperty, value); }
        }
        public static readonly DependencyProperty StringBrushProperty =
            DependencyProperty.Register("StringBrush", typeof(Brush), typeof(JsonElementToValueColor));

        public Brush NumberBrush
        {
            get { return (Brush)GetValue(NumberBrushProperty); }
            set { SetValue(NumberBrushProperty, value); }
        }
        public static readonly DependencyProperty NumberBrushProperty =
            DependencyProperty.Register("NumberBrush", typeof(Brush), typeof(JsonElementToValueColor));


        public Brush BooleanBrush
        {
            get { return (Brush)GetValue(BooleanBrushProperty); }
            set { SetValue(BooleanBrushProperty, value); }
        }
        public static readonly DependencyProperty BooleanBrushProperty =
            DependencyProperty.Register("BooleanBrush", typeof(Brush), typeof(JsonElementToValueColor));

        public Brush NullBrush
        {
            get { return (Brush)GetValue(NullBrushProperty); }
            set { SetValue(NullBrushProperty, value); }
        }
        public static readonly DependencyProperty NullBrushProperty =
            DependencyProperty.Register("NullBrush", typeof(Brush), typeof(JsonElementToValueColor));

        public Brush ObjectBrush
        {
            get { return (Brush)GetValue(ObjectBrushProperty); }
            set { SetValue(ObjectBrushProperty, value); }
        }
        public static readonly DependencyProperty ObjectBrushProperty =
            DependencyProperty.Register("ObjectBrush", typeof(Brush), typeof(JsonElementToValueColor));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is JsonContainer json))
                return null;

            switch (json.Element.ValueKind)
            {
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return ObjectBrush;
                case JsonValueKind.String:
                    return StringBrush;
                case JsonValueKind.Number:
                    return NumberBrush;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return BooleanBrush;
                case JsonValueKind.Null:
                    return NullBrush;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
