using System;
using System.Drawing;
using System.IO;
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
            // Завантажуємо іконку відносно папки з exe
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                        "Images", "app_logo2.ico");

            _icon = new NotifyIcon
            {
                Text = "Student Planner",
                Icon = File.Exists(iconPath)
                           ? new Icon(iconPath)
                           : SystemIcons.Application,   // fallback якщо файл не знайдено
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
            // Використовуємо налаштовану іконку (app_logo2.ico) замість стандартної
            _icon.ShowBalloonTip(ms, title, text, icon);
        }

        public void Dispose() => _icon.Dispose();
    }
}