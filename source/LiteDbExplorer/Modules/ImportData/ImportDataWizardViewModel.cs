﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Caliburn.Micro;
using JetBrains.Annotations;
using LiteDbExplorer.Modules.Shared;
using LiteDbExplorer.Wpf.Framework;
using ReactiveUI;

namespace LiteDbExplorer.Modules.ImportData
{
    [Export(typeof(ImportDataWizardViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ImportDataWizardViewModel : Conductor<IStepsScreen>.Collection.OneActive, INavigationTarget<ImportDataOptions>
    {
        private IDisposable _activeItemObservable;
        private bool _suppressPreviousPush;

        public ImportDataWizardViewModel()
        {
            DisplayName = "Import Data";
        }

        public Stack<IStepsScreen> PreviousItems { get; } = new Stack<IStepsScreen>();

        public bool CanNext => ActiveItem?.HasNext ?? false;

        public bool CanPrevious => PreviousItems.Count > 1;

        public bool IsBusy { get; private set; }

        public void Init(ImportDataOptions modelParams)
        {
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);

            var importDataHandlerSelector = IoC.Get<ImportDataHandlerSelector>();

            ActivateItem(importDataHandlerSelector);
        }

        public override void ActivateItem(IStepsScreen item)
        {
            _activeItemObservable?.Dispose();

            if (!_suppressPreviousPush)
            {
                PreviousItems.Push(ActiveItem);
            }
            
            base.ActivateItem(item);
            
            _activeItemObservable = item
                .ObservableForProperty(screen => screen.HasNext)
                .Subscribe(args => NotifyOfPropertyChange(nameof(CanNext)));

            InvalidateProperties();
        }

        public override void DeactivateItem(IStepsScreen item, bool close)
        {
            base.DeactivateItem(item, close);

            PreviousItems.Push(item);

            InvalidateProperties();
        }

        [UsedImplicitly]
        public async Task Next()
        {
            if (ActiveItem == null || !ActiveItem.Validate())
            {
                return;
            }

            IsBusy = true;

            if (await ActiveItem?.Next() is IStepsScreen next)
            {
                ActivateItem(next);
            }

            IsBusy = false;
        }

        [UsedImplicitly]
        public void Previous()
        {
            var previous = PreviousItems.Pop();
            if (previous != null)
            {
                _suppressPreviousPush = true;
                ActivateItem(previous);
                _suppressPreviousPush = false;
            }

            InvalidateProperties();
        }

        private void InvalidateProperties()
        {
            NotifyOfPropertyChange(nameof(CanNext));
            NotifyOfPropertyChange(nameof(CanPrevious));
        }
    }

}