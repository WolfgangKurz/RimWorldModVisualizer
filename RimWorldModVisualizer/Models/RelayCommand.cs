using System;
using System.Diagnostics;
using System.Windows.Input;

namespace RimWorldModVisualizer.Models {
	public class RelayCommand : ICommand {
		readonly Action _execute;
		readonly Func<bool> _canExecute;

		public RelayCommand(Action execute) : this(execute, null) {
		}

		public RelayCommand(Action execute, Func<bool> canExecute) {
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

		public void Execute(object parameter) => _execute();

		public Action CurrentAction => _execute;
	}
}
