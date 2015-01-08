using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using XbimXplorer.Querying;

namespace XbimXplorer
{
    static class QueryEngine
    {
        static public List<int> EntititesForType(string type, XbimModel Model)
        {
            List<int> Values = new List<int>();
            var items = Model.Instances.OfType(type, false);
            foreach (var item in items)
            {
                int thisV = item.EntityLabel;
                if (!Values.Contains(thisV))
                    Values.Add(thisV);
            }
            Values.Sort();
            return Values;
        }

        static public IEnumerable<int> RecursiveQuery(XbimModel Model, string Query, IEnumerable<int> StartList, bool ReturnTransverse)
        {
            var proparray = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<int> runningList =  StartList;
            foreach (var StringQuery in proparray)
            {
                TreeQueryItem qi = new TreeQueryItem(runningList, StringQuery, ReturnTransverse);
                runningList = qi.Run(Model);   
            }
            TreeQueryItem qi2 = new TreeQueryItem(runningList, "", ReturnTransverse);
            runningList = qi2.Run(Model);
            foreach (var item in runningList)
            {
                yield return item;    
            }
        }
    }

    public class TreeQueryItem
    {
        private IEnumerable<int> _EntityLabelsToParse;
        private String _QueryCommand;
        private bool transverse = true;

        public TreeQueryItem(IEnumerable<int> labels, string Query, bool ReturnTransversal)
        {
            _QueryCommand = Query;
            _EntityLabelsToParse = labels;
            transverse = ReturnTransversal;
        }

        public IEnumerable<int> Run(XbimModel Model)
        {
            foreach (var label in _EntityLabelsToParse)
            {
                if (transverse)
                    yield return label;
                else if (_QueryCommand.Trim() == "")
                    yield return label;
                var entity = Model.Instances[label];
                if (entity != null)
                {
                    IfcType ifcType = IfcMetaData.IfcType(entity);
                    // directs first
                    SquareBracketIndexer sbi = new SquareBracketIndexer(_QueryCommand);

                    var prop = ifcType.IfcProperties.Where(x => x.Value.PropertyInfo.Name == sbi.Property).FirstOrDefault().Value;
                    if (prop == null) // otherwise test inverses
                    {
                        prop = ifcType.IfcInverses.Where(x => x.PropertyInfo.Name == sbi.Property).FirstOrDefault();
                    }
                    if (prop != null)
                    {
                        object propVal = prop.PropertyInfo.GetValue(entity, null);
                        if (propVal != null)
                        {
                            if (prop.IfcAttribute.IsEnumerable)
                            {
                                IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                                if (propCollection != null)
                                {
                                    propCollection = sbi.getItem(propCollection);
                                    foreach (var item in propCollection)
                                    {
                                        IPersistIfcEntity pe = item as IPersistIfcEntity;
                                        yield return pe.EntityLabel;
                                    }
                                }
                            }
                            else
                            {
                                IPersistIfcEntity pe = propVal as IPersistIfcEntity;
                                if (pe != null && sbi.Index < 1) // index is negative (not specified) or 0
                                    yield return pe.EntityLabel;
                            }
                        }
                    }
                }
            }
        }
    }
}
