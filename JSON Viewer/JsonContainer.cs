using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace JSON_Viewer
{
    public class JsonContainer : INotifyPropertyChanged, IReadOnlyCollection<JsonContainer>
    {
        public JsonElement Element { get; }
        public string Path { get; }
        public int? ArrayIndex { get; }
        public string PropertyName { get; }

        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        public bool IsObject => Element.ValueKind == JsonValueKind.Object || Element.ValueKind == JsonValueKind.Array;
        public bool IsBoolean => Element.ValueKind == JsonValueKind.True || Element.ValueKind == JsonValueKind.False;

        public string AsString => Element.ValueKind == JsonValueKind.String
                ? Element.GetString()
                : Element.ValueKind == JsonValueKind.Null
                    ? (string)null
                    : throw new InvalidOperationException($"Tried to convert a value of type {Element.ValueKind} to a string");

        public float AsNumber => Element.TryGetSingle(out var f)
                ? f
                : throw new InvalidOperationException($"Tried to convert a value of type {Element.ValueKind} to a number");

        public bool AsBool => Element.ValueKind == JsonValueKind.True
                ? true
                : Element.ValueKind == JsonValueKind.False
                    ? false
                    : throw new InvalidOperationException($"Tried to convert a value of type {Element.ValueKind} to a boolean");

        int IReadOnlyCollection<JsonContainer>.Count => this.Children.Length;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public JsonContainer this[string query]
        {
            get
            {
                if (Element.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Tried to string-index a value of type {Element.ValueKind}");

                if (query.Contains('.'))
                {
                    return this.Query(query);
                }

                foreach (var item in Children)
                {
                    if (item.PropertyName == query)
                        return item;
                }

                throw new KeyNotFoundException();
            }
        }

        public JsonContainer this[int index]
        {
            get
            {
                if (Element.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException($"Tried to index a value of type {Element.ValueKind}");

                return Children[index];
            }
        }

        public IEnumerator<JsonContainer> GetEnumerator()
        {
            return ((IReadOnlyCollection<JsonContainer>)this.Children).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyCollection<JsonContainer>)this.Children).GetEnumerator();
        }

        public static implicit operator float(JsonContainer json) => json.AsNumber;

        public static implicit operator string(JsonContainer json) => json.AsString;

        public static implicit operator bool(JsonContainer json) => json.AsBool;
    }
}
