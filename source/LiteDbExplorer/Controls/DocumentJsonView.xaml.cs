﻿using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;
using LiteDbExplorer.Controls.Editor;
using LiteDbExplorer.Controls.JsonViewer;
using LiteDbExplorer.Core;
using LiteDbExplorer.Presentation;
using LiteDbExplorer.Wpf.Modules.AvalonEdit;

namespace LiteDbExplorer.Controls
{
    /// <summary>
    /// Interaction logic for DocumentJsonView.xaml
    /// </summary>
    public partial class DocumentJsonView : UserControl
    {
        readonly FoldingManager _foldingManager;
        readonly BraceFoldingStrategy _foldingStrategy;
        private readonly SearchReplacePanel _searchReplacePanel;

        public DocumentJsonView()
        {
            InitializeComponent();

            jsonEditor.ShowLineNumbers = true;
            jsonEditor.Encoding = Encoding.UTF8;
            
            jsonEditor.Options.EnableEmailHyperlinks = false;
            jsonEditor.Options.EnableHyperlinks = false;
            
            _foldingManager = FoldingManager.Install(jsonEditor.TextArea);
            _foldingStrategy = new BraceFoldingStrategy();
            _searchReplacePanel = SearchReplacePanel.Install(jsonEditor);
            _searchReplacePanel.IsFindOnly = true;

            jsonEditor.TextArea.MaxWidth = SystemParameters.VirtualScreenWidth;
            jsonEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
            jsonEditor.TextArea.TextView.ElementGenerators.Add(new TruncateLongLines(LineMaxLength));

            CommandBindings.Add(new CommandBinding(Commands.FindNext, (sender, e) => _searchReplacePanel.FindNext(), CanExecuteWithOpenSearchPanel));
            CommandBindings.Add(new CommandBinding(Commands.FindPrevious, (sender, e) => _searchReplacePanel.FindPrevious(), CanExecuteWithOpenSearchPanel));

            SetTheme();
            
            Loaded += (sender, args) => ThemeManager.CurrentThemeChanged += ThemeManagerOnCurrentThemeChanged;
            Unloaded += (sender, args) => ThemeManager.CurrentThemeChanged -= ThemeManagerOnCurrentThemeChanged;

        }

        private void ThemeManagerOnCurrentThemeChanged(object sender, EventArgs e)
        {
            SetTheme();
        }

        public static readonly DependencyProperty DocumentSourceProperty = DependencyProperty.Register(
            nameof(DocumentSource),
            typeof(object),
            typeof(DocumentJsonView),
            new PropertyMetadata(null, propertyChangedCallback: OnDocumentSourceChanged));

        public object DocumentSource
        {
            get => (object) GetValue(DocumentSourceProperty);
            set => SetValue(DocumentSourceProperty, value);
        }

        public static readonly DependencyProperty LineMaxLengthProperty = DependencyProperty.Register(
            nameof(LineMaxLength), typeof(int), typeof(DocumentJsonView), new PropertyMetadata(1024));

        public int LineMaxLength
        {
            get => (int) GetValue(LineMaxLengthProperty);
            set => SetValue(LineMaxLengthProperty, value);
        }

        public void UpdateDocument()
        {
            if (DocumentSource != null && DocumentSource is IJsonSerializerProvider provider)
            {
                SetJson(provider);
            }
            else
            {
                ResetJson();
            }
        }

        private void SetTheme()
        {
            string theme = null;
            if (App.Settings.ColorTheme == ColorTheme.Dark)
            {
                theme = JsonHighlightingProvider.ThemeDark;
                jsonEditor.TextArea.Foreground = new SolidColorBrush(Colors.White);
                
                // _searchReplacePanel.MarkerBrush = new SolidColorBrush(Color.FromArgb(63, 144, 238, 144));
                jsonEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.FromRgb(206, 145, 120));
            }
            else
            {
                // _searchReplacePanel.MarkerBrush = new SolidColorBrush(Color.FromArgb(153, 144, 238, 144));
                jsonEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.FromRgb(26, 13, 171));
                jsonEditor.TextArea.Foreground = new SolidColorBrush(Colors.Black);
            }

            jsonEditor.SyntaxHighlighting = LocalHighlightingManager.Current.LoadDefinitionFromName(JsonHighlightingProvider.Name, theme);
        }

        private void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_searchReplacePanel.IsClosed)
            {
                e.CanExecute = false;
                e.ContinueRouting = true;
            }
            else
            {
                e.CanExecute = true;
                e.Handled = true;
            }
        }

        private static void OnDocumentSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DocumentJsonView documentJsonView))
            {
                return;
            }

            documentJsonView.UpdateDocument();
        }

        private void SetJson(IJsonSerializerProvider provider)
        {
            ThreadPool.QueueUserWorkItem(o => {
                var content = provider.Serialize(true);
                
                Dispatcher.BeginInvoke((Action) (() =>
                {
                    jsonEditor.Document.Text = content;
                    _foldingStrategy.UpdateFoldings(_foldingManager, jsonEditor.Document);   

                }), DispatcherPriority.Normal);
            });
        }

        private void ResetJson()
        {
            jsonEditor.Document.Text = string.Empty;
            _foldingStrategy.UpdateFoldings(_foldingManager, jsonEditor.Document);
        }

        private class TruncateLongLines : VisualLineElementGenerator
        {
            private readonly int _maxLength;
            const string Ellipsis = " ... ";

            public TruncateLongLines(int? maxLength = null)
            {
                _maxLength = maxLength ?? 10000;
            }

            public override int GetFirstInterestedOffset(int startOffset)
            {
                var line = CurrentContext.VisualLine.LastDocumentLine;
                if (line.Length > _maxLength)
                {
                    var ellipsisOffset = line.Offset + _maxLength - Ellipsis.Length;
                    if (startOffset <= ellipsisOffset)
                    {
                        return ellipsisOffset;
                    }
                }
                return -1;
            }

            public override VisualLineElement ConstructElement(int offset)
            {
                var formattedTextElement = new FormattedTextElement(Ellipsis, CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset)
                {
                    BackgroundBrush = new SolidColorBrush(Color.FromArgb(153, 238, 144, 144))
                };
                
                return formattedTextElement;
            }
        }
    }
}