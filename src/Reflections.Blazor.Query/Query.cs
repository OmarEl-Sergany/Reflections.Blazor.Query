using System;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public static class Query
    {
    private static Microsoft.JSInterop.IJSRuntime? _jsRuntime;

        public static void Configure(Microsoft.JSInterop.IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

        public static QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _jsRuntime ?? throw new InvalidOperationException("Call Query.Configure(JS) before using Query."));
        }
    }
}
