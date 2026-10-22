namespace Ducks.Admin.Desktop.Stores;

public class UserSessionStore
{
    public string? JwtToken { get; private set; }
    public string? AdminEmail { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(JwtToken);

    public event Action? StateChanged;

    public void SetSession(string token, string email)
    {
        JwtToken = token;
        AdminEmail = email;
        StateChanged?.Invoke();
    }

    public void ClearSession()
    {
        JwtToken = null;
        AdminEmail = null;
        StateChanged?.Invoke();
    }
}