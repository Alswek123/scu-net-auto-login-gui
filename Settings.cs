using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Scunet {
    // Keep the original JSON object so other users, remarks and future keys survive a save.
    internal sealed class JsonFile {
        readonly string path;
        string original;
        internal readonly Dictionary<string,object> Data;
        static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        internal JsonFile(string path) {
            this.path = path;
            original = Read();
            if (original == null) { Data = new Dictionary<string,object>(); return; }
            try {
                Data = Serializer.DeserializeObject(original) as Dictionary<string,object>;
                if (Data == null) throw new FormatException();
            } catch (Exception) {
                throw new InvalidDataException("无法读取 " + Path.GetFileName(path) + "。请检查 JSON 格式后重新打开。");
            }
        }
        string Read() { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null; }
        internal bool Exists { get { return original != null; } }
        internal void Check() {
            if (Read() != original) throw new IOException(Path.GetFileName(path) + " 已被其他程序修改，请重新打开登录器。");
        }
        internal void Save() {
            Check();
            string next = Serializer.Serialize(Data);
            if (next == original) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllText(temp, next, new UTF8Encoding(false));
                if (original == null) File.Move(temp, path);
                else File.Replace(temp, path, null);
                original = next;
            } finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        internal static object Get(Dictionary<string,object> obj, string key, object fallback) {
            string actual = obj.Keys.FirstOrDefault(k => String.Equals(k,key,StringComparison.OrdinalIgnoreCase));
            return actual == null ? fallback : obj[actual];
        }
        internal static void Put(Dictionary<string,object> obj, string key, object value) {
            string actual = obj.Keys.FirstOrDefault(k => String.Equals(k,key,StringComparison.OrdinalIgnoreCase));
            obj[actual ?? key] = value;
        }
    }

    internal sealed class Settings {
        internal const string ModernExe = "Sal.CLI.exe", LegacyExe = "scu_net_auto_login.exe";
        internal readonly string Exe, Dir;
        internal readonly bool Modern;
        internal readonly bool Imported;
        readonly JsonFile config, tool;
        readonly Dictionary<string,object> user;
        internal string Username, Password, Service, CookieEai, CookieKey, Core, Commands;
        internal int Retry, StartupDelay, Port;
        internal decimal Wait, Interval;
        internal bool Wifi, Hotspot, Reconnect, Proxy;

        internal Settings(string executable) {
            Exe = Path.GetFullPath(executable);
            string name = Path.GetFileName(Exe);
            Modern = String.Equals(name, ModernExe, StringComparison.OrdinalIgnoreCase);
            if ((!Modern && !String.Equals(name, LegacyExe, StringComparison.OrdinalIgnoreCase)) || !File.Exists(Exe))
                throw new FileNotFoundException("请选择 Sal.CLI.exe 或 scu_net_auto_login.exe。");
            Dir = Path.GetDirectoryName(Exe);
            config = new JsonFile(Path.Combine(Dir, Modern ? @"config\config.json" : @"Config\setting.json"));
            tool = new JsonFile(Path.Combine(Dir, @"Config\scunet_config_tool.json"));
            if (Modern && !config.Exists && File.Exists(Path.Combine(Dir,@"Config\setting.json"))) {
                ImportLegacy(new JsonFile(Path.Combine(Dir,@"Config\setting.json")).Data);
                Imported=true;
            }
            string listKey = Modern ? "UserList" : "userList";
            object[] users = JsonFile.Get(config.Data,listKey,new object[0]) as object[];
            if (users == null) throw new InvalidDataException("配置中的账号列表格式有误。");
            if (users.Length == 0) {
                user = new Dictionary<string,object>();
                JsonFile.Put(config.Data,listKey,new object[]{user});
            } else {
                user = users[0] as Dictionary<string,object>;
                if (user == null) throw new InvalidDataException("第一个账号的配置格式有误。");
            }
            Username = Convert.ToString(JsonFile.Get(user,"username",""));
            Password = Convert.ToString(JsonFile.Get(user,"password",""));
            Service = Convert.ToString(JsonFile.Get(user,"service","EDUNET"));
            int factor = Modern ? 1000 : 1;
            Wait = Number(Modern ? "InitialDelayMs" : "initialDelay",Modern ? 0 : 5) / (decimal)factor;
            Interval = Number(Modern ? "RetryDelayMs" : "retryDelay",5*factor) / (decimal)factor;
            Retry = Number("retryCount",1);
            StartupDelay = Convert.ToInt32(JsonFile.Get(tool.Data,"startupDelaySeconds",-1));
            Wifi = Flag(Modern ? "ConnectSCUNETWifi" : "enableConnectSCUNETWifi", false);
            Hotspot = Flag("enableHotspot",false);
            if (!Modern) {
                Reconnect = Flag("enableAutoTick",false);
                Proxy = Flag("bypassCampusNetworkSocks5Proxy",true);
                Core = StringValue("bypassCampusNetworkCore");
                Port = Number("bypassCampusNetworkCorePort",0);
                object[] commands = JsonFile.Get(config.Data,"bypassCampusNetworkCoreCommand",new object[0]) as object[];
                if (commands == null) throw new InvalidDataException("旧版核心命令列表格式有误。");
                Commands = String.Join(Environment.NewLine, commands.Select(Convert.ToString));
                CookieEai = StringValue("cookieEaiSess");
                CookieKey = StringValue("cookieUUkey");
            }
        }
        int Number(string key, int fallback) { return Convert.ToInt32(JsonFile.Get(config.Data,key,fallback),CultureInfo.InvariantCulture); }
        bool Flag(string key,bool fallback) { return Convert.ToBoolean(JsonFile.Get(config.Data,key,fallback)); }
        string StringValue(string key) { return Convert.ToString(JsonFile.Get(config.Data,key,"")); }
        void Set(string key,object value) { JsonFile.Put(config.Data,key,value); }

        void ImportLegacy(Dictionary<string,object> old) {
            var accounts=JsonFile.Get(old,"userList",new object[0]) as object[];
            if(accounts==null)throw new InvalidDataException("旧配置中的账号列表格式有误。");
            foreach(object item in accounts) {
                var acc=item as Dictionary<string,object>;
                if(acc==null)throw new InvalidDataException("旧配置中的账号格式有误。");
                string service=Convert.ToString(JsonFile.Get(acc,"service","EDUNET"));
                JsonFile.Put(acc,"service",service.ToLowerInvariant());
            }
            Set("UserList",accounts);
            Set("InitialDelayMs",checked(Convert.ToInt32(JsonFile.Get(old,"initialDelay",5))*1000));
            Set("RetryDelayMs",checked(Convert.ToInt32(JsonFile.Get(old,"retryDelay",5))*1000));
            Set("RetryCount",JsonFile.Get(old,"retryCount",1));
            Set("EnableHotspot",JsonFile.Get(old,"enableHotspot",false));
            Set("ConnectSCUNETWifi",JsonFile.Get(old,"enableConnectSCUNETWifi",false));
        }

        internal void Save() {
            if (!File.Exists(Exe)) throw new FileNotFoundException("登录器已移动，请重新选择文件。");
            string folder = Path.Combine(Dir,"Config");
            Directory.CreateDirectory(folder);
            // Serialize simultaneous saves by two instances of this editor.
            using (new FileStream(Path.Combine(folder,"scunet_config_tool.lock"),FileMode.OpenOrCreate,
                                  FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose)) {
                config.Check(); tool.Check();
                JsonFile.Put(user,"username",Username.Trim());
                JsonFile.Put(user,"password",Password);
                JsonFile.Put(user,"service",Service);
                int factor = Modern ? 1000 : 1;
                Set(Modern ? "InitialDelayMs" : "initialDelay",Decimal.ToInt32(Wait*factor));
                Set(Modern ? "RetryDelayMs" : "retryDelay",Decimal.ToInt32(Interval*factor));
                Set(Modern ? "RetryCount" : "retryCount",Retry);
                Set(Modern ? "EnableHotspot" : "enableHotspot",Hotspot);
                Set(Modern ? "ConnectSCUNETWifi" : "enableConnectSCUNETWifi",Wifi);
                if (!Modern) {
                    Set("enableAutoTick",Reconnect); Set("bypassCampusNetworkSocks5Proxy",Proxy);
                    Set("bypassCampusNetworkCore",Core); Set("bypassCampusNetworkCorePort",Port);
                    Set("bypassCampusNetworkCoreCommand",Commands.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)
                        .Select(s=>s.Trim()).Where(s=>s.Length>0).ToArray());
                    Set("cookieEaiSess",CookieEai); Set("cookieUUkey",CookieKey);
                }
                JsonFile.Put(tool.Data,"startupDelaySeconds",StartupDelay);
                config.Save(); tool.Save();
            }
        }
    }
}
