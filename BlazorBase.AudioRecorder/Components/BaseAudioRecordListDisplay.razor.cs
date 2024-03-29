﻿using BlazorBase.AudioRecorder.Models;
using BlazorBase.CRUD.Enums;
using BlazorBase.CRUD.Models;
using BlazorBase.CRUD.Services;
using BlazorBase.CRUD.ViewModels;
using BlazorBase.Files.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Reflection;
using static BlazorBase.CRUD.Components.General.BaseDisplayComponent;

namespace BlazorBase.AudioRecorder.Components;

public partial class BaseAudioRecordListDisplay : ComponentBase, IBasePropertyListDisplay
{
    #region Parameters
    [Parameter] public IBaseModel Model { get; set; } = null!;
    [Parameter] public PropertyInfo Property { get; set; } = null!;
    [Parameter] public IBaseDbContext DbContext { get; set; } = null!;
    [Parameter] public IStringLocalizer ModelLocalizer { get; set; } = null!;
    [Parameter] public DisplayItem DisplayItem { get; set; } = null!;
    [Parameter] public GUIType IsDisplayedInGuiType { get; set; }
    [Parameter] public string? Class { get; set; }
    #endregion

    #region Injects
    [Inject] protected IStringLocalizer<BaseAudioRecordListDisplay> Localizer { get; set; } = null!;
    #endregion

    #region Member
    protected bool DisplayShowFileButton;
    protected bool DisplayDownloadFileButton;
    #endregion

    #region Init

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        DisplayShowFileButton = Property.GetCustomAttribute(typeof(HideShowFileButtonAttribute)) is not HideShowFileButtonAttribute hideShowButtonAttr ||
                                    !hideShowButtonAttr.HideInGUITypes.Contains(IsDisplayedInGuiType);
        DisplayDownloadFileButton = Property.GetCustomAttribute(typeof(HideDownloadFileButtonAttribute)) is not HideDownloadFileButtonAttribute hideDownloadButtonAttr ||
                                     !hideDownloadButtonAttr.HideInGUITypes.Contains(IsDisplayedInGuiType);
    }

    public Task<bool> IsHandlingPropertyRenderingAsync(IBaseModel model, DisplayItem displayItem, EventServices eventServices)
    {
        return Task.FromResult(typeof(IBaseAudioRecord).IsAssignableFrom(displayItem.Property.PropertyType));
    }

    #endregion
}
