using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using Xbim.WindowsUI.DPoWValidation.Injection;

namespace Xbim.WindowsUI.DPoWValidation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            var uc = ContainerBootstrapper.Instance.Container;
            var mw = uc.Resolve<MainWindow>();
            mw.Show();
        }
    }
}
