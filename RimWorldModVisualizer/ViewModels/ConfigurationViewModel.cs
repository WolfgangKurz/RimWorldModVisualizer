using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Livet;

using RimWorldModVisualizer.Enums;
using RimWorldModVisualizer.Models;
using System.Reflection;

namespace RimWorldModVisualizer.ViewModels {
	public class ConfigurationViewModel : ViewModel {
		public ConfigurationSearchType SearchProgress { get; private set; } = ConfigurationSearchType.None;

		private string _RimWorldDir { get; set; }
		public string RimWorldDir {
			get { return this._RimWorldDir; }
			set {
				if (this._RimWorldDir != value) {
					this._RimWorldDir = value;
					this.RaisePropertyChanged();
				}
			}
		}

		private string _WorkshopDir { get; set; }
		public string WorkshopDir {
			get { return this._WorkshopDir; }
			set {
				if (this._WorkshopDir != value) {
					this._WorkshopDir = value;
					this.RaisePropertyChanged();
				}
			}
		}

		private string _ConfigurationDir { get; set; }
		public string ConfigurationDir {
			get { return this._ConfigurationDir; }
			set {
				if (this._ConfigurationDir != value) {
					this._ConfigurationDir = value;
					this.RaisePropertyChanged();
				}
			}
		}

		public ConfigurationViewModel() {
			var config = new INI();
			if (config.KeyExists("RimWorldDir", "Directories")) {
				this.RimWorldDir = config.Read("RimWorldDir", "Directories");
				this.WorkshopDir = config.Read("WorkshopDir", "Directories");
				this.ConfigurationDir = config.Read("ConfigurationDir", "Directories");

				new Task(async () => {
					await Task.Delay(500);
					this.ConfirmPath();
				}).Start();
				return;
			}

			new Task(async () => {
				try {
					this.SearchProgress = ConfigurationSearchType.FindingSteam;

					var steamPath = Registry.LocalMachine
						.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
						.GetValue("InstallPath") as string;

					this.SearchProgress = ConfigurationSearchType.FindingRimWorld;

					var libraryfolders = File.ReadAllLines(Path.Combine(steamPath, "steamapps", "libraryfolders.vdf"))
						.Skip(4)
						.TakeWhile(x => x != "}")
						.Select(x => {
							var part = x.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
							var path = Regex.Unescape(part[1]);
							return Path.Combine(
								path.Substring(1, path.Length - 2),
								"steamapps"
							);
						});

					var foundGame = false;
					foreach(var dir in libraryfolders) {
						var manifest = Path.Combine(dir, $"appmanifest_{RimWorld.AppId}.acf");
						var gamedir = Path.Combine(dir, "common", "RimWorld");
						var workshopdir = Path.Combine(dir, "workshop", "content", RimWorld.AppId.ToString());

						if (!File.Exists(manifest)) continue;

						foundGame = true;
						this.RimWorldDir = gamedir;
						this.WorkshopDir = workshopdir;
						break;
					}
					if (!foundGame) {
						this.SearchProgress = ConfigurationSearchType.NotFound;
						return;
					}
					this.SearchProgress = ConfigurationSearchType.FindingConfiguration;

					var localLow = SystemDir.GetKnownFolderPath(SystemDir.LocalLow);
					var configDir = Path.Combine(localLow, "Ludeon Studios", "RimWorld by Ludeon Studios");
					if (!Directory.Exists(configDir)) {
						this.SearchProgress = ConfigurationSearchType.NotFound;
						return;
					}
					this.ConfigurationDir = configDir;

					await Task.Delay(500);
					this.ConfirmPath();
				} catch {
					this.SearchProgress = ConfigurationSearchType.NotFound;
				}
			}).Start();
		}

		public void ConfirmPath() {
			var config = new INI();
			config.Write("RimWorldDir", this.RimWorldDir, "Directories");
			config.Write(
				"WorkshopDir",
				string.IsNullOrEmpty(this.WorkshopDir)
					? Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
					: this.WorkshopDir,
				"Directories"
			);
			config.Write("ConfigurationDir", this.ConfigurationDir, "Directories");
			this.Close();
		}
		public void Cancel() {
			Application.Instance.Shutdown();
		}
	}
}
