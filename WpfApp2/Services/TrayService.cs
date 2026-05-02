using System;
using System.Drawing;
using System.Windows.Forms;
using WinForms = System.Windows.Forms;

namespace WpfApp2.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _icon;

        public event Action? OpenRequested;

        public TrayService()
        {
            _icon = new NotifyIcon
            {
                Text = "Student Planner",
                Icon = SystemIcons.Application,
                Visible = true
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Відкрити", null, (_, __) => OpenRequested?.Invoke());
            menu.Items.Add("Вийти", null, (_, __) =>
            {
                _icon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            _icon.ContextMenuStrip = menu;
            _icon.DoubleClick += (_, __) => OpenRequested?.Invoke();
        }

        public void Notify(string title, string text,
            ToolTipIcon icon = ToolTipIcon.Info, int ms = 4000)
        {
            _icon.ShowBalloonTip(ms, title, text, icon);
        }

        public void Dispose() => _icon.Dispose();
    }
}