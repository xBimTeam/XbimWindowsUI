using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.COBieLite.Validation
{
    internal class CompoundClassificationString
    {
        private List<CompoundClassificationPart> _parts = new List<CompoundClassificationPart>();
        private string _compoundClassification;

        public CompoundClassificationString(string compoundClassification)
        {
            _compoundClassification = compoundClassification;
            if (string.IsNullOrEmpty(compoundClassification))
                return;
            foreach (var prt in _compoundClassification.Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries))
            {
                _parts.Add(new CompoundClassificationPart(prt));
            }
        }

        public IEnumerable<T> FindMatches<T>(FacilityType facility) where T : class
        {
            if (typeof (T) == typeof (AssetTypeInfoType))
            {
                var matchTypes = facility.AssetTypes.AssetType.Where(
                    x => Matches(x.AssetTypeCategory)
                    );
                foreach (var mtch in matchTypes)
                {
                    yield return mtch as T;
                }
            }

        }

        internal bool Matches(string otherClassificationString)
        {
            if (string.IsNullOrEmpty(otherClassificationString))
                return false;
            if (CompoundClassificationString.IsCompound(otherClassificationString))
            {
                return false;
            }
            else
            {
                return _parts.Any(part => otherClassificationString.Contains(part.ClassificationValue));
            }
        }

        public static bool IsCompound(string classificationString)
        {
            return false;
        }
    }

    internal class CompoundClassificationPart
    {
        private string _part;

        public CompoundClassificationPart(string part)
        {
            _part = part;

            if (String.IsNullOrEmpty(_part))
                return;
            var split = _part.Split(new[] {":"}, StringSplitOptions.None);
            if (split.Length == 0)
                return;
            ClassificationName = split[0];
            if (split.Length > 1)
            {
                ClassificationValue = split[1];
            }
            if (split.Length > 2)
            {
                UserDefined = split[2];
            }
        }

        public string ClassificationName = "";
        public string ClassificationValue = "";
        public string UserDefined = "";

        
    }
}
