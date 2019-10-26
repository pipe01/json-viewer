using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JSON_Viewer
{
    public static class JsonPath
    {
        private static readonly Regex ArrayAccessPattern = new Regex(@"(?<=\[)\d+(?=\])");

        public static JsonElement Query(this JsonElement element, string path)
        {
            var parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                var arrayAccess = ArrayAccessPattern.Match(parts[i]);

                if (arrayAccess.Success)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                        element = element.EnumerateArray().ElementAt(int.Parse(arrayAccess.Value));
                    else
                        throw new FormatException("tried to index a non-array");
                }
                else
                {
                    var prop = element.EnumerateObject().SingleOrDefault(o => o.Name == parts[i]);

                    if (prop.Name == null)
                        throw new InvalidOperationException($"cannot find property '{parts[i]}'");

                    element = prop.Value;
                }
            }

            return element;
        }
    }
}
