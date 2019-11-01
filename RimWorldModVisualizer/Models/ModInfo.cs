using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace RimWorldModVisualizer.Models {
	internal class ModInfo {
		public bool Loaded { get; }

		public string Name { get; }
		public string Author { get; }
		public string Description { get; }

		public string Version { get; }

		public string[] SupportedVersions { get; }

		public ModInfo(string path) {
			var aboutPath = Path.Combine(path, "About.xml");
			if (!File.Exists(aboutPath)) return;

			var doc = new XmlDocument();
			doc.Load(aboutPath);
			this.Name = doc.SelectSingleNode("/ModMetaData/name").InnerText;
			this.Author = doc.SelectSingleNode("/ModMetaData/author").InnerText;
			this.Description = new Func<string>(() => {
				var list = doc
					.SelectSingleNode("/ModMetaData/description")
					.InnerText
					.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.ToArray();
				var html = string.Join("<br>", list).Replace("\\n", "<br>");

				html = new Regex("<color=([^>]+)>", RegexOptions.IgnoreCase)
					.Replace(html, "<span style=\"color:$1\">");
				html = new Regex("</color>", RegexOptions.IgnoreCase)
					.Replace(html, "</span>");

				return html;
			})();

			this.SupportedVersions = doc.SelectNodes("/ModMetaData/supportedVersions/li")
				.Cast<XmlNode>()
				.Concat(
					doc.SelectNodes("/ModMetaData/targetVersion")
						.Cast<XmlNode>()
				)
				.Select(x => x.InnerText)
				.ToArray();

			var manifestDir = Path.Combine(path, "Manifest.xml");
			if (File.Exists(manifestDir)) {
				doc.Load(manifestDir);
				this.Version = doc.SelectSingleNode("/Manifest/version").InnerText;
			}

			this.Loaded = true;
		}
	}
}
