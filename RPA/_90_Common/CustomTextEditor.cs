using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace RPA._90_Common
{
    class CustomTextEditor : TextEditor
    {
        public ICSharpCode.AvalonEdit.Document.TextDocument AddDocument
        {
            get
            {
                return (ICSharpCode.AvalonEdit.Document.TextDocument)GetValue(AddDocumentProperty);
            }
            set
            {
                SetValue(AddDocumentProperty, value);
            }
        }
        private static readonly DependencyProperty AddDocumentProperty = DependencyProperty.Register(
            "AddDocument",
            typeof(ICSharpCode.AvalonEdit.Document.TextDocument),
            typeof(CustomTextEditor),
            new PropertyMetadata(new ICSharpCode.AvalonEdit.Document.TextDocument())
        );

        private CompletionWindow _CompletionWindow;
        private Document _ScriptDocument;
        private Regex _CharRegex = new Regex(@"(?<Char>^\w)");

        public CustomTextEditor() : base()
        {
            SearchPanel.Install(TextArea);
            TextArea.TextEntering += TextArea_TextEntering;
            TextArea.TextEntered += TextArea_TextEntered;
        }

        public void SetCompletion(Type inHostObjectType = null)
        {
            var workspace = new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies));
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary
            );
            var scriptProjectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                "Script",
                "Script",
                LanguageNames.CSharp,
                isSubmission: true,
                hostObjectType: inHostObjectType
            )
            .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(inHostObjectType.Assembly.Location) })
            .WithCompilationOptions(compilationOptions);

            var scriptProject = workspace.AddProject(scriptProjectInfo);
            var scriptDocumentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(scriptProject.Id), "Script",
                sourceCodeKind: SourceCodeKind.Script,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(""), VersionStamp.Create())));
            _ScriptDocument = workspace.AddDocument(scriptDocumentInfo);
            var completionService = CompletionService.GetService(_ScriptDocument);
            var results = completionService.GetCompletionsAsync(_ScriptDocument, 0).Result;
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if ((_CompletionWindow == null) && (!this.IsReadOnly))
            {
                int prevPos = TextArea.Caret.Offset - 1;

                if (_CharRegex.IsMatch(e.Text))
                {
                    if (prevPos < 0)
                    {
                        ShowCompletionWindow();
                    }
                    else if (!_CharRegex.IsMatch(Document.Text.Substring(prevPos, 1)))
                    {
                        ShowCompletionWindow();
                    }
                }
            }
            else
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    _CompletionWindow.Close();
                }
            }
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (_CompletionWindow == null)
            {
                if (
                    (e.Text == "(") ||
                    (e.Text == ".") ||
                    (e.Text == ",") ||
                    (e.Text == " ") ||
                    (e.Text == "{")
                )
                {
                    ShowCompletionWindow();
                }
            }
        }

        private void ShowCompletionWindow()
        {
            var completionItems = GetCompletionList(
                AddDocument.Text.Length + TextArea.Caret.Offset,
                AddDocument.Text + Document.Text
            );

            if (completionItems != null)
            {
                _CompletionWindow = new CompletionWindow(TextArea);
                IList<ICompletionData> data = _CompletionWindow.CompletionList.CompletionData;

                foreach (var item in completionItems)
                {
                    data.Add(item);
                }
                _CompletionWindow.Show();
                _CompletionWindow.Closed += delegate
                {
                    _CompletionWindow = null;
                };
            }
        }

        private IEnumerable<CompletionData> GetCompletionList(int position, string code)
        {
            _ScriptDocument = _ScriptDocument.WithText(SourceText.From(code));
            var completionService = CompletionService.GetService(_ScriptDocument);
            var results = completionService.GetCompletionsAsync(_ScriptDocument, position).Result;

            if (results == null)
            {
                return null;
            }
            else
            {
                return results.Items.Select(item => new CompletionData()
                {
                    Content = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                    Text = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                    Description = string.Join(", ", item.Tags)
                });
            }
        }
    }

    public class CompletionData : ICompletionData
    {
        public object Content { get; set; }
        public object Description { get; set; }
        public ImageSource Image { get; set; }
        public double Priority { get; set; }
        public string Text { get; set; }
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}
