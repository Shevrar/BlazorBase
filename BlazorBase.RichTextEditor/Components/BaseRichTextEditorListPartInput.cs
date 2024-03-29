﻿using BlazorBase.CRUD.Models;
using System.Threading.Tasks;

namespace BlazorBase.RichTextEditor.Components;

public class BaseRichTextEditorListPartInput : BaseRichTextEditorInput, IBasePropertyCardInput, IBasePropertyListPartInput
{
    protected override Task OnInitializedAsync()
    {
        HidePropertyName = true;
        return base.OnInitializedAsync();
    }
}
