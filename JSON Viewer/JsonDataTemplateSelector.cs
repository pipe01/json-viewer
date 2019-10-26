using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace JSON_Viewer
{
    [ContentProperty(nameof(Templates))]
    public class JsonDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate[] Templates { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itemType = item?.GetType() ?? throw new ArgumentNullException(nameof(item));

            //if (itemType == typeof(JArray))
            //{
            //    return new DataTemplate(typeof(JArray))
            //    {
            //        Template = new TemplateContent
            //    }
            //}

            foreach (var template in Templates)
            {
                if (((Type)template.DataType) == itemType)
                    return template;
            }

            return null;
        }
    }
}
