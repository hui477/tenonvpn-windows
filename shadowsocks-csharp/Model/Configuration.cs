using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Shadowsocks.Controller;
using Shadowsocks.LipP2P;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public string version;

        public List<Server> configs;

        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public bool isIPv6Enabled = false;
        public int localPort;
        public bool portableMode = true;
        public string pacUrl;
        public bool useOnlinePac;
        public bool secureLocalPac = true;
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public bool checkPreRelease;
        public bool isVerboseLogging;
        public LogViewerConfig logViewer;
        public ProxyConfig proxy;
        public HotkeyConfig hotkey;
        private static readonly object locker = new object();
        private static bool loaded = false;
        private static Configuration config_loaded;

        private static readonly string CONFIG_FILE = "gui-config.json";
        [JsonIgnore]
        public string localHost => GetLocalHost();
        private string GetLocalHost() {
            return isIPv6Enabled ? "[::1]" : "127.0.0.1";
        }
        public Server GetCurrentServer()
        {
            if (index >= 0 && index < configs.Count)
                return configs[index];
            else
                return GetDefaultServer();
        }

        public static void CheckServer(Server server)
        {
            CheckServer(server.server);
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckTimeout(server.timeout, Server.MaxServerTimeoutSec);
        }

        public static bool ChecksServer(Server server)
        {
            try
            {
                CheckServer(server);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void TraceInfo()
        {
            string info = null;
            StackTrace st = new StackTrace(true);
            StackFrame[] sf = st.GetFrames();
            for (int i = 0; i < sf.Length; ++i)
            {
                info = info + "\r\n" + " FileName=" + sf[i].GetFileName() + " fullname=" + sf[i].GetMethod().DeclaringType.FullName + " function=" + sf[i].GetMethod().Name + " FileLineNumber=" + sf[i].GetFileLineNumber();
            }
            Logging.Debug(info);
        }

        public static void RandomNode()
        {
            lock (locker)
            {
                if (!loaded)
                {
                    return;
                }
                try
                {
                    string route_ip = "";
                    ushort route_port = 0;
                    string def_route = P2pLib.GetInstance().local_country_;
                    if (P2pLib.GetInstance().default_routing_map_.ContainsKey(P2pLib.GetInstance().local_country_))
                    {
                        def_route = P2pLib.GetInstance().default_routing_map_[P2pLib.GetInstance().local_country_];
                    }

                    int res = P2pLib.GetInstance().GetOneRouteNode(def_route, ref route_ip, ref route_port);
                    if (res != 0)
                    {
                        res = P2pLib.GetInstance().GetOneRouteNode(P2pLib.GetInstance().choose_country_, ref route_ip, ref route_port);
                        if (res != 0)
                        {
                            foreach (var country in P2pLib.GetInstance().def_vpn_country)
                            {
                                res = P2pLib.GetInstance().GetOneRouteNode(country, ref route_ip, ref route_port);
                                if (res == 0)
                                {
                                    break;
                                }
                            }
                        }

                        if (res != 0)
                        {
                            MessageBox.Show("waiting to get route nodes from p2p network. try again...");
                            return;
                        }
                    }

                    string vpn_ip = "";
                    ushort vpn_port = 0;
                    string svr_passwd = "";
                    res = P2pLib.GetInstance().GetOneVpnNode(P2pLib.GetInstance().choose_country_, ref vpn_ip, ref vpn_port, ref svr_passwd);
                    if (res != 0)
                    {
                        if (res != 0)
                        {
                            foreach (var country in P2pLib.GetInstance().now_countries_)
                            {
                                res = P2pLib.GetInstance().GetOneVpnNode(country, ref vpn_ip, ref vpn_port, ref svr_passwd);
                                if (res == 0)
                                {
                                    break;
                                }
                            }
                        }

                        if (res != 0)
                        {
                            MessageBox.Show("waiting to get vpn nodes from p2p network. try again...");
                            return;
                        }
                    }

                    string ip = vpn_ip;
                    ushort port = vpn_port;
                    if (P2pLib.GetInstance().use_smart_route_)
                    {
                        ip = route_ip;
                        port = route_port;
                    }

                    Server server = new Server()
                    {
                        server = ip,
                        server_port = port,
                        password = svr_passwd,
                        method = P2pLib.GetInstance().enc_method_,
                        plugin = "",
                        plugin_opts = "",
                        plugin_args = "",
                        remarks = "",
                        timeout = 10,
                    };

                    config_loaded.configs.Clear();
                    config_loaded.configs.Add(server);
                    config_loaded.proxy.CheckConfig();
                    loaded = true;

                    P2pLib.GetInstance().ChooseOneVpnNode();
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                        Logging.LogUsefulException(e);
                }
            }
        }

        public static Configuration Load()
        {
            lock (locker) {
                if (loaded) {
                    return config_loaded;
                }
                try
                {
                    //                 string configContent = File.ReadAllText(CONFIG_FILE);
                    config_loaded = new Configuration(); //JsonConvert.DeserializeObject<Configuration>(configContent);
                    config_loaded.global = true;
                    config_loaded.enabled = true;
                    config_loaded.isDefault = true;

                    if (config_loaded.configs == null)
                        config_loaded.configs = new List<Server>();
    //                 if (config.configs.Count == 0)
    //                     config.configs.Add(GetDefaultServer());
                    if (config_loaded.localPort == 0)
                        config_loaded.localPort = 1080;
                    if (config_loaded.index == -1 && config_loaded.strategy == null)
                        config_loaded.index = 0;
                    if (config_loaded.logViewer == null)
                        config_loaded.logViewer = new LogViewerConfig();
                    if (config_loaded.proxy == null)
                        config_loaded.proxy = new ProxyConfig();
                    if (config_loaded.hotkey == null)
                        config_loaded.hotkey = new HotkeyConfig();
                    if (!System.Net.Sockets.Socket.OSSupportsIPv6) {
                        config_loaded.isIPv6Enabled = false; // disable IPv6 if os not support
                    }
                    //TODO if remote host(server) do not support IPv6 (or DNS resolve AAAA TYPE record) disable IPv6?

                    string route_ip = "";
                    ushort route_port = 0;
                    string def_route = P2pLib.GetInstance().local_country_;
                    if (P2pLib.GetInstance().default_routing_map_.ContainsKey(P2pLib.GetInstance().local_country_))
                    {
                        def_route = P2pLib.GetInstance().default_routing_map_[P2pLib.GetInstance().local_country_];
                    }

                    int res = P2pLib.GetInstance().GetOneRouteNode(def_route, ref route_ip, ref route_port);
                    if (res != 0) {
                        res = P2pLib.GetInstance().GetOneRouteNode(P2pLib.GetInstance().choose_country_, ref route_ip, ref route_port);
                        if (res != 0)
                        {
                            foreach (var country in P2pLib.GetInstance().now_countries_) {
                                res = P2pLib.GetInstance().GetOneRouteNode(country, ref route_ip, ref route_port);
                                if (res == 0) {
                                    break;
                                }
                            }
                        }

                        if (res != 0) {
                            MessageBox.Show("waiting to get route nodes from p2p network. try again...");
                            return config_loaded;
                        }
                    }

                    string vpn_ip = "";
                    ushort vpn_port = 0;
                    string svr_passwd = "";
                    res = P2pLib.GetInstance().GetOneVpnNode(P2pLib.GetInstance().choose_country_, ref vpn_ip, ref vpn_port, ref svr_passwd);
                    if (res != 0) {
                        if (res != 0) {
                            foreach(var country in P2pLib.GetInstance().now_countries_) {
                                res = P2pLib.GetInstance().GetOneVpnNode(country, ref vpn_ip, ref vpn_port, ref svr_passwd);
                                if (res == 0) {
                                    break;
                                }
                            }
                        }

                        if (res != 0) {
                            MessageBox.Show("waiting to get vpn nodes from p2p network. try again...");
                            return config_loaded;
                        }
                    }

                    string ip = vpn_ip;
                    ushort port = vpn_port;
                    if (P2pLib.GetInstance().use_smart_route_) {
                        ip = route_ip;
                        port = route_port;
                    }

                    Server server = new Server() {
                        server = ip,
                        server_port = port,
                        password = svr_passwd,
                        method = P2pLib.GetInstance().enc_method_,
                        plugin = "",
                        plugin_opts = "",
                        plugin_args = "",
                        remarks = "",
                        timeout = 10,
                    };

                    config_loaded.configs.Add(server);
                    config_loaded.proxy.CheckConfig();
                    loaded = true;
                    return config_loaded;
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                        Logging.LogUsefulException(e);
                    return null;
                }
            }
        }

        public static void Save(Configuration config)
        {
            config.version = P2pLib.kCurrentVersion;
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            if (config.index < -1)
                config.index = -1;
            if (config.index == -1 && config.strategy == null)
                config.index = 0;
            config.isDefault = false;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static Server AddDefaultServerOrServer(Configuration config, Server server = null, int? index = null)
        {
            if (config != null && config.configs != null)
            {
                server = (server ?? GetDefaultServer());

                config.configs.Insert(index.GetValueOrDefault(config.configs.Count), server);

                //if (index.HasValue)
                //    config.configs.Insert(index.Value, server);
                //else
                //    config.configs.Add(server);
            }
            return server;
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception(I18N.GetString("assertion failure"));
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException(I18N.GetString("Port out of range"));
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
                throw new ArgumentException(I18N.GetString("Port can't be 8123"));
        }

        private static void CheckPassword(string password)
        {
            if (password.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
        }

        public static void CheckServer(string server)
        {
            if (server.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
        }

        public static void CheckTimeout(int timeout, int maxTimeout)
        {
            if (timeout <= 0 || timeout > maxTimeout)
                throw new ArgumentException(
                    I18N.GetString("Timeout is invalid, it should not exceed {0}", maxTimeout));
        }

        public static void CheckProxyAuthUser(string user)
        {
            if (user.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Auth user can not be blank"));
        }

        public static void CheckProxyAuthPwd(string pwd)
        {
            if (pwd.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Auth pwd can not be blank"));
        }
    }
}
