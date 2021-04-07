﻿using BlazorBase.CRUD.Attributes;
using BlazorBase.CRUD.Enums;
using BlazorBase.CRUD.Extensions;
using BlazorBase.CRUD.Models;
using BlazorBase.CRUD.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBase.CRUD.Components
{
    public class BaseDisplayComponent : ComponentBase
    {
        public record DisplayGroup(VisibleAttribute GroupAttribute, List<DisplayItem> DisplayItems) { public VisibleAttribute GroupAttribute { get; set; } };
        public record DisplayItem(PropertyInfo Property, VisibleAttribute Attribute, bool IsListProperty);

        #region Injects
        [Inject]
        protected StringLocalizerFactory StringLocalizerFactory { get; set; }
        #endregion

        #region Members
        protected List<PropertyInfo> VisibleProperties = new List<PropertyInfo>();
        protected Dictionary<string, DisplayGroup> DisplayGroups = new Dictionary<string, DisplayGroup>();
        protected Dictionary<PropertyInfo, List<KeyValuePair<string, string>>> ForeignKeyProperties;
        protected static Dictionary<Type, List<KeyValuePair<string, string>>> CachedEnumValueDictionary { get; set; } = new Dictionary<Type, List<KeyValuePair<string, string>>>();
        protected Dictionary<Type, List<KeyValuePair<string, string>>> CachedForeignKeys { get; set; } = new Dictionary<Type, List<KeyValuePair<string, string>>>();
        #endregion

        protected void SetUpDisplayLists(Type modelType, GUIType guiType)
        {
            VisibleProperties = modelType.GetVisibleProperties(guiType);

            foreach (var property in VisibleProperties)
            {
                var attribute = property.GetCustomAttributes(typeof(VisibleAttribute)).First() as VisibleAttribute;
                attribute.DisplayGroup = String.IsNullOrEmpty(attribute.DisplayGroup) ? "General" : attribute.DisplayGroup;

                if (!DisplayGroups.ContainsKey(attribute.DisplayGroup))
                    DisplayGroups[attribute.DisplayGroup] = new DisplayGroup(attribute, new List<DisplayItem>());

                DisplayGroups[attribute.DisplayGroup].DisplayItems.Add(new DisplayItem(property, attribute, property.IsListProperty()));
            }

            SortDisplayLists();
        }

        protected void SortDisplayLists()
        {
            foreach (var displayGroup in DisplayGroups)
            {
                displayGroup.Value.DisplayItems.Sort((x, y) => x.Attribute.DisplayOrder.CompareTo(y.Attribute.DisplayOrder));
                displayGroup.Value.GroupAttribute = displayGroup.Value.DisplayItems.First().Attribute;
            }

            DisplayGroups = DisplayGroups.OrderBy(entry => entry.Value.GroupAttribute.DisplayGroupOrder).ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task PrepareForeignKeyProperties(Type modelType, BaseService service)
        {
            if (ForeignKeyProperties != null)
                return;

            ForeignKeyProperties = new Dictionary<PropertyInfo, List<KeyValuePair<string, string>>>();

            var foreignKeyProperties = VisibleProperties.Where(entry => entry.IsForeignKey());
            foreach (var foreignKeyProperty in foreignKeyProperties)
            {
                var foreignKey = foreignKeyProperty.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
                var foreignProperty = modelType.GetProperties().Where(entry => entry.Name == foreignKey.Name).First();
                var foreignKeyType = foreignProperty.GetCustomAttribute<RenderTypeAttribute>()?.RenderType ?? foreignProperty.PropertyType;

                if (!typeof(IBaseModel).IsAssignableFrom(foreignKeyType))
                    continue;

                if (CachedForeignKeys.ContainsKey(foreignKeyType))
                {
                    ForeignKeyProperties.Add(foreignKeyProperty, CachedForeignKeys[foreignKeyType]);
                    continue;
                }
               
                var entries = (await service.GetDataAsync(foreignKeyType));
                var primaryKeys = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>(null, String.Empty)
                };
                var displayKeyProperty = foreignKeyType.GetDisplayKeyProperty();

                foreach (var entry in entries)
                {
                    var primaryKeysAsString = ((IBaseModel)entry).GetPrimaryKeysAsString();
                    if (displayKeyProperty == null)
                        primaryKeys.Add(new KeyValuePair<string, string>(primaryKeysAsString, primaryKeysAsString));
                    else
                        primaryKeys.Add(new KeyValuePair<string, string>(primaryKeysAsString, displayKeyProperty.GetValue(entry).ToString()));
                }

                CachedForeignKeys.Add(foreignKeyType, primaryKeys);
                ForeignKeyProperties.Add(foreignKeyProperty, primaryKeys);
            }
        }

        protected List<KeyValuePair<string, string>> GetEnumValues(Type enumType)
        {
            if (CachedEnumValueDictionary.ContainsKey(enumType))
                return CachedEnumValueDictionary[enumType];

            var result = new List<KeyValuePair<string, string>>();
            var values = Enum.GetNames(enumType);
            var localizer = StringLocalizerFactory.GetLocalizer(enumType);
            foreach (var value in values)
                result.Add(new KeyValuePair<string, string>(value, localizer[value]));

            CachedEnumValueDictionary.Add(enumType, result);
            return result;
        }
    }
}
