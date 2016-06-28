using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Presentation.Extensions;

namespace MyProject
{
    // this function to investigate the warining sometimes received:
    // Warning: Found conflicts between different versions of the same dependent assembly
    [TestClass]
    public class UtilityTest
    {
        [TestMethod]
        public void CanReadFileInfo()
        {
            var a = new XbimAssemblyInfo(typeof(IfcWall));
            var dt = a.CompilationTime;
            Assert.AreNotEqual(dt, DateTime.MinValue);
        }

        [TestMethod]
        public void FindConflictingReferences()
        {
            var assemblies = GetAllAssemblies(@".");
 
            var references = GetReferencesFromAllAssemblies(assemblies);
 
            var groupsOfConflicts = FindReferencesWithTheSameShortNameButDiffererntFullNames(references);
 
            foreach (var group in groupsOfConflicts)
            {
                Debug.Write(String.Format("Possible conflicts for {0}:\r\n", group.Key));
                foreach (var reference in group)
                {
                    Debug.Write(
                        String.Format(
                            "- {0} references {1}\r\n",
                            reference.Assembly.Name.PadRight(35),
                            reference.ReferencedAssembly.FullName));
                }
            }
        }
 
        private IEnumerable<IGrouping<string, Reference>> FindReferencesWithTheSameShortNameButDiffererntFullNames(List<Reference> references)
        {
            return from reference in references
                   group reference by reference.ReferencedAssembly.Name
                       into referenceGroup
                       where referenceGroup.ToList().Select(reference => reference.ReferencedAssembly.FullName).Distinct().Count() > 1
                       select referenceGroup;
        }
 
        private List<Reference> GetReferencesFromAllAssemblies(List<Assembly> assemblies)
        {
            var references = new List<Reference>();
            foreach (var assembly in assemblies)
            {
                foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    references.Add(new Reference
                    {
                        Assembly = assembly.GetName(),
                        ReferencedAssembly = referencedAssembly
                    });
                }
            }
            return references;
        }
 
        private List<Assembly> GetAllAssemblies(string path)
        {
            var ret = new List<Assembly>();
            var files = new List<FileInfo>();
            var directoryToSearch = new DirectoryInfo(path);
            files.AddRange(directoryToSearch.GetFiles("*.dll", SearchOption.AllDirectories));
            files.AddRange(directoryToSearch.GetFiles("*.exe", SearchOption.AllDirectories));
            foreach (var file in files)
            {
                try
                {
                    ret.Add(Assembly.LoadFile(file.FullName));
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                }
            }
            return ret;
        }
 
        private class Reference
        {
            public AssemblyName Assembly { get; set; }
            public AssemblyName ReferencedAssembly { get; set; }
        }
 
    }
}
