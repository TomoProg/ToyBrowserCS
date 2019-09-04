using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ToyBrowser
{
    public class Css
    {
        public class Stylesheet
        {
            public List<Rule> Rules { get; set; }
        }

        public class Rule
        {
            public List<Selector> Selectors { get; set; }
            public List<Declaration> Declarations { get; set; }

        }

        public abstract class Selector
        {
            public abstract Specificity GetSpecificity();
        }

        public class SimpleSelector : Selector
        {
            public string TagName { get; set; }
            public string Id { get; set; }
            public List<string> Classes { get; set; }

            public override Specificity GetSpecificity()
            {
                return new Specificity(
                    Id == null ? 0 : 1
                    , Classes.Count
                    , TagName == null ? 0 : 1
                    );
            }
        }

        public class Declaration
        {
            public string Name { get; private set; }
            public Value Value { get; private set; }

            public Declaration(string name, Value value)
            {
                Name = name;
                Value = value;
            }
        }

        public class Value
        {
            public decimal ToPx()
            {
                switch(this)
                {
                    case Length l:
                        return l.Value;
                    default:
                        return 0;
                }
            }
        }

        public enum Unit
        {
            Px
        }

        public class Color
        {
            public int R { get; private set; }
            public int G { get; private set; }
            public int B { get; private set; }
            public int A { get; private set; }
            public Color(int r, int g, int b, int a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }
        }

        public class Specificity
        {
            public int IdValue { get; private set; }
            public int ClassValue { get; private set; }
            public int TagNameValue { get; private set; }

            public Specificity(int idValue, int classValue, int tagNameValue)
            {
                IdValue = idValue;
                ClassValue = classValue;
                TagNameValue = tagNameValue;
            }

            public int Calc()
            {
                return IdValue + ClassValue + TagNameValue;
            }
        }

        public class Keyword : Value
        {
            public string Value { get; private set; }

            public Keyword(string value)
            {
                Value = value;
            }
        }

        public class Length : Value
        {
            public decimal Value { get; private set; }
            public Unit Unit { get; private set; }

            public Length(decimal value, Unit unit)
            {
                Value = value;
                Unit = unit;
            }

            public static Length Zero(Unit unit)
            {
                return new Length(0, unit);
            }
        }

        public class ColorValue : Value
        {
            public Color Value { get; private set; }

            public ColorValue(Color value)
            {
                Value = value;
            }
        }

        public static Stylesheet Parse(string source)
        {
            var parser = new Parser(0, source);
            return new Stylesheet() { Rules = parser.ParseRules() };
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

            public List<Rule> ParseRules()
            {
                var rules = new List<Rule>();

                while (true)
                {
                    ConsumeWhitespace();
                    if (EOF()) break;
                    rules.Add(ParseRule());
                }

                return rules;
            }

            public Rule ParseRule()
            {
                return new Rule() { Selectors = ParseSelectors(), Declarations = ParseDeclarations() };
            }

            private List<Selector> ParseSelectors()
            {
                var selectors = new List<Selector>();
                while (true)
                {
                    selectors.Add(ParseSimpleSelector());
                    ConsumeWhitespace();
                    var c = NextChar();
                    if(c == ',')
                    {
                        ConsumeChar();
                        ConsumeWhitespace();
                    }
                    else if(c == '{')
                    {
                        break;
                    }
                    else
                    {
                        Trace.Fail($"Unexpected character {c} in selector list");
                    }
                }
                return selectors.OrderBy(s => s.GetSpecificity().Calc()).ToList();
            }

            public SimpleSelector ParseSimpleSelector()
            {
                var selector = new SimpleSelector() { TagName = null, Id = null, Classes = new List<string>() };
                while(!EOF())
                {
                    var c = NextChar();
                    if(c == '#')
                    {
                        ConsumeChar();
                        selector.Id = ParseIdentifier();
                    }
                    else if(c == '.')
                    {
                        ConsumeChar();
                        selector.Classes.Add(ParseIdentifier());
                    }
                    else if (c == '*')
                    {
                        ConsumeChar();
                    }
                    else if(ValidIdentifierChar(c))
                    {
                        selector.TagName = ParseIdentifier();
                    }
                    else
                    {
                        break;
                    }
                }
                return selector;
            }

            private List<Declaration> ParseDeclarations()
            {
                Trace.Assert(ConsumeChar() == '{');
                var declarations = new List<Declaration>();
                while(true)
                {
                    ConsumeWhitespace();
                    if(NextChar() == '}')
                    {
                        ConsumeChar();
                        break;
                    }
                    declarations.Add(ParseDeclaration());
                }
                return declarations;
            }

            private Declaration ParseDeclaration()
            {
                var propertyName = ParseIdentifier();
                ConsumeWhitespace();
                Trace.Assert(ConsumeChar() == ':');
                ConsumeWhitespace();
                var value = ParseValue();
                ConsumeWhitespace();
                Trace.Assert(ConsumeChar() == ';');

                return new Declaration(propertyName, value);
            }

            private Value ParseValue()
            {
                var c = NextChar();
                if(char.IsDigit(c))
                {
                    return ParseLength();
                }
                else if(c == '#')
                {
                    return ParseColor();
                }
                else
                {
                    return new Keyword(ParseIdentifier());
                }
            }

            private Value ParseLength()
            {
                return new Length(ParseFloat(), ParseUnit());
            }

            private decimal ParseFloat()
            {
                var s = ConsumeWhile(c => char.IsDigit(c) || c == '.');
                return decimal.Parse(s);
            }

            private Unit ParseUnit()
            {
                var s = ParseIdentifier().ToLower();
                switch(s)
                {
                    case "px":
                        return Unit.Px;

                    default:
                        throw new NotSupportedException($"\"{s}\" is unrecognized unit");
                }
            }

            private Value ParseColor()
            {
                Trace.Assert(ConsumeChar() == '#');
                var color = new Color(ParseHexPair(), ParseHexPair(), ParseHexPair(), 255);
                return new ColorValue(color);
            }

            private int ParseHexPair()
            {
                var s = Input.Substring(Pos, 2);
                Pos += 2;
                return Convert.ToInt32(s, 16);
            }

            private string ParseIdentifier()
            {
                return ConsumeWhile(ValidIdentifierChar);
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

            private bool EOF()
            {
                return Pos >= Input.Length;
            }

            private bool ValidIdentifierChar(char c) => Regex.IsMatch(c.ToString(), @"[a-z]|[A-Z]|%d|-|_");
        }
    }
}
