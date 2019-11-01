using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RimWorldModVisualizer.Models
{
	public class RawMod {
		public string Id { get; set; }
		public string Name { get; set; }
		public Color Color { get; set; } = Colors.Transparent;
		public bool Enabled { get; set; }

		public List<RawMod> Childs { get; set; } = new List<RawMod>();
	}
}
