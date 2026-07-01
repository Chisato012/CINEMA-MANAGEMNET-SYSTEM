using TestModel.Models;

namespace TestModel.Services
{
    public interface ILmStudioService
    {
        Task<ChatResponse> AskAsync(string userMessage);
    }
}
