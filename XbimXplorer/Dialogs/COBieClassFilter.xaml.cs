using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Xbim.COBie;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for COBieClassFilter.xaml
    /// </summary>
    public partial class CoBieClassFilter
    {
        /// <summary>
        /// 
        /// </summary>
        public FilterValues DefaultFilters { get; set; } //gives us the initial list of types
        /// <summary>
        /// 
        /// </summary>
        public FilterValues UserFilters { get; set; }    //hold the user required class types, as required by the user
        //collection classes holding the checkedListItem class, which in tern holds the class types to display,  ObservableCollection implement the INotifyCollectionChanged
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<CheckedListItem<Type>> ClassFilterComponent { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<CheckedListItem<Type>> ClassFilterType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<CheckedListItem<Type>> ClassFilterAssembly { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userFilters"></param>
        public CoBieClassFilter(FilterValues userFilters)
        {
            InitializeComponent();

            UserFilters = userFilters; //hold the amendments, as required by the user
            DefaultFilters = new FilterValues(); //gives us the initial list of types
            
            //initialize the collection classes for the list box's
            ClassFilterComponent = new ObservableCollection<CheckedListItem<Type>>();
            ClassFilterType = new ObservableCollection<CheckedListItem<Type>>();
            ClassFilterAssembly = new ObservableCollection<CheckedListItem<Type>>();

            //fill in the collections to display the check box's in the list box's
            InitExcludes(ClassFilterComponent, DefaultFilters.ObjectType.Component, UserFilters.ObjectType.Component);
            InitExcludes(ClassFilterType, DefaultFilters.ObjectType.Types, UserFilters.ObjectType.Types);
            InitExcludes(ClassFilterAssembly, DefaultFilters.ObjectType.Assembly, UserFilters.ObjectType.Assembly);

            DataContext = this;
        }

        /// <summary>
        /// Initialize the ObservableCollection's 
        /// </summary>
        /// <param name="obsColl"></param>
        /// <param name="defaultExcludeTypes"></param>
        /// <param name="userExcludeTypes">List of Type, holding user's  list of class types</param>
        private void InitExcludes(ObservableCollection<CheckedListItem<Type>> obsColl, IEnumerable<Type> defaultExcludeTypes, List<Type> userExcludeTypes)
        {
            foreach (Type typeobj in defaultExcludeTypes)
            {
                obsColl.Add(new CheckedListItem<Type>(typeobj, userExcludeTypes.Contains(typeobj))); //see if in user list, if so check it
            }
        }

        /// <summary>
        /// OK button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SetExcludes(ClassFilterComponent, UserFilters.ObjectType.Component);
            SetExcludes(ClassFilterType, UserFilters.ObjectType.Types);
            SetExcludes(ClassFilterAssembly, UserFilters.ObjectType.Assembly);
            
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// set the UserFilters to the required class types to exclude 
        /// </summary>
        /// <param name="obsColl">ObservableCollection</param>
        /// <param name="userExcludeTypes">List of Type, holding user's  list of class types</param>
        private void SetExcludes(ObservableCollection<CheckedListItem<Type>> obsColl, List<Type> userExcludeTypes)
        {
            foreach (CheckedListItem<Type> item in obsColl)
            {
                if (item.IsChecked)
                {
                    if (!userExcludeTypes.Contains(item.Item))
                        userExcludeTypes.Add(item.Item);
                }
                else
                {
                    userExcludeTypes.Remove(item.Item);
                }
            }
        }
    }
}
