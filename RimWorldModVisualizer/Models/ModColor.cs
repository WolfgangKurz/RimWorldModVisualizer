using RimWorldModVisualizer.Enums;
using RimWorldModVisualizer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;

namespace RimWorldModVisualizer.Models {
	internal class ModColor {
		private static Regex RGBARegex = new Regex(@"^RGBA\(([0-9\.]+), ?([0-9\.]+), ?([0-9\.]+), ?([0-9\.]+)\)$", RegexOptions.Compiled);

		public static void SaveModsColor(string path, ModViewModel[] Mods) {
			var modmanager = Path.Combine(path, "Config", "Mod_1507748539_ModManager.xml");
			if (!File.Exists(modmanager)) return;

			File.Copy(
				modmanager,
				Path.Combine(
					Path.GetDirectoryName(modmanager),
					Path.GetFileNameWithoutExtension(modmanager) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(modmanager) + ".bak"
				)
			);

			var doc = new XmlDocument();

			var SettingsBlock = doc.CreateElement("SettingsBlock");
			var ModSettings = doc.CreateElement("ModSettings");
			ModSettings.SetAttribute("Class", "ModManager.ModManagerSettings");

			ModSettings.AppendChild(doc.CreateElement("ModAttributes"));

			var list = doc.CreateElement("ButtonAttributes");

			Action<ModViewModel> explore = null;
			explore = new Action<ModViewModel>(x => {
				if (x.Type != ModType.Directory && x.Color != Colors.Transparent) {

					var el = doc.CreateElement("li");

					var nodeName = doc.CreateElement("Name");
					nodeName.InnerText = x.Name;
					el.AppendChild(nodeName);

					var nodeColor = doc.CreateElement("Color");
					nodeColor.InnerText = $"RGBA({x.Color.ScR}, {x.Color.ScG}, {x.Color.ScB}, {x.Color.ScA})";
					el.AppendChild(nodeColor);

					list.AppendChild(el);
				}

				foreach (var y in x.Childs)
					explore(y);
			});
			foreach (var x in Mods) explore(x);

			ModSettings.AppendChild(list);
			SettingsBlock.AppendChild(ModSettings);
			doc.AppendChild(SettingsBlock);

			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;
			settings.Indent = true;

			var writer = XmlWriter.Create(modmanager, settings);
			doc.Save(writer);
		}

		public static Color GetModColor(string path, string id) {
			var modmanager = Path.Combine(path, "Config", "Mod_1507748539_ModManager.xml");
			if (!File.Exists(modmanager)) return Colors.Transparent;

			var doc = new XmlDocument();
			doc.Load(modmanager);

			var mod = doc.SelectNodes("/SettingsBlock/ModSettings/ModAttributes/li")
				.Cast<XmlNode>()
				.FirstOrDefault(x => x["Identifier"].InnerText == id);
			if(mod != null && mod["Color"] != null)
				return ModColor.ParseRGBA(mod["Color"].InnerText);

			mod = doc.SelectNodes("/SettingsBlock/ModSettings/ButtonAttributes/li")
				.Cast<XmlNode>()
				.FirstOrDefault(x => x["Name"].InnerText == id);
			if(mod != null && mod["Color"] != null)
				return ModColor.ParseRGBA(mod["Color"].InnerText);

			return Colors.Transparent;
		}

		public static Color Parse(string c) {
			if (c == null) return Colors.Transparent;
			var cp = c.Split(',').Select(x => byte.Parse(x)).ToArray();
			return Color.FromRgb(cp[0], cp[1], cp[2]);
		}

		private static Color ParseRGBA(string RGBA) {
			if (!RGBARegex.IsMatch(RGBA)) return Colors.Transparent;

			var matches = RGBARegex.Match(RGBA);
			return Color.FromScRgb(
				float.Parse(matches.Groups[4].Value),
				float.Parse(matches.Groups[1].Value),
				float.Parse(matches.Groups[2].Value),
				float.Parse(matches.Groups[3].Value)
			);
		}

		public static string ToString(Color c) {
			return $"{c.R},{c.G},{c.B}";
		}
	}
}
