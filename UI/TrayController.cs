using System;
using System.Drawing;
using System.Windows.Forms;
using MenubarDock.Core;
using MenubarDock.Diagnostics;

namespace MenubarDock.UI
{
    public class TrayController : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ConfigurationManager _config;
        private readonly Action _toggleAction;
        private readonly Action _openSettingsAction;
        private ToolStripMenuItem? _toggleItem;

        public TrayController(ConfigurationManager config, Action toggleAction, Action openSettingsAction)
        {
            _config = config;
            _toggleAction = toggleAction;
            _openSettingsAction = openSettingsAction;

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "MenubarDock - macOS Menu Bar for Windows 11",
                Visible = true
            };

            BuildContextMenu();
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            _toggleItem = new ToolStripMenuItem(
                _config.Settings.IsEnabled ? "Hide Menu Bar" : "Show Menu Bar",
                null,
                (s, e) => _toggleAction());
            _toggleItem.Font = new Font(_toggleItem.Font, FontStyle.Bold);

            var settingsItem = new ToolStripMenuItem("Settings...", null, (s, e) => _openSettingsAction());
            var exitItem = new ToolStripMenuItem("Exit MenubarDock", null, (s, e) =>
            {
                System.Windows.Application.Current.Shutdown();
            });

            menu.Items.Add(_toggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(settingsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => _openSettingsAction();
        }

        public void UpdateState(bool isVisible)
        {
            if (_toggleItem != null)
            {
                _toggleItem.Text = isVisible ? "Hide Menu Bar" : "Show Menu Bar";
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
