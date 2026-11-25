using System;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("SCUNET 配置器")]
[assembly: AssemblyProduct("SCUNET Config Tool")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Scunet {
    internal static class Program {
        [STAThread]
        static int Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                if(args.Length>0) {
                    if(args.Length==1 && args[0]=="--startup-off") new StartupTask().Disable();
                    else if(args.Length==4 && args[0]=="--startup-on")
                        new StartupTask().Enable(args[1],Int32.Parse(args[2]),args[3]);
                    else return 2;
                } else Application.Run(new Editor());
                return 0;
            } catch(Exception e) {
                MessageBox.Show(e.Message,"SCUNET 配置器",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
