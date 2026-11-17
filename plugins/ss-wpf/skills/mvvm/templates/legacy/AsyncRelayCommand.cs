// Minimal async command for toolchains that cannot use CommunityToolkit.Mvvm
// (Visual Studio 2012, .NET Framework 4.5, C# 5).
// Names match CommunityToolkit.Mvvm.Input.AsyncRelayCommand: while the task runs,
// CanExecute is false (no concurrent runs), IsRunning is true, and Cancel() signals the
// CancellationToken passed to a cancelable delegate.
// As in the toolkit, an exception from the task is rethrown on the UI thread when the
// command runs from a binding, so catch expected exceptions in the command's method.
// Replace the namespace with <RootNamespace>.Mvvm when copying into a project.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Mvvm
{
    /// <summary>
    /// A command that runs an asynchronous operation and reports its state.
    /// </summary>
    public interface IAsyncRelayCommand : IRelayCommand, INotifyPropertyChanged
    {
        Task ExecutionTask { get; }

        bool IsRunning { get; }

        bool CanBeCanceled { get; }

        bool IsCancellationRequested { get; }

        Task ExecuteAsync(object parameter);

        void Cancel();
    }

    /// <summary>
    /// A command that runs a Task-returning delegate, optionally with cancellation.
    /// </summary>
    public sealed class AsyncRelayCommand : IAsyncRelayCommand
    {
        private readonly Func<CancellationToken, Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly bool _supportsCancellation;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _executionTask;

        public AsyncRelayCommand(Func<Task> execute)
            : this(execute, null)
        {
        }

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = delegate(CancellationToken token) { return execute(); };
            _canExecute = canExecute;
            _supportsCancellation = false;
        }

        public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute)
            : this(cancelableExecute, null)
        {
        }

        public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute)
        {
            if (cancelableExecute == null)
            {
                throw new ArgumentNullException("cancelableExecute");
            }

            _execute = cancelableExecute;
            _canExecute = canExecute;
            _supportsCancellation = true;
        }

        public event EventHandler CanExecuteChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public Task ExecutionTask
        {
            get { return _executionTask; }
        }

        public bool IsRunning
        {
            get { return _executionTask != null && !_executionTask.IsCompleted; }
        }

        public bool CanBeCanceled
        {
            get { return _supportsCancellation && IsRunning; }
        }

        public bool IsCancellationRequested
        {
            get { return _cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested; }
        }

        public bool CanExecute(object parameter)
        {
            return !IsRunning && (_canExecute == null || _canExecute());
        }

        // ICommand entry point used by bindings. async void is required by ICommand;
        // awaiting here rethrows a failed task on the UI thread instead of losing it.
        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Starts the operation and returns its task. Tests await this method.
        /// </summary>
        public Task ExecuteAsync(object parameter)
        {
            CancellationToken token = CancellationToken.None;
            if (_supportsCancellation)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                }

                _cancellationTokenSource = new CancellationTokenSource();
                token = _cancellationTokenSource.Token;
            }

            Task task = _execute(token);
            _executionTask = task;
            RaiseStateChanged();
            ObserveCompletion(task);
            return task;
        }

        public void Cancel()
        {
            if (_cancellationTokenSource != null && CanBeCanceled)
            {
                _cancellationTokenSource.Cancel();
                OnPropertyChanged("IsCancellationRequested");
                OnPropertyChanged("CanBeCanceled");
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

        // Refreshes the state once the task ends. The task's exception is not handled
        // here: Execute or the caller of ExecuteAsync observes it.
        private async void ObserveCompletion(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception)
            {
                // Observed by the awaiting caller; only the state refresh matters here.
            }

            if (ReferenceEquals(task, _executionTask))
            {
                RaiseStateChanged();
            }
        }

        private void RaiseStateChanged()
        {
            OnPropertyChanged("ExecutionTask");
            OnPropertyChanged("IsRunning");
            OnPropertyChanged("CanBeCanceled");
            OnPropertyChanged("IsCancellationRequested");
            NotifyCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
