using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Xbim.Presentation.Extensions
{
    public class XbimAssemblyInfo
    {
        private readonly Assembly _assembly;

        public XbimAssemblyInfo(Assembly assembly)
        {
            _assembly = assembly;
        }

        public XbimAssemblyInfo(Type type)
        {
            _assembly = Assembly.GetAssembly(type);
            // PluginVersion.Text = string.Format("Assembly Version: {0}", assembly.GetName().Version);
            // var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            // PluginVersion.Text += string.Format("\r\nFile Version: {0}", fvi.FileVersion);
        }

        public Version AssemblyVersion => _assembly.GetName().Version;

        public string FileVersion
        {
            get
            {
                var fvi = FileVersionInfo.GetVersionInfo(_assembly.Location);
                return fvi.FileVersion;
            }
        }

        public DateTime CompilationTime
        {
            get
            {
                var ret = DateTime.MinValue;
                var versArray = FileVersion.Split(new[] {"."}, StringSplitOptions.None);

                if (versArray.Length != 4)
                    return DateTime.MinValue;
                try
                {
                    var dateYear = 2000 + Convert.ToInt32(versArray[2].Substring(0, 2));
                    var dateMonth = Convert.ToInt32(versArray[2].Substring(2, 2));
                    var dateday = Convert.ToInt32(versArray[3].Substring(0, 2));
                    var dateMinuteOfDay = Convert.ToInt32(versArray[3].Substring(2)) * 2;
                    var dateMinute = dateMinuteOfDay % 60;
                    var dateHour = dateMinuteOfDay / 60;

                    return new DateTime(dateYear, dateMonth, dateday, dateHour, dateMinute, 0);
                }
                catch (Exception)
                {
                    return ret;
                }
            }
        }
    }
}
