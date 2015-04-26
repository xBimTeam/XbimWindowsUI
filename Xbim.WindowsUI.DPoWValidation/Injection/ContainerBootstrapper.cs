using System.Windows.Forms;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.WindowsUI.DPoWValidation.ViewModels;

namespace Xbim.WindowsUI.DPoWValidation.Injection
{
    internal class ContainerBootstrapper
    {
        private static ContainerBootstrapper _instance;

        private IUnityContainer _container;

        private ContainerBootstrapper()
        {
            _container = new UnityContainer();
            _container.RegisterType<ISaveFileSelector, SaveFileSelector>();
            _container.RegisterType<ValidationViewModel, ValidationViewModel>();
            _container.RegisterType<ICanShow, MainWindow>();
        }

        public IUnityContainer Container
        {
            get
            {
                return _container;
            }
        }


        public static ContainerBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ContainerBootstrapper();
                }
                return _instance;
            }
        }
    }
}