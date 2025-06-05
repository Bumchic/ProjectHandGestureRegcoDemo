using System;
using System.Windows.Input;
using Avalonia.Threading;
using HandRegcoDemo0.Services;

namespace HandRegcoDemo0.ViewModels
{
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        public ICommand LiveCommand { get; }
        public ICommand RecordCommand { get; }
        public ICommand ExitCommand { get; }

        public event Action? RequestOpenLive;
        public event Action? RequestExit;

        public MainMenuViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            LiveCommand = new RelayCommand(() =>
            {
                Dispatcher.UIThread.Post(() => _navigationService.OpenCameraView());
            });

            RecordCommand = new RelayCommand(() =>
            {
                // logic cho RecordCommand
            });

            ExitCommand = new RelayCommand(() =>
            {
                Dispatcher.UIThread.Post(() => _navigationService.ExitApplication());
            });
        }
    }

    // RelayCommand đơn giản cho ICommand
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
