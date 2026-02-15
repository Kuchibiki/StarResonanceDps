using System.Windows;
using StarResonanceDpsAnalysis.WPF.Config;

namespace StarResonanceDpsAnalysis.WPF.Services;

public class ApplicationControlService(IConfigManager configManager) : IApplicationControlService
{
    public void Shutdown()
    {
        Application.Current.Shutdown();
    }
}