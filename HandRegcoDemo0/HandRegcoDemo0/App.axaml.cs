using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using HandRegcoDemo0.ViewModels;
using HandRegcoDemo0.Views;
using HandRegcoDemo0.Services;
using Avalonia.Themes.Fluent;
namespace HandRegcoDemo0;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Thêm FluentAvaloniaTheme trước, để theme có hiệu lực khi tạo cửa sổ
        var fluentTheme = new FluentAvaloniaTheme();
        Styles.Insert(0, fluentTheme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainMenuView = new MainMenuView();
            var navigationService = new NavigationService(mainMenuView);
            var mainMenuViewModel = new MainMenuViewModel(navigationService);
            mainMenuView.DataContext = mainMenuViewModel;

            desktop.MainWindow = mainMenuView;
        }

        base.OnFrameworkInitializationCompleted();
    }



    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
