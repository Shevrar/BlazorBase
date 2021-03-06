using BlazorBase.CRUD.Attributes;
using BlazorBase.CRUD.Enums;
using BlazorBase.CRUD.EventArguments;
using BlazorBase.CRUD.Extensions;
using BlazorBase.CRUD.Models;
using BlazorBase.CRUD.Services;
using BlazorBase.CRUD.ViewModels;
using BlazorBase.Modules;
using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.Components
{
    public class BaseDisplayComponent : ComponentBase
    {
        public class DisplayGroup
        {
            public DisplayGroup(VisibleAttribute groupAttribute, List<DisplayItem> displayItems)
            {
                GroupAttribute = groupAttribute;
                DisplayItems = displayItems;
            }
            public VisibleAttribute GroupAttribute { get; set; }
            public List<DisplayItem> DisplayItems { get; set; }
        }

        public class DisplayItem
        {
            public DisplayItem(PropertyInfo property, VisibleAttribute attribute, bool isReadonly, bool isKey, bool isListProperty,
                DateInputMode dateInputMode, string displayPropertyPath, Type displayPropertyType, bool isSortable, Enums.SortDirection sortDirection, bool isFilterable)
            {
                Property = property;
                Attribute = attribute;
                IsReadOnly = isReadonly;
                IsKey = isKey;
                IsListProperty = isListProperty;
                DateInputMode = dateInputMode;
                DisplayPropertyPath = displayPropertyPath;
                DisplayPropertyType = displayPropertyType;
                IsSortable = isSortable;
                SortDirection = sortDirection;
                IsFilterable = isFilterable;
            }

            public PropertyInfo Property { get; set; }
            public VisibleAttribute Attribute { get; set; }
            public bool IsReadOnly { get; set; }
            public bool IsKey { get; set; }
            public bool IsListProperty { get; set; }
            public DateInputMode DateInputMode { get; set; }
            public Enums.SortDirection SortDirection { get; set; }
            public FilterType FilterType { get; set; }
            public object FilterValue { get; set; }
            public string DisplayPropertyPath { get; set; }
            public Type DisplayPropertyType { get; set; }
            public bool IsSortable { get; set; }
            public bool IsFilterable { get; set; }
        }

        #region Injects
        [Inject] protected ErrorHandler ErrorHandler { get; set; }
        [Inject] protected IStringLocalizerFactory StringLocalizerFactory { get; set; }
        [Inject] protected IStringLocalizer<BaseDisplayComponent> BaseDisplayComponentLocalizer { get; set; }
        #endregion

        #region Protected Properties
        protected virtual List<PropertyInfo> VisibleProperties { get; set; } = new();
        protected virtual Dictionary<string, DisplayGroup> DisplayGroups { get; set; } = new();
        protected virtual Dictionary<PropertyInfo, List<KeyValuePair<string, string>>> ForeignKeyProperties { get; set; }
        protected static ConcurrentDictionary<long, List<KeyValuePair<string, string>>> CachedEnumValueDictionary { get; set; } = new();
        protected virtual Dictionary<Type, List<KeyValuePair<string, string>>> CachedForeignKeys { get; set; } = new();
        protected virtual Dictionary<PropertyInfo, List<KeyValuePair<string, string>>> UsesCustomLookupDataProperties { get; set; } = new();
        #endregion

        #region Member
        protected MarkupString InvalidSummaryFeedback;
        protected bool ShowInvalidFeedback = false;
        #endregion

        protected virtual List<PropertyInfo> GetVisibleProperties(Type modelType, GUIType guiType, IBaseModel componentModelInstance = null)
        {
            if (componentModelInstance == null)
                return modelType.GetVisibleProperties(guiType);

            return componentModelInstance.GetVisibleProperties(guiType);
        }

        protected virtual void SetUpDisplayLists(Type modelType, GUIType guiType, IBaseModel componentModelInstance = null)
        {
            VisibleProperties = GetVisibleProperties(modelType, guiType, componentModelInstance);

            foreach (var property in VisibleProperties)
            {
                var attribute = property.GetCustomAttributes(typeof(VisibleAttribute)).First() as VisibleAttribute;
                attribute.DisplayGroup = String.IsNullOrEmpty(attribute.DisplayGroup) ? BaseDisplayComponentLocalizer["General"] : attribute.DisplayGroup;
                var dateInputMode = property.GetCustomAttribute<DateDisplayModeAttribute>()?.DateInputMode ?? DateInputMode.Date;
                var customPropertyPath = property.GetCustomAttribute<CustomSortAndFilterPropertyPathAttribute>();

                if (!DisplayGroups.ContainsKey(attribute.DisplayGroup))
                    DisplayGroups[attribute.DisplayGroup] = new DisplayGroup(attribute, new List<DisplayItem>());

                (string DisplayPath, Type DisplayType) displayPathAndType;
                bool sortAndFilterable;
                if (customPropertyPath == null)
                {
                    displayPathAndType = GetDisplayPropertyPathAndType(property);
                    sortAndFilterable = GetPropertyIsSortAndFilterable(property);
                }
                else
                {
                    displayPathAndType = (customPropertyPath.Path, customPropertyPath.PathType);
                    sortAndFilterable = true;
                }

                DisplayGroups[attribute.DisplayGroup].DisplayItems.Add(
                    new DisplayItem(property, attribute, property.IsReadOnlyInGUI(), property.IsKey(),
                        property.IsListProperty(), dateInputMode, displayPathAndType.DisplayPath,
                        displayPathAndType.DisplayType, sortAndFilterable, attribute.SortDirection, sortAndFilterable
                ));
            }

            SortDisplayLists();
        }

        protected virtual (string DisplayPath, Type DisplayType) GetDisplayPropertyPathAndType(PropertyInfo property)
        {
            if (!property.IsForeignKey() || property.IsListProperty())
                return (property.Name, property.PropertyType);

            var foreignKey = property.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
            var foreignProperty = property.ReflectedType.GetProperties().Where(entry => entry.Name == foreignKey.Name).FirstOrDefault();
            var foreignKeyType = foreignProperty.GetCustomAttribute<RenderTypeAttribute>()?.RenderType ?? foreignProperty?.PropertyType;

            if (foreignKeyType == null)
                return (property.Name, property.PropertyType);
            if (!typeof(IBaseModel).IsAssignableFrom(foreignKeyType))
                return (property.Name, property.PropertyType);

            var displayKeyProperties = foreignKeyType.GetDisplayKeyProperties();
            if (displayKeyProperties.Count == 0)
                displayKeyProperties = foreignKeyType.GetKeyProperties();

            List<string> displayPropertyPaths = new();
            foreach (var displayKeyProperty in displayKeyProperties)
                displayPropertyPaths.Add($"{foreignKey.Name}.{displayKeyProperty.Name}");

            return (String.Join("|", displayPropertyPaths), displayKeyProperties[0].PropertyType);
        }

        protected virtual bool GetPropertyIsSortAndFilterable(PropertyInfo property)
        {
            if (property.HasAttribute(typeof(NotMappedAttribute)))
                return false;

            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();
            if (getMethod == null || setMethod == null)
                return false;

            //Check if get and set method are not overridden with custom logic and are just normal property get and set methods
            return Attribute.IsDefined(getMethod, typeof(CompilerGeneratedAttribute)) && Attribute.IsDefined(setMethod, typeof(CompilerGeneratedAttribute));
        }

        protected virtual void SortDisplayLists()
        {
            foreach (var displayGroup in DisplayGroups)
            {
                displayGroup.Value.DisplayItems.Sort((x, y) => x.Attribute.DisplayOrder.CompareTo(y.Attribute.DisplayOrder));
                displayGroup.Value.GroupAttribute = displayGroup.Value.DisplayItems.First().Attribute;
            }

            DisplayGroups = DisplayGroups.OrderBy(entry => entry.Value.GroupAttribute.DisplayGroupOrder).ToDictionary(x => x.Key, x => x.Value);
        }

        protected virtual async Task PrepareForeignKeyProperties(BaseService service, IBaseModel instance = null)
        {
            if (ForeignKeyProperties != null)
                return;

            ForeignKeyProperties = new Dictionary<PropertyInfo, List<KeyValuePair<string, string>>>();

            var foreignKeyProperties = VisibleProperties.Where(entry => entry.IsForeignKey());
            foreach (var foreignKeyProperty in foreignKeyProperties)
            {
                var isReadonly = foreignKeyProperty.IsReadOnlyInGUI();
                var foreignKey = foreignKeyProperty.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
                var foreignProperty = foreignKeyProperty.ReflectedType.GetProperties().Where(entry => entry.Name == foreignKey.Name).FirstOrDefault();
                var foreignKeyType = foreignProperty.GetCustomAttribute<RenderTypeAttribute>()?.RenderType ?? foreignProperty?.PropertyType;

                if (foreignKeyType == null)
                    throw new CRUDException(BaseDisplayComponentLocalizer["Can not find the foreign key property type in the class {0}, on the property {1}. This is a development error, maybe the foreign property name is spelled  wrong in the property attribute.", foreignKeyProperty.DeclaringType, foreignKeyProperty.Name]);

                if (!typeof(IBaseModel).IsAssignableFrom(foreignKeyType))
                    continue;

                if (CachedForeignKeys.ContainsKey(foreignKeyType))
                {
                    ForeignKeyProperties.Add(foreignKeyProperty, CachedForeignKeys[foreignKeyType]);
                    continue;
                }

                var displayKeyProperties = foreignKeyType.GetDisplayKeyProperties();
                var primaryKeys = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>(null, String.Empty)
                };

                if (isReadonly && instance != null)
                {
                    var foreignKeyValue = foreignKeyProperty.GetValue(instance);
                    if (foreignKeyValue != null)
                    {
                        var entry = await service.GetAsync(foreignKeyType, foreignKeyValue);
                        AddEntryToForeignKeyList(entry as IBaseModel, primaryKeys, displayKeyProperties);
                    }

                    ForeignKeyProperties.Add(foreignKeyProperty, primaryKeys);
                    continue;
                }

                var entries = await service.GetDataAsync(foreignKeyType);
                foreach (var entry in entries)
                    AddEntryToForeignKeyList(entry as IBaseModel, primaryKeys, displayKeyProperties);

                CachedForeignKeys.Add(foreignKeyType, primaryKeys);
                ForeignKeyProperties.Add(foreignKeyProperty, primaryKeys);
            }
        }

        protected void AddEntryToForeignKeyList(IBaseModel model, List<KeyValuePair<string, string>> foreignKeyList, List<PropertyInfo> displayKeyProperties)
        {
            var primaryKeysAsString = model.GetPrimaryKeysAsString();

            if (displayKeyProperties.Count == 0)
                foreignKeyList.Add(new KeyValuePair<string, string>(primaryKeysAsString, primaryKeysAsString));
            else
                foreignKeyList.Add(new KeyValuePair<string, string>(primaryKeysAsString, model?.GetDisplayKeyKeyValuePair(displayKeyProperties)));
        }

        protected virtual async Task PrepareCustomLookupData(IBaseModel cardModel, EventServices eventServices)
        {
            UsesCustomLookupDataProperties.Clear();

            var properties = VisibleProperties.Where(entry => entry.UsesCustomLookupData());
            foreach (var property in properties)
            {
                var useCustomLookupData = property.GetCustomAttribute(typeof(UseCustomLookupData)) as UseCustomLookupData;
                var lookupDataSourceMethod = property.ReflectedType.GetMethod(useCustomLookupData.LookupDataSourceMethodName, BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);
                var parameters = lookupDataSourceMethod?.GetParameters();

                if (lookupDataSourceMethod == null ||
                    parameters.Length != 4 ||
                    parameters[0].ParameterType != typeof(PropertyInfo) ||
                    parameters[1].ParameterType != typeof(IBaseModel) ||
                    parameters[2].ParameterType != typeof(List<KeyValuePair<string, string>>) ||
                    parameters[3].ParameterType != typeof(EventServices) ||
                    lookupDataSourceMethod.ReturnType != typeof(Task) ||
                    !lookupDataSourceMethod.IsStatic)
                    throw new CRUDException(BaseDisplayComponentLocalizer["The signature of the custom lookup data source method {0} in the class {1}, does not match the following signature: public static [async] Task TheMethodName(PropertyInfo propertyInfo, IBaseModel cardModel, List<KeyValuePair<string, string>> lookupData, EventServices eventServices)", useCustomLookupData.LookupDataSourceMethodName, property.ReflectedType.Name]);

                var lookupData = new List<KeyValuePair<string, string>>();
                await (lookupDataSourceMethod.Invoke(null, new object[] { property, cardModel, lookupData, eventServices }) as Task);
                UsesCustomLookupDataProperties.Add(property, lookupData);
            }
        }

        protected virtual long GetEnumTypeDictionaryKey(Type enumType)
        {
            return enumType.GetHashCode() * 10000L + CultureInfo.CurrentUICulture.LCID;
        }


        protected virtual List<KeyValuePair<string, string>> GetEnumValues(Type enumType)
        {
            long key = GetEnumTypeDictionaryKey(enumType);

            if (CachedEnumValueDictionary.ContainsKey(key))
                return CachedEnumValueDictionary[key];

            var result = new List<KeyValuePair<string, string>>();
            var values = Enum.GetNames(enumType);
            var localizer = StringLocalizerFactory.Create(enumType);
            foreach (var value in values)
                result.Add(new KeyValuePair<string, string>(value, localizer[value]));

            CachedEnumValueDictionary.TryAdd(key, result);
            return result;
        }

        protected virtual Dictionary<string, string> RemoveNavigationQueryByType(Type type, string baseQuery)
        {
            var query = QueryHelpers.ParseQuery(baseQuery).ToDictionary(key => key.Key, val => val.Value.ToString());

            var keyProperties = type.GetKeyProperties();

            var primaryKeys = new List<object>();
            foreach (var keyProperty in keyProperties)
                query.Remove(keyProperty.Name);

            return query;
        }

        protected virtual string GetPropertyCaption(EventServices eventServices, IBaseModel model, IStringLocalizer modelLocalizer, DisplayItem displayItem)
        {
            var args = new OnGetPropertyCaptionArgs(model, displayItem, modelLocalizer[displayItem.Property.Name], eventServices);
            model.OnGetPropertyCaption(args);

            return args.Caption;
        }

        #region Feedback

        protected virtual void ShowFormattedInvalidFeedback(string feedback)
        {
            InvalidSummaryFeedback = MarkupStringValidator.GetWhiteListedMarkupString(feedback);
            ShowInvalidFeedback = true;
        }

        protected virtual void ResetInvalidFeedback()
        {
            InvalidSummaryFeedback = (MarkupString)String.Empty;
            ShowInvalidFeedback = false;
        }

        protected virtual async Task OnPageActionInvokedAsync(Exception e)
        {
            ResetInvalidFeedback();
            if (e != null)
                ShowFormattedInvalidFeedback(ErrorHandler.PrepareExceptionErrorMessage(e));

            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
        #endregion
    }
}
