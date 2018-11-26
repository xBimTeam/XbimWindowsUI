using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;


namespace XbimXplorer.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class TreeQueryItem
    {
        private readonly IEnumerable<int> _entityLabelsToParse;
        private readonly string _queryCommand;
        private readonly bool _transverse;

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
        public IEnumerable<int> Run(IfcStore model)
        {
            foreach (var label in _entityLabelsToParse)
            {
                if (_transverse)
                    yield return label;
                else if (_queryCommand.Trim() == "")
                    yield return label;
                var entity = model.Instances[label];
                if (entity == null) 
                    continue;
                var ifcType = model.Metadata.ExpressType(entity);
                // directs first
                var sbi = new SquareBracketIndexer(_queryCommand);

                var prop = ifcType.Properties.FirstOrDefault(x => x.Value.PropertyInfo.Name == sbi.Property).Value ??
                           ifcType.Inverses.FirstOrDefault(x => x.PropertyInfo.Name == sbi.Property);
                var propVal = prop?.PropertyInfo.GetValue(entity, null);
                if (propVal == null) 
                    continue;
                if (prop.EntityAttribute.IsEnumerable)
                {
                    var propCollection = propVal as IEnumerable<object>;
                    if (propCollection == null) 
                        continue;
                    propCollection = sbi.GetItem(propCollection);
                    foreach (var item in propCollection)
                    {
                        var pe = item as IPersistEntity;
                        if (pe != null) yield return pe.EntityLabel;
                    }
                }
                else
                {
                    var pe = propVal as IPersistEntity;
                    if (pe != null && sbi.Index < 1) // index is negative (not specified) or 0
                        yield return pe.EntityLabel;
                }
            }
        }
    }
}