using System;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public sealed class QuerySelection
    {
        private readonly QueryRenderState _state;

        internal QuerySelection(string selector, QueryRenderState state)
        {
            Selector = selector;
            _state = state;
        }

        public string Selector { get; }

        public QuerySelection Css(
            string property,
            string value)
        {
            _state.SetCss(Selector, property, value);
            return this;
        }
    }
}
