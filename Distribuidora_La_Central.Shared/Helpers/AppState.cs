public class AppState
{
    public bool IsLoggedIn { get; private set; }
    public string Usuario { get; private set; }
    public string Rol { get; private set; }

    public event Action OnChange;

    public void Login(string usuario, string rol)
    {
        Usuario = usuario;
        Rol = rol;
        IsLoggedIn = true;
        NotifyStateChanged();
    }

    public void Logout()
    {
        Usuario = null;
        Rol = null;
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
