using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            tileViewControl1.VirtualListSize = 20;
            tileViewControl1.DrawItem += tileViewControl1_DrawItem;
            tileViewControl1.SearchForVirtualItem += tileViewControl1_SearchItem;
            
            MarginTextBox.Text = tileViewControl1.TileMargin.Left.ToString();
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
                    Padding p= tileViewControl1.TilePadding;
                    switch (l.Name)
                    {
                        case "MarginTextBox": tileViewControl1.TileMargin = new Padding(res); break;
                        case "PaddingLeftTextBox": p.Left = res; tileViewControl1.TilePadding = p; break;
                        case "PaddingTopTextBox": p.Top = res; tileViewControl1.TilePadding = p; break;
                        case "PaddingRightTextBox": p.Right = res; tileViewControl1.TilePadding = p; break;
                        case "PaddingBottomTextBox": p.Bottom = res; tileViewControl1.TilePadding = p; break;
                        default: MessageBox.Show("Unknown textbox: " + l.Name); break;
                    }
                }
            });
            EventHandler h = t.Invoke;
            MarginTextBox.TextChanged += h;
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

        void tileViewControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            Point pp = new Point(e.Bounds.X + tileViewControl1.TilePadding.Left, e.Bounds.Y + tileViewControl1.TilePadding.Top);

            /*if (e.State == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(tileViewControl1.HighlightColor, new Rectangle(e.Bounds.Location, tileViewControl1.TotalTileSize - tileViewControl1.TileMargin.Size));
                //ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(mp, m_TotalTileSize - m_TileMargin.Size));
            }*/
            e.Graphics.DrawRectangle(Pens.Pink, new Rectangle(e.Bounds.Location, tileViewControl1.TotalTileSize - tileViewControl1.TileMargin.Size - new Size(1, 1))); // border
            e.Graphics.FillRectangle(Brushes.Pink, new Rectangle(pp, tileViewControl1.TileSize)); // tile itself

            //TODO: Rewrite text drawing completely
            string text = e.Index.ToString();
            SizeF ts = e.Graphics.MeasureString(text, SystemFonts.DefaultFont);
            Point tp = new Point(e.Bounds.X + tileViewControl1.TileSize.Width / 2 - (int)ts.Width / 2, e.Bounds.Y + tileViewControl1.TilePadding.Top + tileViewControl1.TileSize.Height + (int)ts.Height / 2);

            e.Graphics.DrawString(text, SystemFonts.DefaultFont, Brushes.Black, tp.X, tp.Y); // text

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //int idx = tileViewControl1.GetIndexAtLocation(tileViewControl1.PointToClient(contextMenuStrip1.Location));
            //if (idx > -1)
            MessageBox.Show("Index " + tileViewControl1.SelectedIndex.ToString());
        }
    }
}
