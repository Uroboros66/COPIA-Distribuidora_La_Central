namespace Distribuidora_La_Central
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Distribuidora_La_Central" };
        }
    }
}
