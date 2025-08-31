namespace PantryPal.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // No service access here — it's set in MauiProgram
        MainPage = new AppShell();
    }
}
