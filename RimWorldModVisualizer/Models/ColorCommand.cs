using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;

namespace RimWorldModVisualizer.Models {
	public class ColorCommand : ICommand {
		readonly Action<Color> _execute;
		readonly Func<bool> _canExecute;

		public ColorCommand(Action<Color> execute) : this(execute, null) {
		}

		public ColorCommand(Action<Color> execute, Func<bool> canExecute) {
			_execute = execute ?? throw new ArgumentNullException("execute");
			_canExecute = canExecute;
		}

		[DebuggerStepThrough]
		public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

		public event EventHandler CanExecuteChanged {
			add {
				if (_canExecute != null)
					CommandManager.RequerySuggested += value;
			}
			remove {
				if (_canExecute != null)
					CommandManager.RequerySuggested -= value;
			}
		}

		public void Execute(object parameter) => _execute((Color)parameter);

		public Action<Color> CurrentAction => _execute;
	}
}
