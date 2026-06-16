namespace ChatApp.Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "网上沟通交流系统", Width = 1200, Height = 800 };
    }
}
