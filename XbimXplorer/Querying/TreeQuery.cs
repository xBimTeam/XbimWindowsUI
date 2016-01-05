using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Ifc2x3.IO;
using XbimXplorer.Querying;

namespace XbimXplorer
{
    static class QueryEngine
    {
        static public List<int> EntititesForType(string type, XbimModel model)
        {
            List<int> values = new List<int>();
            var items = model.Instances.OfType(type, false);
            foreach (var item in items)
            {
                int thisV = item.EntityLabel;
                if (!values.Contains(thisV))
                    values.Add(thisV);
            }
            values.Sort();
            return values;
        }

        static public IEnumerable<int> RecursiveQuery(XbimModel model, string query, IEnumerable<int> startList, bool returnTransverse)
        {
            var proparray = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<int> runningList =  startList;
            foreach (var stringQuery in proparray)
            {
                TreeQueryItem qi = new TreeQueryItem(runningList, stringQuery, returnTransverse);
                runningList = qi.Run(model);   
            }
            TreeQueryItem qi2 = new TreeQueryItem(runningList, "", returnTransverse);
            runningList = qi2.Run(model);
            foreach (var item in runningList)
            {
                yield return item;    
            }
        }
    }
}
