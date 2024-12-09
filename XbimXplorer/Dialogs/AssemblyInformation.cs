// code from http://stackoverflow.com/questions/383686/how-do-you-loop-through-currently-loaded-assemblies/26300241#26300241
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using XbimXplorer.PluginSystem;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    ///     Intent: Get referenced assemblies, either recursively or flat. Not thread safe, if running in a multi
    ///     threaded environment must use locks.
    /// </summary>
    internal static class GetReferencedAssemblies
    {
        

        public class MissingAssembly
        {
            public MissingAssembly(string missingAssemblyName, string missingAssemblyNameParent)
            {
                MissingAssemblyName = missingAssemblyName;
                MissingAssemblyNameParent = missingAssemblyNameParent;
            }

            public string MissingAssemblyName { get; set; }
            public string MissingAssemblyNameParent { get; set; }
        }

        private static Dictionary<string, Assembly> _dependentAssemblyList;
        private static List<MissingAssembly> _missingAssemblyList;
		private static MetadataLoadContext _assemblyLoadContext;


		public static void Initialise(Assembly assembly)
		{
			_dependentAssemblyList = new Dictionary<string, Assembly>();
			_missingAssemblyList = new List<MissingAssembly>();
			if (_assemblyLoadContext != null)
			{
				_assemblyLoadContext.Dispose();
			}
			_assemblyLoadContext = CreateAssemblyLoadContext(assembly);

		}

        /// <summary>
        ///     Intent: Get assemblies referenced by entry assembly. Not recursive.
        /// </summary>
        public static List<string> MyGetReferencedAssembliesFlat(this Type type)
        {
            var results = type.Assembly.GetReferencedAssemblies();
            return results.Select(o => o.FullName).OrderBy(o => o).ToList();
        }

        /// <summary>
        ///     Intent: Get assemblies currently dependent on entry assembly. Recursive.
        /// </summary>
        public static Dictionary<string, Assembly> MyGetReferencedAssembliesRecursive(this Assembly assembly)
        {
            Initialise(assembly);
			
			InternalGetDependentAssembliesRecursive(assembly, _assemblyLoadContext);
			

           // TOOD: We used to exclude assemblies in the GAC. That's a deprecated concept in netcore. Review?

            return _dependentAssemblyList;
        }

        /// <summary>
        ///     Intent: Get missing assemblies.
        /// </summary>
        public static List<MissingAssembly> MyGetMissingAssembliesRecursive(this Assembly assembly)
        {
			Initialise(assembly);
			InternalGetDependentAssembliesRecursive(assembly, _assemblyLoadContext);

			return _missingAssemblyList;
        }

		private static MetadataLoadContext CreateAssemblyLoadContext(Assembly assembly)
		{
			var appDir = Path.GetDirectoryName(assembly.Location);
			var pluginDir = PluginManagement.GetPluginsDirectory().FullName;
			string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
			string[] appAssemblies = Directory.GetFiles(appDir, "*.dll");
			// Create the list of assembly paths consisting of runtime assemblies and the input file.
			var paths = new List<string>(appAssemblies.Concat(runtimeAssemblies));

			var resolver = new PathAssemblyResolver(paths);
			// a netcore replacement for Assembly.ReflectionOnlyLoad
			var mlc = new MetadataLoadContext(resolver);
			return mlc;
		}

		/// <summary>
		///     Intent: Internal recursive class to get all dependent assemblies, and all dependent assemblies of
		///     dependent assemblies, etc.
		/// </summary>
		private static void InternalGetDependentAssembliesRecursive(Assembly assembly, MetadataLoadContext context)
        {
           
            //Load assemblies with newest versions first. Omitting the ordering results in false positives on
            // _missingAssemblyList.
            var referencedAssemblies = assembly.GetReferencedAssemblies()
                .OrderByDescending(o => o.Version);


            foreach (var r in referencedAssemblies)
            {
                if (String.IsNullOrEmpty(assembly.FullName))
                {
                    continue;
                }

                if (_dependentAssemblyList.ContainsKey(r.FullName.MyToName()) == false)
                {
                    try
                    {
                        var a = context.LoadFromAssemblyName(r.FullName);
                        _dependentAssemblyList[a.FullName.MyToName()] = a;
                        InternalGetDependentAssembliesRecursive(a, context);
                    }
                    catch
                    {
                        _missingAssemblyList.Add(new MissingAssembly(r.FullName.Split(',')[0], assembly.FullName.MyToName()));
                    }
                }
                
            }

            
        }

        private static string MyToName(this string fullName)
        {
            return fullName.Split(',')[0];
        }
    }
}
