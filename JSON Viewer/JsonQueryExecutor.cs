using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JSON_Viewer
{
    public static class JsonQueryExecutor
    {
        private static readonly ScriptOptions Options =
            ScriptOptions.Default
            .AddReferences(typeof(Enumerable).Assembly, Assembly.GetExecutingAssembly())
            .AddImports("System.Linq");

        public static async Task<object> RunQuery(JsonContainer root, string query)
        {
            var func = await CSharpScript.EvaluateAsync<Func<JsonContainer, object>>(query, Options);
            return func(root);
        }
    }
}
