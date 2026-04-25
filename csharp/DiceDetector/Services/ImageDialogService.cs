using DiceDetector.Services.Interfaces;
using Microsoft.Win32;

namespace DiceDetector.Services
{
    public class ImageDialogService : IImageDialogService
    {
        public string? OpenImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Vyber obrázek"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
