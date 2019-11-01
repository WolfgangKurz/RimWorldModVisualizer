using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Livet;
using Livet.Messaging;
using Livet.Messaging.Windows;

namespace RimWorldModVisualizer.Models {
	internal static class ViewModelExtension {
		public static void Transition(this ViewModel source, ViewModel viewModel, Type windowType, bool isOwned = false) {
			var message = new TransitionMessage(
				windowType,
				viewModel,
				isOwned ? TransitionMode.Modal : TransitionMode.NewOrActive,
				isOwned ? "Window.Transition.Child" : "Window.Transition"
			);
			source.Messenger.Raise(message);
		}

		public static void Close(this ViewModel source) {
			source.Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Window.WindowAction"));
		}
	}
}
