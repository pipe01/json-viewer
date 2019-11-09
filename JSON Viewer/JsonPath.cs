using System;
using System.Text.RegularExpressions;

namespace JSON_Viewer
{
    public static class JsonPath
    {
        private static readonly Regex ArrayAccessPattern = new Regex(@"(?<=\[)\d+(?=\])");

        public static JsonContainer Query(this JsonContainer element, string path)
        {
            var parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                var arrayAccess = ArrayAccessPattern.Match(parts[i]);

                if (arrayAccess.Success)
                {
                    element = element[int.Parse(arrayAccess.Value)];
                }
                else
                {
                    element = element[parts[i]];
                }
            }

            return element;
        }
    }
}
