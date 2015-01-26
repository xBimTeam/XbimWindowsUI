using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.Model;
using Xbim.COBieLite;

namespace Validation.ValidationObjects
{
    class CobieAssetTypeRequirement : IValidationRequirement
    {
        
        private Xbim.COBieLite.AssetTypeInfoType CRequirement;

        public CobieAssetTypeRequirement(Xbim.COBieLite.AssetTypeInfoType assetTypeInfoType)
        {
            this.CRequirement = assetTypeInfoType;
            ReqPropNames = new HashSet<string>();

            if (CRequirement.Assets.Asset != null)
            {
                var attributeCollectionType = CRequirement.Assets.Asset[0].AssetAttributes;
                if (attributeCollectionType != null)
                    foreach (var reqAttribute in attributeCollectionType.Attribute)
                    {
                        if (
                            reqAttribute.AttributeCategory == "Required"
                            ||
                            reqAttribute.AttributeCategory == "Submitted"
                            )
                            ReqPropNames.Add(reqAttribute.propertySetName + "." + reqAttribute.AttributeName);
                    }
            }
        }

        private HashSet<string> ReqPropNames = new HashSet<string>();

        public string Name
        {
            get
            {
                if (CRequirement == null)
                    return @"Undefined";
                return CRequirement.AssetTypeCategory;
            }
        }

        internal IEnumerable<ReportResult> Validate(Xbim.COBieLite.FacilityType modelFacilityType, int filterLabel = -1, StringBuilder reporter = null)
        {
            // attributes
            var matchTypes =
                modelFacilityType.AssetTypes.AssetType.Where(
                    x => x.AssetTypeCategory == CRequirement.AssetTypeCategory
                    );
            foreach (var typeToTest in matchTypes)
            {
                IEnumerable<AssetInfoType> assetList = null;

                assetList = (filterLabel == -1) 
                    ? typeToTest.Assets.Asset 
                    : typeToTest.Assets.Asset.Where(x => x.externalID == filterLabel.ToString(CultureInfo.InvariantCulture));

                foreach (var modelAsset in assetList)
                {
                    if (reporter != null)
                        reporter.AppendFormat("===\r\nReporting {0} (#{1})\r\n", modelAsset.AssetName, modelAsset.externalID);

                    HashSet<string> found = new HashSet<string>();

                    int matchingAttributes = 0;
                    foreach (var attribute in modelAsset.AssetAttributes.Attribute)
                    {
                        string fullName = attribute.propertySetName + "." + attribute.AttributeName;
                        if (ReqPropNames.Contains(fullName))
                        {
                            found.Add(fullName);
                            matchingAttributes++;
                        }
                    }

                    // reporting
                    bool pass = (matchingAttributes == ReqPropNames.Count);
                    if (pass)
                    {
                        if (reporter != null)
                            reporter.AppendFormat("All attributes found ({0})\r\n", matchingAttributes);
                    }
                    else if (reporter != null)
                    {
                        foreach (var reqPropName in ReqPropNames)
                        {
                            if (!found.Contains(reqPropName))
                                reporter.AppendFormat("Missing attribute: {0}\r\n", reqPropName);
                        }
                    }

                    int el = -1;
                    Int32.TryParse(modelAsset.externalID, out el);
                    yield return new ReportResult(this, el, pass);
                }
            }
        }
    }
}
