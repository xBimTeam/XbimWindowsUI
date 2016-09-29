using System.IO;
using Xbim.CobieLiteUk;

namespace Xbim.WindowsUI.DPoWValidation.IO
{
    public class FacilityReader
    {
        public string Message = "";

        public Facility LoadFacility(string fileToLoad)
        {
            if (string.IsNullOrEmpty(fileToLoad))
            {
                Message = "No file provided.";
                return null;
            }
            if (!File.Exists(fileToLoad))
            {
                Message = string.Format("File {0} not found.", fileToLoad);
                return null;
            }
            Facility requirementFacility = null;

            switch (Path.GetExtension(fileToLoad.ToLowerInvariant()))
            {
                case ".json":
                    requirementFacility = Facility.ReadJson(fileToLoad);
                    break;
                case ".xml":
                    requirementFacility = Facility.ReadXml(fileToLoad);
                    break;
                case ".xls":
                case ".xlsx":
                    string msg;
                    requirementFacility = Facility.ReadCobie(fileToLoad, out msg);
                    break;
                case ".zip":
                    requirementFacility = Facility.ReadZip(fileToLoad);
                    break;
            }
            return requirementFacility;
        }
    }
}
