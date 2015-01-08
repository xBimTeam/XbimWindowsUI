using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xbim.COBie;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for COBieClassFilter.xaml
    /// </summary>
    public partial class COBieClassFilter : Window
    {
        public FilterValues DefaultFilters { get; set; } //gives us the initial list of types
        public FilterValues UserFilters { get; set; }    //hold the user required class types, as required by the user
        //collection classes holding the checkedListItem class, which in tern holds the class types to display,  ObservableCollection implement the INotifyCollectionChanged
        public ObservableCollection<CheckedListItem<Type>> ClassFilterComponent { get; set; }
        public ObservableCollection<CheckedListItem<Type>> ClassFilterType { get; set; }
        public ObservableCollection<CheckedListItem<Type>> ClassFilterAssembly { get; set; }

        public COBieClassFilter(FilterValues userFilters)
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
        /// <param name="obsColl">ObservableCollection<CheckedListItem<Type>></param>
        /// <param name="defaultExcludeTypes">List of Type, holding default list of class types<CheckedListItem<Type>></param>
        /// <param name="userExcludeTypes">List of Type, holding user's  list of class types</param>
        private void InitExcludes(ObservableCollection<CheckedListItem<Type>> obsColl, List<Type> defaultExcludeTypes, List<Type> userExcludeTypes)
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
            this.Close();
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
