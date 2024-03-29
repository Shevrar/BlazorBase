﻿using BlazorBase.CRUD.Services;
using Microsoft.Extensions.Localization;
using System;

namespace BlazorBase.CRUD.ViewModels;

/// <summary>
/// Collection of services which can be used in events
/// </summary>
/// <param name="ServiceProvider"></param>
/// <param name="DbContext"></param>
/// <param name="Localizer"></param>
public record EventServices(IServiceProvider ServiceProvider, IBaseDbContext DbContext, IStringLocalizer Localizer)
{
}
