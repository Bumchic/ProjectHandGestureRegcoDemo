using HandRegcoDemo0.ViewModels;
using HandRegcoDemo0.Views;
using System;
using Avalonia.Controls;

namespace HandRegcoDemo0.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Window _mainMenuWindow;

        public NavigationService(MainMenuView mainMenuView)
        {
            _mainMenuWindow = mainMenuView;
        }

        public void ExitApplication()
        {
            _mainMenuWindow?.Close();
        }

        public void OpenCameraView()
        {
            var cameraView = new CameraView
            {
                DataContext = new CameraViewModel()
            };

            cameraView.Show();

            
        }
    }
}
