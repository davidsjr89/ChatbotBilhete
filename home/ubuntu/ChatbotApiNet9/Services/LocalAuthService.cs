using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using System.Text;
using System.Text.Json;

namespace ChatbotApiNet9.Services
{
    public class LocalAuthService : IAuthService
    {
        private readonly string _userFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        private List<User> _users = new List<User>();
        private readonly ILogger<LocalAuthService> _logger;

        public LocalAuthService(ILogger<LocalAuthService> logger)
        {
            _logger = logger;
            LoadUsersFromFile();
        }
        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists.", username);
                return false; // Username already exists
            }

            var passwordHash = HashPassword(password);
            var newUser = new User { Username = username, PasswordHash = passwordHash };
            _users.Add(newUser);
            await SaveUsersToFileAsync();
            _logger.LogInformation("User {Username} registered successfully.", username);
            return true;
        }

        public Task<bool> RegisterAsync(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Username} not found.", username);
                return Task.FromResult(false); // User not found
            }

            if(!VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Incorrect password for user {Username}.", username);
                return Task.FromResult(false); // Incorrect password
            }

            _logger.LogInformation("User {Username} logged in successfully.", username);
            return Task.FromResult(true); // Login successful
        }

        private void LoadUsersFromFile()
        {
            try
            {
                if (File.Exists(_userFilePath))
                {
                    var json = File.ReadAllText(_userFilePath);
                    _users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                    _logger.LogInformation("Loaded {UserCount} users from {FilePath}", _users.Count, _userFilePath);
                }
                else
                {
                    _logger.LogInformation("User file not found at {FilePath}, starting with empty list.", _userFilePath);
                    _users = new List<User>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users from file {FilePath}", _userFilePath);
                _users = new List<User>(); // Start fresh if loading fails
            }
        }

        private async Task SaveUsersToFileAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_users, options);
                await File.WriteAllTextAsync(_userFilePath, json);
                _logger.LogInformation("Successfully saved {UserCount} users to {FilePath}", _users.Count, _userFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving users to file {FilePath}", _userFilePath);
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                // Convert byte array to a string representation (e.g., Base64 or Hex)
                // Using Hex for better readability in the JSON file, though Base64 is more compact.
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private bool VerifyPassword(string providedPassword, string storedHash)
        {
            var hashOfProvidedPassword = HashPassword(providedPassword);
            return hashOfProvidedPassword == storedHash;
        }
    }
}