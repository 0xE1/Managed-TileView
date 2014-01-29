using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileView
{
    public delegate void CacheItemsEventHandler(object sender, CacheItemEventArgs e);

    public class CacheItemEventArgs : EventArgs
    {
        private List<int> indices;

        public CacheItemEventArgs(List<int> indices)
        {
            this.indices = indices;
        }

        public List<int> Indices
        {
            get
            {
                return indices;
            }
        }

        public bool Success;
    }

}
