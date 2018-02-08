using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AspNetCoreWebApiSamples.Entities;
using Eits.Multitenant.Client.Managment.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreWebApiSamples.Helpers
{
    public class EntityFrameworkPatch
    {
        //for when we can't use JsonPatchDocument,
        //maybe because we don't use Json and use something like message pack
        private readonly LibraryContext _libraryContext;

        public EntityFrameworkPatch(LibraryContext libraryContext)
        {
            _libraryContext = libraryContext;
        }

        public async Task ApplyPatch<TEntity, TKey>(TEntity entityName, List<PatchDto> patchDtos) where TEntity : class
        {
            var nameValuePairProperties = patchDtos.ToDictionary(a => a.PropertyName, a => a.PropertyValue);

            var dbEntityEntry = _libraryContext.Entry(entityName);
            dbEntityEntry.CurrentValues.SetValues(nameValuePairProperties);
            dbEntityEntry.State = EntityState.Modified;
            await _libraryContext.SaveChangesAsync();

            /* example of what it accept over http, note that property names should match the entity
                 [
                    {
                     "PropertyName": "ModifiedDate",
                     "PropertyValue": null
                    },
                    {
                     "PropertyName": "CreatedDate",
                     "PropertyValue": "2020-01-30T12:53:51.6240518"
                    }
                ]
             */
        }
    }
}

/* previous implementation
 public void ApplyPatch<TEntity, TUpdateDto, TKey>(TEntity entityName, TUpdateDto entityUpdateDto)
            where TEntity : class
            where TUpdateDto : class
        {
            var updatableProperties = entityUpdateDto.GetType()
                                       .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Select(x => new { Name = x.Name, Value = x.GetValue(entityUpdateDto, null) })
                                       .Where(y => y.Value != null &&
                                       !String.IsNullOrWhiteSpace(y.Value.ToString()) &&
                                       y.Name != "Id" &&
                                       y.Value.ToString() != default(DateTime).ToString(CultureInfo.InvariantCulture) &&
                                       y.Value.ToString() != 0.ToString())
                                       .ToDictionary(a => a.Name, a => a.Value);

            var dbEntityEntry = _libraryContext.Entry(entityName);
            dbEntityEntry.CurrentValues.SetValues(updatableProperties);
            dbEntityEntry.State = EntityState.Modified;
            _libraryContext.SaveChanges();
        }
*/

/* https://codereview.stackexchange.com/questions/37304/update-only-modified-fields-in-entity-framework
 public virtual void Update(T entity)
    {
        //dbEntityEntry.State = EntityState.Modified; --- I cannot do this.

        //Ensure only modified fields are updated.
        var dbEntityEntry = DbContext.Entry(entity);
        foreach (var property in dbEntityEntry.OriginalValues.PropertyNames)
        {
            var original = dbEntityEntry.OriginalValues.GetValue<object>(property);
            var current = dbEntityEntry.CurrentValues.GetValue<object>(property);
            if (original != null && !original.Equals(current))
                dbEntityEntry.Property(property).IsModified = true;
        }
    }
*/