using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyBrowser;
using System.Drawing;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            var html = System.IO.File.ReadAllText("sample.html");
            var css = System.IO.File.ReadAllText("sample.css");

            var viewport = new Layout.Dimensions();
            viewport.Content.Width = 800;
            viewport.Content.Height = 600;

            var rootNode = Html.Parse(html);
            var stylesheet = Css.Parse(css);
            var styleRoot = Style.StyleTree(rootNode, stylesheet);
            var layoutRoot = Layout.LayoutTree(styleRoot, viewport);

            var canvas = Painting.Paint(layoutRoot, viewport.Content);
            var bitmap = new Bitmap(800, 600);
            for(int x = 0; x < canvas.Width; x++)
            {
                for(int y = 0; y < canvas.Height; y++)
                {
                    var px = canvas.Pixels[y * canvas.Width + x];
                    bitmap.SetPixel(x, y, Color.FromArgb(px.A, px.R, px.G, px.B));
                }
            }
            bitmap.Save("result.png");
        }
    }
}
