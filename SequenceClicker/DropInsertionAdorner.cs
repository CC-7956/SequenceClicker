using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SequenceClicker
{
    public class DropInsertionAdorner : Adorner
    {
        private bool _isAbove;
        private UIElement _adornedElement;

        public DropInsertionAdorner(UIElement adornedElement, bool isAbove)
            : base(adornedElement)
        {
            _adornedElement = adornedElement;
            _isAbove = isAbove;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var rect = new Rect(_adornedElement.RenderSize);
            double y = _isAbove ? 0 : rect.Bottom;

            var pen = new Pen(Brushes.Red, 2);
            drawingContext.DrawLine(pen, new Point(0, y), new Point(rect.Right, y));
        }
    }
}