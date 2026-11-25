using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace Scunet {
    internal sealed class Editor : Form {
        readonly Color accent=Color.FromArgb(22,86,130), muted=Color.FromArgb(75,88,100);
        readonly TabControl tabs=new TabControl();
        readonly TabPage account=new TabPage("账号与网络"), timing=new TabPage("登录与自启"), advanced=new TabPage("旧版高级设置");
        readonly TextBox path=Input(), username=Input(), password=Input(), core=Input(), commands=Input(), cookieA=Input(), cookieB=Input();
        readonly ComboBox service=new ComboBox();
        readonly NumericUpDown wait=Number(), interval=Number(), retry=Number(), delay=Number(), port=Number();
        readonly CheckBox wifi=Check("登录前连接 SCUNET Wi-Fi"), hotspot=Check("登录成功后开启热点"),
                          reconnect=Check("断线后持续重试"), proxy=Check("启用上游 SOCKS5 代理设置"), reveal=Check("显示密码");
        readonly Label status=new Label(), taskStatus=new Label();
        readonly Button save=new Button(), launch=new Button();
        readonly string remembered=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SCUNET Config Tool","login-path.txt");
        readonly string[] networks={"校园网","中国电信","中国移动","中国联通"};
        readonly string[] codes={"EDUNET","CHINATELECOM","CHINAMOBILE","CHINAUNICOM"};
        Settings model;
        bool loading, dirty;

        internal Editor() {
            Text="SCUNET 配置器"; Font=new Font("Microsoft YaHei UI",9F);
            AutoScaleMode=AutoScaleMode.Dpi; StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(650,560); MinimumSize=new Size(540,480);
            BackColor=Color.White; Padding=new Padding(18,14,18,12);
            var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Margin=Padding.Empty};
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int h in new[]{52,54,0,32,48}) shell.RowStyles.Add(new RowStyle(h==0?SizeType.Percent:SizeType.Absolute,h==0?100:h));
            Controls.Add(shell);
            var heading=new Label{Text="SCUNET 配置器",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,
                Font=new Font(Font.FontFamily,19F,FontStyle.Bold),ForeColor=accent};
            shell.Controls.Add(heading,0,0);
            var picker=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=1,ColumnCount=3,Margin=Padding.Empty};
            picker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            picker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); picker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            picker.Controls.Add(Caption("登录器"),0,0); path.ReadOnly=true; path.TabStop=false; path.AccessibleName="当前登录器";
            picker.Controls.Add(path,1,0); picker.Controls.Add(Action("选择文件…",Browse),2,0);
            shell.Controls.Add(picker,0,1); tabs.Dock=DockStyle.Fill;
            shell.Controls.Add(tabs,0,2); tabs.TabPages.Add(account); tabs.TabPages.Add(timing);

            var a=Page(account);
            Row(a,"账号",username); password.UseSystemPasswordChar=true; Row(a,"密码",password);
            Row(a,"",reveal); reveal.CheckedChanged+=(s,e)=>password.UseSystemPasswordChar=!reveal.Checked;
            service.DropDownStyle=ComboBoxStyle.DropDownList; service.Anchor=AnchorStyles.Left|AnchorStyles.Right;
            Row(a,"运营商",service); Row(a,"网络",wifi); Row(a,"",hotspot); Row(a,"",reconnect);

            var t=Page(timing);
            Row(t,"登录前等待（秒）",wait); Row(t,"重试次数",retry); Row(t,"重试间隔（秒）",interval);
            Row(t,"登录 Windows 后延迟（秒）",delay);
            taskStatus.AutoSize=true; taskStatus.ForeColor=muted;
            Row(t,"自启状态",taskStatus);
            var acts=new FlowLayoutPanel{AutoSize=true,WrapContents=true,Dock=DockStyle.Fill,Margin=Padding.Empty};
            acts.Controls.Add(Action("启用 / 更新",()=>ChangeTask(true)));
            acts.Controls.Add(Action("取消自启",()=>ChangeTask(false)));
            Row(t,"",acts);
            var g=Page(advanced); Row(g,"代理",proxy); Row(g,"核心名称",core); Row(g,"核心端口",port);
            commands.Multiline=true; commands.ScrollBars=ScrollBars.Vertical; commands.Height=55; commands.AcceptsReturn=true;
            Row(g,"核心命令（每行一条）",commands);
            cookieA.UseSystemPasswordChar=cookieB.UseSystemPasswordChar=true;
            Row(g,"Cookie EaiSess",cookieA); Row(g,"Cookie UUkey",cookieB);

            status.Dock=DockStyle.Fill; status.TextAlign=ContentAlignment.MiddleLeft; status.AutoEllipsis=true; status.AccessibleRole=AccessibleRole.StatusBar;
            shell.Controls.Add(status,0,3);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};
            Setup(save,"保存配置",()=>SaveForm()); save.BackColor=accent; save.ForeColor=Color.White;
            save.FlatStyle=FlatStyle.Flat; save.FlatAppearance.BorderSize=0;
            Setup(launch,"测试登录",TestLogin); footer.Controls.Add(save); footer.Controls.Add(launch); shell.Controls.Add(footer,0,4);
            AcceptButton=save;
            tabs.Enabled=save.Enabled=launch.Enabled=false;
            foreach(var f in new[]{username,password,core,commands,cookieA,cookieB}) f.TextChanged+=(s,e)=>Changed();
            foreach(var n in new[]{wait,interval,retry,delay,port}) n.ValueChanged+=(s,e)=>Changed();
            foreach(var box in new[]{wifi,hotspot,reconnect,proxy}) box.CheckedChanged+=(s,e)=>Changed();
            service.SelectedIndexChanged+=(s,e)=>Changed();
            FormClosing+=(s,e)=>{if(!CanDiscard())e.Cancel=true;};
            string file=RememberedOrAdjacent();
            if(file!=null) OpenFile(file);
            else { tabs.Enabled=save.Enabled=launch.Enabled=false; Say("请选择校园网登录器，或把配置器放到登录器旁边。"); }
        }
        static TextBox Input() { return new TextBox{Anchor=AnchorStyles.Left|AnchorStyles.Right,Margin=new Padding(3,4,6,4)}; }
        static NumericUpDown Number() { return new NumericUpDown{Width=140,Anchor=AnchorStyles.Left}; }
        static CheckBox Check(string text) { return new CheckBox{Text=text,AutoSize=true,Anchor=AnchorStyles.Left}; }
        static Label Caption(string text) { return new Label{Text=text,AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(3,4,14,4)}; }
        static void Setup(Button b,string text,Action click) {
            b.Text=text; b.AutoSize=true; b.MinimumSize=new Size(91,32); b.Anchor=AnchorStyles.Left;
            b.Click+=(s,e)=>click();
        }
        static Button Action(string text,Action click) { var b=new Button(); Setup(b,text,click); return b; }
        static TableLayoutPanel Page(TabPage p) {
            p.BackColor=Color.White; p.AutoScroll=true; p.Padding=new Padding(12,10,12,10);
            var grid=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,RowCount=0};
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            p.Controls.Add(grid); return grid;
        }
        static void Row(TableLayoutPanel grid,string name,Control c) {
            int r=grid.RowCount++; grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var label=Caption(name); grid.Controls.Add(label,0,r); c.Margin=new Padding(3,6,3,6); grid.Controls.Add(c,1,r);
            if(name.Length>0 && !(c is CheckBox)) c.AccessibleName=name;
        }
        void Say(string text,bool error=false) { status.Text=text; status.ForeColor=error?Color.Firebrick:muted; }
        void Changed() { if(!loading && model!=null) {dirty=true; Say("有未保存的修改");} }
        bool CanDiscard() { return !dirty || MessageBox.Show(this,"放弃尚未保存的修改？","SCUNET 配置器",
            MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes; }
        string RememberedOrAdjacent() {
            string local=AppDomain.CurrentDomain.BaseDirectory;
            string saved=null;
            try {if(File.Exists(remembered))saved=File.ReadAllText(remembered);}catch(IOException){}catch(UnauthorizedAccessException){}
            if(File.Exists(saved) && String.Equals(Path.GetDirectoryName(saved),local.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))return saved;
            foreach(var name in new[]{Settings.ModernExe,Settings.LegacyExe}) {
                var file=Path.Combine(local,name); if(File.Exists(file)) return file;
            }
            return File.Exists(saved)?saved:null;
        }
        void Browse() {
            using(var d=new OpenFileDialog{Title="选择校园网登录器",Filter="校园网登录器|Sal.CLI.exe;scu_net_auto_login.exe",CheckFileExists=true}) {
                if(model!=null)d.InitialDirectory=model.Dir;
                if(d.ShowDialog(this)!=DialogResult.OK || !CanDiscard() || !OpenFile(d.FileName))return;
                try {Directory.CreateDirectory(Path.GetDirectoryName(remembered)); File.WriteAllText(remembered,model.Exe);}
                catch(IOException){Say("已打开；无法记住本次目录。");}catch(UnauthorizedAccessException){Say("已打开；无法记住本次目录。");}
            }
        }
        static void Range(NumericUpDown n,decimal v,decimal min,decimal max,int places=0) {
            n.Minimum=Math.Min(min,v); n.Maximum=Math.Max(max,v); n.DecimalPlaces=places; n.Value=v;
        }
        internal bool OpenFile(string file) {
            try {
                var next=new Settings(file);
                loading=true; model=next; path.Text=model.Exe;
                int taskDelay=RefreshTask();
                if(next.StartupDelay<0)next.StartupDelay=taskDelay;
                username.Text=model.Username; password.Text=model.Password; reveal.Checked=false;
                service.Items.Clear(); service.Items.AddRange(networks);
                int i=Array.FindIndex(codes,s=>String.Equals(s,model.Service,StringComparison.OrdinalIgnoreCase));
                if(i<0){i=service.Items.Add(model.Service);} service.SelectedIndex=i;
                wifi.Checked=model.Wifi; hotspot.Checked=model.Hotspot; reconnect.Checked=model.Reconnect;
                reconnect.Visible=!model.Modern;
                if(model.Modern) tabs.TabPages.Remove(advanced); else if(!tabs.TabPages.Contains(advanced))tabs.TabPages.Add(advanced);
                Range(wait,model.Wait,0,model.Modern?2147483:300,model.Modern?3:0);
                Range(interval,model.Interval,0,model.Modern?2147483:120,model.Modern?3:0);
                Range(retry,model.Retry,model.Modern?-1:1,model.Modern?Int32.MaxValue:10); Range(delay,model.StartupDelay,0,86400);
                proxy.Checked=model.Proxy; core.Text=model.Core; Range(port,model.Port,0,65535); commands.Text=model.Commands;
                cookieA.Text=model.CookieEai; cookieB.Text=model.CookieKey; dirty=false;
                tabs.Enabled=save.Enabled=launch.Enabled=true; Say(model.Modern?"当前配置：config\\config.json":"当前配置：Config\\setting.json（旧版登录器）");
                if(model.Imported)Say("已载入旧配置的账号和网络设置，保存后写入新版配置。");
                return true;
            } catch(Exception e){Say(e.Message,true);return false;} finally {loading=false;}
        }
        int RefreshTask() {
            try { int sec; taskStatus.Text=new StartupTask().Status(out sec,model==null?null:model.Exe); return sec; }
            catch(Exception){taskStatus.Text="无法读取计划任务";return 10;}
        }
        internal bool SaveForm() {
            if(model==null)return false;
            try {
                model.Username=username.Text; model.Password=password.Text;
                int i=service.SelectedIndex;
                if(i>=0 && i<codes.Length)model.Service=model.Modern?codes[i].ToLowerInvariant():codes[i];
                model.Wifi=wifi.Checked; model.Hotspot=hotspot.Checked; model.Reconnect=reconnect.Checked;
                model.Wait=wait.Value; model.Interval=interval.Value; model.Retry=(int)retry.Value; model.StartupDelay=(int)delay.Value;
                model.Proxy=proxy.Checked; model.Core=core.Text; model.Port=(int)port.Value;
                model.Commands=commands.Text; model.CookieEai=cookieA.Text; model.CookieKey=cookieB.Text;
                model.Save(); dirty=false; Say("配置已保存"); return true;
            } catch(Exception e){Say(e.Message,true);return false;}
        }
        void TestLogin() {
            if(!SaveForm())return;
            try {
                using(Process p=Process.Start(new ProcessStartInfo(model.Exe){WorkingDirectory=model.Dir,UseShellExecute=false})) {}
                Say("已启动登录器，请在登录器窗口查看结果。");
            } catch(Exception e){Say(e.Message,true);}
        }
        void ChangeTask(bool enable) {
            if(enable && !SaveForm())return;
            try {
                string sid=WindowsIdentity.GetCurrent().User.Value;
                try {
                    var task=new StartupTask();
                    if(enable)task.Enable(model.Exe,(int)delay.Value,sid); else task.Disable();
                } catch(Exception e) {
                    if(e.HResult!=unchecked((int)0x80070005))throw;
                    string args=enable?"--startup-on \""+model.Exe+"\" "+(int)delay.Value+" "+sid:"--startup-off";
                    using(Process helper=Process.Start(new ProcessStartInfo(Application.ExecutablePath,args){UseShellExecute=true,Verb="runas"})) {
                        helper.WaitForExit(); if(helper.ExitCode!=0)throw new IOException("计划任务未能更新。");
                    }
                }
                RefreshTask(); Say(enable?"已更新当前用户的登录自启任务。":"已取消登录自启。");
            } catch(Exception e){RefreshTask();Say(e.Message,true);}
        }
    }
}
