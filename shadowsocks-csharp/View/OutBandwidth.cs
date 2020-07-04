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
using Shadowsocks.LipP2P;

namespace Shadowsocks.View
{
    public partial class OutBandwidth : Form
    {
        private string version;

        public OutBandwidth()
        {
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.Text = I18N.GetString("out of bandwidth");
        }

        private void OutBandwidth_Load(object sender, EventArgs e)
        {
            this.label1.Text = I18N.GetString("Free traffic used up, buy tenon or use tomorrow.");
            this.button2.Text = I18N.GetString("BUY");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(P2pLib.GetInstance().buy_tenon_ip_ + "/chongzhi/" + P2pLib.GetInstance().account_id_);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(P2pLib.GetInstance().buy_tenon_ip_ + "/chongzhi/" + P2pLib.GetInstance().account_id_);

        }
    }
}
