using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBrowser
{
    public class Layout
    {
        public class Rect
        {
            public decimal X { get; set; }
            public decimal Y { get; set; }
            public decimal Width { get; set; }
            public decimal Height { get; set; }

            public Rect ExpandedBy(EdgeSizes edge)
            {
                return new Rect()
                {
                    X = X - edge.Left,
                    Y = Y - edge.Top,
                    Width = Width + edge.Left + edge.Right,
                    Height = Height + edge.Top + edge.Bottom
                };
            }
        }

        public class Dimensions
        {
            public Rect Content { get; private set; } = new Rect();
            public EdgeSizes Padding { get; private set; } = new EdgeSizes();
            public EdgeSizes Border { get; private set; } = new EdgeSizes();
            public EdgeSizes Margin { get; private set; } = new EdgeSizes();

            public Rect PaddingBox()
            {
                return Content.ExpandedBy(Padding);
            }

            public Rect BorderBox()
            {
                return Content.ExpandedBy(Border);
            }

            public Rect MarginBox()
            {
                return Content.ExpandedBy(Margin);
            }
        }

        public class EdgeSizes
        {
            public decimal Left { get; set; }
            public decimal Right { get; set; }
            public decimal Top { get; set; }
            public decimal Bottom { get; set; }
        }

        public class LayoutBox
        {
            public Dimensions Dimensions { get; private set; }
            public BoxType BoxType { get; private set; }
            public List<LayoutBox> Children { get; private set; }

            public LayoutBox(BoxType boxType)
            {
                Dimensions = new Dimensions();
                BoxType = boxType;
                Children = new List<LayoutBox>();
            }

            public LayoutBox GetInlineContainer()
            {
                switch(BoxType)
                {
                    case BoxType.BlockNode:
                        if(Children.Last().BoxType != BoxType.AnonymousBlock)
                        {
                            Children.Add(new LayoutBox(BoxType.AnonymousBlock));
                        }
                        return Children.Last();

                    default:
                        return this;
                }
            }

            public void Layout(Dimensions containingBlock)
            {
                switch(BoxType)
                {
                    case BoxType.BlockNode:
                        LayoutBlock(containingBlock);
                        break;

                    case BoxType.InlineNode:
                        break;

                    case BoxType.AnonymousBlock:
                        break;

                }
            }

            private Style.StyledNode GetStyleNode()
            {
                switch(BoxType)
                {
                    case BoxType.AnonymousBlock:
                        throw new LayoutException("Anonymous block box has no style node");
                    default:
                        return new Style.StyledNode();
                }
            }

            private void LayoutBlock(Dimensions containingBlock)
            {
                CalculateBlockWidth(containingBlock);
                CalculateBlockPosition(containingBlock);
                LayoutBlockChildren();
                CalculateBlockHeight();
            }

            private void CalculateBlockWidth(Dimensions containingBlock)
            {
                var style = GetStyleNode();

                var auto = new Css.Keyword("auto");
                var width = style.GetValue("width") ?? auto;

                var zero = Css.Length.Zero(Css.Unit.Px);

                var marginLeft = style.Lookup("margin-left", "margin", zero);
                var marginRight = style.Lookup("margin-right", "margin", zero);

                var borderLeft = style.Lookup("border-left-width", "border-width", zero);
                var borderRight = style.Lookup("border-right-width", "border-width", zero);

                var paddingLeft = style.Lookup("padding-left", "padding", zero);
                var paddingRight = style.Lookup("padding-right", "padding", zero);

                var total = new Css.Value[] {
                    marginLeft, marginRight, borderLeft, borderRight,
                    paddingLeft, paddingRight, width
                }.Select(v => v.ToPx()).Sum();

                if(width != auto && total > containingBlock.Content.Width)
                {
                    if(marginLeft == auto)
                    {
                        marginLeft = Css.Length.Zero(Css.Unit.Px);
                    }
                    if (marginRight == auto)
                    {
                        marginRight = Css.Length.Zero(Css.Unit.Px);
                    }
                }

                var underflow = containingBlock.Content.Width - total;

                if(width != auto && marginLeft != auto && marginRight != auto)
                {
                    marginRight = new Css.Length(marginRight.ToPx(), Css.Unit.Px);
                }
                else if(width != auto && marginLeft != auto && marginRight == auto)
                {
                    marginRight = new Css.Length(underflow, Css.Unit.Px);
                }
                else if (width != auto && marginLeft == auto && marginRight != auto)
                {
                    marginRight = new Css.Length(underflow, Css.Unit.Px);
                }
                else if (width == auto)
                {
                    if(marginLeft == auto)
                    {
                        marginLeft = Css.Length.Zero(Css.Unit.Px);
                    }
                    if(marginRight == auto)
                    {
                        marginRight = Css.Length.Zero(Css.Unit.Px);
                    }

                    if(underflow >= 0)
                    {
                        width = new Css.Length(underflow, Css.Unit.Px);
                    }
                    else
                    {
                        width = Css.Length.Zero(Css.Unit.Px);
                        marginRight = new Css.Length(marginRight.ToPx() + underflow, Css.Unit.Px);
                    }
                }
                else if(width != auto && marginLeft == auto && marginRight == auto)
                {
                    marginLeft = new Css.Length(underflow / 2, Css.Unit.Px);
                    marginRight = new Css.Length(underflow / 2, Css.Unit.Px);
                }

                Dimensions.Content.Width = width.ToPx();

                Dimensions.Padding.Left = paddingLeft.ToPx();
                Dimensions.Padding.Right = paddingRight.ToPx();

                Dimensions.Border.Left = borderLeft.ToPx();
                Dimensions.Border.Right = borderRight.ToPx();

                Dimensions.Margin.Left = marginLeft.ToPx();
                Dimensions.Margin.Right = marginRight.ToPx();
            }

            private void CalculateBlockPosition(Dimensions containingBlock)
            {
                var style = GetStyleNode();
                var zero = Css.Length.Zero(Css.Unit.Px);

                Dimensions.Margin.Top = style.Lookup("margin-top", "margin", zero).ToPx();
                Dimensions.Margin.Bottom = style.Lookup("margin-bottom", "margin", zero).ToPx();

                Dimensions.Border.Top = style.Lookup("border-top-width", "border-width", zero).ToPx();
                Dimensions.Border.Bottom = style.Lookup("border-bottom-width", "border-width", zero).ToPx();

                Dimensions.Padding.Top = style.Lookup("padding-top", "padding", zero).ToPx();
                Dimensions.Padding.Bottom = style.Lookup("padding-bottom", "padding", zero).ToPx();

                Dimensions.Content.X = containingBlock.Content.X
                    + Dimensions.Margin.Left
                    + Dimensions.Border.Left
                    + Dimensions.Padding.Left;

                Dimensions.Content.Y = containingBlock.Content.Height
                    + Dimensions.Content.Y
                    + Dimensions.Margin.Top
                    + Dimensions.Border.Top
                    + Dimensions.Padding.Top;
            }

            private void LayoutBlockChildren()
            {
                foreach(var child in Children)
                {
                    child.Layout(Dimensions);
                    Dimensions.Content.Height = Dimensions.Content.Height + child.Dimensions.MarginBox().Height;
                }
            }

            private void CalculateBlockHeight()
            {
                var height = GetStyleNode().GetValue("height");
                if(height != null)
                {
                    Dimensions.Content.Height = height.ToPx();
                }
            }
        }

        public enum BoxType
        {
            BlockNode,
            InlineNode,
            AnonymousBlock
        }

        //public class BoxType2
        //{
        //    public Style.StyledNode StyledNode { get; private set; }
        //}

        //public class BlockNode : BoxType2
        //{
        //}

        //public class InlineNode : BoxType2
        //{
        //}

        //public class AnonymousBlock : BoxType2
        //{
        //}

        private static LayoutBox BuildLayoutTree(Style.StyledNode styleNode)
        {
            var display = styleNode.GetDisplay();
            BoxType boxType;
            switch (display)
            {
                case Style.Display.Block:
                    boxType = BoxType.BlockNode;
                    break;

                case Style.Display.Inline:
                    boxType = BoxType.InlineNode;
                    break;

                default:
                    throw new LayoutException("Root node has display: none.");
            }

            var root = new LayoutBox(boxType);
            foreach (var child in styleNode.Children)
            {
                switch (child.GetDisplay())
                {
                    case Style.Display.Block:
                        root.Children.Add(BuildLayoutTree(child));
                        break;

                    case Style.Display.Inline:
                        root.GetInlineContainer().Children.Add(BuildLayoutTree(child));
                        break;
                }
            }

            return root;
        }

        public class LayoutException : Exception
        {
            public LayoutException() : base() { }
            public LayoutException(string message) : base(message) { }
            public LayoutException(string message, Exception inner) : base(message, inner) { }
        }

        public static LayoutBox LayoutTree(Style.StyledNode node, Dimensions containingBlock)
        {
            //containingBlock.Content.Height = 0;
            var rootBox = BuildLayoutTree(node);
            rootBox.Layout(containingBlock);
            return rootBox;
        }
    }
}
