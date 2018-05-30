﻿using System;
using System.Collections.Generic;
using log4net;
using Xbim.Ifc;

namespace XbimXplorer.Commands
{
    internal static class QueryEngine
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(QueryEngine));

        public static List<int> EntititesForType(string type, IfcStore model)
        {
            var values = new List<int>();
            try
            {
                var items = model.Instances.OfType(type, false);
                if (items == null)
                    return new List<int>();
                foreach (var item in items)
                {
                    var thisV = item.EntityLabel;
                    if (!values.Contains(thisV))
                        values.Add(thisV);
                }
                values.Sort();
            }
            catch (Exception ex)
            {
                   Log.Error($"Error getting entities for type:{type}.", ex);
            }
            
            return values;
        }

        public static IEnumerable<int> RecursiveQuery(IfcStore model, string query, IEnumerable<int> startList, bool returnTransverse)
        {
            var proparray = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var runningList =  startList;
            foreach (var stringQuery in proparray)
            {
                var qi = new TreeQueryItem(runningList, stringQuery, returnTransverse);
                runningList = qi.Run(model);   
            }
            var qi2 = new TreeQueryItem(runningList, "", returnTransverse);
            runningList = qi2.Run(model);
            foreach (var item in runningList)
            {
                yield return item;    
            }
        }
    }
}
