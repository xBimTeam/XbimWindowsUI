using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.CSharp;
using Microsoft.Win32;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;


namespace Xbim.Presentation
{
    /// <summary>
    /// This class can compile the code in the runtime and can be used to select 
    /// certain products with C# code;
    /// </summary>
    public partial class DynamicProductSelectionControl
    {
        public DynamicProductSelectionControl()
        {
            InitializeComponent();
            TxtCode.Text = _codeTemplate;
        }

        #region Code skeleton
        private readonly string _codeSkeleton1 = @"
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using Xbim.Ifc4.Interfaces;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.Transactions;
using Xbim.XbimExtensions.Transactions.Extensions;



namespace DynamicQuery
{
    public class Query
    {
        private StringWriter Output = new StringWriter();
        public string GetOutput()
        {
            if (Output == null) return """";
            return Output.ToString();
        }
            ";

        readonly string _codeTemplate =
@"//This will perform selection of the objects. 
//Selected objects with the geometry will be highlighted
public IEnumerable<IfcProduct> Select(IModel model)
{
    Output.WriteLine(""Hello selected products"");
    return model.Instances.OfType<IfcWall>();
}

//This will hide all objects except for the returned ones
public IEnumerable<IfcProduct> ShowOnly(IModel model)
{
    Output.WriteLine(""Hello visible products!"");
    return model.Instances.Where<IfcProduct>(p => p.Name != null &&  ((string)p.Name).ToLower().Contains(""wall""));
}

//This will execute arbitrary code with no return value
public void Execute(IModel model)
{
    IEnumerable<IfcSpace> spaces = model.Instances.OfType<IfcSpace>();
    foreach (IfcSpace space in spaces)
    {
        Output.WriteLine(space.Name + "" - "" + space.LongName);
    }
}
";

        readonly string _codeSkeleton2 =
@"
    }
}
";
        #endregion

        #region Model Dependency Property
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(DynamicProductSelectionControl), new UIPropertyMetadata(null));
        #endregion

        #region ProductSelectionChanged infrastructure
        public delegate void ProductSelectionChangedEventHandler(object sender, ProductSelectionChangedEventArgs a);
        public event ProductSelectionChangedEventHandler ProductSelectionChanged;

        protected virtual void OnRaiseProductSelectionChangedEvent(IEnumerable<IIfcProduct> products)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            ProductSelectionChangedEventHandler handler = ProductSelectionChanged;

            // Event will be null if there are no subscribers 
            if (handler != null)
            {
                //create argument
                ProductSelectionChangedEventArgs e = new ProductSelectionChangedEventArgs(products);

                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        public class ProductSelectionChangedEventArgs : EventArgs
        {
            public ProductSelectionChangedEventArgs(IEnumerable<IIfcProduct> selection)
            {
                Selection = selection;
            }

            public IEnumerable<IIfcProduct> Selection { get; }
        }
        #endregion

        #region ProductVisibilityChanged infrastructure
        public delegate void ProductVisibilityChangedEventHandler(object sender, ProductVisibilityChangedEventArgs a);
        public event ProductVisibilityChangedEventHandler ProductVisibilityChanged;

        protected virtual void OnRaiseProductVisibilityChangedEvent(IEnumerable<IIfcProduct> products)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            ProductVisibilityChangedEventHandler handler = ProductVisibilityChanged;

            // Event will be null if there are no subscribers 
            if (handler != null)
            {
                //create argument
                ProductVisibilityChangedEventArgs e = new ProductVisibilityChangedEventArgs(products);

                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        public class ProductVisibilityChangedEventArgs : EventArgs
        {
            public ProductVisibilityChangedEventArgs(IEnumerable<IIfcProduct> selection)
            {
                Selection = selection;
            }

            public IEnumerable<IIfcProduct> Selection { get; }
        }
        #endregion

        private void btnPerform_Click(object sender, RoutedEventArgs e)
        {
            if (Model == null)
            {
                MessageBox.Show("There is no model available.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string code = TxtCode.Text;
            if (String.IsNullOrEmpty(code) || String.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("You have to insert some C# code.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            //create compiler
            Dictionary<string, string> providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
            CSharpCodeProvider provider = new CSharpCodeProvider(providerOptions);
            CompilerParameters compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false
            };
            compilerParams.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.DataSetExtensions.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.Linq.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.Common.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.Ifc2x3.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.IO.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.Ifc.Extensions.dll");

            //get the code together
            string source = _codeSkeleton1 + code + _codeSkeleton2;
            //compile the source
            CompilerResults results = provider.CompileAssemblyFromSource(compilerParams, source);

            //check the result
            if (results.Errors.Count != 0)
            {
                StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                foreach (CompilerError error in results.Errors)
                {
                    errors.AppendFormat("Line {0},{1}\t: {2}\n", error.Line, error.Column, error.ErrorText);
                }
                MessageBox.Show("Compilation of your code has failed. \n" + errors, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //create instance of the objekct from the compiled assembly
            object o = results.CompiledAssembly.CreateInstance("DynamicQuery.Query");
            if (o == null)
                throw new Exception("Compiled code does not contain class DynamicQuery.Query");
            MethodInfo miSelect = o.GetType().GetMethod("Select", new[] { typeof(IModel) });
            MethodInfo miShowOnly = o.GetType().GetMethod("ShowOnly", new[] { typeof(IModel) });
            MethodInfo miExecute = o.GetType().GetMethod("Execute", new[] { typeof(IModel) });
            MethodInfo miOutput = o.GetType().GetMethod("GetOutput");
            

            //check for existance of the methods
            if (miOutput == null)
                //this function should be there because it is my infrastructure
                throw new Exception("Code doesn't contain predefined method with the signature: public string GetOutput();");
            try
            {
                if (miSelect != null)
                {
                        IEnumerable<IIfcProduct> prods = miSelect.Invoke(o, new object[] { Model }) as IEnumerable<IIfcProduct> ?? new List<IIfcProduct>();

                        //raise the event about the selection change
                        OnRaiseProductSelectionChangedEvent(prods);
                }
                if (miShowOnly != null)
                {
                        IEnumerable<IIfcProduct> prods = miShowOnly.Invoke(o, new object[] { Model }) as IEnumerable<IIfcProduct> ?? new List<IIfcProduct>();

                        //raise the event about the selection change
                        OnRaiseProductVisibilityChangedEvent(prods);
                }
                if (miExecute != null)
                {
                   
                    if (Model != null)
                    {
                        using (var txn = Model.BeginTransaction("Dynamic Product Selection"))
                        {
                            miExecute.Invoke(o, new object[] { Model });
                            txn.Commit();
                        }
                    }
                    else
                        miExecute.Invoke(o, new object[] { Model });
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException;
                if (innerException != null)
                    MessageBox.Show("There was a runtime exception during the code execution: \n" + innerException.Message + "\n" + innerException.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else throw;
            }
           

           

            //get messages from the compiled code
            string msg = miOutput.Invoke(o, null) as string;
            TxtOutput.Text += msg;
            TxtOutput.ScrollToEnd();

        }

        private void btnDefault_Click(object sender, RoutedEventArgs e)
        {
            TxtCode.Text = _codeTemplate;

        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".cs",
                CheckFileExists = true,
                Multiselect = false,
                Title = "Choose existing code file...",
                ValidateNames = true
            };
            if (dlg.ShowDialog() == true)
            {
                TxtCode.Text = File.ReadAllText(dlg.FileName);
            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".cs";
            dlg.OverwritePrompt = true;
            dlg.Title = "Save the code as...";
            dlg.ValidateNames = true;

            if (dlg.ShowDialog() == true)
            {
                Stream file = dlg.OpenFile();
                TextWriter wr = new StreamWriter(file);
                wr.Write(TxtCode.Text);
                wr.Close();
                file.Close();
            }

        }

        private void btnSaveOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.OverwritePrompt = true;
            dlg.Title = "Save the output as...";
            dlg.ValidateNames = true;

            if (dlg.ShowDialog() == true)
            {
                Stream file = dlg.OpenFile();
                TextWriter wr = new StreamWriter(file);
                wr.Write(TxtOutput.Text);
                wr.Close();
                file.Close();
            }
        }
    }

   
}
