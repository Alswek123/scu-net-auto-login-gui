using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Xml;

namespace Scunet {
    internal sealed class StartupTask {
        internal const string DefaultName = "SCUNET Auto Login";
        readonly string name;
        readonly dynamic scheduler, folder;
        internal StartupTask(string name = DefaultName) {
            this.name = name;
            scheduler = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service",true));
            scheduler.Connect();
            folder = scheduler.GetFolder(@"\");
        }
        dynamic Find() {
            try { return folder.GetTask(name); }
            catch (COMException e) { if ((uint)e.ErrorCode == 0x80070002u) return null; throw; }
            catch (FileNotFoundException) { return null; }
        }
        internal string Status(out int delay,string selectedExe=null) {
            delay=10;
            dynamic task=Find();
            if(task==null) return "未启用";
            foreach(dynamic trigger in task.Definition.Triggers)
                if((int)trigger.Type==9) {
                    string iso=(string)trigger.Delay;
                    delay=String.IsNullOrEmpty(iso) ? 0 : (int)XmlConvert.ToTimeSpan(iso).TotalSeconds;
                    break;
                }
            if (!(bool)task.Enabled) return "已禁用";
            if(selectedExe!=null) {
                dynamic actions=task.Definition.Actions;
                if((int)actions.Count!=1 || (int)actions.Item(1).Type!=0 ||
                   !String.Equals(((string)actions.Item(1).Path).Trim('"'),selectedExe,StringComparison.OrdinalIgnoreCase))
                    return "已启用（指向其他登录器）";
            }
            return "已启用";
        }
        internal void Disable() { if(Find()!=null) folder.DeleteTask(name,0); }
        internal void Enable(string exe,int seconds,string sid) {
            string file=Path.GetFileName(exe);
            if (!File.Exists(exe) || !(String.Equals(file,Settings.ModernExe,StringComparison.OrdinalIgnoreCase) ||
                String.Equals(file,Settings.LegacyExe,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("登录器路径无效。");
            if(seconds<0 || seconds>86400) throw new ArgumentOutOfRangeException("seconds");
            var account=new SecurityIdentifier(sid);
            dynamic task=scheduler.NewTask(0);
            task.RegistrationInfo.Description="登录 Windows 后启动 SCUNET 校园网登录器。";
            task.Principal.UserId=account.Value;
            task.Principal.LogonType=3; // InteractiveToken
            task.Principal.RunLevel=0;
            task.Settings.StartWhenAvailable=true;
            task.Settings.DisallowStartIfOnBatteries=false;
            task.Settings.StopIfGoingOnBatteries=false;
            task.Settings.MultipleInstances=2; // IgnoreNew
            dynamic trigger=task.Triggers.Create(9); // Logon
            trigger.UserId=account.Value;
            trigger.Delay=XmlConvert.ToString(TimeSpan.FromSeconds(seconds));
            dynamic action=task.Actions.Create(0); // Exec
            action.Path=exe; action.WorkingDirectory=Path.GetDirectoryName(exe);
            folder.RegisterTaskDefinition(name,task,6,account.Value,null,3,null);
        }
    }
}
