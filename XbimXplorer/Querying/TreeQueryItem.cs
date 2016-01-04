using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.IO;

namespace XbimXplorer.Querying
{
    /// <summary>
    /// 
    /// </summary>
    public class TreeQueryItem
    {
        private IEnumerable<int> _entityLabelsToParse;
        private String _queryCommand;
        private bool _transverse;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="query"></param>
        /// <param name="returnTransversal"></param>
        public TreeQueryItem(IEnumerable<int> labels, string query, bool returnTransversal)
        {
            _queryCommand = query;
            _entityLabelsToParse = labels;
            _transverse = returnTransversal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public IEnumerable<int> Run(XbimModel model)
        {
            foreach (var label in _entityLabelsToParse)
            {
                if (_transverse)
                    yield return label;
                else if (_queryCommand.Trim() == "")
                    yield return label;
                var entity = model.Instances[label];
                if (entity != null)
                {
                    var ifcType = model.Metadata.ExpressType(entity);
                    // directs first
                    SquareBracketIndexer sbi = new SquareBracketIndexer(_queryCommand);

                    var prop = ifcType.Properties.Where(x => x.Value.PropertyInfo.Name == sbi.Property).FirstOrDefault().Value;
                    if (prop == null) // otherwise test inverses
                    {
                        prop = ifcType.Inverses.Where(x => x.PropertyInfo.Name == sbi.Property).FirstOrDefault();
                    }
                    if (prop != null)
                    {
                        object propVal = prop.PropertyInfo.GetValue(entity, null);
                        if (propVal != null)
                        {
                            if (prop.EntityAttribute.IsEnumerable)
                            {
                                IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                                if (propCollection != null)
                                {
                                    propCollection = sbi.GetItem(propCollection);
                                    foreach (var item in propCollection)
                                    {
                                        IPersistEntity pe = item as IPersistEntity;
                                        if (pe != null) yield return pe.EntityLabel;
                                    }
                                }
                            }
                            else
                            {
                                IPersistEntity pe = propVal as IPersistEntity;
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