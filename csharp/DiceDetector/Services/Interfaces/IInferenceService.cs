using DiceDetector.Models;

namespace DiceDetector.Services.Interfaces
{
    public interface IInferenceService
    {
        Task<InferenceResult> RunAsync(string imagePath);
    }
}
