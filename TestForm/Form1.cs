using System;
using System.Drawing;
using System.Windows.Forms;

namespace SampleForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();
            tileViewControl1.VirtualListSize = 20;
            tileViewControl1.DrawItem += tileViewControl1_DrawItem;
            tileViewControl1.SearchForVirtualItem += tileViewControl1_SearchItem;

            MarginTextBox.Text = tileViewControl1.TileMargin.Left.ToString();
            BorderTextBox.Text = tileViewControl1.TileBorderWidth.ToString();
            TileSizeTextBox.Text = tileViewControl1.TileSize.Width.ToString();
            VirtualItemsTextBox.Text = tileViewControl1.VirtualListSize.ToString();
            PaddingLeftTextBox.Text = tileViewControl1.TilePadding.Left.ToString();
            PaddingRightTextBox.Text = tileViewControl1.TilePadding.Right.ToString();
            PaddingTopTextBox.Text = tileViewControl1.TilePadding.Top.ToString();
            PaddingBottomTextBox.Text = tileViewControl1.TilePadding.Bottom.ToString();
            Action<object, EventArgs> t = new Action<object, EventArgs>((o, e) =>
            {
                TextBox l = o as TextBox;
                int res = 0;
                if (int.TryParse(l.Text, out res))
                {
                    Padding p = tileViewControl1.TilePadding;
                    switch (l.Name)
                    {
                        case "MarginTextBox": tileViewControl1.TileMargin = new Padding(res); break;
                        case "BorderTextBox": tileViewControl1.TileBorderWidth = res; break;
                        case "TileSizeTextBox": tileViewControl1.TileSize = new Size(res, res); break;
                        case "VirtualItemsTextBox": if (res > 0) tileViewControl1.VirtualListSize = res; else VirtualItemsTextBox.Text = "0"; break;
                        case "PaddingLeftTextBox": p.Left = res; tileViewControl1.TilePadding = p; break;
                        case "PaddingTopTextBox": p.Top = res; tileViewControl1.TilePadding = p; break;
                        case "PaddingRightTextBox": p.Right = res; tileViewControl1.TilePadding = p; break;
                        case "PaddingBottomTextBox": p.Bottom = res; tileViewControl1.TilePadding = p; break;
                        default: MessageBox.Show("Unknown textbox: " + l.Name); return;
                    }
                    //tileViewControl1.Invalidate();
                }
            });
            EventHandler h = t.Invoke;
            MarginTextBox.TextChanged += h;
            BorderTextBox.TextChanged += h;
            TileSizeTextBox.TextChanged += h;
            VirtualItemsTextBox.TextChanged += h;
            PaddingLeftTextBox.TextChanged += h;
            PaddingRightTextBox.TextChanged += h;
            PaddingTopTextBox.TextChanged += h;
            PaddingBottomTextBox.TextChanged += h;

        }

        void tileViewControl1_SearchItem(object sender, SearchForVirtualItemEventArgs e)
        {
            int res;
            if (int.TryParse(e.Text, out res))
            {
                if (res<tileViewControl1.VirtualListSize)
                    e.Index = res;
            }
        }

        void tileViewControl1_DrawItem(object sender, TileView.TileViewControl.DrawTileListItemEventArgs e)
        {
            Point pp = new Point(e.Bounds.X + tileViewControl1.TilePadding.Left, e.Bounds.Y + tileViewControl1.TilePadding.Top);
            
            e.Graphics.FillRectangle(Brushes.Pink, new Rectangle(pp, tileViewControl1.TileSize)); // tile itself

            //TODO: Rewrite text drawing completely
            string text = e.Index.ToString();
            /*
            SizeF ts = e.Graphics.MeasureString(text, tileViewControl1.Font);
            PointF tp = new PointF(pp.X + tileViewControl1.TileSize.Width / 2 - ts.Width / 2, pp.Y + tileViewControl1.TileSize.Height + ts.Height / 2);

            e.Graphics.DrawString(text, tileViewControl1.Font, new SolidBrush(tileViewControl1.ForeColor), tp.X, tp.Y); // text*/

            RectangleF rect = new RectangleF(e.Bounds.X, e.Bounds.Top + tileViewControl1.TilePadding.Top + tileViewControl1.TileSize.Height,tileViewControl1.TileSize.Width, tileViewControl1.TilePadding.Bottom);
            /*SizeF strLayout = new SizeF(tileViewControl1.TileSize.Width, tileViewControl1.TilePadding.Bottom);
            SizeF strSize = e.Graphics.MeasureString(text, e.Font, strLayout, tileViewControl1.TextStringFormat);
            PointF strPos = new PointF(e.Bounds.X + (e.Bounds.Width - strSize.Width) / 2, e.Bounds.Top + tileViewControl1.TilePadding.Top + tileViewControl1.TileSize.Height + ( strLayout.Height/2 - strSize.Height / 2));
            //new RectangleF(new PointF((ImagesListView.TileSize.Width - strSize.Width) / 2 + e.Bounds.Left, e.Bounds.Y + ImagesListView.TilePadding.Top + ImagesListView.TileSize.Height + (e.Font.Height - strSize.Height / 2))
            if (tileViewControl1.TilePadding.Bottom - strSize.Height < 2) return; //No space for text
            e.Graphics.DrawString(text, e.Font, SystemBrushes.ControlText, new RectangleF(strPos, strSize), tileViewControl1.TextStringFormat);*/
            e.DrawText(rect, text);
            //cannot use new SolidBrush(e.ForeColor) due to change on Focus to white
            //e.Graphics.DrawRectangle(Pens.Red, strPos.X, strPos.Y, strSize.Width, strSize.Height);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //int idx = tileViewControl1.GetIndexAtLocation(tileViewControl1.PointToClient(contextMenuStrip1.Location));
            //if (idx > -1)
            MessageBox.Show("Index " + tileViewControl1.FocusIndex.ToString());
        }
    }
}
