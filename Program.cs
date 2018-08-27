using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows.Forms;

namespace MsraLauncher
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                var msra = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\msra.exe");

                if (File.Exists(msra) == false)
                {
                    MessageBox.Show(String.Format("{0} was not found", msra), "File not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                SetupRegistry(msra);

                if (args.Length == 0)
                {
                    return;
                }

                Uri uri = new Uri(args[0]);

                if (uri.Scheme.Equals("msra", StringComparison.CurrentCultureIgnoreCase))
                {
                    var host = String.Format("{0}{1}", uri.Host, uri.IsDefaultPort ? "" : String.Format(":{0}", uri.Port));

                    var arguments = new List<string>();

                    if (String.IsNullOrEmpty(uri.Query) == false)
                    {
                        var query = HttpUtility.ParseQueryString(uri.Query);

                        foreach (var key in query.AllKeys)
                        {
                            switch (key)
                            {
                                case "offerra":
                                    arguments.Add(String.Format("/{0}", key));
                                    break;
                            }
                        }

                    }

                    arguments.Add(host);

                    Execute(msra, arguments);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Msra Launcher - Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Execute(string exe, params string[] args)
        {
            Execute(exe, args.AsEnumerable());
        }

        static void Execute(string exe, IEnumerable<string> args)
        {
            var path = new FileInfo(exe);

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = path.Name;
            p.StartInfo.WorkingDirectory = path.Directory.FullName;
            p.StartInfo.Arguments = String.Join(" ", args.Where(s => String.IsNullOrEmpty(s) == false));
            p.Start();
        }

        static void SetupRegistry(string exe)
        {
            var launcher = Path.Combine(Environment.CurrentDirectory, "MsraLauncher.exe");

            var hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);

            var msra = hkcr.OpenSubKey("msra");

            if (msra == null)
            {
                try
                {
                    msra = hkcr.CreateSubKey("msra");

                    var DefaultIcon = msra.CreateSubKey("DefaultIcon");
                    var Shell = msra.CreateSubKey("Shell");
                    var Open = Shell.CreateSubKey("Open");
                    var Command = Open.CreateSubKey("Command");

                    msra.SetValue("", "URL:Remote Assistance Client Launcher");
                    msra.SetValue("URL Protocol", "");
                    DefaultIcon.SetValue("", String.Format("\"{0}\",1", launcher));
                    Command.SetValue("", String.Format("\"{0}\" \"%1\"", launcher));

                    MessageBox.Show("URL handler registered", "Msra Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Could not register as URL handler, please run again as administrator", "Msra Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
