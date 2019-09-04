using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBrowserCS
{
    public class Style
    {
        public enum Display
        {
            Inline,
            Block,
            None
        }

        public class StyledNode
        {
            public Dom.Node Node { get; private set; }
            public Dictionary<string, Css.Value> SpecifiedValues { get; private set; }
            public List<StyledNode> Children { get; private set; }

            public StyledNode()
            {
                Node = new Dom.Node();
                SpecifiedValues = new Dictionary<string, Css.Value>();
                Children = new List<StyledNode>();
            }

            public StyledNode(Dom.Node node, Dictionary<string, Css.Value> specifiedValues, List<StyledNode> children)
            {
                Node = node;
                SpecifiedValues = specifiedValues;
                Children = children;
            }

            public Css.Value GetValue(string name)
            {
                if (SpecifiedValues.TryGetValue(name, out Css.Value result))
                {
                    return result;
                }
                return null;
            }

            public Css.Value Lookup(string name, string fallbackName, Css.Value defaultValue)
            {
                return GetValue(name) ?? GetValue(fallbackName) ?? defaultValue;
            }

            public Display GetDisplay()
            {
                if(GetValue("display") is Css.Keyword keyword)
                {
                    switch (keyword.Value)
                    {
                        case "block":
                            return Display.Block;
                        case "none":
                            return Display.None;
                        default:
                            return Display.Inline;
                    }
                }
                return Display.Inline;
            }
        }

        public static StyledNode StyleTree(Dom.Node root, Css.Stylesheet stylesheet)
        {
            var specifiedValues = new Dictionary<string, Css.Value>();
            switch(root.NodeType)
            {
                case Dom.ElementData elem:
                    specifiedValues = GetSpecifiedValues(elem, stylesheet);
                    break;
            }

            var children = root.Children.Select(child => StyleTree(child, stylesheet)).ToList();

            return new StyledNode(root, specifiedValues, children);
        }

        private static Dictionary<string, Css.Value> GetSpecifiedValues(Dom.ElementData elem, Css.Stylesheet stylesheet)
        {
            var values = new Dictionary<string, Css.Value>();
            var rules = MatchingRules(elem, stylesheet)
                .OrderBy(rule => rule.Specificity.Calc())
                .Select(r => r.Rule);

            foreach(var rule in rules)
            {
                foreach(var declaration in rule.Declarations)
                {
                    values[declaration.Name] = declaration.Value;
                }
            }

            return values;
        }

        private class MatchedRule
        {
            public Css.Specificity Specificity { get; private set; }
            public Css.Rule Rule { get; private set; }
            public MatchedRule(Css.Specificity specificity, Css.Rule rule)
            {
                Specificity = specificity;
                Rule = rule;
            }
        }

        private static List<MatchedRule> MatchingRules(Dom.ElementData elem, Css.Stylesheet stylesheet)
        {
            return stylesheet.Rules
                .Select(rule => MatchRule(elem, rule))
                .Where(rule => rule != null)
                .ToList();
        }

        private static MatchedRule MatchRule(Dom.ElementData elem, Css.Rule rule)
        {
            var selector = rule.Selectors.FirstOrDefault(s => Matches(elem, s));
            if(selector == null)
            {
                return null;
            }
            return new MatchedRule(selector.GetSpecificity(), rule);
        }

        private static bool Matches(Dom.ElementData elem, Css.Selector selector)
        {
            switch(selector)
            {
                case Css.SimpleSelector simpleSelector:
                    return MatchesSimpleSelector(elem, simpleSelector);
                default:
                    return false;
            }
        }

        private static bool MatchesSimpleSelector(Dom.ElementData elem, Css.SimpleSelector selector)
        {
            if(selector.TagName != "*" && selector.TagName != elem.TagName)
            {
                return false;
            }

            if(selector.Id != "*" && selector.Id != elem.Id)
            {
                return false;
            }

            var elemClasses = elem.Classes;
            if(selector.Classes.All(c => c != "*") && selector.Classes.Any(c => !elemClasses.Contains(c)))
            {
                return false;
            }

            return true;
        }
    }
}
