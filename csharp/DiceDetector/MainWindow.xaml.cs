using System.Windows;
using DiceDetector.Models;
using DiceDetector.View;
using DiceDetector.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace DiceDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = App.Services.GetRequiredService<MainViewModel>();
            DataContext = vm;
            vm.RequestShowDiceDetail += OnShowDiceDetail;
        }

        private void OnShowDiceDetail(DetectionResult result)
        {
            var window = new DiceDetailWindow(result) { Owner = this };
            window.ShowDialog();
        }
    }
}