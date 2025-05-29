namespace ChatbotApiNet9.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(string username, string password);
        Task<bool> LoginAsync(string username, string password);
    }
}
