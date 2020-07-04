using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using howto_ownerdraw_image_and_text;
using Shadowsocks.Controller;
using Shadowsocks.LipP2P;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.View
{
    public partial class ConfigForm : Form
    {
        private ShadowsocksController controller;
        private ProxyForm proxyForm;
        private VersionControl upgradeForm;
        private OutBandwidth outbandForm;

        private int check_vip_times = 0;
        private bool upgrade_is_shown = false;
        private string upgrade_shown_version = "";

        public ConfigForm(ShadowsocksController controller)
        {
            Font = SystemFonts.MessageBoxFont;
            InitializeComponent();

            // a dirty hack
            PerformLayout();

            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;
        }

        private SynchronizationContext _syncContext = null;
        private Thread progressThread = null;

        private void ConfigForm_Load_1(object sender, EventArgs e)
        {
            // Make a font for the item text.
            Font font = new Font("Times New Roman", 14);
            Random ran = new Random();
            int n = ran.Next(100, 1000);

            // Make image and text data.
            ImageAndText[] planets =
            {
                new ImageAndText(Resources.us, "America" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.sg, "Singapore" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.br, "Brazil" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.de, "Germany" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.nl, "Netherlands" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.fr, "France" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.kr, "Korea" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.jp, "Japan" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.ca, "Canada" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.au, "Australia" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.hk, "Hong Kong" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.in1, "India" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.gb, "England" + '\n' + ran.Next(100, 1000) + " nodes", font),
                new ImageAndText(Resources.cn, "China" + '\n' + ran.Next(100, 1000) + " nodes", font),
            };

            cboPlanets.DisplayImagesAndText(planets);
            int select_idx = 0;
            for (int i = 0;i < P2pLib.GetInstance().now_countries_.Count; ++i)
            {
                if (P2pLib.GetInstance().now_countries_[i] == P2pLib.GetInstance().choose_country_)
                {
                    select_idx = i;
                    break;
                }
            }
            cboPlanets.SelectedIndex = select_idx;

            this.button2.Text = I18N.GetString("UPGRADE");
            this.button4.Text = I18N.GetString("SETTINGS");
            this.label7.Text = I18N.GetString("Tenon p2p network protecting your privacy.");
            this.label6.Text = I18N.GetString("Balance");
            this.label9.Text = I18N.GetString("");
            this.label10.Text = I18N.GetString("waiting server...");
            this.label12.Text = I18N.GetString("Share to friends and earn the tenon coin.");

            _syncContext = SynchronizationContext.Current;
            progressThread = new Thread(() =>
            {
                while (true)
                {
                    _syncContext.Post(ResetBalance, null);
                    Thread.Sleep(1000);
                }
            });
            progressThread.IsBackground = true;
            progressThread.Start();
        }

        public void ResetBalance(object obj)
        {
            autoShowUpgrade();
            checkServerInfo(false);
            if (!P2pLib.GetInstance().server_status.Equals("ok"))
            {
                if (P2pLib.GetInstance().server_status.Equals("cni"))
                {
                    this.label8.Text = I18N.GetString("Agent service is not supported in your country or region.");
                }

                if (P2pLib.GetInstance().server_status.Equals("cnn"))
                {
                    this.label8.Text = I18N.GetString("Connect p2p vpn server failed.");
                }

                if (P2pLib.GetInstance().server_status.Equals("oul"))
                {
                    this.label8.Text = I18N.GetString("Your account is logged in elsewhere.");
                }
                SynchronizationContext syncContext = SynchronizationContext.Current;
                if (P2pLib.GetInstance().connectSuccess)
                {
                    if (!P2pLib.GetInstance().disConnectStarted)
                    {
                        P2pLib.GetInstance().disConnectStarted = true;

                        Thread tmp_thread = new Thread(() =>
                        {
                            controller.Stop();
                            controller.ToggleEnable(false);
                            P2pLib.GetInstance().connectSuccess = false;
                            P2pLib.GetInstance().connectStarted = false;
                            syncContext.Post(ConnectButton.ThreadRefresh, null);
                            P2pLib.GetInstance().disConnectStarted = false;
                        });
                        tmp_thread.IsBackground = true;
                        tmp_thread.Start();
                    }
                }
            }

            long balance = P2pLib.GetInstance().Balance();
            if (balance >= 0)
            {
                P2pLib.GetInstance().now_balance = balance;
                this.label10.Text = balance + " Tenon";
            }

            if (P2pLib.GetInstance().now_balance < 0)
            {
                P2pLib.createAccount();
            }

            if (check_vip_times < 10)
            {
                long tm = P2pLib.GetInstance().CheckVIP();
                if (P2pLib.GetInstance().payfor_timestamp == 0 || tm != long.MaxValue)
                {
                    if (tm != long.MaxValue && tm != 0)
                    {
                        check_vip_times = 11;
                    }
                    P2pLib.GetInstance().payfor_timestamp = tm;
                }
                ++check_vip_times;
            }
            else
            {
                P2pLib.GetInstance().PayforVpn();
            }

//             if (P2pLib.GetInstance().vip_left_days == -1 &&
//                     P2pLib.GetInstance().now_balance != -1)
//             {
//                 this.label9.Text = "";
//                 pictureBox2.Visible = false;
//                 button5.Visible = true;
//             }

            if (P2pLib.GetInstance().vip_left_days >= 0)
            {
//                 pictureBox2.Visible = true;
//                 button5.Visible = false;
                this.label9.Text = I18N.GetString("Due in ") + P2pLib.GetInstance().vip_left_days + I18N.GetString("days");
            }
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            this.label8.Text = I18N.GetString("");
            if (P2pLib.GetInstance().server_status.Equals("bwo"))
            {
                showOutbandDialog();
                P2pLib.GetInstance().server_status = "ok";
                return;
            }
            SynchronizationContext syncContext = SynchronizationContext.Current;
            if (P2pLib.GetInstance().connectSuccess)
            {
                if (P2pLib.GetInstance().disConnectStarted)
                {
                    return;
                }
                P2pLib.GetInstance().disConnectStarted = true;

                Thread tmp_thread = new Thread(() =>
                {
                    controller.Stop();
                    controller.ToggleEnable(false);
                    P2pLib.GetInstance().connectSuccess = false;
                    P2pLib.GetInstance().connectStarted = false;
                    syncContext.Post(ConnectButton.ThreadRefresh, null);
                    P2pLib.GetInstance().disConnectStarted = false;
                });
                tmp_thread.IsBackground = true;
                tmp_thread.Start();
            }
            else
            {
                if (P2pLib.GetInstance().connectStarted)
                {
                    return;
                }

                P2pLib.GetInstance().connectStarted = true;
                P2pLib.GetInstance().use_smart_route_ = true;
                int country_idx = cboPlanets.SelectedIndex;
                Thread tmp_thread = new Thread(() =>
                {
                    P2pLib.GetInstance().choose_country_ = P2pLib.GetInstance().now_countries_[country_idx];
                    Configuration.RandomNode();
                    controller.ToggleEnable(true);
                    controller.ToggleGlobal(true);
                    P2pLib.GetInstance().connectSuccess = true;
                    P2pLib.GetInstance().connectStarted = false;
                    syncContext.Post(this.ConnectButton.ThreadRefresh, null);
                });
                tmp_thread.IsBackground = true;
                tmp_thread.Start();
            }
        }

        private void switch_WOC1_StateChanged(object sender, EventArgs e)
        {
            P2pLib.GetInstance().use_smart_route_ = true;
            if (!P2pLib.GetInstance().connectSuccess)
            {
                return;
            }

            if (P2pLib.GetInstance().connectStarted || P2pLib.GetInstance().disConnectStarted)
            {
                return;
            }

            P2pLib.GetInstance().connectSuccess = false;
            P2pLib.GetInstance().connectStarted = true;
            int country_idx = cboPlanets.SelectedIndex;
            SynchronizationContext syncContext = SynchronizationContext.Current;
            Thread tmp_thread = new Thread(() =>
            {
                P2pLib.GetInstance().choose_country_ = P2pLib.GetInstance().now_countries_[country_idx];
                Configuration.RandomNode();
                controller.ToggleEnable(true);
                controller.ToggleGlobal(true);
                P2pLib.GetInstance().connectSuccess = true;
                P2pLib.GetInstance().connectStarted = false;
                syncContext.Post(this.ConnectButton.ThreadRefresh, null);
            });
            tmp_thread.IsBackground = true;
            tmp_thread.Start();

        }

        private void cboPlanets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (P2pLib.GetInstance().connectStarted || P2pLib.GetInstance().disConnectStarted)
            {
                return;
            }

            int country_idx = cboPlanets.SelectedIndex;
            if (P2pLib.GetInstance().choose_country_ == P2pLib.GetInstance().now_countries_[country_idx])
            {
                return;
            }

            P2pLib.GetInstance().connectSuccess = false;
            P2pLib.GetInstance().connectStarted = true;
            P2pLib.GetInstance().use_smart_route_ = true;

            SynchronizationContext syncContext = SynchronizationContext.Current;
            Thread tmp_thread = new Thread(() =>
            {
                P2pLib.GetInstance().choose_country_ = P2pLib.GetInstance().now_countries_[country_idx];
                Configuration.RandomNode();
                controller.ToggleEnable(true);
                controller.ToggleGlobal(true);
                P2pLib.GetInstance().connectSuccess = true;
                P2pLib.GetInstance().connectStarted = false;
                syncContext.Post(this.ConnectButton.ThreadRefresh, null);
            });
            tmp_thread.IsBackground = true;
            tmp_thread.Start();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (proxyForm != null)
            {
                proxyForm.Activate();
            }
            else
            {
                proxyForm = new ProxyForm(controller);
                proxyForm.Show();
                proxyForm.Activate();
                proxyForm.FormClosed += tmp_proxyForm_FormClosed;
            }
        }

        void tmp_proxyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            proxyForm.Dispose();
            proxyForm = null;
            Utils.ReleaseMemory(true);
        }

        private void autoShowUpgrade()
        {
            string ver = P2pLib.GetInstance().GetLatestVer();
            if (ver.IsNullOrEmpty())
            {
                return;
            }

            bool has_windows = false;
            string[] ver_split = ver.Split(',');
            for (int i = 0; i < ver_split.Length; ++i)
            {
                string[] item = ver_split[i].Split(';');
                if (item.Length < 3)
                {
                    continue;
                }

                if (item[0].Equals("windows"))
                {

                    if (String.Compare(item[1], P2pLib.kCurrentVersion) <= 0)
                    {
                            return;
                    }
                    else
                    {
                        if (String.Compare(item[1], upgrade_shown_version) != 0)
                        {
                            upgrade_shown_version = item[1];
                            has_windows = true;
                        }
                    }
                }
            }

            if (!has_windows)
            {
                return;
            }

            if (upgrade_is_shown)
            {
                return;
            }

            upgrade_is_shown = true;
            if (upgradeForm != null)
            {
                upgradeForm.Activate();
            }
            else
            {
                upgradeForm = new VersionControl(ver);
                upgradeForm.Show();
                upgradeForm.Activate();
                upgradeForm.FormClosed += tmp_upgradeForm_FormClosed;
            }
        }

        private void checkServerInfo(bool show_message)
        {
            string ver = P2pLib.GetInstance().GetLatestVer();
            if (ver.IsNullOrEmpty())
            {
                if (show_message)
                {
                    MessageBox.Show(I18N.GetString("Already the latest version."));
                }

                return;
            }
           
            bool has_windows = false;
            string[] ver_split = ver.Split(',');
            for (int i = 0; i < ver_split.Length; ++i)
            {
                string[] item = ver_split[i].Split(';');
                if (item.Length < 3)
                {
                    continue;
                }

                if (item[0].Equals("windows"))
                {

                    if (String.Compare(item[1], P2pLib.kCurrentVersion) <= 0)
                    {
                        if (show_message)
                        {
                            MessageBox.Show(I18N.GetString("Already the latest version."));
                            return;
                        }
                    }
                    else
                    {
                        has_windows = true;
                    }
                }

                if (item[0].Equals("share_ip"))
                {
                    if (!item[1].IsNullOrEmpty())
                    {
                        if (item[1].StartsWith("http"))
                        {
                            P2pLib.GetInstance().share_ip_ = item[1];
                        }
                        else
                        {
                            P2pLib.GetInstance().share_ip_ = "http://" + item[1];
                        }
                    }
                }

                if (item[0].Equals("buy_ip"))
                {
                    if (!item[1].IsNullOrEmpty())
                    {
                        if (item[1].StartsWith("http"))
                        {
                            P2pLib.GetInstance().buy_tenon_ip_ = item[1];
                        }
                        else
                        {
                            P2pLib.GetInstance().buy_tenon_ip_ = "http://" + item[1];
                        }
                    }
                }

                if (item[0].Equals("er"))
                {
                    P2pLib.GetInstance().SetExRouting(item[1]);
                }
            }

            if (!has_windows)
            {
                if (show_message) {
                    MessageBox.Show(I18N.GetString("Already the latest version."));
                }
                return;
            }

            if (upgrade_is_shown)
            {
                return;
            }

            if (!show_message)
            {
                return;
            }
            upgrade_is_shown = true;
            if (upgradeForm != null)
            {
                upgradeForm.Activate();
            }
            else
            {
                upgradeForm = new VersionControl(ver);
                upgradeForm.Show();
                upgradeForm.Activate();
                upgradeForm.FormClosed += tmp_upgradeForm_FormClosed;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            checkServerInfo(true);
        }

        void showOutbandDialog()
        {
            if (outbandForm != null)
            {
                outbandForm.Activate();
            }
            else
            {
                outbandForm = new OutBandwidth();
                outbandForm.Show();
                outbandForm.Activate();
                outbandForm.FormClosed += hideOutbandDialog;
            }
        }

        void hideOutbandDialog(object sender, FormClosedEventArgs e)
        {
            outbandForm.Dispose();
            outbandForm = null;
            Utils.ReleaseMemory(true);
        }

        void tmp_upgradeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            upgradeForm.Dispose();
            upgradeForm = null;
            Utils.ReleaseMemory(true);
            upgrade_is_shown = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (proxyForm != null)
            {
                proxyForm.Activate();
            }
            else
            {
                proxyForm = new ProxyForm(controller);
                proxyForm.Show();
                proxyForm.Activate();
                proxyForm.FormClosed += tmp_proxyForm_FormClosed;
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            // goto brower
            System.Diagnostics.Process.Start(P2pLib.GetInstance().buy_tenon_ip_ + "/chongzhi/" + P2pLib.GetInstance().account_id_);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // share
            Clipboard.SetDataObject(P2pLib.GetInstance().share_ip_ + "?id=" + P2pLib.GetInstance().account_id_);
            MessageBox.Show(I18N.GetString("Copy sharing link succeeded."));
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {
            // share
            Clipboard.SetDataObject(P2pLib.GetInstance().share_ip_ + "?id=" + P2pLib.GetInstance().account_id_);
            MessageBox.Show(I18N.GetString("Copy sharing link succeeded."));
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/tenondvpn/tenonvpn-join");
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(P2pLib.GetInstance().buy_tenon_ip_ + "/chongzhi/" + P2pLib.GetInstance().account_id_);
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(P2pLib.GetInstance().buy_tenon_ip_ + "/chongzhi/" + P2pLib.GetInstance().account_id_);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/tenondvpn/tenonvpn-join");

        }
    }
}
