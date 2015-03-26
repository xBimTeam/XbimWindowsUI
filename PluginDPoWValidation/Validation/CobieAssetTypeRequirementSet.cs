using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.Model;
using Xbim.COBieLite;


namespace Xbim.COBieLite.Validation
{
    class CobieAssetTypeRequirementSet : IValidationRequirementSet
    {
        
        private readonly Xbim.COBieLite.AssetTypeInfoType _assetRequirement;

        public CobieAssetTypeRequirementSet(Xbim.COBieLite.AssetTypeInfoType assetTypeInfoType)
        {
            this._assetRequirement = assetTypeInfoType;
            details = new List<CobieAssetTypeRequirementDetail>();
            foreach (var attrib in _assetRequirement.AssetTypeAttributes.Where(x => x.propertySetName == @"[required]"))
            {
                details.Add(
                    new CobieAssetTypeRequirementDetail()
                    {
                        PropertyName = attrib.AttributeName,
                        Comment =  attrib.AttributeDescription
                    }
                    );

            }
        }

        private readonly List<CobieAssetTypeRequirementDetail> details = new List<CobieAssetTypeRequirementDetail>();

        public string Name
        {
            get
            {
                if (_assetRequirement == null)
                    return @"Undefined";
                return _assetRequirement.AssetTypeCategory;
            }
        }

        public IEnumerable<ReportResult> Validate(Xbim.COBieLite.FacilityType modelFacilityType, int filterLabel = -1, StringBuilder reporter = null)
        {
            // attributes
            
            var classification = new CompoundClassificationString(_assetRequirement.AssetTypeCategory);

            foreach (var typeToTest in classification.FindMatches<AssetTypeInfoType>(modelFacilityType))
            {
                // need more check on specific assets
                IEnumerable<AssetInfoType> assetList = null;

                assetList = (filterLabel == -1)
                    ? typeToTest.Assets.Asset
                    : typeToTest.Assets.Asset.Where(x => x.externalID == filterLabel.ToString(CultureInfo.InvariantCulture));

                var assetLevelRequirements = MissingFrom(typeToTest);
                if (assetLevelRequirements.Any())
                {
                    foreach (var modelAsset in assetList)
                    {
                        if (reporter != null)
                            reporter.AppendFormat("===\r\nReporting {0} (#{1})\r\n", modelAsset.AssetName, modelAsset.externalID);

                        var matching =
                            assetLevelRequirements.Select(x => x.PropertyName)
                                .Intersect(modelAsset.AssetAttributes.Select(at => at.AttributeName));

                        var reqCnt = assetLevelRequirements.Count(); 
                        var machCnt = matching.Count();

                        var sb = new StringBuilder();

                        var pass = (reqCnt == machCnt);
                        if (!pass)
                        {
                            sb.AppendFormat("{0} of {1} requirements matched.\r\n\r\n", machCnt, reqCnt);
                            sb.AppendLine("Missing attributes:");
                            foreach (var req in assetLevelRequirements)
                            {
                                if (!matching.Contains(req.PropertyName))
                                {
                                    sb.AppendFormat("{0}\r\n{1}\r\n\r\n", req.PropertyName, req.Comment);
                                }
                            }
                        }

                        var el = -1;
                        Int32.TryParse(modelAsset.externalID, out el);
                        yield return new ReportResult(this, el, pass, sb.ToString());
                    }
                }
                else
                {
                    foreach (var modelAsset in assetList)
                    {
                        var el = -1;
                        Int32.TryParse(modelAsset.externalID, out el);
                        yield return new ReportResult(this, el, true, "");
                    }
                }
            }
        }

        private IEnumerable<CobieAssetTypeRequirementDetail> MissingFrom(AssetTypeInfoType typeToTest)
        {
            var req = new HashSet<string>(details.Select(x => x.PropertyName));
            var got = new HashSet<string>(typeToTest.AssetTypeAttributes.Select(x => x.AttributeName));

            req.RemoveWhere(got.Contains);
            return req.Select(left => details.FirstOrDefault(x => x.PropertyName == left));
        }


        public IEnumerable<IValidationRequirementDetail> Details
        {
            get { throw new NotImplementedException(); }
        }
    }
}
