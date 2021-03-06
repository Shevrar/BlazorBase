using BlazorBase.CRUD.Attributes;
using BlazorBase.CRUD.Enums;
using BlazorBase.CRUD.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace BlazorBase.CRUD.Extensions
{
    public static class PropertyInfoExtension
    {

        public static string GetPlaceholderText(this PropertyInfo propertyInfo)
        {
            if (Attribute.IsDefined(propertyInfo, typeof(PlaceholderTextAttribute)))
                return (propertyInfo.GetCustomAttribute(typeof(PlaceholderTextAttribute)) as PlaceholderTextAttribute).Placeholder;
            else
                return String.Empty;
        }

        public static bool HasAttribute(this PropertyInfo propertyInfo, Type type)
        {
            return Attribute.IsDefined(propertyInfo, type);
        }

        public static T GetAttribute<T>(this PropertyInfo propertyInfo) where T : class
        {
            return propertyInfo.GetCustomAttribute(typeof(T)) as T;
        }

        public static bool TryGetAttribute<T>(this PropertyInfo propertyInfo, out T attribute) where T : class
        {
            if (propertyInfo.HasAttribute(typeof(T)))
            {
                attribute = propertyInfo.GetCustomAttribute(typeof(T)) as T;
                return true;
            }
            else
            {
                attribute = default;
                return false;
            }
        }

        public static bool IsKey(this PropertyInfo propertyInfo)
        {
            return propertyInfo.HasAttribute(typeof(KeyAttribute));
        }

        public static bool IsForeignKey(this PropertyInfo propertyInfo)
        {
            return propertyInfo.HasAttribute(typeof(ForeignKeyAttribute));
        }

        public static bool UsesCustomLookupData(this PropertyInfo propertyInfo)
        {
            return propertyInfo.HasAttribute(typeof(UseCustomLookupData));
        }


        public static bool IsDisplayKey(this PropertyInfo propertyInfo)
        {
            return propertyInfo.HasAttribute(typeof(DisplayKeyAttribute));
        }

        public static bool IsVisibleInGUI(this PropertyInfo propertyInfo, GUIType? guiType = null)
        {
            bool isVisible;
            if (guiType == null)
                isVisible = propertyInfo.HasAttribute(typeof(VisibleAttribute));
            else
                isVisible = propertyInfo.GetCustomAttribute(typeof(VisibleAttribute)) is VisibleAttribute visible && !visible.HideInGUITypes.Contains(guiType.Value);

            if (isVisible && IsBaseModelDateProperty(propertyInfo))
                return !HideBaseModelDateProperties(propertyInfo, guiType);
            else
                return isVisible;
        }

        public static bool IsBaseModelDateProperty(this PropertyInfo propertyInfo)
        {
            return typeof(BaseModel).IsAssignableFrom(propertyInfo.ReflectedType) && (propertyInfo.Name == nameof(BaseModel.CreatedOn) || propertyInfo.Name == nameof(BaseModel.ModifiedOn));
        }

        public static bool HideBaseModelDateProperties(this PropertyInfo propertyInfo, GUIType? guiType = null)
        {
            return propertyInfo.ReflectedType.GetCustomAttribute(typeof(HideBaseModelDatePropertiesInGUIAttribute)) is HideBaseModelDatePropertiesInGUIAttribute hideProperty &&
                (
                    hideProperty.HideCreatedOn && propertyInfo.Name == nameof(BaseModel.CreatedOn) ||
                    hideProperty.HideModifiedOn && propertyInfo.Name == nameof(BaseModel.ModifiedOn)
                ) && (guiType == null || hideProperty.HideInGUITypes.Contains(guiType.Value));
        }

        public static bool IsReadOnlyInGUI(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute(typeof(EditableAttribute)) is EditableAttribute editable && !editable.AllowEdit;
        }

        public static bool IsListProperty(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return false;

            return propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }
    }
}
