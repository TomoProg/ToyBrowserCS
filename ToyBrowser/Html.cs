using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ToyBrowser
{
    public class Html
    {
        public static Dom.Node Parse(string source)
        {
            var nodes = new Parser(0, source).ParseNodes();

            if (nodes.Count == 1)
            {
                return nodes[0];
            }

            return Dom.CreateElement("html", new Dictionary<string, string>(), nodes);
        }

        public class Parser
        {
            public int Pos { get; private set; }
            public string Input { get; private set; }

            public Parser(int pos, string input)
            {
                Pos = pos;
                Input = input;
            }

            public List<Dom.Node> ParseNodes()
            {
                var nodes = new List<Dom.Node>();
                while (true)
                {
                    ConsumeWhitespace();
                    if (EOF() || StartsWith("</"))
                    {
                        break;
                    }
                    nodes.Add(ParseNode());
                }
                return nodes;
            }

            private Dom.Node ParseNode()
            {
                return NextChar() == '<' ? ParseElement() : ParseText();
            }

            private Dom.Node ParseElement()
            {
                Trace.Assert(ConsumeChar() == '<');
                var tagName = ParseTagName();
                var attrs = ParseAttributes();
                Trace.Assert(ConsumeChar() == '>');

                var children = ParseNodes();

                Trace.Assert(ConsumeChar() == '<');
                Trace.Assert(ConsumeChar() == '/');
                Trace.Assert(ParseTagName() == tagName);
                Trace.Assert(ConsumeChar() == '>');

                return Dom.CreateElement(tagName, attrs, children);
            }

            private string ParseTagName()
            {
                return ConsumeWhile(c => char.IsLetterOrDigit(c));
            }

            private Dictionary<string, string> ParseAttributes()
            {
                var attributes = new Dictionary<string, string>();
                while (true)
                {
                    ConsumeWhitespace();
                    if (NextChar() == '>')
                    {
                        break;
                    }
                    var attribute = ParseAttr();
                    attributes[attribute.Name] = attribute.Value;
                }
                return attributes;
            }

            private class Attribute
            {
                public string Name { get; private set; }
                public string Value { get; private set; }
                public Attribute(string name, string value)
                {
                    Name = name;
                    Value = value;
                }
            }

            private Attribute ParseAttr()
            {
                var name = ParseTagName();
                Trace.Assert(ConsumeChar() == '=');
                var value = ParseAttrValue();
                return new Attribute(name, value);
            }

            private string ParseAttrValue()
            {
                var openQuote = ConsumeChar();
                Trace.Assert(openQuote == '"' || openQuote == '\'');
                var value = ConsumeWhile(c => c != openQuote);
                Trace.Assert(ConsumeChar() == openQuote);
                return value;
            }

            private Dom.Node ParseText()
            {
                return Dom.CreateText(ConsumeWhile(c => c != '<'));
            }

            private void ConsumeWhitespace()
            {
                ConsumeWhile(c => char.IsWhiteSpace(c));
            }

            private string ConsumeWhile(Func<char, bool> test)
            {
                var result = "";
                while (!EOF() && test(NextChar()))
                {
                    result += ConsumeChar().ToString();
                }
                return result;
            }

            private char ConsumeChar()
            {
                var curChar = Input[Pos];
                Pos += 1;
                return curChar;
            }

            private char NextChar()
            {
                return Input[Pos];
            }

            private bool StartsWith(string str)
            {
                return Input.Substring(Pos).StartsWith(str);
            }

            private bool EOF()
            {
                return Pos >= Input.Length;
            }
        }
    }
}
