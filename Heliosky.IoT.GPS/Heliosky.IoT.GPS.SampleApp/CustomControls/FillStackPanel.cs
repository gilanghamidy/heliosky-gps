using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Heliosky.IoT.GPS.SampleApp.CustomControls
{
    public class FillStackPanel : StackPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if(Orientation == Orientation.Vertical)
            {
                var cellWidth = finalSize.Width;
                var itemCount = Children.Count;
                var cellHeight = finalSize.Height / itemCount;

                Size cellSize = new Size(cellWidth, cellHeight);

                foreach (var item in Children)
                {
                    
                }
            }
            else
            {

            }

            return finalSize;
        }
    }
}
