using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace howto_ownerdraw_image_and_text
{
    public class ImageAndText
    {
        // Margins around owner drawn ComboBoxes.
        private const int MarginWidth = 4;
        private const int MarginHeight = 4;

        public Image Picture;
        public string Text;
        public Font Font;

        private int pictureHeight = 0;
        private int pictureWidth = 0;

        public ImageAndText(Image picture, string text, Font font)
        {
            Picture = picture;
            pictureHeight = Picture.Height / 2;
            pictureWidth = Picture.Width / 2;
            Text = text;
            Font = font;
        }

        // Set the size needed to display the image and text.
        private int Width, Height;
        private bool SizeCalculated = false;
        public void MeasureItem(MeasureItemEventArgs e)
        {
            // See if we've already calculated this.
            if (!SizeCalculated)
            {
                SizeCalculated = true;

                // See how much room the text needs.
                SizeF text_size = e.Graphics.MeasureString(Text, Font);

                // The height is the maximum of the image height and text height.
                Height = 2 * MarginHeight +
                    (int)Math.Max(pictureHeight, text_size.Height);

                // The width is the sum of the image and text widths.
                Width = (int)(4 * MarginWidth + pictureWidth + text_size.Width);
            }

            e.ItemWidth = Width;
            e.ItemHeight = Height;
        }

        // Draw the item.
        public void DrawItem(DrawItemEventArgs e)
        {
            // Clear the background appropriately.
            e.DrawBackground();
            RectangleF rect_back = new RectangleF(
                e.Bounds.X,
                e.Bounds.Y,
                e.Bounds.Width, e.Bounds.Height);
            if (e.Bounds.Height < pictureHeight)
            {
                SolidBrush brush = new SolidBrush(Color.FromArgb(3, 134, 120));//定义画刷
                e.Graphics.FillRectangle(brush, rect_back);//填充颜色
                Pen pen1 = new Pen(Color.FromArgb(3, 134, 120), 12);
                e.Graphics.DrawRectangle(pen1, Rectangle.Round(rect_back));//绘制边框
            } else
            {
                SolidBrush brush = new SolidBrush(Color.WhiteSmoke);//定义画刷
                e.Graphics.FillRectangle(brush, rect_back);//填充颜色
                Pen pen1 = new Pen(Color.WhiteSmoke, 12);
                e.Graphics.DrawRectangle(pen1, Rectangle.Round(rect_back));//绘制边框

            }
            // Draw the image.
            float hgt = e.Bounds.Height - 2 * MarginHeight;
            float scale = hgt / pictureHeight;
            float wid = pictureWidth * scale;
            RectangleF rect = new RectangleF(
                e.Bounds.X + MarginWidth,
                e.Bounds.Y + MarginHeight,
                wid, hgt);

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            e.Graphics.DrawImage(Picture, rect);

            // Draw the text.
            // If we're drawing on the control,
            // draw only the first line of text.
            string[] item_split = Text.Split('\n');
            string country_text = item_split[0] + "\n";
            string nodes_text = item_split[1];
//             if (e.Bounds.Height < pictureHeight)
//                 visible_text = Text.Substring(0, Text.IndexOf('\n'));

            // Make a rectangle to hold the text.
            wid = e.Bounds.Width - rect.Right - 3 * MarginWidth;
            rect = new RectangleF(rect.Right + 2 * MarginWidth, rect.Y, wid, hgt);

            RectangleF rect1 = new RectangleF(rect.X, rect.Y, wid, hgt / 2);
            RectangleF rect2 = new RectangleF(rect.X, rect.Y + (hgt / 2), wid, hgt / 2);

            Font font1 = new Font("Times New Roman", 14, FontStyle.Bold);
            Font font2 = new Font("Times New Roman", 10, FontStyle.Bold);
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                if (e.Bounds.Height < pictureHeight)
                {
                    e.Graphics.DrawString(country_text, font1, Brushes.White, rect1, sf);
                    e.Graphics.DrawString(nodes_text, font2, Brushes.White, rect2, sf);
                } else
                {
                    e.Graphics.DrawString(country_text, font1, Brushes.Black, rect1, sf);
                    e.Graphics.DrawString(nodes_text, font2, Brushes.Black, rect2, sf);
                    e.Graphics.DrawRectangle(Pens.Gainsboro, Rectangle.Round(rect));
                }
            }

            // Draw the focus rectangle if appropriate.
            e.DrawFocusRectangle();
        }
    }
}
