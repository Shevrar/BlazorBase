﻿using BlazorBase.CRUD.EventArguments;
using BlazorBase.CRUD.Models;
using BlazorBase.CRUD.ViewModels;
using BlazorBase.Helper;
using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.Components.Card
{
    public partial class BaseModalCard<TModel> where TModel : class, IBaseModel, new()
    {
        #region Parameter

        #region Events
        [Parameter] public EventCallback<OnGetPropertyCaptionArgs> OnGetPropertyCaption { get; set; }
        [Parameter] public EventCallback OnCardClosed { get; set; }
        [Parameter] public EventCallback<OnCreateNewEntryInstanceArgs> OnCreateNewEntryInstance { get; set; }
        [Parameter] public EventCallback<OnBeforeAddEntryArgs> OnBeforeAddEntry { get; set; }
        [Parameter] public EventCallback<OnAfterAddEntryArgs> OnAfterAddEntry { get; set; }
        [Parameter] public EventCallback<OnBeforeUpdateEntryArgs> OnBeforeUpdateEntry { get; set; }
        [Parameter] public EventCallback<OnAfterUpdateEntryArgs> OnAfterUpdateEntry { get; set; }
        [Parameter] public EventCallback<OnBeforeConvertPropertyTypeArgs> OnBeforeConvertPropertyType { get; set; }
        [Parameter] public EventCallback<OnBeforePropertyChangedArgs> OnBeforePropertyChanged { get; set; }
        [Parameter] public EventCallback<OnAfterPropertyChangedArgs> OnAfterPropertyChanged { get; set; }
        [Parameter] public EventCallback<OnBeforeCardSaveChangesArgs> OnBeforeSaveChanges { get; set; }
        [Parameter] public EventCallback<OnAfterCardSaveChangesArgs> OnAfterSaveChanges { get; set; }

        #region List Events
        [Parameter] public EventCallback<OnCreateNewListEntryInstanceArgs> OnCreateNewListEntryInstance { get; set; }
        [Parameter] public EventCallback<OnBeforeAddListEntryArgs> OnBeforeAddListEntry { get; set; }
        [Parameter] public EventCallback<OnAfterAddListEntryArgs> OnAfterAddListEntry { get; set; }
        [Parameter] public EventCallback<OnBeforeConvertListPropertyTypeArgs> OnBeforeConvertListPropertyType { get; set; }
        [Parameter] public EventCallback<OnBeforeListPropertyChangedArgs> OnBeforeListPropertyChanged { get; set; }
        [Parameter] public EventCallback<OnAfterListPropertyChangedArgs> OnAfterListPropertyChanged { get; set; }
        [Parameter] public EventCallback<OnBeforeRemoveListEntryArgs> OnBeforeRemoveListEntry { get; set; }
        [Parameter] public EventCallback<OnAfterRemoveListEntryArgs> OnAfterRemoveListEntry { get; set; }
        [Parameter] public EventCallback<OnAfterMoveListEntryUpArgs> OnAfterMoveListEntryUp { get; set; }
        [Parameter] public EventCallback<OnAfterMoveListEntryDownArgs> OnAfterMoveListEntryDown { get; set; }
        #endregion

        #endregion

        [Parameter] public string SingleDisplayName { get; set; }
        [Parameter] public string ExplainText { get; set; }
        [Parameter] public bool ShowEntryByStart { get; set; }
        [Parameter] public Func<EventServices, Task<IBaseModel>> EntryToBeShownByStart { get; set; }
        [Parameter] public TModel ComponentModelInstance { get; set; }
        [Parameter] public bool ShowActions { get; set; } = true;

        #endregion

        #region Injects
        [Inject] protected IStringLocalizer<BaseCard<TModel>> Localizer { get; set; }
        [Inject] protected IStringLocalizer<TModel> ModelLocalizer { get; set; }
        [Inject] protected IMessageHandler MessageHandler { get; set; }
        #endregion

        #region Properties
        public TModel CurrentModelInstance { get { return BaseCard?.CurrentModelInstance; } }
        #endregion

        #region Member
        protected Modal Modal = default!;
        protected BaseCard<TModel> BaseCard = default!;
        protected bool ContinueByUnsavedChanges = false;
        protected bool ViewMode = false;
        #endregion

        #region Init

        protected override Task OnInitializedAsync()
        {
            if (String.IsNullOrEmpty(SingleDisplayName))
                SingleDisplayName = ModelLocalizer[typeof(TModel).Name];
            else
                SingleDisplayName = ModelLocalizer[SingleDisplayName];

            return base.OnInitializedAsync();
        }

        #endregion

        public async Task ShowModalAsync(bool addingMode = false, bool viewMode = false, params object[] primaryKeys)
        {
            HideModal(); // Close modal to allow a currently open modal to be replaced with the new model to be displayed

            ContinueByUnsavedChanges = false;
            ViewMode = viewMode;

            await BaseCard.ShowAsync(addingMode, viewMode, primaryKeys);
            Modal.Show();
        }

        public virtual async Task ReloadEntityFromDatabase()
        {
            await BaseCard.ReloadEntityFromDatabase();
        }

        public async Task<bool> SaveModalAsync()
        {
            return await BaseCard.SaveCardAsync();
        }

        public async Task SaveAndCloseModalAsync()
        {
            if (await SaveModalAsync())
                HideModal();
        }

        public void HideModal()
        {
            Modal.Hide();
        }

        public void OnModalClosing(ModalClosingEventArgs args)
        {
            if (!ContinueByUnsavedChanges && HasUnsavedChanges())
            {
                args.Cancel = true;
                return;
            }

            BaseCard.ResetCard();
            InvokeAsync(async () => await OnCardClosed.InvokeAsync(null));
        }

        protected bool HasUnsavedChanges()
        {
            var result = AsyncHelper.RunSync(() => BaseCard.HasUnsavedChangesAsync());
            if (!result)
                return false;

            MessageHandler.ShowConfirmDialog(
                Localizer["Unsaved changes"],
                Localizer["There are currently unsaved changes, these will be lost when you leave the card, continue anyway?"],
                confirmButtonText: Localizer["Leave Card"],
                confirmButtonColor: Color.Secondary,
                abortButtonText: Localizer["Abort"],
                abortButtonColor: Color.Primary,
                onClosing: (closingArgs, result) => UserHandleUnsavedChangesConfirmDialog(result));

            return true;
        }

        protected virtual Task UserHandleUnsavedChangesConfirmDialog(ConfirmDialogResult result)
        {
            if (result == ConfirmDialogResult.Aborted)
                return Task.CompletedTask;

            ContinueByUnsavedChanges = true;
            HideModal();

            return Task.CompletedTask;
        }
    }
}
