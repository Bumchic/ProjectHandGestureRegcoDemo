using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HandRegcoDemo0.ViewModels;
using System;

namespace HandRegcoDemo0.Views
{
    public partial class MainMenuView : Window
    {
        public MainMenuView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Event handler for Live button
        public void OnLiveButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show Camera view
                var cameraView = new CameraView();
                cameraView.Show();

                // Close main menu
                this.Close();
            }
            catch (Exception ex)
            {
                // Handle any errors
                System.Diagnostics.Debug.WriteLine($"Error opening CameraView: {ex.Message}");
            }
        }

        // Event handler for Record button  
        public void OnRecordButtonClick(object? sender, RoutedEventArgs e)
        {
            // Open Camera view in Record mode
            var cameraView = new CameraView();
            // You can set a property or pass parameter to indicate Record mode
            cameraView.Show();

            // Optionally close main menu or hide it
            this.Hide();
        }

        // Event handler for Exit button
        public void OnExitButtonClick(object? sender, RoutedEventArgs e)
        {
            // Close the application
            this.Close();
        }

        // Window event handlers
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            // Initialize any required components when window opens
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            // Cleanup when window is closing
        }
    }
}