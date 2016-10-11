using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;


namespace Xbim.Presentation
{
    public static class XbimModelPresentationExtensions
    {
       

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
        /// <param name="instanceCollection"></param>
        /// <param name="matSel">The material select to search.</param>
        /// <param name="deepSearch">
        /// True if the function needs to execute a deeper semantical analysis of the relations (it can expand the query result).
        /// False if a direct analysis of explicit associations with the specific MaterialSet.
        /// </param>
        public static IEnumerable<IIfcDefinitionSelect> GetInstancesOfMaterial(this IEntityCollection instanceCollection, IIfcMaterialSelect matSel, bool deepSearch)
        {
            // Debug.WriteLine(string.Format("GetInstance {0}, {1}", matSel.EntityLabel.ToString(), DeepSearch));
            if (matSel is IIfcMaterial)
            {
                // straight return of objects of all associations
                var assocs = instanceCollection.OfType<IIfcRelAssociatesMaterial>().Where(
                    x => x.RelatingMaterial.EntityLabel ==matSel.EntityLabel
                    );
                foreach (var assoc in assocs)
                {
                    // ... and returns one object at a time in the enumerable
                    foreach (var item in assoc.RelatedObjects)
                    {
                        yield return item;
                    }
                }
            }
            else if (matSel is IIfcMaterialLayer)
            {
                if (!deepSearch)
                {
                    // straight return of objects of all associations
                    var assocs = instanceCollection.OfType<IIfcRelAssociatesMaterial>().Where(
                        x =>x.RelatingMaterial.EntityLabel == matSel.EntityLabel
                        );
                    foreach (var assoc in assocs)
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
                    foreach (var straightMatch in GetInstancesOfMaterial(instanceCollection, ((IIfcMaterialLayer)matSel).ToMaterialLayerSet, false))
                        yield return straightMatch;
                }
            }
            else if (matSel is IIfcMaterialList)
            {
                if (!deepSearch)
                {
                    // straight return of objects of all associations
                    var assocs = instanceCollection.OfType<IIfcRelAssociatesMaterial>().Where(
                        x =>x.RelatingMaterial.EntityLabel == matSel.EntityLabel
                        );
                    foreach (var assoc in assocs)
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
                    var baseMatArray = ((IIfcMaterialList)matSel).Materials.Select(x => x.EntityLabel).ToArray(); // this is the signature.
                    var cmp = EqualityComparer<int>.Default;
                    foreach (var testingMaterialList in instanceCollection.OfType<IIfcMaterialList>())
                    {
                        bool bDoesMatch;
                        if (testingMaterialList.EntityLabel == matSel.EntityLabel)
                        { // no need to compare
                            bDoesMatch = true;
                        }
                        else
                        {
                            var compMatArray = testingMaterialList.Materials.Select(x => x.EntityLabel).ToArray(); // this is the other signature.
                            bDoesMatch = ArraysEqual(baseMatArray, compMatArray, cmp);
                        }
                        if (bDoesMatch)
                        {
                            foreach (var straightMatch in GetInstancesOfMaterial(instanceCollection, testingMaterialList, false))
                                yield return straightMatch;
                        }
                    }
                }
            }
            else if (matSel is IIfcMaterialLayerSet)
            {
                // no difference in deep mode available for this type

                // given a material layerset ...
                // ... search for all its usages modes ...
                var lsUsages = instanceCollection.OfType<IIfcMaterialLayerSetUsage>().Where(
                    x => x.ForLayerSet.EntityLabel ==((IIfcMaterialLayerSet)matSel).EntityLabel
                    );
                foreach (var lsUsage in lsUsages)
                {
                    // ... then for each usage mode, searches the relations with objects ...
                    foreach (var item in GetInstancesOfMaterial(instanceCollection, lsUsage, false))
                    {
                        yield return item;
                    }
                }
            }
            else if (matSel is IIfcMaterialLayerSetUsage)
            {
                if (deepSearch)
                {
                    // identify the underlying material layer set and return all its usages.
                    foreach (var item in instanceCollection.GetInstancesOfMaterial(((IIfcMaterialLayerSetUsage)matSel).ForLayerSet, false))
                    {
                        yield return item;
                    }
                }
                else
                {
                    // straight return of objects of all associations
                    var assocs = instanceCollection.OfType<IIfcRelAssociatesMaterial>().Where(
                        x => x.RelatingMaterial.EntityLabel == matSel.EntityLabel
                        );
                    foreach (var assoc in assocs)
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
