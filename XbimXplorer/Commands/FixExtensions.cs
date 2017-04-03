using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.Commands
{
    public static class FixExtensions
    {
        public static bool LiesBetween(this IIfcCartesianPoint eval, IIfcCartesianPoint prev, IIfcCartesianPoint next)
        {
            var lTot = prev.DistanceFrom(next);
            var lPrev = eval.DistanceFrom(prev);
            var lNext = eval.DistanceFrom(next);

            if (lNext < eval.Model.ModelFactors.Precision)
                return true;

            var delta = lPrev + lNext - lTot;
            // this is brutal
            if (delta < eval.Model.ModelFactors.Precision)
                return true;
            return false;
        }

        public static double DistanceFrom(this IIfcCartesianPoint eval, IIfcCartesianPoint other)
        {
            var tot =
                Math.Pow(other.X - eval.X, 2) +
                Math.Pow(other.Y - eval.Y, 2) +
                Math.Pow(other.Z - eval.Z, 2);
            return Math.Sqrt(tot);
        }
    }
}
