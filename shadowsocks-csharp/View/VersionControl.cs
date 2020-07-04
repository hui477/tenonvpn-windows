using ZXing.QrCode.Internal;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class VersionControl : Form
    {
        private string version;

        public VersionControl(string ver)
        {
            this.version = ver;
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.Text = I18N.GetString("About Upgrade");
        }

        private void VersionControl_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string down_url = "";
            string[] ver_split = version.Split(',');
            for (int i = 0; i < ver_split.Length; ++i)
            {
                string[] item = ver_split[i].Split(';');
                if (item.Length < 3)
                {
                    continue;
                }

                if (item[0].Equals("windows"))
                {
                    down_url = item[2];
                    break;
                }
            }

            if (down_url.IsNullOrEmpty())
            {
                MessageBox.Show($"invalid download url.{down_url}");
                return;
            }

            System.Diagnostics.Process.Start(down_url);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
