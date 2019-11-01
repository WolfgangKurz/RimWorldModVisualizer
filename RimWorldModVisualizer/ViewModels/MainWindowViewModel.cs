using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Livet;
using Livet.Messaging;

using RimWorldModVisualizer.Enums;
using RimWorldModVisualizer.Models;
using RimWorldModVisualizer.Views;

namespace RimWorldModVisualizer.ViewModels {
	public class MainWindowViewModel : ViewModel {
		private string _SearchText { get; set; } = "";
		public string SearchText {
			get { return this._SearchText; }
			set {
				if(this._SearchText != value) {
					this._SearchText = value;

					Action<ModViewModel> match = null;
					match = new Action<ModViewModel>(x => {
						x.IsMatch = x.Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
						if (x.Childs != null)
							foreach (var y in x.Childs)
								match(y);
					});
					foreach (var item in this.ModList)
						match(item);

					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(this.ModList));
				}
			}
		}

		private ObservableCollection<ModViewModel> _ModList { get; set; }
		public ObservableCollection<ModViewModel> ModList {
			get {return this._ModList; }
			set {
				if (this._ModList != value) {
					this._ModList = value;
					this.RaisePropertyChanged();
				}
			}
		}

		private ModViewModel _SelectedMod { get; set; }
		public ModViewModel SelectedMod {
			get { return this._SelectedMod; }
			set {
				if(this._SelectedMod != value) {
					this._SelectedMod = value;
					this.RaisePropertyChanged();
				}
			}
		}

		private string _DirectoryName { get; set; } = "New directory";
		public string DirectoryName {
			get { return this._DirectoryName; }
			set {
				if(this._DirectoryName != value) {
					this._DirectoryName = value;
					this.RaisePropertyChanged();
				}
			}
		}

		public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

		private string _RWMSDBVersion { get; set; } = "...";
		public string RWMSDBVersion {
			get { return this._RWMSDBVersion; }
			private set {
				if (this._RWMSDBVersion != value) {
					this._RWMSDBVersion = value;
					this.RaisePropertyChanged();
				}
			}
		}

		public Color[][] ColorGrid => ColorTable.ColorGrid;

		private RWMSDB RWMSDB { get; set; }
		private RWMSDBCategory RWMSDBCat { get; set; }

		public ICommand LoadedCommand { get; }

		public ICommand MenuLoadRWModsCommand { get; }
		public ICommand MenuOpenCommand { get; }
		public ICommand MenuSaveCommand { get; }
		public ICommand MenuSaveAsCommand { get; }

		public ICommand MenuSortCommand { get; }
		public ICommand MenuSortNoRemoveCommand { get; }

		public ICommand MenuRecolorWithDir { get; }
		public ICommand MenuColorResetAll { get; }
		public ICommand MenuColorResetMods { get; }

		public ICommand SetModColorCommand { get; }

		public ICommand AddDirectoryCommand { get; }
		public ICommand RemoveDirectoryCommand { get; }

		public MainWindowViewModel() {
			this.LoadedCommand = new RelayCommand(() => this.Loaded());

			this.MenuLoadRWModsCommand = new RelayCommand(() => this.LoadRWMods());
			this.MenuOpenCommand = new RelayCommand(() => this.OpenMods());
			this.MenuSaveCommand = new RelayCommand(() => this.SaveMods());
			this.MenuSaveAsCommand = new RelayCommand(() => this.SaveModsAs());

			this.MenuSortCommand = new RelayCommand(() => this.SortWithRWMSDB());
			this.MenuSortNoRemoveCommand = new RelayCommand(() => this.SortWithRWMSDB(false));

			this.MenuRecolorWithDir = new RelayCommand(() => this.RecolorWithDir());
			this.MenuColorResetAll = new RelayCommand(() => this.ResetColor(true));
			this.MenuColorResetMods = new RelayCommand(() => this.ResetColor(false));

			this.SetModColorCommand = new ColorCommand(c => {
				if (this.SelectedMod == null) return;
				this.SelectedMod.Color = c;
			});

			this.AddDirectoryCommand = new RelayCommand(() => this.AddDirectory());
			this.RemoveDirectoryCommand = new RelayCommand(() => this.RemoveDirectory());
		}

		private void Loaded() {
			var catalog = new ConfigurationViewModel();
			this.Transition(catalog, typeof(Configuration), true);

			var config = new INI();
			if (!config.KeyExists("ConfigurationDir", "Directories")) {
				// Missing RimWorld path
				this.Close();
				return;
			}

			this.LoadDefaultMods();

			this.LoadRWMSDB();
		}

		private void LoadRWMSDB() {
			new Task(async () => {
				this.RWMSDB = await RWMSDB.Fetch();
				this.RWMSDBCat = await RWMSDBCategory.Fetch();

				this.RWMSDBVersion = string.Format("{0}, {1}",
					this.RWMSDB.version.ToString(),
					DateTime.ParseExact(this.RWMSDB.timestamp, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture) // Sat Oct 19 20:16:50 2019
						.ToString("yyyy-MM-dd hh:mm:ss")
				);
			}).Start();
		}

		public void LoadRWMods() {
			this.LoadDefaultMods();
			this.SearchText = "";
		}

		/// <summary>
		/// Load default mods.
		/// Load "Default.rwmv.xml" if exists.
		/// If not exists, Load "ModsConfig.xml".
		/// </summary>
		public bool LoadDefaultMods() {
			var config = new INI();
			var ModsConfigDir = Path.Combine(
				config.Read("ConfigurationDir", "Directories"),
				"Config"
			);

			var rwmvPath = Path.Combine(ModsConfigDir, "Default.rwmv.xml");
			if (File.Exists(rwmvPath)) {
				return LoadMods(rwmvPath);
			}

			var ModsConfig = Path.Combine(
				ModsConfigDir,
				"ModsConfig.xml"
			);
			if (!File.Exists(ModsConfig)) {
				// Failed to find "ModsConfig.xml" file.
				return false;
			}
			return LoadMods(ModsConfig);
		}
		public bool LoadMods(string path) {
			var config = new INI();

			// Mod list
			var mods = new List<ModViewModel>();

			// Listing local mods (except Core)
			var LocalModDir = Path.Combine(config.Read("RimWorldDir", "Directories"), "Mods");
			var LocalMods = Directory.GetDirectories(LocalModDir)
				.Select(x => Path.GetFileName(x))
				.ToArray();

			// Listing steam mods
			var SteamModDir = Path.Combine(config.Read("WorkshopDir", "Directories"));
			var SteamMods = Directory.GetDirectories(SteamModDir)
				.Select(x => Path.GetFileName(x))
				.ToArray();

			var mc = new ModsConfig(path);
			if (!mc.Loaded) return false;

			var ActivatedMods = mc.activeMods();
			var PlainList = new List<ModViewModel>();
			var MissingList = new List<string>();

			Action<RawMod, ModViewModel> explore = null;
			explore = new Action<RawMod, ModViewModel>((node, target) => {
				ModViewModel mod = null;

				if (node.Name != null && node.Name.Length > 0) {
					mod = new ModViewModel(
						node.Id,
						node.Name
					) {
						Expanded = false
					};
				} else {
					var isLocal = LocalMods.Contains(node.Id);
					mod = new ModViewModel(
						node.Id,
						Path.Combine(isLocal ? LocalModDir : SteamModDir, node.Id),
						isLocal ? ModType.Local : ModType.Steam
					);
				}

				if (mod.Id != null) {
					if (node.Color != Colors.Transparent)
						mod.Color = node.Color;

					mod.Enabled = node.Enabled;

					if (target == null)
						mods.Add(mod);
					else
						target.Childs.Add(mod);

					PlainList.Add(mod);
				} else {
					if (node.Name == null || node.Name.Length == 0)
						MissingList.Add(node.Id);
				}

				if (node.Childs != null) {
					foreach (var y in node.Childs)
						explore(y, mod.Id == null ? target : mod);
				}
			});
			foreach (var raw in ActivatedMods)
				explore(raw, null);

			var DeactivatedMods = LocalMods
				.Concat(SteamMods)
				.Where(x => !PlainList.Any(y => y.Id == x))
				.ToArray();
			foreach (var modName in DeactivatedMods) {
				var isLocal = LocalMods.Contains(modName);
				mods.Add(new ModViewModel(
					modName,
					Path.Combine(isLocal ? LocalModDir : SteamModDir, modName),
					isLocal ? ModType.Local : ModType.Steam
				) {
					Enabled = false
				});
			}

			this.ModList = new ObservableCollection<ModViewModel>(
				mods.Select(x => {
					if (x.Color == Colors.Transparent)
						x.Color = ModColor.GetModColor(config.Read("ConfigurationDir", "Directories"), x.Id);
					return x;
				})
			);
			this.SelectedMod = this.ModList.FirstOrDefault();

			if (MissingList.Count > 0) {
				File.WriteAllLines(
					Path.Combine(
						Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
						"MissingMods.txt"
					),
					MissingList.Select(x => "https://steamcommunity.com/sharedfiles/filedetails/?id=" + x)
				);

				MessageBox.Show(
					"Failed to load all mod list, missing mods:" + Environment.NewLine
					 + string.Join(Environment.NewLine, MissingList.Select(x => "- " + x))
					 + Environment.NewLine + Environment.NewLine
					 + "Check 'MissingMods.txt' on RWMV directory.",
					"RimWorldModVisualizer",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation
				);
			}
			return true;
		}

		public void OpenMods() {
			var config = new INI();
			var ModsConfig = Path.Combine(
				config.Read("ConfigurationDir", "Directories"),
				"Config"
			);

			var dialog = new OpenFileDialog {
				InitialDirectory = ModsConfig,
				FileName = Path.Combine(ModsConfig, "ModsConfig.xml"),
				Filter = "RimWorldModVisualizer Mod list (*.rwmv.xml)|*.rwmv.xml|RimWorld ModsConfig.xml|ModsConfig.xml|ModManager Mod list (*.xml)|*.xml|All Files (*.*)|*.*",
			};
			if (dialog.ShowDialog() == DialogResult.Cancel) return;

			LoadMods(dialog.FileName);
		}
		public void SaveMods() {
			var config = new INI();

			this.SaveModsTo( // Save as RWMV xml
				Path.Combine(
					config.Read("ConfigurationDir", "Directories"),
					"Config",
					"Default.rwmv.xml"
				)
			);

			// Save as RimWorld xml
			ModsConfig.SaveAsRimWorldModsConfig(
				Path.Combine(
					config.Read("ConfigurationDir", "Directories"),
					"Config",
					"ModsConfig.xml"
				),
				this.ModList.ToArray()
			);
			ModColor.SaveModsColor(
				Path.Combine(
					config.Read("ConfigurationDir", "Directories")
				),
				this.ModList.ToArray()
			);
			MessageBox.Show("Save done.", "RimWorldModVisualizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		public void SaveModsAs() {
			var config = new INI();
			var ModsConfig = Path.Combine(
				config.Read("ConfigurationDir", "Directories"),
				"Config"
			);

			var dialog = new SaveFileDialog {
				InitialDirectory = ModsConfig,
				FileName = Path.Combine(ModsConfig, "ModsConfig.xml"),
				Filter = "RimWorldModVisualizer Mod list (*.rwmv.xml)|*.rwmv.xml|All Files (*.*)|*.*",
			};
			if (dialog.ShowDialog() == DialogResult.Cancel) return;

			this.SaveModsTo(dialog.FileName);
		}

		private void SaveModsTo(string path) {
			var doc = new XmlDocument();

			var modsNode = doc.CreateElement("Mods");

			var activeMods = new List<ModViewModel>();
			Action<XmlElement, ModViewModel> explore = null;
			explore = new Action<XmlElement, ModViewModel>((node, mod) => {
				var li = doc.CreateElement("li");
				if (mod.Color != Colors.Transparent)
					li.SetAttribute("Color", ModColor.ToString(mod.Color));

				if (mod.Type == ModType.Directory)
					li.SetAttribute("Name", mod.Name);

				li.SetAttribute("Id", mod.Id);
				li.SetAttribute("Enabled", mod.Enabled ? "1" : "0");

				foreach (var y in mod.Childs)
					explore(li, y);

				node.AppendChild(li);
			});
			foreach (var x in this.ModList) 
				explore(modsNode, x);

			var root = doc.CreateElement("RWMV");
			root.AppendChild(modsNode);

			doc.AppendChild(root);

			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;
			settings.Indent = true;

			var writer = XmlWriter.Create(path, settings);
			doc.Save(writer);
		}

		public void SortWithRWMSDB(bool RemoveEmptyDirectory = true) {
			if (this.RWMSDB == null) return;

			var unknown = new ModViewModel("unknown", "Unknown mods on RWMSDB") {
				Color = Colors.DarkGray
			};

			// Search all mods
			Action<ModViewModel> explore = null;
			explore = new Action<ModViewModel>(x => {
				if (x.Type != ModType.Directory)
					unknown.Childs.Add((ModViewModel)x.Clone(false));

				foreach (var y in x.Childs)
					explore(y);
			});
			foreach (var x in this.ModList) explore(x);

			var list = new ObservableCollection<ModViewModel>();

			var rnd = new Random();
			this.RWMSDBCat.cat
				.OrderBy(x => x.Value.Weight)
				.ToList()
				.ForEach(x => list.Add(
					new ModViewModel(x.Key, x.Value.Description) {
						Expanded = false,
						Color = ColorTable.ColorList[rnd.Next(ColorTable.ColorList.Length)]
					}
				));
			list.Add(unknown);

			var db = this.RWMSDB.db;
			foreach (var i in db) {
				var targetName = i.Key;
				var targetCategory = i.Value;

				var targets = unknown.Childs.Where(x => x.Type != ModType.Directory && x.Name == targetName).ToArray();
				if(targets.Length > 0) {
					var dest = list.FirstOrDefault(x => x.Type == ModType.Directory && x.Id == targetCategory);
					if (dest == null) continue; // Category not found, keep unknown directory

					foreach (var target in targets)
						unknown.Childs.Remove(target);

					if (targets.Length == 1) {
						// Single mods, just move to destination
						dest.Childs.Add(targets.First());
					} else {
						// Multiple mods, move to new directory that created to destination
						var newDir = new ModViewModel("grp_" + targetName, "📚 " + targetName);
						foreach (var target in targets)
							newDir.Childs.Add(target);
						dest.Childs.Add(newDir);
					}
				}
			}

			if (RemoveEmptyDirectory)
				list.Where(x => x.Type == ModType.Directory && x.Childs.Count == 0)
					.ToList()
					.ForEach(x => list.Remove(x));

			this.ModList = list;
		}

		public void RecolorWithDir() {
			Action<ModViewModel, Color> explore = null;
			explore = new Action<ModViewModel, Color>((x, c) => {
				foreach (var y in x.Childs)
					explore(y, x.Color);

				if (x.Type != ModType.Directory)
					x.Color = c;
			});
			foreach (var x in this.ModList) explore(x, x.Color);
		}
		public void ResetColor(bool IncludesDirectory) {
			Action<ModViewModel> explore = null;
			explore = new Action<ModViewModel>(x => {
				if (IncludesDirectory || (x.Type != ModType.Directory))
					x.Color = Colors.Transparent;

				foreach (var y in x.Childs)
					explore(y);
			});
			foreach (var x in this.ModList) explore(x);
		}

		private void AddDirectory() {
			var rnd = new Random();
			this.ModList.Add(
				new ModViewModel(DateTime.UtcNow.Ticks.ToString(), this.DirectoryName) {
					Expanded = false,
					Color = ColorTable.ColorList[rnd.Next(ColorTable.ColorList.Length)]
				}
			);
		}
		private void RemoveDirectory() {
			if (this.SelectedMod.Type != ModType.Directory) return;

			var cur = this.SelectedMod;
			var list = this.ModList.ToList();

			Func<ModViewModel, ModViewModel, ModViewModel> explore = null;
			explore = new Func<ModViewModel, ModViewModel, ModViewModel>((search, current) => {
				if (current.Childs.Contains(search)) return current;

				foreach (var y in current.Childs) {
					var c = explore(search, y);
					if (c != null) return c;
				}
				return null;
			});

			if (list.Contains(cur)) { // Root directory
				var index = list.FindIndex(x => x == cur);
				list.InsertRange(index, cur.Childs);
				list.Remove(cur);

				this.ModList = new ObservableCollection<ModViewModel>(list);
			} else {
				var parent = list.FirstOrDefault(x => explore(cur, x) != null);
				if (parent == null) return;

				var childs = parent.Childs.ToList();
				var index = childs.FindIndex(x => x == cur);
				childs.InsertRange(index, cur.Childs);
				childs.Remove(cur);

				parent.Childs = new ObservableCollection<ModViewModel>(childs);
			}
		}
	}
}
