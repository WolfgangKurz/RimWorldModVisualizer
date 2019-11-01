using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using RimWorldModVisualizer.Models;
using RimWorldModVisualizer.Views;
using RimWorldModVisualizer.ViewModels;

namespace RimWorldModVisualizer {
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	sealed partial class Application {
		public MainWindowViewModel MainWindowViewModel { get; private set; }

		protected override void OnStartup(StartupEventArgs e) {
			this.MainWindowViewModel = new MainWindowViewModel();
			this.MainWindow = new MainWindow { DataContext = this.MainWindowViewModel };
			this.MainWindow.Show();

			base.OnStartup(e);
		}
	}
}
