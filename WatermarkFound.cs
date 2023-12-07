using iText.Kernel.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfRemoveWaterMark
{
    class WatermarkFound
    {
        public List<Rectangle> warterMarkBounds;
        public int page;

        public WatermarkFound(int page, List<Rectangle> warterMarkBounds)
        {
            this.page = page;
            this.warterMarkBounds = warterMarkBounds;
        }
    }
}
