﻿using BlazorBase.User.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System;

namespace BlazorBase.User.Components;
public partial class BaseLoginMenuItem : ComponentBase
{
    [Inject] protected IStringLocalizer<BaseLoginMenuItem> Localizer { get; set; }
    [Inject] protected IBlazorBaseUserOptions Options { get; set; }

    public string Greeting { get; set; }

    protected override void OnInitialized()
    {
        if (DateTime.Now < DateTime.Today.AddHours(3) || DateTime.Now > DateTime.Today.AddHours(17))
            Greeting = Localizer["Good evening,"];
        else if (DateTime.Now < DateTime.Today.AddHours(12))
            Greeting = Localizer["Good morning,"];
        else
            Greeting = Localizer["Good afternoon,"];
    }
}