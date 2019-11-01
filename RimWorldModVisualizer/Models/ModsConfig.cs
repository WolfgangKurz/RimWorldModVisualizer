using RimWorldModVisualizer.Enums;
using RimWorldModVisualizer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RimWorldModVisualizer.Models {
	public class ModsConfig {
		private XmlDocument doc { get; }

		public bool Loaded { get; private set; }

		protected int Type { get; private set; }

		public ModsConfig(string path) {
			this.doc = new XmlDocument();

			try {
				this.Type = 0;
				this.Loaded = false;

				doc.Load(path);

				if (doc.SelectNodes("/ModsConfigData/activeMods/li").Count > 0) {
					this.Loaded = true;
					this.Type = 1; // ModsConfig
				} else if (doc.SelectNodes("/ModList/modIds/li").Count > 0) {
					this.Loaded = true;
					this.Type = 2; // ModManager
				} else if (doc.SelectNodes("/RWMV/Mods/li").Count > 0) {
					this.Loaded = true;
					this.Type = 3; // RWMV
				}
			} catch {
				this.Loaded = false;
			}
		}

		public RawMod[] activeMods() {
			switch (this.Type) {
				case 1:
					return doc.SelectNodes("/ModsConfigData/activeMods/li")
						.Cast<XmlNode>()
						.Select(x => new RawMod() {
							Id = x.InnerText,
							Enabled = true
						})
						.ToArray();
				case 2:
					return doc.SelectNodes("/ModList/modIds/li")
						.Cast<XmlNode>()
						.Select(x => new RawMod() {
							Id = x.InnerText,
							Enabled = true
						})
						.ToArray();
				case 3: {
						var list = new List<RawMod>();
						Action< XmlNode, RawMod> explore = null;
						explore = new Action<XmlNode, RawMod>((node, target) => {
							var mod = new RawMod();

							mod.Id = node.Attributes["Id"].Value;
							mod.Enabled = node.Attributes["Enabled"].Value == "1";

							mod.Name = node.Attributes["Name"]?.Value;
							mod.Color = ModColor.Parse(node.Attributes["Color"]?.Value);

							if (target == null)
								list.Add(mod);
							else
								target.Childs.Add(mod);

							if (node.HasChildNodes) {
								foreach (var y in node.ChildNodes.Cast<XmlNode>())
									explore(y, mod);
							}
						});

						var root = doc.SelectNodes("/RWMV/Mods/li").Cast<XmlNode>();
						foreach (var x in root) explore(x, null);

						return list.ToArray();
					}
				default:
					return null;
			}
		}

		public static void SaveAsRimWorldModsConfig(string path, ModViewModel[] mods) {
			var ver = File.ReadAllText(Path.Combine(new INI().Read("RimWorldDir", "Directories"), "Version.txt")).Trim();

			// Search all mods
			var activeMods = new List<ModViewModel>();
			Action<ModViewModel> explore = null;
			explore = new Action<ModViewModel>(x => {
				if (x.Type != ModType.Directory && x.Enabled)
					activeMods.Add((ModViewModel)x.Clone(true));

				foreach (var y in x.Childs)
					explore(y);
			});
			foreach (var x in mods) explore(x);

			var doc = new XmlDocument();

			var modsNode = doc.CreateElement("activeMods");
			foreach (var mod in activeMods) {
				var li = doc.CreateElement("li");
				li.InnerText = mod.Id;
				modsNode.AppendChild(li);
			}

			var verNode = doc.CreateElement("version");
			verNode.InnerText = ver;

			var root = doc.CreateElement("ModsConfigData");
			root.AppendChild(verNode);
			root.AppendChild(modsNode);

			doc.AppendChild(root);

			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;
			settings.Indent = true;

			var writer = XmlWriter.Create(path, settings);
			doc.Save(writer);
		}
	}
}
