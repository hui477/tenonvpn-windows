using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.LipP2P;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class ProxyForm : Form
    {
        private ShadowsocksController controller;

        public ProxyForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;

            string trans_str = P2pLib.GetInstance().Trans();

            if (!trans_str.IsNullOrEmpty())
            {
                string[] items = trans_str.Split(';');     
                for (int i = 0; i < items.Length; ++i)
                {
                    string[] item = items[i].Split(',');
                    if (item.Length < 4)
                    {
                        continue;
                    }
                    int index = this.dataGridView1.Rows.Add();
                    this.dataGridView1.Rows[index].Cells[0].Value = item[0];
                    this.dataGridView1.Rows[index].Cells[1].Value = item[1];
                    this.dataGridView1.Rows[index].Cells[2].Value = (
                        item[2].Substring(0, 7).ToUpper() + "..." +
                        item[2].Substring(item[2].Length - 7, 7).ToUpper());
                    this.dataGridView1.Rows[index].Cells[3].Value = item[3];
                }
            }
            richTextBox2.Text = P2pLib.GetInstance().prikey_.ToUpper();
            richTextBox1.Text = P2pLib.GetInstance().account_id_.ToUpper();
            long balance = P2pLib.GetInstance().Balance();
            if (balance >= 0)
            {
                label4.Text = balance + " Tenon";
                label5.Text = Math.Round(balance * 0.002, 3) + "$";
            }
            else
            {
                label4.Text = "-- Tenon";
                label5.Text = "--$";
            }

            this.label2.Text = I18N.GetString("Account");
            this.label3.Text = I18N.GetString("Balance");
            this.label6.Text = I18N.GetString("Transactions");
            this.label7.Text = I18N.GetString("Global Mode");
            this.button1.Text = I18N.GetString("copy");
            this.button3.Text = I18N.GetString("copy");
            this.button2.Text = I18N.GetString("paste");
            this.checkBox1.Checked = P2pLib.GetInstance().global_mode_;
        }


        private void ProxyTypeLabel_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void textBox_WOC1_Load(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // prikey copy
            Clipboard.SetDataObject(P2pLib.GetInstance().prikey_);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // prikey paste
            IDataObject iData = Clipboard.GetDataObject();
            if (iData.GetDataPresent(DataFormats.Text))
            {
                string prikey = ((String)iData.GetData(DataFormats.Text)).Trim();
                if (prikey.Equals(P2pLib.GetInstance().prikey_))
                {
                    return;
                }

                if (prikey.Length != 64)
                {
                    MessageBox.Show(I18N.GetString("invalid private key."));
                    return;
                }

                if (!P2pLib.GetInstance().SavePrivateKey(prikey))
                {
                    MessageBox.Show(I18N.GetString("Set up to 3 private keys."));
                    return;
                }

                if (!P2pLib.GetInstance().ResetPrivateKey(prikey))
                {
                    MessageBox.Show(I18N.GetString("invalid private key."));
                    return;
                }

                MessageBox.Show(I18N.GetString("after success reset private key, must restart program."));
                Application.Exit();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // account copy
            Clipboard.SetDataObject(P2pLib.GetInstance().account_id_);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // buy
            System.Diagnostics.Process.Start(P2pLib.GetInstance().buy_tenon_ip_  + "/chongzhi/" + P2pLib.GetInstance().account_id_);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox tmpBox = sender as CheckBox;
            P2pLib.GetInstance().SetGlobalMode(tmpBox.Checked);
        }

        private void label7_Click(object sender, EventArgs e)
        {
        }
    }
}
