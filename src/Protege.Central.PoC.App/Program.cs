namespace Protege.Central.PoC.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new DashboardForm());
    }
}
