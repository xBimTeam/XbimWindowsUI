using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.Commands
{
    internal class Fixer
    {
        public int Fix(IfcStore store)
        {
            return FixFaces(store);
        }

        private int FixFaces(IfcStore store)
        {
            int icnt = 0;
            foreach (var face in store.Instances.OfType<IIfcFace>())
            {
                icnt += FixFace(face);
            }
            return icnt;
        }

        private int FixFace(IIfcFace face)
        {
            int icnt = 0;
            foreach (var ifcFaceBound in face.Bounds)
            {
                icnt += FixFace(ifcFaceBound);
            }
            return icnt;
        }

        private int FixFace(IIfcFaceOuterBound ifcFaceBound)
        {
            return FixFace(ifcFaceBound.Bound);
        }

        private int FixFace(IIfcLoop bound)
        {
            if (bound is IIfcPolyLoop)
            {
                return  FixFace((IIfcPolyLoop)bound);
            }
            return 0;
        }

        private int FixFace(IIfcPolyLoop bound)
        {
            int icnt = 0;
            if (bound.EntityLabel == 119019)
            {
                
            }
            var iEval = 0;
            while (iEval < bound.Polygon.Count)
            {
                var iPrec = iEval - 1;
                if (iPrec < 0)
                    iPrec = bound.Polygon.Count - 1;
                var iNext = iEval + 1;
                if (iNext >= bound.Polygon.Count)
                    iNext = 0;
                
                var needsRemoving =  FixExtensions.AreAligned(
                    bound.Polygon[iEval],
                    bound.Polygon[iPrec],
                    bound.Polygon[iNext]
                    );
                
                if (needsRemoving)
                {
                    using (var txn = bound.Model.BeginTransaction("removing"))
                    {
                        bound.Polygon.RemoveAt(iEval);
                        txn.Commit();
                    }
                    icnt++;
                }
                else
                {
                    iEval++;
                }
            }
            return icnt;
        }

        private int FixFace(IIfcFaceBound ifcFaceBound)
        {
            if (ifcFaceBound is IIfcFaceOuterBound)
            {
                return FixFace((IIfcFaceOuterBound)ifcFaceBound);
            }
            return 0;
        }
    }
}
