using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HandRegcoDemo0.ViewModels;

namespace HandRegcoDemo0.Views
{
    public partial class MainMenuView : Window
    {
        public MainMenuView()
        {
            InitializeComponent();
            if (DataContext is MainMenuViewModel vm)
            {
                vm.RequestOpenLive += OpenLive;
                vm.RequestExit += CloseMenu;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OpenLive()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var cam = new CameraView { DataContext = new CameraViewModel() };
                cam.Show();
                this.Close();
            });
        }


        private void CloseMenu()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => this.Close());
        }

    }
}
