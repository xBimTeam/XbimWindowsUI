#nullable enable
using System;
using Xbim.Common.Configuration;
using Xbim.Geometry.Abstractions;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc4.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Xbim.Presentation
{
	internal class Infrastructure
	{
		internal static IXbimGeometryEngine? GetGeometryEngine(Common.IModel? model)
		{
			if (model is null)
				return null;
			var geomFactory = XbimServices.Current.ServiceProvider.GetService<IXbimGeometryServicesFactory>();
			var loggerFactory = XbimServices.Current.GetLoggerFactory();
			if (geomFactory == null)
			{
				throw new InvalidOperationException("An implementation of IXbimGeometryServicesFactory could not be found.\n\nTo fix this add the following before calling any xbim functionality:\n\n XbimServices.Current.ConfigureServices(opt => opt.AddXbimToolkit(conf => conf.AddGeometryServices()));");
			}
			var _engine = geomFactory.CreateGeometryEngine(XGeometryEngineVersion.V6, model, loggerFactory);
			return _engine;
		}
	}
}
#nullable restore