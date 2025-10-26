/*
    This library is intended as a starting point for creating Business Logic Layer in database oriented applications.
    Copyright (C) 2019 Srdjan Rudic
    Email: blaster7th@gmail.com

    This library is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this library. If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Dapper;
using System.Collections;
using System.Reflection;

namespace BlasteR.Base
{
    /// <summary>
    /// Generic class for accessing data through the Bll layer. Should be used as a base class for every other Bll class.
    /// </summary>
    /// <typeparam name="T">Type used for accessing the data.</typeparam>
    public interface IBaseBll<T>
    where T : BaseEntity
    {
        T this[int id] { get; set; }

        IUnitOfWork UnitOfWork { get; }
        IDbConnection DB { get; }
        IDbTransaction Transaction { get; }

        int Delete(IEnumerable<int> entityIds, bool forceHardDelete = false);
        int Delete(IEnumerable<T> entities, bool forceHardDelete = false);
        bool Delete(int id, bool forceHardDelete = false);
        bool Delete(T entity, bool forceHardDelete = false);
        int DeleteAll(bool forceHardDelete = false);
        IList<T> GetAll(bool includeDeleted = false);
        T GetById(int id);
        IList<T> GetByIds(IEnumerable<int> entityIds);
        int Insert(IEnumerable<T> entities);
        T Insert(T entity);
        int Save(IEnumerable<T> entities);
        T Save(T entity);
    }

    public class BaseBll<T> : IBaseBll<T> where T : BaseEntity
    {
        public IUnitOfWork UnitOfWork { get; protected set; }
        public IDbConnection DB => UnitOfWork.DB;
        public IDbTransaction Transaction => UnitOfWork.Transaction;
        public string User { get; protected set; }
        public string TableName { get; protected set; }

        /// <summary>
        /// Constructor creates instance of the BaseBll class.
        /// </summary>
        /// <param name="db">DbConnection to the database.</param>
        public BaseBll(IUnitOfWork unitOfWork)
        {
            try
            {
                UnitOfWork = unitOfWork;
                User = unitOfWork.User;
                string tableName = typeof(T).GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()?.Name;
                TableName = tableName ?? Pluralize(typeof(T).Name);
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }
        }

        protected static string Pluralize(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            if (str.EndsWith("y") && str.Length > 1 && !"aeiou".Contains(str[str.Length - 2]))
                return str.Substring(0, str.Length - 1) + "ies";

            if (str.EndsWith("s") || str.EndsWith("x") || str.EndsWith("z") ||
                str.EndsWith("ch") || str.EndsWith("sh"))
                return str + "es";

            return str + "s";
        }

        /// <summary>
        /// Gets single entity of type T, or default (null) if entity with that value does not exist.
        /// </summary>
        /// <param name="id">Id of the record.</param>
        /// <returns>Single or default value of type T.</returns>
        public virtual T GetById(int id)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            T result;
            try
            {
                string sql = $"SELECT * FROM {TableName} WHERE Id = @id;";
                result = DB.QuerySingle<T>(sql, param: new { id }, transaction: Transaction);
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Returns elements of type T which is contained in entityIds enumerable.
        /// </summary>
        /// <param name="entityIds">IEnumerable of entityIds which should be returned.</param>
        /// <returns>IEnumerable of requested entities.</returns>
        public virtual IList<T> GetByIds(IEnumerable<int> entityIds)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            IList<T> result;
            try
            {
                string sql = $"SELECT * FROM {TableName} WHERE Id IN @entityIds ORDER BY CreatedAt;";
                result = DB.Query<T>(sql, param: new { entityIds }, transaction: Transaction).ToList();
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Gets all records of type T.
        /// </summary>
        /// <returns>IList of all entities of type T.</returns>
        public virtual IList<T> GetAll(bool includeDeleted = false)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            IList<T> result;
            try
            {
                string excludeDeleted = string.Empty;
                if (!includeDeleted && typeof(SoftDeletableEntity).IsAssignableFrom(typeof(T)))
                    excludeDeleted = " WHERE IsDeleted = FALSE";

                string sql = $"SELECT * FROM {TableName}{excludeDeleted} ORDER BY CreatedAt;";

                result = DB.Query<T>(sql, param: null, transaction: Transaction).ToList();
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Sets creation time and last edit time to the moment of insertion and inserts record to database.
        /// </summary>
        /// <param name="entity">Entity of type T to insert.</param>
        /// <returns>Newly inserted entity.</returns>
        public virtual T Insert(T entity)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            try
            {
                entity.Id = 0;
                PrivateSave(entity, new List<object>());
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return entity;
        }

        /// <summary>
        /// Inserts range of entities to the database.
        /// </summary>
        /// <param name="entities">Entities of type T to insert.</param>
        /// <returns>Number of entities inserted.</returns>
        public virtual int Insert(IEnumerable<T> entities)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            int result;
            try
            {
                foreach (T entity in entities)
                {
                    entity.Id = 0;
                    PrivateSave(entity, new List<object>());
                }
                result = entities.Count();
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Inserts or updates entity of type T.
        /// </summary>
        /// <param name="entity">Entity of type T to insert or update.</param>
        /// <returns>Newly saved entity.</returns>
        public virtual T Save(T entity)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            try
            {
                PrivateSave(entity, new List<object>());
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return entity;
        }

        /// <summary>
        /// Inserts or updates range of entities.
        /// </summary>
        /// <param name="entities">Entities of type T to insert or update.</param>
        /// <returns>Number of entities saved.</returns>
        public virtual int Save(IEnumerable<T> entities)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            int result;
            try
            {
                foreach (T entity in entities)
                {
                    PrivateSave(entity, new List<object>());
                }

                result = entities.Count();
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        public BaseEntity PrivateSave(BaseEntity entity, List<object> savedEntities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (savedEntities.Contains(entity))
                return entity;

            var type = entity.GetType();
            var properties = type.GetProperties()
                .Where(p => p.Name != "Id" &&
                            !p.CustomAttributes.Any(x => typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute).IsAssignableFrom(x.AttributeType)) &&
                            (p.PropertyType.IsPrimitive ||
                             p.PropertyType == typeof(string) ||
                             p.PropertyType == typeof(float) ||
                             p.PropertyType == typeof(double) ||
                             p.PropertyType == typeof(decimal) ||
                             p.PropertyType == typeof(DateTime) ||
                             p.PropertyType == typeof(DateTimeOffset) ||
                             p.PropertyType == typeof(byte[]) ||
                             p.PropertyType.IsEnum ||
                             (p.PropertyType.IsGenericType &&
                              p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))) &&
                            p.CanWrite)
                .ToList();

            var propertiesWOModified = properties.Where(x => x.Name != "ModifiedAt" && x.Name != "ModifiedBy").ToList();

            // If child entities already have Ids, update reference Ids in the parent.
            foreach (var property in type.GetProperties())
            {
                if (typeof(BaseEntity).IsAssignableFrom(property.PropertyType))
                {
                    var childEntity = (BaseEntity)property.GetValue(entity);
                    if (childEntity != null)
                    {
                        // This block saves children if the parent has a reference Id to the child.
                        var idProperty = type.GetProperty(property.Name + "Id");
                        if (idProperty != null && (idProperty.PropertyType == typeof(int) || idProperty.PropertyType == typeof(int?)))
                        {
                            if (childEntity.Id == 0)
                            {
                                Type childType = property.PropertyType;

                                var materializedBll = MaterializeBll(childType);

                                var methodToInvoke = materializedBll.GetType().GetMethod("PrivateSave", new Type[] { childType, typeof(List<object>) });

                                PropertyInfo parentReference = GetParentReferenceOfTheChild(entity, childEntity);

                                if (parentReference != null)
                                    parentReference.SetValue(childEntity, entity.Id);

                                methodToInvoke.Invoke(materializedBll, new object[] { childEntity, savedEntities });
                            }

                            object currentChildIdInParent = idProperty.GetValue(entity);
                            if (currentChildIdInParent == null || currentChildIdInParent is 0)
                                idProperty.SetValue(entity, childEntity.Id);
                        }
                    }
                }
            }

            var columns = string.Join(",", properties.Select(p => '`' + p.Name + '`'));
            var parameters = string.Join(",", properties.Select(p => $"@{p.Name}"));

            if (entity.Id == 0)
            {
                entity.CreatedAt = DateTime.Now;
                entity.CreatedBy = User;
                var insertQuery = $"INSERT INTO {TableName} ({columns}) VALUES ({parameters});";

                // Get the appropriate last insert ID function based on database type
                string lastIdFunction = GetLastInsertIdFunction(DB);
                insertQuery += $" {lastIdFunction};";

                entity.Id = DB.ExecuteScalar<int>(insertQuery, param: entity, transaction: Transaction);
            }
            else
            {
                entity.ModifiedAt = DateTime.Now;
                entity.ModifiedBy = User;
                var updateQuery = $"UPDATE {TableName} SET {string.Join(",", properties.Select(p => $"`{p.Name}`=@{p.Name}"))} WHERE Id=@Id " +
                                    $"AND ({string.Join(" OR ", propertiesWOModified.Select(p => $"((`{p.Name}`!=@{p.Name}) OR (`{p.Name}` IS NULL AND @{p.Name} IS NOT NULL) OR (`{p.Name}` IS NOT NULL AND @{p.Name} IS NULL))"))})";
                DB.Execute(updateQuery, param: entity, transaction: Transaction);
            }

            savedEntities.Add(entity);

            // Save and update reference IDs for child entities
            foreach (var property in type.GetProperties())
            {
                if (typeof(BaseEntity).IsAssignableFrom(property.PropertyType))
                {
                    var childEntity = (BaseEntity)property.GetValue(entity);
                    if (childEntity != null)
                    {
                        // This block saves children if parent does not have a reference Id to the child (meaning, the child has a reference to the parent).
                        var idProperty = type.GetProperty(property.Name + "Id");
                        if (idProperty == null)
                        {
                            Type childType = property.PropertyType;

                            var materializedBll = MaterializeBll(childType);

                            var methodToInvoke = materializedBll.GetType().GetMethod("PrivateSave", new Type[] { childType, typeof(List<object>) });

                            PropertyInfo parentReferenceId = GetParentReferenceOfTheChild(entity, childEntity);

                            if (parentReferenceId != null)
                            {
                                object currentParentReferenceId = parentReferenceId.GetValue(childEntity);
                                if (currentParentReferenceId == null || currentParentReferenceId is 0)
                                    parentReferenceId.SetValue(childEntity, entity.Id);
                            }

                            methodToInvoke.Invoke(materializedBll, new object[] { childEntity, savedEntities });
                        }
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) &&
                         property.PropertyType.GetGenericArguments().Any(t => typeof(BaseEntity).IsAssignableFrom(t)))
                {
                    var childEntities = (IEnumerable)property.GetValue(entity);
                    if (childEntities != null)
                    {
                        Type childType = property.PropertyType.GetGenericArguments().FirstOrDefault();

                        var materializedBll = MaterializeBll(childType);

                        var methodToInvoke = materializedBll.GetType().GetMethod("PrivateSave", new Type[] { childType, typeof(List<object>) });

                        PropertyInfo parentReference = null;
                        foreach (var childEntity in childEntities)
                        {
                            if (parentReference == null)
                                parentReference = GetParentReferenceOfTheChild(entity, childEntity);

                            if (parentReference != null)
                                parentReference.SetValue(childEntity, entity.Id);

                            methodToInvoke.Invoke(materializedBll, new object[] { (BaseEntity)childEntity, savedEntities });
                        }
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Deletes entity of type T from the database.
        /// </summary>
        /// <param name="id">Id of the entity to delete.</param>
        /// <returns>True if successfully deleted.</returns>
        public virtual bool Delete(int id, bool forceHardDelete = false)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            bool result = false;
            try
            {
                string sql = $"DELETE FROM {TableName} WHERE Id = @id;";
                if (typeof(SoftDeletableEntity).IsAssignableFrom(typeof(T)) && !forceHardDelete)
                    sql = $"UPDATE {TableName} SET IsDeleted = TRUE, DeletedAt = @deletedAt, DeletedBy = @deletedBy WHERE Id = @id;";

                result = DB.Execute(sql, param: new { id, deletedAt = DateTime.Now, deletedBy = User }, transaction: Transaction) > 0;
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Deletes entity of type T from the database.
        /// </summary>
        /// <param name="entity">Entity to delete.</param>
        /// <returns>True if successfully deleted.</returns>
        public virtual bool Delete(T entity, bool forceHardDelete = false)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            bool result = Delete(entity.Id, forceHardDelete);

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Deletes range of entities from the database.
        /// </summary>
        /// <param name="entityIds">IEnumerable of entityIds to delete.</param>
        /// <returns>Number of deleted entities.</returns>
        public virtual int Delete(IEnumerable<int> entityIds, bool forceHardDelete = false)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            int result = 0;
            try
            {
                string sql = $"DELETE FROM {TableName} WHERE Id IN @entityIds;";
                if (typeof(SoftDeletableEntity).IsAssignableFrom(typeof(T)) && !forceHardDelete)
                    sql = $"UPDATE {TableName} SET IsDeleted = TRUE, DeletedAt = @deletedAt, DeletedBy = @deletedBy WHERE Id IN @entityIds;";

                result = DB.Execute(sql, param: new { entityIds, deletedAt = DateTime.Now, deletedBy = User }, transaction: Transaction);
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Deletes range of entities from the database.
        /// </summary>
        /// <param name="entities">IEnumerable of entites to delete.</param>
        /// <returns>Number of deleted entities.</returns>
        public virtual int Delete(IEnumerable<T> entities, bool forceHardDelete = false)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            int result = Delete(entities.Select(y => y.Id), forceHardDelete);

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Deletes all entities of type T from the database.
        /// </summary>
        /// <returns>Number of deleted entities.</returns>
        public virtual int DeleteAll(bool forceHardDelete = false)
        {
            DateTime methodStart = BaseLogger.LogMethodStart(this);

            int result = 0;
            try
            {
                string sql = $"DELETE FROM {TableName};";
                if (typeof(SoftDeletableEntity).IsAssignableFrom(typeof(T)) && !forceHardDelete)
                    sql = $"UPDATE {TableName} SET IsDeleted = TRUE, DeletedAt = @deletedAt, DeletedBy = @deletedBy;";

                result = DB.Execute(sql, param: new { deletedAt = DateTime.Now, deletedBy = User }, transaction: Transaction);
            }
            catch (Exception ex)
            {
                BaseLogger.Log(LogLevel.Error, ex.Message, ex);
                throw;
            }

            BaseLogger.LogMethodEnd(this, methodStart);
            return result;
        }

        /// <summary>
        /// Gets or sets entity of type T.
        /// </summary>
        /// <param name="id">Id of the entity to get or set.</param>
        /// <returns>Entity of type T.</returns>
        public virtual T this[int id]
        {
            get
            {
                return GetById(id);
            }
            set
            {
                Save(value);
            }
        }

        private PropertyInfo GetParentReferenceOfTheChild(object parent, object child)
        {
            Type childType = child.GetType();
            var propertiesInChildOfAParentType = childType.GetProperties().Where(x => typeof(T).IsAssignableFrom(x.PropertyType));
            var parentPropertyInChild = propertiesInChildOfAParentType.Where(x => x.GetValue(child) == parent).FirstOrDefault();

            if (parentPropertyInChild == null)
                parentPropertyInChild = propertiesInChildOfAParentType.FirstOrDefault();

            if (parentPropertyInChild != null)
                return childType.GetProperty(parentPropertyInChild.Name + "Id");

            return null;
        }

        private object MaterializeBll(Type type)
        {
            Type baseBllType = typeof(BaseBll<>);
            Type bllType = baseBllType.MakeGenericType(type);
            var materializedBll = Activator.CreateInstance(bllType, UnitOfWork);

            return materializedBll;
        }

        private static string lastInsertIdFunction = null;
        private static string GetLastInsertIdFunction(IDbConnection db)
        {
            if (!string.IsNullOrWhiteSpace(lastInsertIdFunction))
                return lastInsertIdFunction;

            string dbType = db.GetType().Name.ToLower();

            if (dbType.Contains("mysql"))
            {
                lastInsertIdFunction = "SELECT LAST_INSERT_ID()";
            }
            else if (dbType.Contains("sqlite"))
            {
                lastInsertIdFunction = "SELECT last_insert_rowid()";
            }
            else if (dbType.Contains("sqlserver") || dbType.Contains("sqlclient"))
            {
                lastInsertIdFunction = "SELECT SCOPE_IDENTITY()";
            }
            else if (dbType.Contains("npgsql") || dbType.Contains("postgres"))
            {
                lastInsertIdFunction = "SELECT LASTVAL()";
            }
            else
            {
                throw new NotSupportedException($"Database type {dbType} is not supported.");
            }

            return lastInsertIdFunction;
        }
    }

    public class BaseBLL<T> : BaseBll<T> where T : BaseEntity
    {
        public BaseBLL(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}
