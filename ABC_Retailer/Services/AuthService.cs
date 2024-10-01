using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> RegisterAsync(string email, string password, string userName)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetUserByEmailAsync(email);
        if (existingUser != null)
            return false; // User already exists

        // Hash the password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create new user
        var user = new User
        {
            RowKey = email,  // Using email as RowKey
            Email = email,
            UserName = userName,
            PasswordHash = passwordHash
        };

        return await _userRepository.CreateUserAsync(user);
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null)
            return null;

        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (isValid)
        {
            // Add user information to claims
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsAdmin", user.Email.EndsWith(".admin").ToString())  // Add a custom claim for admin users
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return user;
        }

        return null;
    }

}
