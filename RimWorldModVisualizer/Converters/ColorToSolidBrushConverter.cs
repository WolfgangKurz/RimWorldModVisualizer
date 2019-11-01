using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RimWorldModVisualizer.Converters {
	public class ColorToSolidBrushConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var color = value is Color ? (Color)value : Colors.Transparent;
			return new SolidColorBrush(color);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return Binding.DoNothing;
		}
	}
}
