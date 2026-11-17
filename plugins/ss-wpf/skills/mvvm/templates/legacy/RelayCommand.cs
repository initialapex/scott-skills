// Minimal command types for toolchains that cannot use CommunityToolkit.Mvvm
// (Visual Studio 2012, .NET Framework 4.5, C# 5).
// Names match CommunityToolkit.Mvvm.Input. Like the toolkit, CanExecuteChanged is raised
// only by NotifyCanExecuteChanged(); it does not hook CommandManager.RequerySuggested.
// Replace the namespace with <RootNamespace>.Mvvm when copying into a project.

using System;
using System.Windows.Input;

namespace MyApp.Mvvm
{
    /// <summary>
    /// A command whose CanExecute state can be refreshed on demand.
    /// </summary>
    public interface IRelayCommand : ICommand
    {
        /// <summary>
        /// Raises CanExecuteChanged so that bound controls query CanExecute again.
        /// </summary>
        void NotifyCanExecuteChanged();
    }

    /// <summary>
    /// A command that runs a delegate and takes no parameter.
    /// </summary>
    public sealed class RelayCommand : IRelayCommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute();
            }
        }

        public void NotifyCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// A command that runs a delegate with a typed parameter (CommandParameter in XAML).
    /// </summary>
    public sealed class RelayCommand<T> : IRelayCommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            T argument;
            if (!TryGetArgument(parameter, out argument))
            {
                return false;
            }

            return _canExecute == null || _canExecute(argument);
        }

        public void Execute(object parameter)
        {
            T argument;
            if (!TryGetArgument(parameter, out argument))
            {
                return;
            }

            if (_canExecute == null || _canExecute(argument))
            {
                _execute(argument);
            }
        }

        public void NotifyCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        // A null parameter is accepted only when T can hold null (reference or Nullable<T>).
        private static bool TryGetArgument(object parameter, out T argument)
        {
            if (parameter == null)
            {
                argument = default(T);
                return default(T) == null;
            }

            if (parameter is T)
            {
                argument = (T)parameter;
                return true;
            }

            argument = default(T);
            return false;
        }
    }
}
