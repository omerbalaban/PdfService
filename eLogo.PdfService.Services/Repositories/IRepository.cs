using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<T> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<long> CountAsync(Expression<Func<T, bool>> predicate = null, CancellationToken cancellationToken = default);
    }
}
