using System;
using System.Windows.Input;

namespace Winder.Util
{
	/// <summary>
	/// Syntactic sugar for creating commands based on delegates or lambdas.
	/// </summary>
	public class DelegateCommand<TInput> : ICommand
	{
		/// <summary>
		/// Creates an <see cref="ICommand"/> with the specified execute action.
		/// If canExecute is null, then this command can always be executed.
		/// </summary>
		public DelegateCommand(Action<TInput> execute, Func<TInput, bool> canExecute = null) {
			_execute = execute;
			_canExecute = canExecute;
		}

		private readonly Action<TInput> _execute;
		private readonly Func<TInput, bool> _canExecute;

		public event EventHandler CanExecuteChanged;

		bool ICommand.CanExecute(object parameter) {
			var input = (TInput)parameter;
			return _canExecute?.Invoke(input) ?? true;
		}

		void ICommand.Execute(object parameter) {
			var input = (TInput)parameter;
			_execute(input);
		}
	}
}