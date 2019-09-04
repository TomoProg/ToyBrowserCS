using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyBrowser.Ext;

namespace ToyBrowser
{
    public class Painting
    {
        public class DisplayCommand
        {

        }

        public class SolidColor : DisplayCommand
        {
            public Css.Color Color { get; private set; }
            public Layout.Rect Rect { get; private set; }
            public SolidColor(Css.Color color, Layout.Rect rect)
            {
                Color = color;
                Rect = rect;
            }
        }

        private static List<DisplayCommand> BuildDisplayList(Layout.LayoutBox layoutRoot)
        {
            var list = new List<DisplayCommand>();
            RenderLayoutBox(list, layoutRoot);
            return list;
        }

        private static void RenderLayoutBox(List<DisplayCommand> list, Layout.LayoutBox layoutBox)
        {
            RenderBackground(list, layoutBox);
            RenderBorders(list, layoutBox);

            foreach(var child in layoutBox.Children)
            {
                RenderLayoutBox(list, child);
            }
        }

        private static void RenderBackground(List<DisplayCommand> list, Layout.LayoutBox layoutBox)
        {
            var color = GetColor(layoutBox, "background");
            list.Add(new SolidColor(color, layoutBox.Dimensions.BorderBox()));
        }

        private static Css.Color GetColor(Layout.LayoutBox layoutBox, string name)
        {
            switch(layoutBox.BoxType)
            {
                case Layout.BoxType.BlockNode:
                case Layout.BoxType.InlineNode:
                    var value = new Style.StyledNode().GetValue(name);
                    switch(value)
                    {
                        case Css.ColorValue cValue:
                            return cValue.Value;
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        private static void RenderBorders(List<DisplayCommand> list, Layout.LayoutBox layoutBox)
        {
            var color = GetColor(layoutBox, "border-color");
            if (color == null)
            {
                return;
            }

            var borderBox = layoutBox.Dimensions.BorderBox();
            var rect = new Layout.Rect()
            {
                X = borderBox.X
                , Y = borderBox.Y
                , Width = layoutBox.Dimensions.Border.Left
                , Height = borderBox.Height
            };
            list.Add(new SolidColor(color, rect));

            rect = new Layout.Rect()
            {
                X = borderBox.X + borderBox.Width - layoutBox.Dimensions.Border.Right
                , Y = borderBox.Y
                , Width = layoutBox.Dimensions.Border.Right
                , Height = borderBox.Height
            };
            list.Add(new SolidColor(color, rect));

            rect = new Layout.Rect()
            {
                X = borderBox.X
                , Y = borderBox.Y
                , Width = borderBox.Width
                , Height = layoutBox.Dimensions.Border.Top
            };
            list.Add(new SolidColor(color, rect));

            rect = new Layout.Rect()
            {
                X = borderBox.X
                , Y = borderBox.Y + borderBox.Height - layoutBox.Dimensions.Border.Bottom
                , Width = borderBox.Width
                , Height = layoutBox.Dimensions.Border.Bottom
            };
            list.Add(new SolidColor(color, rect));
        }

        public class Canvas
        {
            public List<Css.Color> Pixels { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }

            public Canvas(int width, int height)
            {
                var white = new Css.Color(255, 255, 255, 255);
                Pixels = Enumerable.Repeat(white, width * height).ToList();
                Width = width;
                Height = height;
            }

            public void PaintItem(DisplayCommand item)
            {
                switch (item)
                {
                    case SolidColor solidColor:
                        var x0 = (int)solidColor.Rect.X.Adjust(0, Width);
                        var y0 = (int)solidColor.Rect.Y.Adjust(0, Height);
                        var x1 = (int)((solidColor.Rect.X + solidColor.Rect.Width).Adjust(0, Width));
                        var y1 = (int)((solidColor.Rect.Y + solidColor.Rect.Height).Adjust(0, Height));

                        foreach(var y in Enumerable.Range(y0, y1))
                        {
                            foreach(var x in Enumerable.Range(x0, x1))
                            {
                                Pixels[x + y * Width] = solidColor.Color;
                            }
                        }
                        break;
                }
            }
        }

        public static Canvas Paint(Layout.LayoutBox layoutRoot, Layout.Rect bounds)
        {
            var displayList = BuildDisplayList(layoutRoot);
            var canvas = new Canvas((int)bounds.Width, (int)bounds.Height);
            foreach(var item in displayList)
            {
                canvas.PaintItem(item);
            }
            return canvas;
        }
    }
}
