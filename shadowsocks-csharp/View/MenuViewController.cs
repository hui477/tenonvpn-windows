using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;

using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Linq;
using Shadowsocks.LipP2P;

namespace Shadowsocks.View
{
    public class MenuViewController
    {
        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just do anything related to the config form

        private ShadowsocksController controller;
        private UpdateChecker updateChecker;

        private NotifyIcon _notifyIcon;
        private Icon icon, icon_in, icon_out, icon_both, previousIcon;

        private bool _isFirstRun;
        private bool _isStartupChecking;

        private ContextMenu contextMenu1;

        private ConfigForm configForm;
        private ProxyForm proxyForm;
        private LogForm logForm;
        private HotkeySettingsForm hotkeySettingsForm;



        // color definition for icon color transformation
        private readonly Color colorMaskBlue = Color.FromArgb(255, 25, 125, 191);
        private readonly Color colorMaskDarkSilver = Color.FromArgb(128, 192, 192, 192);
        private readonly Color colorMaskLightSilver = Color.FromArgb(192, 192, 192, 192);
        private readonly Color colorMaskEclipse = Color.FromArgb(192, 64, 64, 64);

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();
            //             controller.ConfigChanged += controller_ConfigChanged;
            //             controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            //             controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            //             controller.Errored += controller_Errored;
            //             controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            //             controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIconAndNotifyText();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;
            _notifyIcon.BalloonTipClosed += _notifyIcon_BalloonTipClosed;
            controller.TrafficChanged += controller_TrafficChanged;

            this.updateChecker = new UpdateChecker();
            updateChecker.CheckUpdateCompleted += updateChecker_CheckUpdateCompleted;

            Configuration config = controller.GetConfigurationCopy();

            if (config.isDefault)
            {
                _isFirstRun = true;
            }
            else if (config.autoCheckUpdate)
            {
                _isStartupChecking = true;
                updateChecker.CheckUpdate(config, 3000);
            }

            ShowConfigForm();
        }

        private void notifyIcon1_DoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowConfigForm();
            }
        }

        private void ShowConfigForm()
        {
            if (configForm != null)
            {
                configForm.Activate();
            }
            else
            {
                configForm = new ConfigForm(controller);
                configForm.Show();
                configForm.Activate();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm.Dispose();
            configForm = null;
            Utils.ReleaseMemory(true);
            if (_isFirstRun)
            {
                CheckUpdateForFirstRun();
                ShowFirstTimeBalloon();
                _isFirstRun = false;
            }
        }

        private void controller_TrafficChanged(object sender, EventArgs e)
        {
            if (icon == null)
                return;

            Icon newIcon;

            bool hasInbound = controller.trafficPerSecondQueue.Last().inboundIncreasement > 0;
            bool hasOutbound = controller.trafficPerSecondQueue.Last().outboundIncreasement > 0;

            if (hasInbound && hasOutbound)
                newIcon = icon_both;
            else if (hasInbound)
                newIcon = icon_in;
            else if (hasOutbound)
                newIcon = icon_out;
            else
                newIcon = icon;

            if (newIcon != this.previousIcon)
            {
                this.previousIcon = newIcon;
                _notifyIcon.Icon = newIcon;
            }
        }

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), I18N.GetString("Shadowsocks Error: {0}", e.GetException().Message));
        }

        #region Tray Icon

        private void UpdateTrayIconAndNotifyText()
        {
            Configuration config = controller.GetConfigurationCopy();
            bool enabled = config.enabled;
            bool global = config.global;

            Color colorMask = SelectColorMask(enabled, global);
            Size iconSize = SelectIconSize();

            UpdateIconSet(colorMask, iconSize, out icon, out icon_in, out icon_out, out icon_both);

            previousIcon = icon;
            _notifyIcon.Icon = previousIcon;

            string serverInfo = null;
            if (controller.GetCurrentStrategy() != null)
            {
                serverInfo = controller.GetCurrentStrategy().Name;
            }
            else
            {
                serverInfo = config.GetCurrentServer().FriendlyName();
            }
            // show more info by hacking the P/Invoke declaration for NOTIFYICONDATA inside Windows Forms
            string text = I18N.GetString("TenonVPN") + " " + P2pLib.kCurrentVersion;
            if (text.Length > 127)
            {
                text = text.Substring(0, 126 - 3) + "...";
            }
            ViewUtils.SetNotifyIconText(_notifyIcon, text);
        }

        /// <summary>
        /// Determine the icon size based on the screen DPI.
        /// </summary>
        /// <returns></returns>
        /// https://stackoverflow.com/a/40851713/2075611
        private Size SelectIconSize()
        {
            Size size = new Size(32, 32);
            int dpi = ViewUtils.GetScreenDpi();
            if (dpi < 97)
            {
                // dpi = 96;
                size = new Size(16, 16);
            }
            else if (dpi < 121)
            {
                // dpi = 120;
                size = new Size(20, 20);
            }
            else if (dpi < 145)
            {
                // dpi = 144;
                size = new Size(24, 24);
            }
            else
            {
                // dpi = 168;
                size = new Size(28, 28);
            }
            return size;
        }

        private Color SelectColorMask(bool isProxyEnabled, bool isGlobalProxy)
        {
            Color colorMask = Color.White;

            Utils.WindowsThemeMode currentWindowsThemeMode = Utils.GetWindows10SystemThemeSetting(controller.GetCurrentConfiguration().isVerboseLogging);

            if (isProxyEnabled)
            {
                if (isGlobalProxy)  // global
                {
                    colorMask = colorMaskBlue;
                }
                else  // PAC
                {
                    if (currentWindowsThemeMode == Utils.WindowsThemeMode.Light)
                    {
                        colorMask = colorMaskEclipse;
                    }
                }
            }
            else  // disabled
            {
                if (currentWindowsThemeMode == Utils.WindowsThemeMode.Light)
                {
                    colorMask = colorMaskDarkSilver;
                }
                else
                {
                    colorMask = colorMaskLightSilver;
                }
            }

            return colorMask;
        }

        private void UpdateIconSet(Color colorMask, Size size,
            out Icon icon, out Icon icon_in, out Icon icon_out, out Icon icon_both)
        {
            Bitmap iconBitmap;

            // generate the base icon
            iconBitmap = ViewUtils.ChangeBitmapColor(Resources.ss32Fill, colorMask);
            iconBitmap = ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32Outline);

            icon = Icon.FromHandle(ViewUtils.ResizeBitmap(iconBitmap, size.Width, size.Height).GetHicon());
            icon_in = Icon.FromHandle(ViewUtils.ResizeBitmap(ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32In), size.Width, size.Height).GetHicon());
            icon_out = Icon.FromHandle(ViewUtils.ResizeBitmap(ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32In), size.Width, size.Height).GetHicon());
            icon_both = Icon.FromHandle(ViewUtils.ResizeBitmap(ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32In, Resources.ss32Out), size.Width, size.Height).GetHicon());
        }



        #endregion

        #region MenuItems and MenuGroups

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new ContextMenu(new MenuItem[] {
                CreateMenuItem("Quit", new EventHandler(this.Quit_Click))
            });
        }

        #endregion

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            UpdateTrayIconAndNotifyText();
        }


        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }

        void controller_UpdatePACFromGFWListError(object sender, System.IO.ErrorEventArgs e)
        {
            ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, ToolTipIcon.Error, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            string result = e.Success
                ? I18N.GetString("PAC updated")
                : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);
        }

        void updateChecker_CheckUpdateCompleted(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks {0} Update Found", updateChecker.LatestVersionNumber + updateChecker.LatestVersionSuffix), I18N.GetString("Click here to update"), ToolTipIcon.Info, 5000);
            }
            else if (!_isStartupChecking)
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("No update is available"), ToolTipIcon.Info, 5000);
            }
            _isStartupChecking = false;
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                updateChecker.NewVersionFound = false; /* Reset the flag */
                if (System.IO.File.Exists(updateChecker.LatestVersionLocalName))
                {
                    string argument = "/select, \"" + updateChecker.LatestVersionLocalName + "\"";
                    System.Diagnostics.Process.Start("explorer.exe", argument);
                }
            }
        }

        private void _notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                updateChecker.NewVersionFound = false; /* Reset the flag */
            }
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            controller.ToggleEnable(false);
            P2pLib.GetInstance().connectSuccess = false;
            P2pLib.GetInstance().connectStarted = false;
            P2pLib.GetInstance().disConnectStarted = false;
            Process.GetCurrentProcess().Kill();
            System.Environment.Exit(0);
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void CheckUpdateForFirstRun()
        {
            Configuration config = controller.GetConfigurationCopy();
            if (config.isDefault) return;
            _isStartupChecking = true;
            updateChecker.CheckUpdate(config, 3000);
        }

        private void ShowFirstTimeBalloon()
        {
//             _notifyIcon.BalloonTipTitle = I18N.GetString("Shadowsocks is here");
//             _notifyIcon.BalloonTipText = I18N.GetString("You can turn on/off Shadowsocks in the context menu");
//             _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
//             _notifyIcon.ShowBalloonTip(0);
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
//             Process.Start("https://github.com/shadowsocks/shadowsocks-windows");
        }
    }
}
