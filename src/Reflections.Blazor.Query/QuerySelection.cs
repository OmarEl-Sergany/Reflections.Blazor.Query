using System;

namespace Reflections.Blazor.Query
{
    public sealed class QuerySelection
    {
        private readonly QueryRenderState _state;
        private readonly QuerySelection? _previous;

        internal QuerySelection(string selector, QueryRenderState state, QuerySelection? previous = null)
        {
            Selector = selector;
            _state = state;
            _previous = previous;
        }

        public string Selector { get; }

        public QuerySelection Css(
            string property,
            string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(property);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _state.SetCss(Selector, property, value);
            return this;
        }

        public QuerySelection Add(string selector)
        {
            ValidateSelector(selector);
            return Push($"{Selector}, {selector}");
        }

        public QuerySelection AddBack(string? selector = null)
        {
            if (_previous is null)
                return this;

            var previousSelector = selector is null
                ? _previous.Selector
                : $":is({_previous.Selector}):is({ValidateSelector(selector)})";
            return Push($"{Selector}, {previousSelector}");
        }

        public QuerySelection End() => _previous ?? this;

        public QuerySelection Find(string selector)
        {
            ValidateSelector(selector);
            return Push($":is({Selector}) {selector}");
        }

        public QuerySelection Children(string? selector = null)
        {
            var childSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($":is({Selector}) > {childSelector}");
        }

        public QuerySelection Filter(string selector) =>
            Push($":is({Selector}):is({ValidateSelector(selector)})");

        public QuerySelection Not(string selector) =>
            Push($":is({Selector}):not({ValidateSelector(selector)})");

        public QuerySelection Has(string selector) =>
            Push($":is({Selector}):has({ValidateSelector(selector)})");

        public QuerySelection Parent(string? selector = null)
        {
            var parentSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($"{parentSelector}:has(> :is({Selector}))");
        }

        public QuerySelection Parents(string? selector = null)
        {
            var ancestorSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($"{ancestorSelector}:has(:is({Selector}))");
        }

        public QuerySelection Next(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($":is({Selector}) + {siblingSelector}");
        }

        public QuerySelection NextAll(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($":is({Selector}) ~ {siblingSelector}");
        }

        public QuerySelection Prev(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($"{siblingSelector}:has(+ :is({Selector}))");
        }

        public QuerySelection PrevAll(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($"{siblingSelector}:has(~ :is({Selector}))");
        }

        public QuerySelection Siblings(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            return Push($"{siblingSelector}:has(~ :is({Selector})), :is({Selector}) ~ {siblingSelector}");
        }

        private QuerySelection Push(string selector) => new(selector, _state, this);

        private static string ValidateSelector(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return selector;
        }
    }
}
