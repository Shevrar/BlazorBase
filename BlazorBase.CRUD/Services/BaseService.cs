using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorBase.CRUD.Extensions;
using BlazorBase.CRUD.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BlazorBase.CRUD.ViewModels;
using BlazorBase.MessageHandling.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using BlazorBase.CRUD.EventArguments;

namespace BlazorBase.CRUD.Services
{
    public class BaseService : IDisposable
    {
        public DbContext DbContext { get; protected set; }
        public IServiceProvider ServiceProvider { get; }
        protected IMessageHandler MessageHandler { get; set; }

        public BaseService(DbContext context, IServiceProvider provider, IMessageHandler messageHandler)
        {
            DbContext = context;
            ServiceProvider = provider;
            MessageHandler = messageHandler;
        }

        public async Task RefreshDbContextAsync()
        {
            await DbContext.DisposeAsync();
            DbContext = ServiceProvider.GetService<DbContext>();
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            DbContext?.Dispose();
        }
        #endregion

        #region GetData

        public async virtual Task<T> GetAsync<T>(params object[] keyValues) where T : class
        {
            return await DbContext.Set<T>().FindAsync(keyValues);
        }

        public async virtual Task<object> GetAsync(Type type, params object[] keyValues)
        {
            return await DbContext.FindAsync(type, keyValues);
        }

        public async virtual Task<T> GetWithAllNavigationPropertiesAsync<T>(params object[] keyValues) where T : class
        {
            var entry = await GetAsync<T>(keyValues);
            if (entry == null)
                return null;

            IEnumerable<INavigation> navigationProperties = DbContext.Model.FindEntityType(typeof(T)).GetNavigations();
            if (navigationProperties == null)
                return entry;

            foreach (var navigationProperty in navigationProperties)
            {
                if (navigationProperty.IsCollection)
                    await DbContext.Entry(entry).Collection(navigationProperty.Name).LoadAsync();
                else
                    await DbContext.Entry(entry).Reference(navigationProperty.Name).LoadAsync();
            }

            return entry;
        }

        public virtual Task<List<T>> GetDataAsync<T>(bool asNoTracking = false) where T : class
        {
            var query = DbContext.Set<T>().AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.ToList());

        }
        public virtual Task<List<T>> GetDataAsync<T>(int index, int count, bool asNoTracking = false) where T : class
        {
            var query = DbContext.Set<T>().Skip(index).Take(count);
            
            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.ToList());
        }

        public virtual Task<List<T>> GetDataAsync<T>(Expression<Func<T, bool>> dataLoadCondition, bool asNoTracking = false) where T : class
        {
            var query = DbContext.Set<T>().Where(dataLoadCondition);
            
            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.ToList());
        }

        public virtual Task<List<T>> GetDataAsync<T>(Expression<Func<T, bool>> dataLoadCondition, int index, int count, bool asNoTracking = false) where T : class
        {
            var query = DbContext.Set<T>().Where(dataLoadCondition).Skip(index).Take(count);

            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.ToList());
        }

        public virtual Task<List<T>> GetDataAsync<T>(Expression<Func<IBaseModel, bool>> dataLoadCondition, bool asNoTracking = false) where T : class, IBaseModel
        {
            var query = DbContext.Set<T>().Where(dataLoadCondition);

            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.Cast<T>().ToList());
        }

        public virtual Task<List<T>> GetDataAsync<T>(Expression<Func<IBaseModel, bool>> dataLoadCondition, int index, int count, bool asNoTracking = false) where T : class, IBaseModel
        {
            var query = DbContext.Set<T>().Where(dataLoadCondition).Skip(index).Take(count);

            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.Cast<T>().ToList());
        }

        /// <summary>
        /// Super Slow -> use only if neccessary!
        /// </summary>
        public virtual Task<List<object>> GetDataAsync(Type type, bool asNoTracking = false)
        {
            var query = DbContext.Set(type).AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            return Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Super Slow -> use only if neccessary!
        /// </summary>
        public virtual IQueryable<object> Set(Type type)
        {
            return DbContext.Set(type);
        }

        public virtual IQueryable<T> Set<T>() where T : class
        {
            return DbContext.Set<T>();
        }
        #endregion

        #region GetSpecialData
        public virtual Guid GetNewPrimaryKey<T>() where T : class
        {
            Guid guid;
            do
            {
                guid = Guid.NewGuid();
            } while (DbContext.Find<T>(guid) != null);

            return guid;
        }

        public async virtual Task<Guid> GetNewPrimaryKeyAsync<T>() where T : class
        {
            Guid guid;
            do
            {
                guid = Guid.NewGuid();
            } while (await DbContext.FindAsync<T>(guid) != null);

            return guid;
        }
        #endregion

        #region Count Data
        public async virtual Task<int> CountDataAsync<T>() where T : class
        {
            return await DbContext.Set<T>().CountAsync();
        }

        public virtual Task<int> CountDataAsync<T>(Expression<Func<T, bool>> dataLoadCondition) where T : class
        {
            return Task.FromResult(DbContext.Set<T>().Where(dataLoadCondition).Count());
        }

        public virtual Task<int> CountDataAsync<T>(Expression<Func<IBaseModel, bool>> dataLoadCondition) where T : class, IBaseModel
        {
            return Task.FromResult(DbContext.Set<T>().Where(dataLoadCondition).Count());
        }
        #endregion

        #region Any
        public virtual Task<bool> AnyAsync<T>(Expression<Func<T, bool>> condition) where T : class, IBaseModel
        {
            return DbContext.Set<T>().AnyAsync(condition);
        }
        #endregion

        #region Change Data
        public async virtual Task ReloadAsync<T>(T entry) where T : class
        {
            await DbContext.Entry(entry).ReloadAsync();
        }

        public virtual bool AddEntry<T>(T entry, bool skipEntryAlreadyExistCheck = false) where T : class, IBaseModel
        {
            if (entry == null)
                return false;

            if (skipEntryAlreadyExistCheck && DbContext.Find<T>(entry.GetPrimaryKeys()) != null)
                return false;

            DbContext.Set<T>().Add(entry);

            return true;
        }

        public async virtual Task<bool> AddEntryAsync<T>(T entry, bool skipEntryAlreadyExistCheck = false) where T : class, IBaseModel
        {
            if (entry == null)
                return false;

            if (skipEntryAlreadyExistCheck && await DbContext.Set<T>().FindAsync(entry.GetPrimaryKeys()) != null)
                return false;

            DbContext.Set<T>().Add(entry);

            return true;
        }

        public virtual void UpdateEntry<T>(T entry) where T : class
        {
            DbContext.Set<T>().Update(entry);
        }
        #endregion

        #region Remove Data
        public async virtual Task<bool> RemoveEntryAsync<T>(params object[] keyValues) where T : class, IBaseModel
        {
            var entryToDelete = await DbContext.Set<T>().FindAsync(keyValues);
            if (entryToDelete == null)
                return false;

            DbContext.Set<T>().Remove(entryToDelete);

            return true;
        }

        public async virtual Task<bool> RemoveEntryAsync<T>(T entry) where T : class, IBaseModel
        {
            var entryToDelete = await DbContext.Set<T>().FindAsync(entry.GetPrimaryKeys());
            if (entryToDelete == null)
                return false;

            DbContext.Set<T>().Remove(entryToDelete);

            return true;
        }

        public virtual void RemoveRange(params object[] entriesToRemove)
        {
            DbContext.RemoveRange(entriesToRemove);
        }

        public virtual void RemoveRange(IEnumerable<object> entriesToRemove)
        {
            DbContext.RemoveRange(entriesToRemove);
        }
        #endregion

        #region SaveChanges

        public async virtual Task<int> SaveChangesAsync()
        {
            var changedEntries = await HandleOnBeforeDbContextEvents();
            var result = await DbContext.SaveChangesAsync();
            await HandleOnAfterDbContextEvents(changedEntries);

            return result;
        }

        protected async Task<(List<EntityEntry> Added, List<EntityEntry> Modified, List<EntityEntry> Deleted)> HandleOnBeforeDbContextEvents()
        {
            DbContext.ChangeTracker.DetectChanges();

            var markedAsAdded = DbContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).ToList();
            var markedAsModified = DbContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified).ToList();
            var markedAsDeleted = DbContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).ToList();

            var eventServices = GetEventServices();

            var addArgs = new OnBeforeDbContextAddEntryArgs(eventServices);
            foreach (var item in markedAsAdded)
                if (item.Entity is IBaseModel model)
                    await model.OnBeforeDbContextAddEntry(addArgs);

            var modifyArgs = new OnBeforeDbContextModifyEntryArgs(eventServices);
            foreach (var item in markedAsModified)
                if (item.Entity is IBaseModel model)
                    await model.OnBeforeDbContextModifyEntry(modifyArgs);

            var deleteArgs = new OnBeforeDbContextDeleteEntryArgs(eventServices);
            foreach (var item in markedAsDeleted)
                if (item.Entity is IBaseModel model)
                    await model.OnBeforeDbContextDeleteEntry(deleteArgs);

            return (markedAsAdded, markedAsModified, markedAsDeleted);
        }

        protected async Task HandleOnAfterDbContextEvents((List<EntityEntry> Added, List<EntityEntry> Modified, List<EntityEntry> Deleted) changedEntries)
        {
            var eventServices = GetEventServices();

            var addArgs = new OnAfterDbContextAddedEntryArgs(eventServices);
            foreach (var item in changedEntries.Added)
                if (item.Entity is IBaseModel model)
                    await model.OnAfterDbContextAddedEntry(addArgs);

            var modifyArgs = new OnAfterDbContextModifiedEntryArgs(eventServices);
            foreach (var item in changedEntries.Modified)
                if (item.Entity is IBaseModel model)
                    await model.OnAfterDbContextModifiedEntry(modifyArgs);

            var deleteArgs = new OnAfterDbContextDeletedEntryArgs(eventServices);
            foreach (var item in changedEntries.Deleted)
                if (item.Entity is IBaseModel model)
                    await model.OnAfterDbContextDeletedEntry(deleteArgs);
        }

        public bool HasUnsavedChanges()
        {
            try
            {
                return DbContext.ChangeTracker.HasChanges();
            }
            catch (Exception) //Prevent Primary Key Null Error Issue
            {
                return true;
            }
        }
        #endregion

        #region Other
        protected EventServices GetEventServices()
        {
            return new EventServices()
            {
                ServiceProvider = ServiceProvider,
                Localizer = null,
                BaseService = this,
                MessageHandler = MessageHandler
            };
        }
        #endregion
    }
}
