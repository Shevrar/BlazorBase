using BlazorBase.CRUD.Enums;
using BlazorBase.CRUD.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.Models
{
    public class PageActionGroup
    {
        public string Caption { get; set; }
        public string ToolTip { get; set; }
        public object Image { get; set; }
        public GUIType[] VisibleInGUITypes { get; set; } = new GUIType[] { GUIType.Card, GUIType.List, GUIType.ListPart };
        public Func<EventServices, Task<bool>> Visible { get; set; } = x => Task.FromResult(true);
        public List<PageAction> PageActions { get; set; } = new List<PageAction>();

        public static class DefaultGroups
        {
            public static readonly string Process = "Process";
            public static readonly string RelatedInformations = "Related Informations";
        }
    }
}
