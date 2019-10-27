using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Documents;

namespace JSON_Viewer
{
    public class JsonContainer : INotifyPropertyChanged
    {
        public JsonElement Element { get; }
        public string Path { get; }
        public int? ArrayIndex { get; }
        public string PropertyName { get; }

        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        public JsonContainer(JsonElement element, string path, int? arrayIndex = null, string propertyName = null)
        {
            this.Element = element;
            this.Path = path;
            this.ArrayIndex = arrayIndex;
            this.PropertyName = propertyName;
        }

        private JsonContainer[] _Children;
        public JsonContainer[] Children
        {
            get
            {
                if (_Children == null)
                {
                    var children = new List<JsonContainer>();

                    if (Element.ValueKind == JsonValueKind.Array)
                    {
                        int i = 0;
                        foreach (var item in Element.EnumerateArray())
                        {
                            children.Add(new JsonContainer(item, $"{this.Path}.[{i}]", arrayIndex: i++));
                        }
                    }
                    else if (Element.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var item in Element.EnumerateObject())
                        {
                            children.Add(new JsonContainer(item.Value, $"{this.Path}.{item.Name}", propertyName: item.Name));
                        }
                    }

                    _Children = children.ToArray();
                }

                return _Children;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
