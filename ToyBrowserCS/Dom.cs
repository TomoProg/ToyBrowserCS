using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBrowserCS
{
    public class Dom
    {
        public class Node
        {
            public List<Node> Children { get; set; }
            public NodeType NodeType { get; set; }
        }

        public class NodeType
        {
        }

        public class Text : NodeType
        {
            public string Data { get; private set; }

            public Text(string data)
            {
                Data = data;
            }
        }

        public class ElementData : NodeType
        {
            public string TagName { get; private set; }
            public Dictionary<string, string> Attributes { get; private set; }
            public string Id
            {
                get
                {
                    return Attributes.TryGetValue("id", out string value) ? value : null;
                }
            }
            public IReadOnlyCollection<string> Classes
            {
                get
                {
                    return Attributes.TryGetValue("class", out string value) ? value.Split(' ') : null;
                }
            }

            public ElementData(string name, Dictionary<string, string> attrs)
            {
                TagName = name;
                Attributes = attrs;
            }
        }

        public static Node CreateText(string data)
        {
            var text = new Text(data);
            return new Node() { Children = new List<Node>(), NodeType = text };
        }

        public static Node CreateElement(string name, Dictionary<string, string> attrs, List<Node> children)
        {
            var elementData = new ElementData(name, attrs);
            return new Node() { Children = children, NodeType = elementData };
        }
    }
}
