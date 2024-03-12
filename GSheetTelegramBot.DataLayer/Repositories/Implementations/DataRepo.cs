using System.Linq.Expressions;
using GSheetTelegramBot.DataLayer.Context;
using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GSheetTelegramBot.DataLayer.Repositories.Implementations;

public class DataRepo<T> : IDataRepo<T> where T : class
{
    private readonly GSheetTelegramBotDbContext _db;

    public DataRepo(GSheetTelegramBotDbContext db)
    {
        _db = db;
    }

    public IQueryable<T> Query()
    {
        return _db.Set<T>().AsQueryable();
    }

    public async Task<T?> GetAsync(int id)
    {
        return await _db.Set<T>().FindAsync(id);
    }

    public async Task<User?> GetByChatIdAsync(long chatId)
    {
        return await _db.Set<User>().FirstOrDefaultAsync(u => u.ChatId == chatId);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _db.Set<T>().ToListAsync();
    }

    public IQueryable<T> IncludeItems(params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _db.Set<T>();
        foreach (var include in includes) query = query.Include(include);
        return query;
    }

    public async Task<T> AddAsync(T model)
    {
        await _db.Set<T>().AddAsync(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task UpdateAsync(T model)
    {
        _db.Set<T>().Update(model);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(List<T> models)
    {
        _db.Set<T>().UpdateRange(models);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _db.Set<T>().FindAsync(id);
        if (item != null)
        {
            _db.Set<T>().Remove(item);
            await _db.SaveChangesAsync();
        }
    }
}