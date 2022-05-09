using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// Item for the context selection
    /// </summary>
    public class ContextSelectionItem : INotifyPropertyChanged
    {
        /// <summary>
        /// implementation of INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Child objects
        /// </summary>
        public ObservableCollection<ContextSelectionItem> Children { get; set; } 
            = new ObservableCollection<ContextSelectionItem>();

        /// <summary>
        /// Reflects if this entity was checked
        /// </summary>
        public bool IsChecked
        {
            get { return _isChecked; }
            set 
            {
                if (this._isChecked != value)
                {
                    _isChecked = value;
                    this.OnPropertyChanged();
                }
                foreach (ContextSelectionItem child in Children)
                {
                    child.IsChecked = value;
                }
            }
        }

        /// <summary>
        /// internal representation of isChecked
        /// </summary>
        private bool _isChecked;

        /// <summary>
        /// Included Context
        /// </summary>
        public IIfcGeometricRepresentationContext RepresentationContext { get; set; }

        /// <summary>
        /// Name of the context
        /// </summary>
        public string Name
        {
            get
            {
                return this.RepresentationContext.ContextIdentifier;
            }
        }

        /// <summary>
        /// empty constructor
        /// </summary>
        public ContextSelectionItem()
        {

        }

        /// <summary>
        /// Constructor with context as content
        /// </summary>
        /// <param name="content"></param>
        public ContextSelectionItem(IIfcGeometricRepresentationContext content)
        {
            this.IsChecked = true;
            this.RepresentationContext = content;
            if (content.HasSubContexts != null)
            {
                foreach (IIfcGeometricRepresentationSubContext child in content.HasSubContexts)
                {
                    this.Children.Add(new ContextSelectionItem(child));
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class RepresentationContextSelection : Window
    {
        public ObservableCollection<ContextSelectionItem> ContextItems { get; set; } 
            = new ObservableCollection<ContextSelectionItem>();

        public RepresentationContextSelection()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the internal Collection of items
        /// </summary>
        /// <param name="items"></param>
        public void SetContextItems(IEnumerable<IIfcGeometricRepresentationContext> items)
        {
            Dictionary<int,IIfcGeometricRepresentationContext> processedItems = new Dictionary<int,IIfcGeometricRepresentationContext>();
            this.ContextItems.Clear();

            foreach(IIfcGeometricRepresentationContext item in items)
            {
                if (processedItems.ContainsKey(item.EntityLabel) == false)
                {
                    ContextSelectionItem parent = new ContextSelectionItem(item);
                    this.ContextItems.Add(parent);
                    processedItems.Add(item.EntityLabel, item);
                }
            }
        }

        private void AcknowledgeSelection(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelSelection(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
