using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Linq;
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
        public event FocusSelectionChangedEventHandler FocusSelectionChanged;

        public IndicesCollection SelectedIndices = new IndicesCollection();

        private int m_FocusIndex = -1;
        /// <summary>
        /// Get or Set SelectedIndex, setting this property to -1 will remove selection, -2 is reserved for "do nothing".
        /// </summary>
        public int FocusIndex
        {
            get { return m_FocusIndex; }
            set
            {
                if (value >= VirtualListSize) throw new ArgumentOutOfRangeException("value");
                if (m_FocusIndex != value)
                {
                    if (value == -2) return;
                    int oldidx = m_FocusIndex;
                    m_FocusIndex = value;
                    /*if (ItemSelectionChanged != null)
                    {
                        foreach (Delegate del in ItemSelectionChanged.GetInvocationList())
                        {
                            del.DynamicInvoke(this, new ListViewItemSelectionChangedEventArgs(null, m_SelectedIndex, true));
                        }
                    }*/

                    if (oldidx != -1)
                        RedrawItem(oldidx);

                    if (FocusSelectionChanged != null)
                        FocusSelectionChanged.Invoke(this, new EventArgs());

                    if (value != -1)
                    {
                        RedrawItem(value);
                        // Scroll to item if it's out of view
                        Point p = GetItemLocation(value);
                        int y = -this.AutoScrollPosition.Y;
                        if (y == p.Y) return;
                        else if (y > p.Y)
                        {
                            this.AutoScrollPosition = new Point(0, p.Y);
                        }
                        else if (p.Y - y + TotalTileSize.Height > this.Height)
                        {//TODO: correct slight misspositioning while this.Height (control) is less than m_TotalTileSize.Height
                            this.AutoScrollPosition = new Point(0, p.Y + TotalTileSize.Height - this.Height);
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
                if (m_FocusIndex >= m_VirtualListSize) FocusIndex = -1; // we are setting public one so all events will be triggered properly
                if (this.Size.IsEmpty) return;
                SelectedIndices.Clear();
                cachedIndices.Clear();
                UpdateAutoScrollSize();
            }
        }

        /// <summary>
        /// This function is being called whenever anything related to size of Tile or VirtualListSize is changed. It also updated ItemsPerRow value.
        /// </summary>
        /// <returns></returns>
        private void UpdateAutoScrollSize()
        {
            if (m_VirtualListSize == 0 || this.Size.IsEmpty) return;
            
            m_ItemsPerRow = this.DisplayRectangle.Width / TotalTileSize.Width;
            
            //if (m_ItemsPerRow < 1) m_ItemsPerRow = 1; // bug from line above, but could not reproduce
            this.AutoScrollMinSize = new Size(0, DivUp(m_VirtualListSize, m_ItemsPerRow) * TotalTileSize.Height); // for now we are supporting only Vertical scrolling.

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

        /*//private static Size m_TotalTileSize = new Size(m_TileSize.Width + m_TileMargin.Horizontal + m_TilePadding.Horizontal, m_TileSize.Height + m_TileMargin.Vertical + m_TilePadding.Vertical);//new Size(256 + 4 + 4, 256 + 4 + 32);
        //public Size TotalTileSize { get { return m_TotalTileSize; } }
        public Size TotalTileSize { get { return new Size(m_TileSize.Width + m_TileMargin.Horizontal + m_TilePadding.Horizontal, m_TileSize.Height + m_TileMargin.Vertical + m_TilePadding.Vertical); } }
        private void UpdateTotalTileSize() { m_TotalTileSize = new Size(m_TileSize.Width + m_TileMargin.Horizontal + m_TilePadding.Horizontal, m_TileSize.Height + m_TileMargin.Vertical + m_TilePadding.Vertical); UpdateAutoScrollSize(); }
        */

        //TODO: Do a performance check of TotalTileSize getter
        public Size TotalTileSize { get { return new Size(m_TileSize.Width + m_TileMargin.Horizontal + m_TilePadding.Horizontal + (int)(m_TileBorder.Width * 2), m_TileSize.Height + m_TileMargin.Vertical + m_TilePadding.Vertical + (int)(m_TileBorder.Width * 2)); } }
        private Size m_TileSize = new Size(256, 256);
        public Size TileSize { get { return m_TileSize; } set { m_TileSize = value; UpdateAutoScrollSize(); } } //UpdateTotalTileSize();
        private  Padding m_TileMargin = new Padding(2, 2, 2, 2); // external
        public Padding TileMargin { get { return m_TileMargin; } set { m_TileMargin = value; UpdateAutoScrollSize(); } } //UpdateTotalTileSize();
        private  Padding m_TilePadding =new Padding(2, 2, 2, 5+15 * 2); // inner // 5=padding from bottom, 15*n=text
        public Padding TilePadding { get { return m_TilePadding; } set { m_TilePadding = value; UpdateAutoScrollSize(); } } //UpdateTotalTileSize();

        private Pen m_TileBorder = new Pen(Brushes.Black, 1.0f);

        public float TileBorderWidth { get { return m_TileBorder.Width; } set { m_TileBorder.Width = value; UpdateAutoScrollSize(); } }

        public Color TileBorderColor { get { return m_TileBorder.Color; } set { m_TileBorder.Color = value; if (m_TileBorder.Width>0) this.Invalidate(); } }

        private Brush m_TileHighlightColor = SystemBrushes.Highlight;
        /// <summary>
        /// Color of selected item background
        /// </summary>
        public Brush TileHighlightColor { get { return m_TileHighlightColor; } set { m_TileHighlightColor = value; if (SelectedIndices.Count > 0) RedrawItems(SelectedIndices.ToList()); } }

        /// <summary>
        /// Backwards compatability with ListView SearchForVirtualItem event. Warning: Have a hight chance to be changed in future.
        /// </summary>
        public event SearchForVirtualItemEventHandler SearchForVirtualItem;
        private System.Timers.Timer inputTimeoutTimer = new System.Timers.Timer(2000);
        private StringBuilder input = new StringBuilder(255);

        /// <summary>
        /// Get or Set Timeout interval for Input.
        /// In Milliseconds. (2000 is default)
        /// </summary>
        public double InputTimeoutInterval { get { return inputTimeoutTimer.Interval; } set { inputTimeoutTimer.Interval = value; } }

        public TileViewControl()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.SetStyle(ControlStyles.UserMouse, true); // to make control focusable
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.SizeChanged += (o, e) => UpdateAutoScrollSize();
            this.MouseDown += (o, e) =>
            {
                int idx = GetIndexAtLocation(e.Location);
                FocusIndex = idx;
                if (idx != -2 && e.Button == System.Windows.Forms.MouseButtons.Left) // no Tile at given location
                {
                    SelectIndex(idx);
                }
            };

            this.SelectedIndices.CollectionChanged += (o, e) => {
                if (e.ItemsChanged.Count == 0) return;
                RedrawItems(e.ItemsChanged);

                if (ItemSelectionChanged != null)
                {
                    foreach (Delegate del in ItemSelectionChanged.GetInvocationList())
                    {
                        del.DynamicInvoke(this, new ListViewItemSelectionChangedEventArgs(null, e.ItemsChanged[0], (e.Action == IndicesCollection.NotifyCollectionChangedAction.Add ? true : false)));
                    }
                }
            };

            inputTimeoutTimer.AutoReset = false;
            inputTimeoutTimer.Elapsed += (o, e) => { input.Clear(); Debug.Print("Text input timeout"); /*inputTimeoutTimer.Stop();*/ };

            this.KeyPress += (o, e) =>
            {
                // here we'll handle input
                // Backspace, remove last char if available
                if (SearchForVirtualItem != null)
                {
                    if (e.KeyChar == '\b' && input.Length > 0)
                        input.Remove(input.Length - 1, 1);
                    else if (!char.IsControl(e.KeyChar))
                        input.Append(e.KeyChar);
                    if (input.Length > 0)
                    {
                        SearchForVirtualItemEventArgs args = new SearchForVirtualItemEventArgs(false, true, false, input.ToString(), new Point(), SearchDirectionHint.Down, m_FocusIndex);
                        SearchForVirtualItem(this, args);
                        if (args.Index != m_FocusIndex && args.Index != -1) FocusIndex = args.Index;
                        e.Handled = true;
                        if (inputTimeoutTimer.Enabled) inputTimeoutTimer.Stop();
                        inputTimeoutTimer.Start();
                    }
                }

                Debug.Print("KeyPress: Handled:{0}, KeyChar:{1}, Key:{2}, input:\"{3}\"", e.Handled, e.KeyChar, ((Enum)(Keys)e.KeyChar).ToString(), input);
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            /*if (Debugger.IsAttached)
            {
                Debugger.Log(0, "Info", "Key: " + keyData.ToString() + "(0x" + keyData.ToString("X") + ") -> '" + ((char)keyData).ToString() + "' -> " + ((Enum)keyData).ToString() + "\r\n");
            }*/

            // Handling control keys so we can change focus with keys
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
            {
                if (m_FocusIndex > -1)
                {
                    int newidx = m_FocusIndex;
                    switch (keyData)
                    {
                        case Keys.Left: if (newidx - 1 > -1) --newidx; break;
                        case Keys.Right: if (newidx + 1 < m_VirtualListSize) ++newidx; break;
                        case Keys.Up: if (newidx - m_ItemsPerRow > -1) newidx -= m_ItemsPerRow; break;
                        case Keys.Down: if (newidx + m_ItemsPerRow < m_VirtualListSize) newidx += m_ItemsPerRow; break;
                    }

                    //if (newidx < 0) newidx = 0;
                    //if (newidx > m_VirtualSize - 1) newidx = m_VirtualSize - 1;
                    FocusIndex = newidx;
                }
                else if (VirtualListSize>0) // if there's no focus, focus on first item visible index
                {
                    FocusIndex = GetVisibleIndices(this.Bounds).First();
                }
                Debug.Print("Handled in CmdKey (Control Keys): " + ((Enum)keyData).ToString());
                return true;
            }
                // (keyData & Keys.Space)==Keys.Space
            else if ((keyData==Keys.Space || (keyData & Keys.Space) == Keys.Space && ((keyData & Keys.Shift) == Keys.Shift || (keyData & Keys.Control) == Keys.Control))
                && input.Length == 0) //ISSUE: Selection via Space would not be handled if there's still an input available, so it would not work right after search
            {
                if (m_FocusIndex >= 0)
                {
                    SelectIndex(m_FocusIndex);
                }
                Debug.Print("Handled in CmdKey (Space): " + ((Enum)keyData).ToString());
                return true;
            }
            else if (keyData == Keys.Escape && inputTimeoutTimer.Enabled)
            {
                Debug.Print("Text input canceled/reset");
                input.Clear();
                inputTimeoutTimer.Stop();
                Debug.Print("Handled in CmdKey (Escape): " + ((Enum)keyData).ToString());
                return true;
            }
            else
            {
                Debug.Print("Passed to base CmdKey: " + ((Enum)keyData).ToString());
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void SelectIndex(int index)
        {
            switch (ModifierKeys)
            {
                case Keys.Control:
                    if (SelectedIndices.Contains(index))
                        SelectedIndices.Remove(index);
                    else
                        SelectedIndices.Add(index); break;
                default:
                    if (!SelectedIndices.Contains(index))
                    {
                        SelectedIndices.Clear();
                        SelectedIndices.Add(index);
                    }
                    break;
            }
        }

        /// <summary>
        /// Redraw Tile with given index
        /// </summary>
        /// <param name="index"></param>
        public void RedrawItem(int index)
        {
            Point p = new Point(index % m_ItemsPerRow * TotalTileSize.Width + this.AutoScrollPosition.X, index / m_ItemsPerRow * TotalTileSize.Height + this.AutoScrollPosition.Y);
            this.Invalidate(new Rectangle(p, TotalTileSize));
        }
        public void RedrawItems(List<int> indices)
        {
            indices.ForEach(i => RedrawItem(i));
        }

        private Point GetItemLocation(int index)
        {
            return new Point(index % m_ItemsPerRow * TotalTileSize.Width, index / m_ItemsPerRow * TotalTileSize.Height);
        }

        /// <summary>
        /// Find index of Tile for given location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Index of Tile in location. Return -2 if no Tile at given location.</returns>
        public int GetIndexAtLocation(Point location)
        {
            int line = (location.Y + -this.AutoScrollPosition.Y) / TotalTileSize.Height;
            int row = (location.X + -this.AutoScrollPosition.X) / TotalTileSize.Width;
            if (DivUp(location.X, TotalTileSize.Width) > m_ItemsPerRow) return -2;

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
            int line = (int)rect.Y / TotalTileSize.Height;
            int row = (int)rect.X / TotalTileSize.Width;
            //int rem;
            //Math.DivRem(-this.AutoScrollPosition.Y, m_TotalTileSize.Height, out rem);
            //rem = (-this.AutoScrollPosition.Y + m_TotalTileSize.Height) - m_TotalTileSize.Height * line;
            int lines = ((int)(rect.Y + rect.Height - 1) / TotalTileSize.Height) - ((int)rect.Y / TotalTileSize.Height) + 1;
            int rows = (int)rect.Width / TotalTileSize.Width;

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
        public event DrawTileListItemEventHandler DrawItem;
        public event CacheItemsEventHandler CacheItems;
        public static StringFormat TextStringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.LineLimit };

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
                Size bs = new Size((int)m_TileBorder.Width * 2, (int)m_TileBorder.Width * 2); // border size
                Size single = new Size(1, 1);
                Point p = GetItemLocation(i);
                Point mp = new Point(p.X + m_TileMargin.Left , p.Y + m_TileMargin.Top); //margined point
                Point bp = new Point(mp.X + (int)m_TileBorder.Width, mp.Y + (int)m_TileBorder.Width); //bordered point
                Point pp = new Point(bp.X + m_TilePadding.Left, bp.Y + m_TilePadding.Top); //padded point
                //m_TileSize + m_TilePadding.Size + bs + m_TileMargin.Size
                Rectangle mr = new Rectangle(mp, m_TileSize + m_TilePadding.Size + bs);
                Rectangle br = new Rectangle(bp, m_TileSize + m_TilePadding.Size);
                Rectangle pr = new Rectangle(pp, m_TileSize);

                if (this.SelectedIndices.Contains(i))
                {
                    e.Graphics.FillRectangle(m_TileHighlightColor, mr);
                }

                if (DrawItem != null)
                    DrawItem(this, new DrawTileListItemEventArgs(e.Graphics, this.Font, br, i, (this.m_FocusIndex == i ? DrawItemState.Selected : DrawItemState.None)));
                else
                {
                    e.Graphics.FillRectangle(Brushes.Pink, pr); // tile itself

                    //TODO: Rewrite text drawing completely
                    string text = i.ToString();
                    SizeF strLayout = new SizeF(TileSize.Width, TilePadding.Bottom);
                    SizeF strSize = e.Graphics.MeasureString(text, this.Font, strLayout, TextStringFormat);
                    PointF strPos = new PointF(br.X + (br.Width - strSize.Width) / 2, br.Top + TilePadding.Top + TileSize.Height + (strLayout.Height / 2 - strSize.Height / 2));
                    //new RectangleF(new PointF((ImagesListView.TileSize.Width - strSize.Width) / 2 + e.Bounds.Left, e.Bounds.Y + ImagesListView.TilePadding.Top + ImagesListView.TileSize.Height + (e.Font.Height - strSize.Height / 2))
                    if (TilePadding.Bottom - strSize.Height < 2) return; //No space for text
                    e.Graphics.DrawString(text, this.Font, SystemBrushes.ControlText, new RectangleF(strPos, strSize), TextStringFormat);
                }
                
                if (m_TileBorder.Width > 0)
                    e.Graphics.DrawRectangle(m_TileBorder, new Rectangle(mr.Location, mr.Size - single));

                if (this.m_FocusIndex == i) // not sure yet on should it be in DrawItem or here...
                {
                    ControlPaint.DrawFocusRectangle(e.Graphics, mr);
                }
            }
            //e.Graphics.DrawRectangle(new Pen(Color.FromArgb(64, Color.Black),1), new Rectangle(new Point((int)e.Graphics.ClipBounds.Location.X, (int)e.Graphics.ClipBounds.Location.Y), new Size((int)e.Graphics.ClipBounds.Width - 1, (int)e.Graphics.ClipBounds.Height-1)));
        }

        public delegate void FocusSelectionChangedEventHandler(object sender, EventArgs e);

        public delegate void DrawTileListItemEventHandler(object sender, DrawTileListItemEventArgs e);
        public class DrawTileListItemEventArgs : DrawItemEventArgs
        {
            public DrawTileListItemEventArgs(Graphics graphics, Font font, Rectangle rect, int index, DrawItemState state)
                : base(graphics, font, rect, index, state)
            {

            }

            public void DrawText(RectangleF rect, string text)
            {
                SizeF strSize = this.Graphics.MeasureString(text, this.Font, rect.Size, TextStringFormat);
                PointF strPos = new PointF(rect.X + (rect.Width - strSize.Width) / 2, rect.Y + (rect.Height / 2 - strSize.Height / 2));
                //new RectangleF(new PointF((ImagesListView.TileSize.Width - strSize.Width) / 2 + e.Bounds.Left, e.Bounds.Y + ImagesListView.TilePadding.Top + ImagesListView.TileSize.Height + (e.Font.Height - strSize.Height / 2))
                if (rect.Bottom - strSize.Height < 2) return; //No space for text
                this.Graphics.DrawString(text, this.Font, SystemBrushes.ControlText, new RectangleF(strPos, strSize), TextStringFormat);
            }
        }
        
    }

    /* Unused
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
    }*/
}
