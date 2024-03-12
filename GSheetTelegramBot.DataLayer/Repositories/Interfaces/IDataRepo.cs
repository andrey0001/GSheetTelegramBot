using System.Linq.Expressions;
using GSheetTelegramBot.DataLayer.DbModels;

namespace GSheetTelegramBot.DataLayer.Repositories.Interfaces;

public interface IDataRepo<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> GetAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    IQueryable<T> IncludeItems(params Expression<Func<T, object>>[] includes);
    Task<T> AddAsync(T model);
    Task UpdateAsync(T model);
    Task UpdateRangeAsync(List<T> models);
    Task DeleteAsync(int id);
    Task<User?> GetByChatIdAsync(long chatId);
}