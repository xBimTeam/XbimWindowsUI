using System.Collections.ObjectModel;
using Xbim.CobieLiteUk.FilterHelper;


namespace XplorerPlugins.Cobie.UI
{
    public class FilterViewModel
    {
        private OutPutFilters _filters;

        public FilterViewModel(OutPutFilters modelFilter)
        {
            _filters = modelFilter;
            ProductFilters = makeFilter(modelFilter.IfcProductFilter);
            TypeObjectFilters = makeFilter(modelFilter.IfcTypeObjectFilter);
            AssemblyFilters = makeFilter(modelFilter.IfcAssemblyFilter);
        }

        private ObservableCollection<FilterTypeConfig> makeFilter(ObjectFilter ifcProductFilter)
        {
            var ret = new ObservableCollection<FilterTypeConfig>();
            foreach (var item in ifcProductFilter.Items)
            {
                ret.Add(new FilterTypeConfig()
                {
                    TypeName = item.Key,
                    Export = item.Value
                });
            }
            return ret;
        }

        public ObservableCollection<FilterTypeConfig> ProductFilters { get; set; }

        public ObservableCollection<FilterTypeConfig> TypeObjectFilters { get; set; }

        public ObservableCollection<FilterTypeConfig> AssemblyFilters { get; set; }
    }
}
