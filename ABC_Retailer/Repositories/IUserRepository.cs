using System.Threading.Tasks;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> CreateUserAsync(User user);
}
