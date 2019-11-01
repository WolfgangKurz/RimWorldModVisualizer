using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Livet;

using RimWorldModVisualizer.Enums;
using RimWorldModVisualizer.Models;

namespace RimWorldModVisualizer.ViewModels {
	public class ModViewModel : ViewModel, ICloneable {
		public string Directory { get; }

		public string Preview {
			get {
				if (this.Type == ModType.Directory) return null;

				var path = Path.Combine(this.Directory, "About", "Preview.png");
				if (File.Exists(path)) return path;
				return null;
			}
		}
		public bool PreviewAvailable => this.Preview != null;

		public string Id { get; }

		public string Name { get; }
		public string Author { get; }
		public string Description { get; }

		public string[] SupportedVersions { get; }
		public string SupportedVersionsText => this.SupportedVersions == null ? "?" : string.Join(", ", this.SupportedVersions);

		public string Version { get; }

		public ModType Type { get; }

		private Color _Color { get; set; } = Colors.Transparent;
		public Color Color {
			get { return this._Color; }
			set {
				if (this._Color != value) {
					this._Color = value;
					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(this.HexColor));
					this.RaisePropertyChanged(nameof(this.HasColor));
				}
			}
		}

		public bool HasColor => this.Color != Colors.Transparent;
		public string HexColor => string.Format("#{3:X2}{0:X2}{1:X2}{2:X2}", this.Color.R, this.Color.G, this.Color.B, this.Color.A);

		private bool _Enabled { get; set; }
		public bool Enabled {
			get { return this._Enabled; }
			set {
				if (this._Enabled != value) {
					this._Enabled = value;
					this.RaisePropertyChanged();
				}
			}
		}

		private bool _IsMatch { get; set; } = true;
		public bool IsMatch {
			get { return this._IsMatch; }
			set {
				if (this._IsMatch != value) {
					this._IsMatch = value;
					this.RaisePropertyChanged();
				}
			}
		}

		private ObservableCollection<ModViewModel> _Childs { get; set; } = new ObservableCollection<ModViewModel>();
		public ObservableCollection<ModViewModel> Childs {
			get { return this._Childs; }
			set {
				if (this._Childs != value) {
					this._Childs = value;
					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(this.HasItems));
				}
			}
		}

		public bool HasItems => this.Childs?.Count > 0;

		private bool _Expanded { get; set; } = true;
		public bool Expanded {
			get { return this._Expanded; }
			set {
				if (this._Expanded != value) {
					this._Expanded = value;
					this.RaisePropertyChanged();
				}
			}
		}

		public ModViewModel(string Id, string dir, ModType type) {
			var info = new ModInfo(Path.Combine(dir, "About"));
			if (!info.Loaded) return;

			this.Directory = dir;

			this.Id = Id;

			this.Name = info.Name;
			this.Author = info.Author;
			this.Description = info.Description;

			this.SupportedVersions = info.SupportedVersions;
			this.Version = info.Version;

			this.Type = type;
		}
		public ModViewModel(string Id, string Name) {
			this.Id = Id;
			this.Name = Name;
			this.Type = ModType.Directory;
		}

		public void ToggleExpanded() {
			if (this.Type != ModType.Directory) return; // Only directory
			this.Expanded = !this.Expanded;
		}

		public object Clone() => this.Clone(false);

		public object Clone(bool SurfaceOnly) {
			var clone = new ModViewModel(this.Id, this.Directory, this.Type);
			clone.Enabled = this.Enabled;
			clone.Expanded = this.Expanded;
			clone.Color = this.Color;

			if (!SurfaceOnly)
				foreach (var c in this.Childs)
					clone.Childs.Add((ModViewModel)c.Clone());
			return clone;
		}
	}
}
