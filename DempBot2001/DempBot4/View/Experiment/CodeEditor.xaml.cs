using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines;
using Dempbot4.Models.ScriptEngines.Messages;
using Dempbot4.ViewModel;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;


namespace Dempbot4.Views.Experiments
{
    /// <summary>
    /// Interaction logic for CodeEditor.xaml
    /// </summary>
    public partial class CodeEditor : UserControl
    {

        PythonScripting ScriptHost;

        public ComboBox VariablesControl { get; private set; }

        Regex rx;

        public CodeEditor()
        {
            InitializeComponent();

            rx = new Regex(@"^((\w+)\.?)+", RegexOptions.RightToLeft | RegexOptions.Compiled | RegexOptions.IgnoreCase);

            textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;

            if (App.Current != null)
            {
                ScriptHost = (PythonScripting)App.Current.Services.GetService(typeof(PythonScripting));
            }
          
          

        }

       

        CompletionWindow completionWindow;

        private void FormFromDot(string text, int offset, string sourceText )
        {
            LastAutoText = text;
            LastAutoOptions = new List<string>();
            var parts = text.Split('.');
            //if (parts.Length > 1)
            //{
            //    foreach (var pVar in AvailableVariables)
            //    {
            //        if (pVar.Name.ToLower() == (parts[0].ToLower()))
            //        {
                        
            //            var subs= ScriptHost.GetMethods(sourceText.TrimEnd('.'));
            //            //if (pVar.Value.GetType().ToString().StartsWith("IronPython") == false)
            //            //{
            //            //    foreach (var sub in pVar.Value.GetType().GetProperties())
            //            //    {
            //            //        subs.Add(sub.Name);
            //            //    }
            //            //    foreach (var sub in pVar.Value.GetType().GetFields())
            //            //    {
            //            //        subs.Add(sub.Name);
            //            //    }
            //            //    foreach (var subMethod in pVar.Value.GetType().GetMethods())
            //            //    {
            //            //        subs.Add(subMethod.Name);
            //            //    }
            //            //}
            //            //else
            //            //{
            //            //    foreach (var field in ScriptHost.GetMethods((string) pVar.Value))
            //            //    {
            //            //        if (field.StartsWith("_") == false)
            //            //            subs.Add(field);
            //            //    }
            //            //}
            //            //subs.Sort();
            //            completionWindow = new CompletionWindow(textEditor.TextArea);
            //            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            //            foreach (var sub in subs)
            //            {

            //                if (ScriptHost.MethodSignatures.TryGetValue(sub, out string signature))
            //                {
            //                    data.Add(new MyCompletionData(signature, offset, sourceText));
            //                }
            //                else
            //                {
            //                    if (parts[1].Length == 0)
            //                        data.Add(new MyCompletionData(sub, offset, sourceText));
            //                    else
            //                    {
            //                        if (sub.ToLower().StartsWith(parts[1].ToLower()))
            //                            data.Add(new MyCompletionData(sub, offset, sourceText));
            //                    }
            //                }


            //            }
            //            if (data.Count > 0)
            //            {
            //                LastAutoOptions = data.Select(x => x.Text).ToList<string>();

            //                completionWindow.Show();
            //                completionWindow.Closed += delegate
            //                {
            //                    completionWindow = null;
            //                };

            //            }
            //            break;
            //        }
            //    }
            //}
        }

        List<string> LastAutoOptions = new List<string>();
        string LastAutoText = null;
        void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            ViewModel.TextContent = textEditor.Text;
            return;
            int offset = textEditor.CaretOffset;
            DocumentLine line = textEditor.Document.GetLineByOffset(offset);
            var currentText = textEditor.Document.GetText(line.Offset, line.Length);
            var EditText = currentText.Substring(0, offset - line.Offset).Trim();

            var lastWord = rx.Match(EditText);
            string text = "";
            if (lastWord.Success)
                text = lastWord.Value.ToLower();


            
            //if (e.Text == ".")
            //{
            //   // FormFromDot(text,offset, lastWord.Value);
            //}
            //else if (LastAutoText!=null && text.Contains(LastAutoText) && LastAutoOptions.Count > 0)
            //{
            //    completionWindow = new CompletionWindow(textEditor.TextArea);
            //    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            //    var parts = text.Split('.');
            //    foreach (var pVar in LastAutoOptions)
            //    {
            //        if (pVar.ToLower().Contains(parts[1].ToLower()))
            //        {
            //            data.Add(new MyCompletionData(pVar.Substring(parts[1].Length), offset, lastWord.Value));
            //        }
            //    }

            //    if (data.Count > 0)
            //    {
            //        completionWindow.Show();
            //        completionWindow.Closed += delegate
            //        {
            //            completionWindow = null;
            //        };
            //    }
            //}
            //else if (EditText.Length > 1)
            //{
            //    completionWindow = new CompletionWindow(textEditor.TextArea);
            //    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

            //    foreach (var pVar in AvailableVariables)
            //    {
            //        if (pVar.Name.ToLower().Contains(text.ToLower()))
            //        {
            //            data.Add(new MyCompletionData(pVar.Name.Substring(text.Length), offset, lastWord.Value));

            //        }
            //    }
            //    if (data.Count > 0)
            //    {
            //        completionWindow.Show();
            //        completionWindow.Closed += delegate
            //        {
            //            completionWindow = null;
            //        };
            //    }
            //}
        }

        void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        CodeEditorViewModel ViewModel;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel =(CodeEditorViewModel) DataContext;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            textEditor.Text = ViewModel.TextContent;
            textEditor.FontSize = ViewModel.FontSize;

        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName=="TextContent" && textEditor.Text!= ViewModel.TextContent) 
            {
                textEditor.Text = ViewModel.TextContent;
            }   
            else if (e.PropertyName == "FontSize")
            {
                textEditor.FontSize = ViewModel.FontSize;
            }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            ViewModel.TextContent = textEditor.Text;
            return;
        }

        private void textEditor_Loaded(object sender, RoutedEventArgs e)
        {
            /*<SyntaxDefinition name="Python" extensions=".py;.pyw">
    <Color name="Comment"       foreground="#FF57A64A" />
    <Color name="String"        foreground="#FFD69D85" />
    <Color name="MethodCall"    foreground="#FFdcdcaa" />
    <Color name="NumberLiteral" foreground="#FFb5cea8" />
    <Color name="Keywords"      foreground="#FF00A0FF" />

    <Color name="CommentMarkerSetTodo"        foreground="#FFFF0000" fontWeight="bold"/>
    <Color name="CommentMarkerSetHackUndone"  foreground="#FF8B008B" fontWeight="bold"/>
  </SyntaxDefinition>*/

            var highlighting = textEditor.SyntaxHighlighting;
            try
            {
                highlighting.GetNamedColor("Comment").Foreground = new SimpleHighlightingBrush(Colors.Green);
                highlighting.GetNamedColor("String").Foreground = new SimpleHighlightingBrush(Color.FromRgb(211, 145, 97));
                highlighting.GetNamedColor("MethodCall").Foreground = new SimpleHighlightingBrush(Colors.LightYellow);
                highlighting.GetNamedColor("NumberLiteral").Foreground = new SimpleHighlightingBrush(Colors.LightSeaGreen);
                highlighting.GetNamedColor("Keywords").Foreground = new SimpleHighlightingBrush(Color.FromRgb(207, 158, 199));


                foreach (var color in highlighting.NamedHighlightingColors)
                {
                    color.FontWeight = null;
                }
                textEditor.SyntaxHighlighting = null;
                textEditor.SyntaxHighlighting = highlighting;
            }
            catch { }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((CodeEditorViewModel)this.DataContext).OnSave(null);
        }

        private void PackIconCodicons_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((CodeEditorViewModel)this.DataContext).OnSave(null);
        }
    }

    /// Implements AvalonEdit ICompletionData interface to provide the entries in the
    /// completion drop down.
    public class MyCompletionData : ICompletionData
    {
        public MyCompletionData(string text, int startIndex, string sourceText)
        {
            this.Text = text;
            this.RealText = sourceText;
            this.StartIndex = startIndex;
        }

        public int StartIndex { get; set; }

        public string RealText
        {
            get;set;
        }

        public double Priority { get { return 1; } }


        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public string Text { get; private set; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content
        {
            get { return this.Text; }
        }

        public object Description
        {
            get { return "Description for " + this.Text; }
        }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {

           //var newText = RealText + this.Text;
           // textArea.Document.Replace(completionSegment, this.Text);
           
            textArea.Document.Insert(completionSegment.Offset, this.Text);
          //  textArea.Document.Replace(completionSegment.Offset - RealText.Length, RealText.Length, RealText);
        }
    }
}
