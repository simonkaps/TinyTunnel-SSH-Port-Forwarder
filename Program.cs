using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Renci.SshNet;

namespace TinyTunnel
{
    static class MainClass
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string Section, string Key, string Default,
            StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileSectionNames(byte[] lpszReturnBuffer, int nSize, string lpFileName);

        private static readonly List<SshClient> Clients = new List<SshClient>();
        private static readonly string EXE = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly string INIPath = new FileInfo(Path.Combine(AssemblyDir(), "connections.ini")).FullName;

        private static bool INIKeyExists(string Key, string Section = null)
        {
            return INIRead(Key, Section).Length > 0;
        }

        private static string[] inisections
        {
            get
            {
                List<string> lista = new List<string>();
                byte[] buffer = new byte[8192];
                GetPrivateProfileSectionNames(buffer, buffer.Length, INIPath);
                string allSections = Encoding.UTF8.GetString(buffer);
                string[] sectionNames = allSections.Split('\0');
                foreach (string sectionName in sectionNames)
                    if (!string.IsNullOrEmpty(sectionName))
                        lista.Add(sectionName);
                return lista.ToArray();
            }
        }

        private static string INIRead(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, INIPath);
            return RetVal.ToString();
        }

        private static string get_ini_value(string key, string section, string defval)
        {
            string value = defval;
            if (INIKeyExists(key, section) && !string.IsNullOrEmpty(INIRead(key, section)))
                value = INIRead(key, section);
            return value;
        }

        private static string AssemblyDir()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        private static Dictionary<string, Dictionary<string, string>> read_ini_settings()
        {
            var entries = new Dictionary<string, Dictionary<string, string>>();
            foreach (var section in inisections)
            {
                var kv = new Dictionary<string, string>
                {
                    {"keyfile", get_ini_value("keyfile", section, null)},
                    {"password", get_ini_value("password", section, null)},
                    {"username", get_ini_value("username", section, null)},
                    {"host", get_ini_value("host", section, null)},
                    {"sshport", get_ini_value("sshport", section, null)},
                    {"localhost", get_ini_value("localhost", section, null)},
                    {"localport", get_ini_value("localport", section, null)},
                    {"remotehost", get_ini_value("remotehost", section, null)},
                    {"remoteport", get_ini_value("remoteport", section, null)},
                    {"enabled", get_ini_value("enabled", section, null)}
                };

                entries.Add(section, kv);
            }

            return entries;
        }

        private static PrivateKeyFile _keyFile(string filename, string pass)
        {
            return new PrivateKeyFile(Path.Combine(AssemblyDir(), filename), pass);
        }

        private static ConnectionInfo sshserverconn(string hostName, int port, string username, PrivateKeyFile key)
        {
            return new ConnectionInfo(hostName, port, username, new PrivateKeyAuthenticationMethod(username, key));
        }

        private static ConnectionInfo sshserverconn(string hostName, int port, string username, string pass)
        {
            return new ConnectionInfo(hostName, port, username, new PasswordAuthenticationMethod(username, pass));
        }

        private static void CreateTunnels()
        {
            foreach (var conn in read_ini_settings())
            {
                var name = conn.Key;
                var enabled = Convert.ToBoolean(Int32.Parse(conn.Value["enabled"]));
                var keyfile = String.IsNullOrEmpty(conn.Value["keyfile"]) ? String.Empty : conn.Value["keyfile"];
                var keypass = String.IsNullOrEmpty(conn.Value["password"]) ? String.Empty : conn.Value["password"];
                var user = conn.Value["username"];
                var host = conn.Value["host"];
                var sshport = Int32.Parse(conn.Value["sshport"]);
                var lhost = conn.Value["localhost"];
                var lport = UInt32.Parse(conn.Value["localport"]);
                var rhost = conn.Value["remotehost"];
                var rport = UInt32.Parse(conn.Value["remoteport"]);
                try
                {
                    if (enabled)
                    {
                        SshClient sshclient;
                        if (!String.IsNullOrEmpty(keyfile) && !String.IsNullOrEmpty(keypass))
                        {
                            var key = _keyFile(keyfile, keypass);
                            sshclient = new SshClient(sshserverconn(host, sshport, user, key));
                        }
                        else
                        {
                            sshclient = new SshClient(sshserverconn(host, sshport, user, keypass));
                        }

                        //do the connection
                        sshclient.Connect();
                        var port = new ForwardedPortLocal(lhost, lport, rhost, rport);
                        sshclient.AddForwardedPort(port);
                        //do the port forward
                        port.Start();
                        Clients.Add(sshclient);
                        //messages displayed
                        var con = (sshclient.IsConnected) ? "connected" : "not connected";
                        Console.WriteLine($"connection {name} has {con}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with connection {name}!\nFull error details:\n{ex.Message}\n\n");
                }
            }
        }

        private static void KillTunnels()
        {
            foreach (var ss in Clients)
                if (ss != null && ss.IsConnected)
                    ss.Disconnect();
        }

        public static void Main()
        {
            Console.WriteLine("TinyTunnel - Created by Simon Kapsalis <simonkapsal@gmail.com>\n");
            Console.CancelKeyPress += delegate
            {
                try { Console.WriteLine("Terminating all tunnels please wait..."); KillTunnels(); }
                catch (Exception) { /**/ }
                Environment.Exit(0);
            };
            CreateTunnels();
            Console.WriteLine("When finished press Ctrl+C to close all connections...");
            while (true)
            {
                Thread.Sleep(50);
            }
        }
    }
}