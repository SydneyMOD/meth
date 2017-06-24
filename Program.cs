using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using Codeplex.Data;
using meth.Properties;
using System.IO.Compression;

namespace meth
{
	class Program
	{
		public static string appPath = Assembly.GetExecutingAssembly().Location;
		public static string USER_PROFILE = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\";
		public static string CONFIG_NAME = ".methconfig";
		public static string help = Resources.help;
		public static dynamic config;
		public static WebClient client = null;

		public static void Log(string text, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ResetColor();
		}

		public static void GetModDatas(string type, bool verb=false)
		{
			string[] subFolders = Directory.GetDirectories(config["payday2_path"] + @"\mods");

			foreach (string subFolder in subFolders)
			{
				try
				{
					var modtxt = DynamicJson.Parse(File.ReadAllText(subFolder + @"\mod.txt"));

					switch (type)
					{
						case "updates":
							foreach (dynamic update in modtxt["updates"]) Console.WriteLine(update["identifier"]);
							break;

						default:
							Console.WriteLine(modtxt[type]);
							break;
					}
				}
				catch (Exception e)
				{
					if (e is XmlException)
					{
						if (verb)
						{
							Log($"Load error: {subFolder}\n", ConsoleColor.Red);
						}
					}
					if (e is FileNotFoundException)
					{
						//Log($"File not found: {subFolder}\n", ConsoleColor.Red);
					}
				}
			}
		}

		public static void Disable(string name)
		{
			string[] subFolders = Directory.GetDirectories(config["payday2_path"] + @"\mods");

			foreach (string subFolder in subFolders)
			{
				try
				{
					var modtxt = DynamicJson.Parse(File.ReadAllText(subFolder + @"\mod.txt"));
					string dirname = Path.GetFileName(Path.GetDirectoryName(subFolder + @"\mod.txt"));
					if (!Directory.Exists(config["payday2_path"] + @"\mods_disable\"))
					{
						Directory.CreateDirectory(config["payday2_path"] + @"\mods_disable\");
					}
					if (dirname == name || modtxt["name"] == name)
					{
						Directory.Move(subFolder, config["payday2_path"] + @"\mods_disable\" + dirname);
					}
				}
				catch
				{
					//
				}
			}
		}

		public static void Enable(string name)
		{
			string[] subFolders = Directory.GetDirectories(config["payday2_path"] + @"\mods_disable");

			foreach (string subFolder in subFolders)
			{
				try
				{
					var modtxt = DynamicJson.Parse(File.ReadAllText(subFolder + @"\mod.txt"));
					string dirname = Path.GetFileName(Path.GetDirectoryName(subFolder + @"\mod.txt"));
					if (dirname == name || modtxt["name"] == name)
					{
						Directory.Move(subFolder, config["payday2_path"] + @"\mods\" + dirname);
					}
				}
				catch
				{
					//Log($"{e.Message}\n", ConsoleColor.Red);
				}
			}
		}

		public static void Download(string name, bool isURL=false)
		{
			Uri url = new Uri(config["download_api"] + name);
			if (isURL)
			{
				url = new Uri(name);
			}
			string path = config["payday2_path"] + $@"\mods\downloads\{name}.zip";
			if (client == null)
			{
				client = new WebClient();
				client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
				client.DownloadFileCompleted += new AsyncCompletedEventHandler(Client_DownloadFileCompleted);
			}

			Console.CursorVisible = false;
			Console.Write($"Download {url}\n");
			//client.DownloadFileAsync(url, path);
			// TODO! use async
			client.DownloadFile(url, path);
		}
		private static void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Console.CursorLeft = 0;
			int count = e.ProgressPercentage / 5;
			string bar = String.Empty;
			while (bar.Length == 20)
			{
				for (int i = 0; i == count; i++)
				{
					bar += "#";
				}
				bar += " ";
			}
			Console.Write($"|{bar}| {e.ProgressPercentage}% ({e.BytesReceived}/{e.TotalBytesToReceive} bytes)");
			//Console.Write("{0}% ({1} / {2} byte)", e.ProgressPercentage, e.TotalBytesToReceive, e.BytesReceived);
		}
		private static void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			Console.CursorLeft = 0;
			if (e.Cancelled) { Log("Cancelled.\n", ConsoleColor.Yellow); }
			else if (e.Error != null) { Log($"ERROR: {e.Error.Message}\n", ConsoleColor.Red); }
			else { Log("Download Completed!\n", ConsoleColor.Green); }
			Console.CursorVisible = true;
		}

		public static void Extract(string name)
		{
			string src = config["payday2_path"] + $@"\mods\downloads\{name}.zip";
			string dest = config["payday2_path"] + $@"\mod\{name}\";

			if (Directory.Exists(dest)) { Directory.Delete(dest, true); }
			try
			{
				ZipFile.ExtractToDirectory(src, dest, Encoding.UTF8);
			}
			catch (Exception e)
			{
				Log("\nERROR: " + e.Message, ConsoleColor.Red);
			}
		}

		static void Main(string[] args)
		{
			// Check config file
			if (!File.Exists(USER_PROFILE + CONFIG_NAME))
			{
				File.WriteAllText(USER_PROFILE + CONFIG_NAME, Resources.config);
				Log("\nConfig file not found.\nDefault config created at\n" + USER_PROFILE + CONFIG_NAME, ConsoleColor.Yellow);
			}

			config = DynamicJson.Parse(File.ReadAllText(USER_PROFILE + CONFIG_NAME, Encoding.UTF8));

			// Check pd2 path
			if (!Directory.Exists(config["payday2_path"]))
			{
				Log("\nFailed to detect PAYDAY2 path!\nPlease specify PAYDAY2 path: ", ConsoleColor.Red);
				string path = Console.ReadLine();
				config["payday2_path"] = path;
				StreamWriter sw = new StreamWriter(USER_PROFILE + CONFIG_NAME, false, Encoding.UTF8);
				sw.Write(config.ToString());
				sw.Close();
				Log("\nUpdated successfully.\n", ConsoleColor.Green);
				Environment.Exit(0);
			}

			if (args.Length != 0)
			{
				switch (args[0])
				{
					case "help":
					case "--help":
						Assembly asm = Assembly.GetExecutingAssembly();
						Version ver = asm.GetName().Version;
						Log($"\nmeth  version {ver}\n", ConsoleColor.Green);
						Console.Write(help);
						break;

					case "disable":
					case "deactivate":
						if (args.Length >= 2) { Disable(args[1]); }
						else { Log("Please specify mod name!\n", ConsoleColor.Red); }
						break;

					case "enable":
					case "activate":
						if (args.Length >= 2) { Enable(args[1]); }
						else { Log("Please specify mod name!\n", ConsoleColor.Red); }
						break;

					case "list":
						bool verb = args.Length >= 2 && args[1] == "--verb"? true : false;
						GetModDatas("name", verb);
						break;

					case "freeze":
						GetModDatas("updates");
						break;

					case "download":
						if (args.Length >= 2) { Download(args[1]); }
						else { Log("Please specify mod name!\n", ConsoleColor.Red); }
						break;
					case "extract":
						if (args.Length >= 2) { Extract(args[1]); }
						else { Log("Please specify file name!\n", ConsoleColor.Red); }
						break;

					case "install":
						bool isURL = args.Length >= 3 && args[1] == "--url" || args[2] == "--url"? true : false;
						if (args.Length >= 2)
						{
							Download(args[1], isURL);
							Extract(args[1]);
						}
						else { Log("Please specify mod identifier!", ConsoleColor.Red); }
						break;
					
					// no command
					default:
						Log("\nUnknown command.\nRead help and try again.\n", ConsoleColor.Red);
						Console.Write(help);
						break;
				}
			}
			else
			{
				Console.Write(help);
			}
		}

	}
}
