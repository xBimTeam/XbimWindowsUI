using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Xbim.IO;
using Xbim.Script;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;

namespace Xbim.Presentation
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingControl
    {
        public ScriptingControl()
        {
            InitializeComponent();
            CreateParser();
        }

        public XbimVariables Results
        {
            get { return _parser.Results; }
        }

        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(ScriptingControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      OnModelChanged));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScriptingControl sw = d as ScriptingControl;
            if (sw != null)
            {
                XbimModel model = e.NewValue as XbimModel;
                if (model != null)
                    //create new parser which is associated to the model if model has been changed
                    sw.CreateParser(model);
            }
        }

        private XbimQueryParser _parser;
        private StringWriter _output = new StringWriter();
        private void CreateParser(XbimModel model = null)
        {
            if (model != null) _parser = new XbimQueryParser(model);
            else _parser = new XbimQueryParser();


            _parser.Output = _output;
            _parser.OnScriptParsed += delegate
            {
                //fire the event
                ScriptParsed();

                //show output in the output window
                OutputWindow.Text = _output.ToString();
                (OutputWindow.Parent as ScrollViewer).ScrollToEnd();
            };
            _parser.OnModelChanged += delegate(object sender, ModelChangedEventArgs e)
            {
                //fire event
                ModelChangedByScript(e.NewModel);
            };

            //open files just created
            _parser.OnFileReportCreated += delegate(object sender, FileReportCreatedEventArgs args)
            {
                var path = args.FilePath;
                if (path != null)
                    Process.Start(path);
            };
        }

        private void SaveScript_Click(object sender, RoutedEventArgs e)
        {
            string script = ScriptInput.Text;
            if (String.IsNullOrEmpty(script))
            {
                MessageBox.Show("There is no script to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bql";
            dlg.Title = "Set file name of the script...";
            dlg.OverwritePrompt = true;
            dlg.ValidateNames = true;
            dlg.FileOk += delegate
            {
                var name = dlg.FileName;
                StreamWriter file = null;
                try
                {
                    file = new StreamWriter(name, false);
                    file.Write(script);
                }
                catch (Exception)
                {
                    MessageBox.Show("Saving script to file failed. Check if the location is writable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (file!= null)
                        file.Close();
                }
            };
            dlg.ShowDialog();
        }


        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bql";
            dlg.Title = "Specify the script file...";
            dlg.ValidateNames = true;
            dlg.FileOk += delegate
            {
                Stream file = null;
                try
                {
                    file = dlg.OpenFile();
                    var reader = new StreamReader(file);
                    var script = reader.ReadToEnd();
                    ScriptInput.Text = script;
                }
                catch (Exception)
                {
                    MessageBox.Show("Loading script from file failed. Check if the file exist and is readable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
            };
            dlg.ShowDialog();
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptInput.Text;
            if (String.IsNullOrEmpty(script))
            {
                MessageBox.Show("There is no script to execute.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var origCurs = Cursor;
            Cursor = Cursors.Wait;
            _parser.Parse(script);
            Cursor = origCurs;

            ErrorsWindow.Visibility = Visibility.Collapsed;
            ErrorsWindow.Text = null;
            MsgWindow.Visibility = Visibility.Collapsed;
            MsgWindow.Text = null;

            if (_parser.Errors.Count != 0)
            {
                foreach (var err in _parser.Errors)
                    ErrorsWindow.Text += err + "\n";
                ErrorsWindow.Visibility = Visibility.Visible;
            }
            else
            {
                MsgWindow.Text += DateTime.Now.ToShortTimeString() + " run OK";
                MsgWindow.Visibility = Visibility.Visible;
            }
        }

        private void ScriptInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var num = ScriptInput.LineCount;
            LineNumbers.Text = "1";

            for (int i = 2; i < num+1; i++)
                LineNumbers.Text += "\n" + i;
        }

        public event ScriptParsedHandler OnScriptParsed;
        private void ScriptParsed()
        {
            if (OnScriptParsed != null)
                OnScriptParsed(this, new ScriptParsedEventArgs());
        }

         public event ModelChangedHandler OnModelChangedByScript;
        private void ModelChangedByScript(XbimModel newModel)
        {
            if (OnModelChangedByScript != null)
                OnModelChangedByScript(this, new ModelChangedEventArgs(newModel));
        }

    }
}
