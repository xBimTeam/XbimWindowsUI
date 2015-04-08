using System.IO;
using System.Linq;
using Xbim.COBieLiteUK;
using Xbim.CobieLiteUK.Validation.Reporting;

namespace Xbim.WindowsUI.DPoWValidation.Extensions
{
    public static class FacilityExtensions
    {
        public static bool IsValidationResult(this Facility fac)
        {
            return fac.Categories != null 
                && 
                fac.Categories.Any(c => c.Classification == @"DPoW" && (c.Code == "Passed" || c.Code == "Failed"));
        }

        internal static string ExportFacility(this Facility fac, FileInfo fInfo)
        {
            var fExt = fInfo.Extension.ToLowerInvariant();
            switch (fExt)
            {
                case @".json":
                    fac.WriteJson(fInfo.FullName);
                    return "Report exported.";
                    break;
                case @".xml":
                    fac.WriteXml(fInfo.FullName);
                    return "Report exported.";
                    break;
                case @".xlsx":
                case @".xls":
                    if (fac.IsValidationResult())
                    {
                        // write xls validation report
                        var xRep = new ExcelValidationReport();
                        var ret = xRep.Create(fac, fInfo.FullName);
                        return ret 
                            ? "Export successful." 
                            : "Export failed.";
                    }
                    else
                    {
                        // write cobie file 
                        string msg;
                        fac.WriteCobie(fInfo.FullName, out msg);
                        return msg;
                    }
                    break;
            }
            return "Warning: nothing done, check report extension.";
        }
    }
}
