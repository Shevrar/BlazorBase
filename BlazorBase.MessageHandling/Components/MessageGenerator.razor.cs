﻿using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using BlazorBase.MessageHandling.Models;
using Blazorise;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBase.MessageHandling.Components
{
    public partial class MessageGenerator
    {
        #region Properties

        protected Dictionary<Guid, ModalInfo> ModalInfos { get; set; } = new Dictionary<Guid, ModalInfo>();
        #endregion

        #region Injects
        [Inject]
        private IMessageHandler MessageHandler { get; set; }

        [Inject]
        private IStringLocalizer<MessageGenerator> Localizer { get; set; }
        #endregion

        #region Init
        protected override async Task OnInitializedAsync()
        {
            await InvokeAsync(() =>
            {
                MessageHandler.OnShowMessage += MessageHandler_OnShowMessage;
                MessageHandler.OnShowConfirmDialog += MessageHandler_OnShowConfirmDialog;
            });
        }
        #endregion

        #region Message Handler
        protected void MessageHandler_OnShowMessage(ShowMessageArgs args)
        {
            ShowMessage(args);
        }

        private void MessageHandler_OnShowConfirmDialog(ShowConfirmDialogArgs args)
        {
            ShowConfirmDialog(args);
        }
        #endregion

        #region Methods

        public void ShowMessage(ShowMessageArgs args)
        {
            if (args.IsHandled)
                return;

            InvokeAsync(() =>
            {
                args.IsHandled = true;

                if (args is ShowConfirmDialogArgs confirmDialogArgs)
                {
                    if (confirmDialogArgs.ConfirmButtonText == null)
                        confirmDialogArgs.ConfirmButtonText = Localizer["Confirm"];

                    if (args.CloseButtonText == null)
                        args.CloseButtonText = Localizer["Abort"];
                }

                if (args.CloseButtonText == null)
                    args.CloseButtonText = Localizer["Ok"];

                if (args.Icon == null)
                {
                    switch (args.MessageType)
                    {
                        case MessageType.Information:
                            args.Icon = FontAwesomeIcons.InfoCircle;
                            break;
                        case MessageType.Error:
                            args.Icon = FontAwesomeIcons.ExclamationTriangle;
                            args.IconStyle = "color: red";
                            break;
                        case MessageType.Warning:
                            args.Icon = FontAwesomeIcons.ExclamationTriangle;
                            args.IconStyle = "color: yellow";
                            break;
                    }
                }

                ModalInfos.Add(Guid.NewGuid(), new ModalInfo(args));

                StateHasChanged();
            });
        }

        public void ShowMessage(string title, string message,
                                MessageType messageType = MessageType.Information,
                                Func<ModalClosingEventArgs, Task> onClosing = null,
                                object icon = null,
                                string closeButtonText = null,
                                Color closeButtonColor = Color.Secondary,
                                ModalSize modalSize = ModalSize.Large)
        {
            ShowMessage(new ShowMessageArgs(title, message, messageType, onClosing, icon, closeButtonText, closeButtonColor, modalSize));
        }

        public void ShowConfirmDialog(string title, string message,
                                  MessageType messageType = MessageType.Information,
                                  Func<ModalClosingEventArgs, ConfirmDialogResult, Task> onClosing = null,
                                  object icon = null,
                                  string confirmButtonText = null,
                                  Color confirmButtonColor = Color.Primary,
                                  string abortButtonText = null,
                                  Color abortButtonTextColor = Color.Secondary,
                                  ModalSize modalSize = ModalSize.Large)
        {
            ShowMessage(new ShowConfirmDialogArgs(title, message, messageType, onClosing, icon, confirmButtonText, confirmButtonColor, abortButtonText, abortButtonTextColor, modalSize));
        }
        public void ShowConfirmDialog(ShowConfirmDialogArgs args)
        {
            ShowMessage(args);
        }
        #endregion

        #region Modal
        protected void OnModalClosed(Guid id)
        {
            ModalInfos.Remove(id);
        }

        protected async Task OnModalClosing(ModalInfo modalInfo, ModalClosingEventArgs args)
        {
            try
            {
                if (modalInfo.Args is ShowConfirmDialogArgs confirmDialogArgs)
                    await (confirmDialogArgs.OnClosing?.Invoke(args, modalInfo.ConfirmDialogResult ?? ConfirmDialogResult.Aborted) ?? Task.CompletedTask);
                else
                    await (modalInfo.Args.OnClosing?.Invoke(args) ?? Task.CompletedTask);
            }
            catch (Exception e)
            {
                ShowMessage(Localizer["Error"], e.Message, MessageType.Error);
            }
        }

        protected void OnConfirmButtonClicked(ModalInfo modalInfo)
        {
            modalInfo.ConfirmDialogResult = ConfirmDialogResult.Confirmed;
            modalInfo.Modal.Hide();
        }

        #endregion
    }


}
