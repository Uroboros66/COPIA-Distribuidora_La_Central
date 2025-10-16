using System.Security.Claims;

public class AuthService
{
    public bool IsAuthenticated { get; private set; }
    public ClaimsPrincipal User { get; private set; } = new ClaimsPrincipal(new ClaimsIdentity());

    public event Action? AuthenticationStateChanged;

    public void Login(string username)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            // Agrega más claims según necesites
        }, "apiauth");

        User = new ClaimsPrincipal(identity);
        IsAuthenticated = true;
        AuthenticationStateChanged?.Invoke();
    }

    public void Logout()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity());
        IsAuthenticated = false;
        AuthenticationStateChanged?.Invoke();
    }
}