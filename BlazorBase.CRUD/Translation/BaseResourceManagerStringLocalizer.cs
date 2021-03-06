using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using BlazorBase.CRUD.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Localization.Internal;
using Microsoft.Extensions.Logging;

namespace BlazorBase.CRUD.Translation
{
    public class BaseResourceManagerStringLocalizer : ResourceManagerStringLocalizer
    {
        readonly BaseResourceManagerStringLocalizerFactory Factory;
        protected IStringLocalizer BaseTypeLocalizer { get; set; } = null;
        public Type CurrentLocalizerType { get; }
        public BaseResourceManagerStringLocalizer(
            Assembly resourceAssembly,
            string baseName,
            IResourceNamesCache resourceNamesCache,
            ILogger logger,
            BaseResourceManagerStringLocalizerFactory factory)
            :  base(new ResourceManager(CorrectBaseNameForGenericClasses(baseName), resourceAssembly), resourceAssembly, CorrectBaseNameForGenericClasses(baseName), resourceNamesCache, logger)
        {
            Factory = factory;

            CurrentLocalizerType = Type.GetType($"{baseName}, {resourceAssembly.FullName}");
            if (CurrentLocalizerType.BaseType != null && CurrentLocalizerType.BaseType != typeof(object))
                BaseTypeLocalizer = Factory.Create(CurrentLocalizerType.BaseType);
        }

        public override LocalizedString this[string name] => GetTranslation(name);
        public override LocalizedString this[string name, params object[] arguments] => GetTranslation(name, arguments);
        protected virtual LocalizedString GetTranslation(string name, params object[] arguments)
        {
            var baseResult = base[name, arguments];
            if (!baseResult.ResourceNotFound)
                return baseResult;

            if (BaseTypeLocalizer != null)
                return BaseTypeLocalizer[name, arguments];

            return new LocalizedString(name, String.Format(name, arguments), true);
        }

        protected static string CorrectBaseNameForGenericClasses(string baseName) {
            if (baseName.Contains('`'))
                baseName = baseName.Remove(baseName.IndexOf('`'));

            return baseName;
        }
    }
}
