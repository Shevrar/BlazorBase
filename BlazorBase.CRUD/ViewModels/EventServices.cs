﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.ViewModels
{
    public class EventServices
    {
        public IServiceProvider ServiceProvider { get; set; }
        public IStringLocalizer Localizer { get; set; }
        public DbContext DbContext { get; set; }
        
    }
}
