using DiceDetector.Services;
using DiceDetector.Services.Interfaces;
using DiceDetector.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DiceDetector
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IImageDialogService, ImageDialogService>();
            services.AddSingleton<IPreprocessingService, PreprocessingService>();
            services.AddSingleton<IInferenceService, OnnxInferenceService>();
            services.AddSingleton<IOverlayRenderer, OverlayRenderer>();
            services.AddSingleton<ICameraService, CameraService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<MainViewModel>();
        }
    }
}

