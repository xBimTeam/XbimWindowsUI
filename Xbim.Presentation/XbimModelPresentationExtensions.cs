using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.Presentation
{
    public static class XbimModelPresentationExtensions
    {
        public static XbimMaterialProvider GetRenderMaterial(this XbimModel model, XbimSurfaceStyle style)
        {
            if (style.IsIfcSurfaceStyle)
            {
                IfcSurfaceStyle surfaceStyle = style.IfcSurfaceStyle(model);
                if (surfaceStyle != null)
                    return new XbimMaterialProvider(surfaceStyle.ToMaterial());

            }
            //nothing specific go for default of type
            return ModelDataProvider.GetDefaultMaterial(style.IfcType);
        }

        // adapted from http://stackoverflow.com/questions/713341/comparing-arrays-in-c-sharp
        //
        private static bool ArraysEqual<T>(T[] a1, T[] a2, EqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the list of Object in a IfcRelAssociatesMaterial with the specified material select.
        /// </summary>
        /// <param name="matSel">The material select to search.</param>
        /// <param name="DeepSearch">
        /// True if the function needs to execute a deeper semantical analysis of the relations (it can expand the query result).
        /// False if a direct analysis of explicit associations with the specific MaterialSet.
        /// </param>
        public static IEnumerable<IfcRoot> GetInstancesOfMaterial(this IXbimInstanceCollection InstanceCollection, IfcMaterialSelect matSel, bool DeepSearch)
        {
            // Debug.WriteLine(string.Format("GetInstance {0}, {1}", matSel.EntityLabel.ToString(), DeepSearch));
            if (matSel is IfcMaterial)
            {
                // straight return of objects of all associations
                var Assocs = InstanceCollection.OfType<IfcRelAssociatesMaterial>().Where(
                    x => x.RelatingMaterial.EntityLabel ==matSel.EntityLabel
                    );
                foreach (var assoc in Assocs)
                {
                    // ... and returns one object at a time in the enumerable
                    foreach (var item in assoc.RelatedObjects)
                    {
                        yield return item;
                    }
                }
            }
            else if (matSel is IfcMaterialLayer)
            {
                if (!DeepSearch)
                {
                    // straight return of objects of all associations
                    var Assocs = InstanceCollection.OfType<IfcRelAssociatesMaterial>().Where(
                        x =>x.RelatingMaterial.EntityLabel == matSel.EntityLabel
                        );
                    foreach (var assoc in Assocs)
                    {
                        // ... and returns one object at a time in the enumerable
                        foreach (var item in assoc.RelatedObjects)
                        {
                            yield return item;
                        }
                    }
                }
                else // this is deep search
                {
                    foreach (var StraightMatch in GetInstancesOfMaterial(InstanceCollection, ((IfcMaterialLayer)matSel).ToMaterialLayerSet, false))
                        yield return StraightMatch;
                }
            }
            else if (matSel is IfcMaterialList)
            {
                if (!DeepSearch)
                {
                    // straight return of objects of all associations
                    var Assocs = InstanceCollection.OfType<IfcRelAssociatesMaterial>().Where(
                        x =>x.RelatingMaterial.EntityLabel == matSel.EntityLabel
                        );
                    foreach (var assoc in Assocs)
                    {
                        // ... and returns one object at a time in the enumerable
                        foreach (var item in assoc.RelatedObjects)
                        {
                            yield return item;
                        }
                    }
                }
                else // this is deep search
                {
                    // a problem with this is that some exporters produce multiple IfcMaterialList that 
                    // they share the same underlying set of Materials, so we are looking for a signature of the underlying materials.
                    // 
                    var BaseMatArray = ((IfcMaterialList)matSel).Materials.Select(x => x.EntityLabel).ToArray(); // this is the signature.
                    var cmp = EqualityComparer<int>.Default;
                    foreach (var testingMaterialList in InstanceCollection.OfType<IfcMaterialList>())
                    {
                        bool bDoesMatch = false;
                        if (testingMaterialList.EntityLabel == matSel.EntityLabel)
                        { // no need to compare
                            bDoesMatch = true;
                        }
                        else
                        {
                            var CompMatArray = ((IfcMaterialList)testingMaterialList).Materials.Select(x => x.EntityLabel).ToArray(); // this is the other signature.
                            bDoesMatch = ArraysEqual<int>(BaseMatArray, CompMatArray, cmp);
                        }
                        if (bDoesMatch)
                        {
                            foreach (var StraightMatch in GetInstancesOfMaterial(InstanceCollection, testingMaterialList, false))
                                yield return StraightMatch;
                        }
                    }
                }
            }
            else if (matSel is IfcMaterialLayerSet)
            {
                // no difference in deep mode available for this type

                // given a material layerset ...
                // ... search for all its usages modes ...
                var lsUsages = InstanceCollection.OfType<IfcMaterialLayerSetUsage>().Where(
                    x => x.ForLayerSet.EntityLabel ==((IfcMaterialLayerSet)matSel).EntityLabel
                    );
                foreach (var lsUsage in lsUsages)
                {
                    // ... then for each usage mode, searches the relations with objects ...
                    foreach (var item in GetInstancesOfMaterial(InstanceCollection, lsUsage, false))
                    {
                        yield return item;
                    }
                }
            }
            else if (matSel is IfcMaterialLayerSetUsage)
            {
                if (DeepSearch)
                {
                    // identify the underlying material layer set and return all its usages.
                    foreach (var item in InstanceCollection.GetInstancesOfMaterial(((IfcMaterialLayerSetUsage)matSel).ForLayerSet, false))
                    {
                        yield return item;
                    }
                }
                else
                {
                    // straight return of objects of all associations
                    var Assocs = InstanceCollection.OfType<IfcRelAssociatesMaterial>().Where(
                        x => x.RelatingMaterial.EntityLabel == matSel.EntityLabel
                        );
                    foreach (var assoc in Assocs)
                    {
                        // ... and returns one object at a time in the enumerable
                        foreach (var item in assoc.RelatedObjects)
                        {
                            yield return item;
                        }
                    }
                }
            }
            else
            {
                Debugger.Break();
                Debug.WriteLine("Unexpected case");
            }
        }
    }
}
