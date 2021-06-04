﻿using System.ComponentModel.Composition;
using Caliburn.Micro;
using LiteDbExplorer.Wpf.Controls;
using LiteDbExplorer.Wpf.Modules.Settings;
using PropertyTools.DataAnnotations;
// ReSharper disable InconsistentNaming

namespace LiteDbExplorer.Modules.DbDocument
{
    [Export(typeof(ISettingsEditor))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DocumentPreviewSettingsViewModel : PropertyChangedBase, ISettingsEditor, IAutoGenSettingsView
    {
        public DocumentPreviewSettingsViewModel()
        {
            DocumentPreview_SplitOrientation = Properties.Settings.Default.DocumentPreview_SplitOrientation.ToSplitOrientation();
            DocumentPreview_ContentMaxLength = Properties.Settings.Default.DocumentPreview_ContentMaxLength;
        }

        public string SettingsPagePath => Properties.Resources.SettingsPageView;
        
        public string SettingsPageName => "_Documents";
        
        public int EditorDisplayOrder => 25;

        public string GroupDisplayName => "Options";

        public object AutoGenContext => this;

        [Category("Document Preview")]
        [DisplayName("Split orientation")]
        public SplitOrientation DocumentPreview_SplitOrientation { get; set; }

        [Category("Document Preview")]
        [DisplayName("Content maximum length")]
        [Spinnable(1, 1, 64, 4069), Width(80)]
        public int DocumentPreview_ContentMaxLength { get; set; }

        public void ApplyChanges()
        {
            Properties.Settings.Default.DocumentPreview_SplitOrientation = DocumentPreview_SplitOrientation.ToOrientation();
            Properties.Settings.Default.DocumentPreview_ContentMaxLength = DocumentPreview_ContentMaxLength;

            Properties.Settings.Default.Save();
        }

        public void DiscardChanges()
        {
            // Ignore
        }
    }
}