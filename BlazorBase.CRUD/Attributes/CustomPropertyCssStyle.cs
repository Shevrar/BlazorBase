﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.Attributes
{
    public class CustomPropertyCssStyleAttribute : Attribute
    {
        public string Style { get; set; }
    }
}
