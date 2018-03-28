using pdftron.PDF.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PDFViewWPFToolsCS2013.Utilities
{
    class DelayRemoveTimer
    {
        private System.Windows.Threading.DispatcherTimer _Timer;
        private Canvas _HostingCanvas;
        private UIElement _ElementToRemove;
        private Tool _Tool;
        private int _PageNumber;
        internal int PageNumber { get { return _PageNumber; } }

        internal DelayRemoveTimer(Canvas host, UIElement toRemove, Tool tool, int pageNumber)
        {
            _HostingCanvas = host;
            _ElementToRemove = toRemove;
            _Tool = tool;
            _PageNumber = pageNumber;

            _Timer = new System.Windows.Threading.DispatcherTimer();
            _Timer.Interval = TimeSpan.FromSeconds(5);
            _Timer.Tick += Timer_Tick;
            _Timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Remove();
        }

        internal void Destroy()
        {
            Remove();
        }

        private void Remove()
        {
            if (_HostingCanvas != null && _HostingCanvas.Children.Contains(_ElementToRemove))
            {
                _HostingCanvas.Children.Remove(_ElementToRemove);
            }
            _Timer.Stop();
        }
    }
}
