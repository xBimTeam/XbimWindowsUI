#nullable enable
using System;
using Xbim.Common.Configuration;
using Xbim.Geometry.Abstractions;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc4.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xbim.Common.Metadata;
using Xbim.Ifc2x3;
using Xbim.Ifc4;
using Xbim.Ifc4x3;

namespace Xbim.Presentation
{
	public class Infrastructure
	{
		public static IXbimGeometryEngine? GetGeometryEngine(Common.IModel? model)
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

		public static Dictionary<string, ExpressMetaData> SchemaMetadatas => new Dictionary<string, ExpressMetaData>
		{
			{"ifc2x3", ExpressMetaData.GetMetadata(new EntityFactoryIfc2x3())},
			{"ifc4", ExpressMetaData.GetMetadata(new EntityFactoryIfc4())},
			{"ifc4x3", ExpressMetaData.GetMetadata(new EntityFactoryIfc4x3Add2())}
		};
	}
}
#nullable restore