using System.Threading.Tasks;

public interface IAuthService
{
    Task<bool> RegisterAsync(string email, string password, string fullName);
    Task<User?> LoginAsync(string email, string password);
}
