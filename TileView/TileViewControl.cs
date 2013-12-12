using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace TileView
{
    [Browsable(true)]
    public partial class TileViewControl: ScrollableControl
    {
        private int m_ItemsPerRow = 0;

        /// <summary>
        /// Backwards compatability with ListView ItemSelectionChanged event. Warning: Have a hight chance to be changed in future.
        /// </summary>
        public event ListViewItemSelectionChangedEventHandler ItemSelectionChanged;

        //TODO: Extend for multi-selection list
        private int m_SelectedIndex = -1;
        /// <summary>
        /// Get or Set SelectedIndex, setting this property to -1 will remove selection, -2 is reserved for "do nothing".
        /// </summary>
        public int SelectedIndex
        {
            get { return m_SelectedIndex; }
            set
            {
                if (value >= VirtualListSize) throw new ArgumentOutOfRangeException("value");
                if (m_SelectedIndex != value)
                {
                    if (value == -2) return;
                    int oldidx = m_SelectedIndex;
                    m_SelectedIndex = value;
                    if (ItemSelectionChanged != null)
                    {
                        foreach (Delegate del in ItemSelectionChanged.GetInvocationList())
                        {
                            del.DynamicInvoke(this, new ListViewItemSelectionChangedEventArgs(null, m_SelectedIndex, true));
                        }
                    }

                    if (oldidx != -1)
                        RedrawItem(oldidx);

                    if (value != -1)
                    {
                        RedrawItem(value);
                        // Scroll to item if it's out of view
                        Point p = GetItemLocation(value);
                        int y = -this.AutoScrollPosition.Y;
                        if (y > p.Y)
                        {
                            this.AutoScrollPosition = new Point(0, p.Y);
                        }
                        else if (p.Y - y + m_TotalTileSize.Height > this.Height)
                        {
                            this.AutoScrollPosition = new Point(0, p.Y + m_TotalTileSize.Height - this.Height);
                        }
                    }
                }
            }
        }

        private int m_VirtualListSize = 0;
        /// <summary>
        /// Get or Set amount of Items to be displayed.
        /// </summary>
        public int VirtualListSize
        {
            get { return m_VirtualListSize; }
            set
            {
                m_VirtualListSize = value;
                if (m_SelectedIndex >= m_VirtualListSize) SelectedIndex = -1; // we are setting public one so all events will be triggered properly
                if (this.Size.IsEmpty) return;
                cachedIndices.Clear();
                UpdateAutoScrollSize();
            }
        }

        private void UpdateAutoScrollSize()
        {
            m_ItemsPerRow = this.DisplayRectangle.Width / m_TotalTileSize.Width;
            if (m_ItemsPerRow < 1) m_ItemsPerRow = 1; // bug from line above, but could not reproduce
            this.AutoScrollMinSize = new Size(0, DivUp(m_VirtualListSize, m_ItemsPerRow) * m_TotalTileSize.Height); // for now we are supporting only Vertical scrolling.
            this.Invalidate();
        }

        private List<int> cachedIndices = new List<int>();

        //http://stackoverflow.com/questions/921180/how-can-i-ensure-that-a-division-of-integers-is-always-rounded-up/924160#924160
        private int DivUp(int a, int b)
        {
            int r = a / b;
            if (((a ^ b) >= 0) && (a % b != 0)) r++;
            return r;
        }

        private Size m_TotalTileSize = new Size(256 + 4 + 4, 256 + 4 + 32);
        public Size TotalTileSize { get { return m_TotalTileSize; } }
        private void UpdateTotalTileSize() { m_TotalTileSize = new Size(m_TileSize.Width + m_TileMargin.Horizontal + m_TilePadding.Horizontal, m_TileSize.Height + m_TileMargin.Vertical + m_TilePadding.Vertical); UpdateAutoScrollSize(); }

        private Size m_TileSize = new Size(256, 256);
        public Size TileSize { get { return m_TileSize; } set { m_TileSize = value; UpdateTotalTileSize(); } }

        private Padding m_TileMargin = new Padding(2, 2, 2, 2); // external
        public Padding TileMargin { get { return m_TileMargin; } set { m_TileMargin = value; UpdateTotalTileSize(); } }

        private Padding m_TilePadding = new Padding(2, 2, 2, 15*3); // inner
        public Padding TilePadding { get { return m_TilePadding; } set { m_TilePadding = value; UpdateTotalTileSize(); } }

        private Brush m_HighlightColor = SystemBrushes.Highlight;
        /// <summary>
        /// Color of selected item background
        /// </summary>
        public Brush HighlightColor { get { return m_HighlightColor; } set { m_HighlightColor = value; if (m_SelectedIndex != -1) RedrawItem(m_SelectedIndex); } }

        /// <summary>
        /// Backwards compatability with ListView SearchForVirtualItem event. Warning: Have a hight chance to be changed in future.
        /// </summary>
        public event SearchForVirtualItemEventHandler SearchForVirtualItem;
        private System.Timers.Timer inputTimeoutTimer = new System.Timers.Timer();
        private StringBuilder input = new StringBuilder(255);

        public TileViewControl()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.SetStyle(ControlStyles.UserMouse, true); // to make control focusable
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.SizeChanged += (o, e) => UpdateAutoScrollSize();
            this.MouseDown += (o, e) => SelectedIndex = GetIndexAtLocation(e.Location);

            inputTimeoutTimer.Interval = 5000;
            inputTimeoutTimer.Elapsed += (o, e) => { input.Clear(); Debug.Print("Text input timeout"); inputTimeoutTimer.Stop(); };

            this.KeyPress += (o, e) =>
            {
                // here we'll handle input
                // Backspace, remove last char if available
                if (e.KeyChar == '\b' && input.Length > 0)
                    input.Remove(input.Length - 1, 1);
                else if (!char.IsControl(e.KeyChar))
                    input.Append(e.KeyChar);
                if (SearchForVirtualItem != null && input.Length > 0)
                {
                    SearchForVirtualItemEventArgs args = new SearchForVirtualItemEventArgs(false, true, false, input.ToString(), new Point(), SearchDirectionHint.Down, m_SelectedIndex);
                    SearchForVirtualItem(this, args);
                    if (args.Index != m_SelectedIndex && args.Index != -1) SelectedIndex = args.Index;
                }
                
                Debug.Print("KeyPress: Handled:{0}, KeyChar:{1}, input:{2}", e.Handled, e.KeyChar, input);
                inputTimeoutTimer.Start();
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //Debug.Print("ProcessCmdKey: Msg:{0}, LParam:{1}, WParam:{2}, KeyData:{3}", msg.Msg, msg.LParam, msg.WParam, keyData);
            // Handling control keys so we can change selection with keys
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
            {
                if (m_SelectedIndex > -1)
                {
                    int newidx = m_SelectedIndex;
                    switch (keyData)
                    {
                        case Keys.Left: if (newidx - 1 > -1) --newidx; break;
                        case Keys.Right: if (newidx + 1 < m_VirtualListSize) ++newidx; break;
                        case Keys.Up: if (newidx - m_ItemsPerRow > -1) newidx -= m_ItemsPerRow; break;
                        case Keys.Down: if (newidx + m_ItemsPerRow < m_VirtualListSize) newidx += m_ItemsPerRow; break;
                    }

                    //if (newidx < 0) newidx = 0;
                    //if (newidx > m_VirtualSize - 1) newidx = m_VirtualSize - 1;
                    SelectedIndex = newidx;
                }
                else if (VirtualListSize>0) // if there's no selection, select first item
                    SelectedIndex = 0;
                return true;
            }
            else if (keyData == Keys.Escape && inputTimeoutTimer.Enabled)
            {
                Debug.Print("Text input canceled");
                input.Clear();
                inputTimeoutTimer.Stop();
                return true;
            }
            else
                return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Redraw Tile with given index
        /// </summary>
        /// <param name="index"></param>
        public void RedrawItem(int index)
        {
            Point p = new Point(index % m_ItemsPerRow * m_TotalTileSize.Width + this.AutoScrollPosition.X, index / m_ItemsPerRow * m_TotalTileSize.Height + this.AutoScrollPosition.Y);
            this.Invalidate(new Rectangle(p, m_TotalTileSize));
        }

        private Point GetItemLocation(int index)
        {
            return new Point(index % m_ItemsPerRow * m_TotalTileSize.Width, index / m_ItemsPerRow * m_TotalTileSize.Height);
        }

        /// <summary>
        /// Find index of Tile for given location. Return -2 if no Tile at given location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Index of Tile in location</returns>
        public int GetIndexAtLocation(Point location)
        {
            int line = (location.Y + -this.AutoScrollPosition.Y) / m_TotalTileSize.Height;
            int row = (location.X + -this.AutoScrollPosition.X)/ m_TotalTileSize.Width;

            if (DivUp(location.X, m_TotalTileSize.Width) > m_ItemsPerRow) return -2;

            int r = (line * m_ItemsPerRow + row);
            if (r >= VirtualListSize) return -2;
            else return r;
        }

        /// <summary>
        /// Get list of indices of Tiles for given rectangle.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns>List of indices. Indices may have discontinuities.</returns>
        private List<int> GetVisibleIndices(RectangleF rect)
        {
            int line = (int)rect.Y / m_TotalTileSize.Height;
            int row = (int)rect.X / m_TotalTileSize.Width;
            //int rem;
            //Math.DivRem(-this.AutoScrollPosition.Y, m_TotalTileSize.Height, out rem);
            //rem = (-this.AutoScrollPosition.Y + m_TotalTileSize.Height) - m_TotalTileSize.Height * line;
            int lines = ((int)(rect.Y + rect.Height - 1) / m_TotalTileSize.Height) - ((int)rect.Y / m_TotalTileSize.Height) + 1;
            int rows = (int)rect.Width / m_TotalTileSize.Width;

            if (lines == 0) lines = 1; // Draw at last one item in visible area.
            if (rows == 0) rows = 1;

            List<int> indices = new List<int>();
            int i;
            for (int l = line; l < line+lines; l++)
            {
                for (int r = row; r < row+rows; r++)
                {
                    i = l * m_ItemsPerRow + r;
                    if (i >= VirtualListSize) break;
                    if (indices.Contains(i)) continue;
                    indices.Add(i);
                }
            }
            //Debug.Print(rect.ToString());
            //Debug.Print("lines:{0}, rows:{1}, line:{2}, row:{3}, indices:[{4}]", lines, rows, line, row, string.Join(",", indices.ToArray()));
            return indices;
        }

        /// <summary>
        /// Backwards compatability with ListView DrawItem event. Warning: Have a hight chance to be changed in future.
        /// </summary>
        public event DrawItemEventHandler DrawItem;
        public event CacheItemsEventHandler CacheItems;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);
            List<int> indices = GetVisibleIndices(e.Graphics.VisibleClipBounds);
            if (CacheItems != null)
            {
                Rectangle rect = this.Bounds;
                rect.Offset(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y);
                List<int> allIndices = GetVisibleIndices(rect);
                if (allIndices.Count > 0 && (cachedIndices.Count == 0 || cachedIndices.Count < allIndices.Count || (cachedIndices[0] > allIndices[0] || cachedIndices[cachedIndices.Count - 1] < allIndices[allIndices.Count - 1])))
                {
                    CacheItemEventArgs ne = new CacheItemEventArgs(allIndices);
                    CacheItems(this, ne);
                    if (ne.Success)
                        cachedIndices = allIndices;
                }
            }
            
            foreach(int i in indices)
            {
                if (i >= VirtualListSize) throw new ArgumentException("index out of range", "i");
                Point p = GetItemLocation(i);
                Point mp = new Point(p.X + m_TileMargin.Left, p.Y + m_TileMargin.Top);

                if (this.m_SelectedIndex == i) // not sure yet on should it be in DrawItem or here...
                {
                    e.Graphics.FillRectangle(m_HighlightColor, new Rectangle(mp, m_TotalTileSize - m_TileMargin.Size));
                    //ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(mp, m_TotalTileSize - m_TileMargin.Size));
                }
                if (DrawItem != null)
                    DrawItem(this, new DrawItemEventArgs(e.Graphics, this.Font, new Rectangle(mp, m_TotalTileSize - m_TileMargin.Size), i, (this.m_SelectedIndex == i ? DrawItemState.Selected : DrawItemState.None)));
                else
                {
                    Point pp = new Point(mp.X + m_TilePadding.Left, mp.Y + m_TilePadding.Top);
                    //e.Graphics.DrawRectangle(Pens.Pink, new Rectangle(mp, m_TotalTileSize - m_TileMargin.Size - new Size(1,1))); // border
                    e.Graphics.FillRectangle(Brushes.Pink, new Rectangle(pp, m_TileSize)); // tile itself

                    //TODO: Rewrite text drawing completely
                    string text = i.ToString();
                    SizeF ts = e.Graphics.MeasureString(text, this.Font);
                    Point tp = new Point(mp.X + m_TileSize.Width / 2 - (int)ts.Width / 2, mp.Y + m_TilePadding.Top + m_TileSize.Height + (int)ts.Height / 2);

                    e.Graphics.DrawString(i.ToString(), this.Font, Brushes.Black, tp.X, tp.Y); // text
                }
            }
            //e.Graphics.DrawRectangle(new Pen(Color.FromArgb(64, Color.Black),1), new Rectangle(new Point((int)e.Graphics.ClipBounds.Location.X, (int)e.Graphics.ClipBounds.Location.Y), new Size((int)e.Graphics.ClipBounds.Width - 1, (int)e.Graphics.ClipBounds.Height-1)));
        }
    }

    /// <summary>
    /// For caching purposes.
    /// </summary>
    public class TileViewItem
    {
        public bool Selected = false;
        public string Name = string.Empty;
        public string ImageKey = string.Empty;

        public TileViewItem(string name)
        {
            this.Name = name;
        }
        public TileViewItem(string name, string imageKey) : this(name)
        {
            this.ImageKey = imageKey;
        }

        public override string ToString()
        {
            return string.Format("Name:{0}, ImageKey:{1}, Selected:{2}", Name, ImageKey, Selected);
        }
    }
}
