using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using FSharp.Literate;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.MDComment
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MDView : UserControl
    {
        DTE2 dte;
        Events events;
        DocumentEvents docEvents;
        string sourceFile;
        public MDView()
        {
            InitializeComponent();
            dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            events = dte.Events;
            docEvents = events.DocumentEvents;
            dte.Events.WindowEvents.WindowActivated += OnWindowActivated;
            docEvents.DocumentSaved += OnDocumentSaved;
             
        }

        Task UpdateMarkdown()
        {
            return Task.Run(() => MDFormatter.format(sourceFile))
                .ContinueWith(t =>
                {
                    if (t.Exception == null)
                    {
                        var outputFile = t.Result;
                        browser.Navigate(new Uri(String.Format("file:///{0}", outputFile)));
                    }
                    else
                    {
                        var msg = String.Format(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""http://www.w3.org/TR/html4/loose.dtd"">
<html><head><title>Error</title></head><body>{0}</body></html>",
                            t.Exception.Message);
                        browser.NavigateToString(msg);
                   }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
         
        private void OnDocumentSaved(Document Document)
        {
            if (sourceFile == Document.FullName)
            {
                UpdateMarkdown();
            }
        }

        private bool HasFSharpExtension(string filename)
        {
            return filename.EndsWith(".fs") || filename.EndsWith(".fsx");
        }

        private void OnWindowActivated(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
        {
            var activated = dte.ActiveDocument;
            if (activated != null && sourceFile != activated.FullName && HasFSharpExtension(activated.Name))
            {
                sourceFile = activated.FullName;
                UpdateMarkdown();
            }
        }
    }
}