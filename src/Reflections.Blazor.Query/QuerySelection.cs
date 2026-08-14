using System;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public sealed class QuerySelection
    {
        private readonly Microsoft.JSInterop.IJSRuntime _jsRuntime;

        internal QuerySelection(string selector, Microsoft.JSInterop.IJSRuntime jsRuntime)
        {
            Selector = selector;
            _jsRuntime = jsRuntime;
        }

        public string Selector { get; }

        public Task<QuerySelection> Css(
            string property,
            string value)
        {
            return ApplyCssAsync(property, value);
        }

        private async Task<QuerySelection> ApplyCssAsync(string property, string value)
        {
            await _jsRuntime.InvokeVoidAsync("ReflectionsBlazorQuery.setCss", Selector, property, value);
            return this;
        }
    }
}
