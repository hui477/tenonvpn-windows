using System;
using System.Runtime.InteropServices;
using Shadowsocks.Util;
using Shadowsocks.Controller;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace Shadowsocks.LipP2P {
    public class P2pLib {
        [DllImport("dll.dll", EntryPoint = "init_network", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr initNetwork(
                IntPtr local_ip,
                UInt16 local_port,
                IntPtr bootstrap,
                IntPtr path,
                IntPtr version,
                IntPtr prikey);
        [DllImport("dll.dll", EntryPoint = "get_socket", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getSocket();
        [DllImport("dll.dll", EntryPoint = "create_account", CallingConvention = CallingConvention.Cdecl)]
        public static extern void createAccount();
        [DllImport("dll.dll", EntryPoint = "get_vpn_nodes", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getNetworkNodes(IntPtr country, uint count, bool route);
        [DllImport("dll.dll", EntryPoint = "get_public_key", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getPublicKey();
        [DllImport("dll.dll", EntryPoint = "get_balance", CallingConvention = CallingConvention.Cdecl)]
        private static extern long getBalance();
        [DllImport("dll.dll", EntryPoint = "transactions", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Transactions(uint begin, uint len);
        [DllImport("dll.dll", EntryPoint = "check_version", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr checkVersion();
        [DllImport("dll.dll", EntryPoint = "reset_private_key", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr resetPrivateKey(IntPtr prikey);
        [DllImport("dll.dll", EntryPoint = "check_vip", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr checkVip();
        [DllImport("dll.dll", EntryPoint = "pay_for_vpn", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr payForVpn(IntPtr acc, IntPtr payfor_gid, long amount);
        [DllImport("dll.dll", EntryPoint = "get_ip_country", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getIpCountry(IntPtr ip);

        private static P2pLib uniqueInstance;
        public string prikey_ = "";
        public string pubkey_;
        public string account_id_;
        public string local_country_;
        public string choose_country_ = "US";
        public string str_vpn_ip_;
        public bool global_mode_ = false;

        public uint vpn_ip_ = 0;
        public ushort vpn_port_ = 0;
        public string seckey_;
        public bool use_smart_route_ = true;
        public string str_route_ip_;
        public uint route_ip_ = 0;
        public ushort route_port_ = 0;
        public string ex_str_route_ip_;
        public uint ex_route_ip_ = 0;
        public ushort ex_route_port_ = 0;

        public int socket_id_;
        public string enc_method_ = "aes-128-cfb";
        public Dictionary<string, string> default_routing_map_ = new Dictionary<string, string>();
        public Dictionary<string, string> ex_routing_map_ = new Dictionary<string, string>();
        public const string kCurrentVersion = "4.0.4";
        private string save_prikey_directory = "C://Users/Public/Documents/iedata/tvdata";
        private HashSet<string> now_prikeys = new HashSet<string>();
        public string share_ip_ = "https://www.tenonvpn.net";
        public string buy_tenon_ip_ = "https://www.tenonvpn.net";

        public long now_balance = -1;
        public List<string> payfor_vpn_accounts_list = new List<string>()
        {
            "dc161d9ab9cd5a031d6c5de29c26247b6fde6eb36ed3963c446c1a993a088262",
            "5595b040cdd20984a3ad3805e07bad73d7bf2c31e4dc4b0a34bc781f53c3dff7",
            "25530e0f5a561f759a8eb8c2aeba957303a8bb53a54da913ca25e6aa00d4c365",
            "9eb2f3bd5a78a1e7275142d2eaef31e90eae47908de356781c98771ef1a90cd2",
            "c110df93b305ce23057590229b5dd2f966620acd50ad155d213b4c9db83c1f36",
            "f64e0d4feebb5283e79a1dfee640a276420a08ce6a8fbef5572e616e24c2cf18",
            "7ff017f63dc70770fcfe7b336c979c7fc6164e9653f32879e55fcead90ddf13f",
            "6dce73798afdbaac6b94b79014b15dcc6806cb693cf403098d8819ac362fa237",
            "b5be6f0090e4f5d40458258ed9adf843324c0327145c48b55091f33673d2d5a4"
        };

        public long payfor_timestamp = 0;
        public long payfor_amount = 0;
        public long vip_left_days = -1;
        public long min_payfor_vpn_tenon = 66;
        public long max_payfor_vpn_tenon = 2000;
        private string payfor_gid = "";
        public string server_status = "ok";

        public bool connectStarted = false;
        public bool connectSuccess = true;
        public bool disConnectStarted = false;
        private string old_vpn_ip = "";
        private HashSet<string> local_web_sites_ = new HashSet<string>();
        private string web_sites_str_ = "192.168.190.128;localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;";
        private string init_web_sites_str_ = "192.168.190.128;localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;";

        public List<string> now_countries_ = new List<string>() {
            "US", "SG", "BR","DE", "NL", "FR","KR", "JP", "CA","AU","HK", "IN", "GB","CN"
        };

        public List<string> def_vpn_country = new List<string>() {
            "US", "FR", "IN", "AU", "DE"
        };

        private static readonly object locker = new object();

        private P2pLib() {
            if (!Directory.Exists(save_prikey_directory))
            {
                Directory.CreateDirectory(save_prikey_directory);
            }

            string prikeys = GetSavedPrikeys();
            string[] tmp_prikeys = prikeys.Split(',');
            for (int i = 0; i < tmp_prikeys.Length; ++i)
            {
                if (tmp_prikeys[i].Trim().Length == 64)
                {
                    if (prikey_.IsNullOrEmpty())
                    {
                        prikey_ = tmp_prikeys[i].Trim();
                    }
                    now_prikeys.Add(tmp_prikeys[i].Trim());
                }
            }
        }

        public long Balance()
        {
            return getBalance();
        }

        public void ServerStatusChange(string svr_status)
        {
            server_status = svr_status;
        }

        public long currentTimeMillis()
        {
            long currentTicks = DateTime.Now.Ticks;
            DateTime dtFrom = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long currentMillis = (currentTicks - dtFrom.Ticks) / 10000;
            return currentMillis;
        }

        public string GetIpCountry(string ip)
        {
            IntPtr tmp_ip = Marshal.StringToHGlobalAnsi(ip);
            IntPtr res_cty = getIpCountry(tmp_ip);
            string res = Marshal.PtrToStringAnsi(res_cty);
            return res;
        }

        public void PayforVpn()
        {
            long day_msec = 3600 * 1000 * 24;
            long days_timestamp = payfor_timestamp / day_msec;
            long cur_timestamp = currentTimeMillis();
            long days_cur = cur_timestamp / day_msec;
            long vip_days = payfor_amount / min_payfor_vpn_tenon;
            if (payfor_timestamp != long.MaxValue && days_timestamp + vip_days > days_cur)
            {
                payfor_gid = "";
                vip_left_days = (days_timestamp + vip_days - days_cur) + (now_balance / min_payfor_vpn_tenon);
                return;
            }
            else
            {
                PayforVipTrans();
            }

            if (!payfor_gid.IsNullOrEmpty())
            {
                CheckVIP();
            }
        }

        public long CheckVIP()
        {
            IntPtr ptr_res = checkVip();
            string res = Marshal.PtrToStringAnsi(ptr_res);
            string[] items = res.Split(',');
            if (items.Length != 2)
            {
                return long.MaxValue;
            }
            payfor_timestamp = long.Parse(items[0]);
            payfor_amount = long.Parse(items[1]);
            return payfor_timestamp;
        }

        private void PayforVipTrans()
        {
            Random random = new Random();
            int rand_num = random.Next(0, payfor_vpn_accounts_list.Count);
            string acc = payfor_vpn_accounts_list[rand_num];
            if (acc.IsNullOrEmpty())
            {
                return;
            }

            long days = now_balance / min_payfor_vpn_tenon;
            if (days > 30)
            {
                days = 30;
            }

            long amount = days * min_payfor_vpn_tenon;
            if (amount <= 0 || amount > now_balance)
            {
                return;
            }
            IntPtr tmp_acc = Marshal.StringToHGlobalAnsi(acc);
            IntPtr gid = Marshal.StringToHGlobalAnsi(payfor_gid);
            IntPtr res_gid = payForVpn(tmp_acc, gid, amount);
            payfor_gid = Marshal.PtrToStringAnsi(res_gid);
        }

        public bool ResetPrivateKey(string prikey)
        {
            IntPtr tmp_prikey = Marshal.StringToHGlobalAnsi(prikey);
            IntPtr ptr_res = resetPrivateKey(tmp_prikey);
            string res = Marshal.PtrToStringAnsi(ptr_res);
            string[] item_split = res.Split(',');
            if (item_split.Length != 2) {
                return false;
            }

            prikey_ = prikey;
            pubkey_ = item_split[0];
            account_id_ = item_split[1];
            return true;
        }

        public static P2pLib GetInstance() {
            if (uniqueInstance == null) {
                lock (locker) {
                    if (uniqueInstance == null) {
                        uniqueInstance = new P2pLib();
                    }
                }
            }
            return uniqueInstance;
        }

        string GetAddressIP()
        {
            ///获取本地的IP地址
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;
        }
        public int InitNetwork() {
            string path = Utils.GetTempPath();
            IntPtr ip = Marshal.StringToHGlobalAnsi("0.0.0.0");
            IntPtr bootstarp = Marshal.StringToHGlobalAnsi("id:113.17.169.103:9001,id:113.17.169.105:9001,id:113.17.169.106:9001,id:113.17.169.93:9001,id:113.17.169.94:9001,id:113.17.169.95:9001,id:45.77.1.236:9001,id:45.32.136.162:9001,id:45.32.136.162:9001,id:149.28.207.71:9001,id:104.238.180.53:9001,id:104.238.183.154:9001,id:149.28.199.47:9001,id:45.32.139.163:9001,");
            IntPtr conf_path = Marshal.StringToHGlobalAnsi(path);
            IntPtr version = Marshal.StringToHGlobalAnsi(kCurrentVersion);
            IntPtr prikey = Marshal.StringToHGlobalAnsi(prikey_);
            IntPtr ptrRet = initNetwork(
                    ip,
                    18993,
                    bootstarp,
                    conf_path,
                    version,
                    prikey);
            string str = Marshal.PtrToStringAnsi(ptrRet);

            if (str.Equals("ERROR"))
            {
                Logging.Error("init network failed!");
                return 1;
            }

            string[] res_arr = str.Split(',');
            if (res_arr.Length < 4) {
                Logging.Error("init network failed, res data invalid!");
                return 1;
            }

            local_country_ = res_arr[0];
            account_id_ = res_arr[1];
            if (!prikey_.Equals(res_arr[2]))
            {
                prikey_ = res_arr[2];
                SavePrivateKey(prikey_);
            }

            string[] def_routes = res_arr[3].Split(';');
            for (int i = 0; i < def_routes.Length; ++i)
            {
                string[] item = def_routes[i].Split(':');
                if (item.Length != 2)
                {
                    continue;
                }

                if (item[0].Length != 2 && item[1].Length != 2)
                {
                    continue;
                }

                default_routing_map_.Add(item[0], item[1]);
            }
            
            
            socket_id_ = getSocket();
            IntPtr pubkeyRet = getPublicKey();
            pubkey_ = Marshal.PtrToStringAnsi(pubkeyRet);
            Logging.Error($"local_country_: {local_country_}");
            Logging.Error($"account_id_: {account_id_}");
            Logging.Error($"prikey_: {prikey_}");
            Logging.Error($"socket_id_: {socket_id_}");
            createAccount();
            InitLocalSites(local_country_);

            Logging.Info("init network success!");
            Console.WriteLine("DDDDDDDDDDDDD init network success!");
            return 0;
        }

        public int GetRemoteNode(ref string ip, ref ushort port) {
            if (use_smart_route_) {
                int res = GetOneRouteNode(ref ip, ref port);
                return res;
            }
            ip = str_vpn_ip_;
            port = vpn_port_;
            return 0;
        }

        public int GetExRouteNode(ref string ip, ref ushort port) {
//             string key = local_country_ + choose_country_;
//             if (ex_routing_map_.ContainsKey(key))
//             {
//                 string ex_country = ex_routing_map_[key];
                int res = GetOneRouteNode("US", ref ip, ref port);
                
                if (res == 0)
                {
                    return 0;
                }
//             }

            return 1;
        }

        public void SetExRouting(string data) {
            ex_routing_map_.Clear();
            string[] data_split = data.Split('1');
            for (int i = 0; i < data_split.Length; ++i) {
                string[] tmp_split = data_split[i].Split('2');
                if (tmp_split.Length == 2) {
                    ex_routing_map_.Add(tmp_split[0], tmp_split[1]);
                }
            }
        }

        public int GetOneRouteNode(ref string ip, ref ushort port) {
            string def_route = local_country_;
            if (default_routing_map_.ContainsKey(local_country_))
            {
                def_route = default_routing_map_[local_country_];
            }

            int res = GetOneRouteNode(def_route, ref ip, ref port);
            if (res == 0) {
                return 0;
            }

            foreach (string country in def_vpn_country) {
                res = GetOneRouteNode(country, ref ip, ref port);
                if (res == 0) {
                    return 0;
                }
            }
            return 1;
        }

        public int ChooseOneVpnNode() {
            string ip = "";
            ushort port = 0;
            string passwd = "";
            int res = GetOneVpnNode(choose_country_, ref ip, ref port, ref passwd);
            if (res == 0) {
                for (int i = 0; i < 5; ++i) {
                    if (old_vpn_ip.Equals(ip)) {
                        break;
                    }

                    res = GetOneVpnNode(choose_country_, ref ip, ref port, ref passwd);
                }
                old_vpn_ip = ip;
                return res;
            }

            foreach (string country in def_vpn_country) {
                res = GetOneVpnNode(country, ref ip, ref port, ref passwd);
                if (res == 0) {
                    for (int i = 0; i < 5; ++i) {
                        if (old_vpn_ip.Equals(ip)) {
                            break;
                        }

                        res = GetOneVpnNode(choose_country_, ref ip, ref port, ref passwd);
                    }
                    old_vpn_ip = ip;
                    return 0;
                }
            }

            return 1;
        }

        public int GetOneRouteNode(string country, ref string ip, ref ushort port) {
            IntPtr tmp_country = Marshal.StringToHGlobalAnsi(country);
            IntPtr ptrRet = getNetworkNodes(tmp_country, 16, true);

            string str = Marshal.PtrToStringAnsi(ptrRet);
            if (str.Equals("ERROR")) {
                Logging.Error("get route network nodes failed!");
                return 1;
            }

            if (str.IsNullOrEmpty()) {
                Logging.Error("get route network nodes failed!");
                return 1;
            }

            if (str.Equals("ERROR")) {
                Logging.Error("get route network nodes failed!");
                return 1;
            }

            string[] node_arr = str.Split(',');
            if (node_arr.Length <= 0) {
                Logging.Error("get route network nodes failed!");
                return 1;
            }

            Random ran = new Random();
            int n = ran.Next(node_arr.Length);
            string[] node_info = (node_arr[n]).Split(':');
            if (node_info.Length < 5) {
                Logging.Error("get route network nodes failed!");
                return 1;
            }

            str_route_ip_ = node_info[0];
            route_ip_ = IpToInt(node_info[0]);
            route_port_ = ushort.Parse(node_info[2]);
            ip = node_info[0];
            port = route_port_;
            Logging.Debug($"route_ip_: {node_info[0]}, {route_ip_}");
            Logging.Debug($"route_port_: {route_port_}");
            return 0;
        }

        public int GetOneVpnNode(string country, ref string ip, ref ushort port, ref string passwd) {
            IntPtr tmp_country = Marshal.StringToHGlobalAnsi(country);
            IntPtr ptrRet = getNetworkNodes(tmp_country, 16, false);
            string str = Marshal.PtrToStringAnsi(ptrRet);
            if (str.IsNullOrEmpty()) {
                Logging.Error("get vpn network nodes failed! res empty.");
                return 1;
            }

            if (str.Equals("ERROR")) {
                Logging.Error("get vpn network nodes failed! res error.");
                return 1;
            }

            string[] node_arr = str.Split(',');
            if (node_arr.Length <= 0) {
                Logging.Error("get vpn network nodes failed! node empty.");
                return 1;
            }

            Random ran = new Random();
            int n = ran.Next(node_arr.Length);
            string[] node_info = (node_arr[n]).Split(':');
            if (node_info.Length < 5) {
                Logging.Error($"get vpn network nodes failed! node info error.{node_arr[n]}, {str}");
                return 1;
            }

            str_vpn_ip_ = node_info[0];
            vpn_ip_ = IpToInt(node_info[0]);
            vpn_port_ = ushort.Parse(node_info[1]);
            seckey_ = node_info[3];
            ip = node_info[0];
            port = vpn_port_;
            passwd = seckey_;
            Logging.Info($"vpn_ip_: {node_info[0]}, {vpn_ip_}");
            Logging.Info($"vpn_port_: {vpn_port_}");
            Logging.Info($"seckey_: {seckey_}");
            return 0;
        }

        public static uint IpToInt(string ip) {
            string[] items = ip.Split('.');
            return uint.Parse(items[3]) << 24
                    | uint.Parse(items[2]) << 16
                    | uint.Parse(items[1]) << 8
                    | uint.Parse(items[0]);
        }

        public string Trans()
        {
            IntPtr ptrRet = Transactions(0, 64);
            return Marshal.PtrToStringAnsi(ptrRet);
        }

        public string GetLatestVer()
        {
            IntPtr ptrRet = checkVersion();
            return Marshal.PtrToStringAnsi(ptrRet);
        }

        public bool SavePrivateKey(string prikey)
        {
            if (prikey.Trim().Length != 64)
            {
                return false;
            }
            
            if (now_prikeys.Contains(prikey))
            {
                now_prikeys.Remove(prikey);
            }

            if (now_prikeys.Count >= 3)
            {
                return false;
            }

            string content = prikey;
            foreach (string key in now_prikeys)
            {
                content += "," + key;
            }
            string file = save_prikey_directory + "/ie";
            if (File.Exists(file))
            {
                FileStream stream2 = File.Open(file, FileMode.OpenOrCreate, FileAccess.Write);
                stream2.Seek(0, SeekOrigin.Begin);
                stream2.SetLength(0);
                StreamWriter sw = new StreamWriter(stream2);
                sw.Write(content);
                sw.Flush();
                sw.Close();
                stream2.Close();
            }
            else
            {
                FileStream fs = new FileStream(file, FileMode.CreateNew);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(content);
                sw.Flush();
                sw.Close();
                fs.Close();
            }

            now_prikeys.Add(prikey);
            return true;
        }

        public string GetSavedPrikeys()
        {
            string file = save_prikey_directory + "/ie";
            string res = "";
            if (File.Exists(file))
            {
                StreamReader sr = new StreamReader(file, Encoding.UTF8);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    res += line;
                }
                
                sr.Close();
            }
            return res;
        }

        public void InitLocalSites(string now_local_country)
        {
            try
            {
                string tmp_global_file = Utils.GetTempPath() + "/global_mode";
                string global_mode = File.ReadAllText(tmp_global_file);
                global_mode_ = Convert.ToBoolean(global_mode);
                if (global_mode_)
                {
                    string arguments = $"global 127.0.0.1:1080 {init_web_sites_str_}";
                    Util.SystemProxy.Sysproxy.ExecSysproxy(arguments);
                    return;
                }

                string tmp_file = Utils.GetTempPath() + "/local_country";
                string old_local_country = File.ReadAllText(tmp_file);
                if (now_local_country != old_local_country)
                {
                    string arguments = $"global 127.0.0.1:1080 {web_sites_str_}";
                    Util.SystemProxy.Sysproxy.ExecSysproxy(arguments);
                    string site_tmp_file = Utils.GetTempPath() + "/pass_site";
                    File.WriteAllText(site_tmp_file, web_sites_str_);
                    string country_tmp_file = Utils.GetTempPath() + "/local_country";
                    File.WriteAllText(country_tmp_file, now_local_country);
                    return;
                }
            }
            catch (Exception e)
            {
                string arguments = $"global 127.0.0.1:1080 {web_sites_str_}";
                Util.SystemProxy.Sysproxy.ExecSysproxy(arguments);
                string ex_site_tmp_file = Utils.GetTempPath() + "/pass_site";
                File.WriteAllText(ex_site_tmp_file, web_sites_str_);
                return;
            }

            try
            {
                string tmp_file = Utils.GetTempPath() + "/pass_site";
                string tmp_str = File.ReadAllText(tmp_file);
                Logging.Info("tmp_file: " + tmp_file + ", get str: " + tmp_str + ", length: " + tmp_str.Length);
                if (tmp_str.Length < 10240 && tmp_str.Length > 0)
                {
                    web_sites_str_ = tmp_str;
                }

                string[] site_split = web_sites_str_.Split(';');
                for (int i = 0; i < site_split.Length; ++i)
                {
                    local_web_sites_.Add(site_split[i]);
                }
                string arguments = $"global 127.0.0.1:1080 {web_sites_str_}";
                Util.SystemProxy.Sysproxy.ExecSysproxy(arguments);
            }
            catch (Exception e)
            {
                Logging.Info("get error: " + e.ToString());
            }
        }

        public void SetGlobalMode(bool global_mode)
        {
            try
            {
                global_mode_ = global_mode;
                string tmp_global_file = Utils.GetTempPath() + "/global_mode";
                string tmp_val = Convert.ToString(global_mode);
                File.WriteAllText(tmp_global_file, tmp_val);
                if (global_mode)
                {
                    string cmd = $"global 127.0.0.1:1080 {init_web_sites_str_}";
                    Util.SystemProxy.Sysproxy.ExecSysproxy(cmd);
                }
            }
            catch (Exception e)
            {
                Logging.Info("get error: " + e.ToString());
            }
        }

        public void AddLocalSites(string site)
        {
            //             string[] site_split = site.Split('.');
            //             string tmp_site = "*." + site_split[site_split.Length - 2] + "." + site_split[site_split.Length - 1]; 
            try
            {
                if (local_web_sites_.Contains(site))
                {
                    return;
                }

                local_web_sites_.Add(site);
                web_sites_str_ += site + ";";
                if (web_sites_str_.Length >= 10240)
                {
                    web_sites_str_ = init_web_sites_str_ + ";" + site;
                    local_web_sites_.Clear();
                    local_web_sites_.Add(site);
                }
                string arguments = $"global 127.0.0.1:1080 {web_sites_str_}";
                Util.SystemProxy.Sysproxy.ExecSysproxy(arguments);
                string tmp_file = Utils.GetTempPath() + "/pass_site";
                File.WriteAllText(tmp_file, web_sites_str_);
            }
            catch (Exception e)
            {
                Logging.Info("get error: " + e.ToString());
            }
        }
    }
}
