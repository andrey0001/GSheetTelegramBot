using GSheetTelegramBot.DataLayer.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace GSheetTelegramBot.DataLayer.Context;

public class GSheetTelegramBotDbContext : DbContext
{
    public GSheetTelegramBotDbContext(DbContextOptions<GSheetTelegramBotDbContext> options)
        : base(options)
    {
        try
        {
            var databaseCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (databaseCreator != null)
            {
                if (!databaseCreator.CanConnect()) databaseCreator.Create();
                if (!databaseCreator.HasTables()) databaseCreator.CreateTables();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<GoogleTable> GoogleTables { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<Subscription>().HasKey(s => s.Id);

        modelBuilder.Entity<Subscription>()
            .HasOne<User>(s => s.User) // Явно указываем навигационное свойство в Subscription
            .WithMany(u => u.Subscriptions) // Явно указываем обратное навигационное свойство в User
            .HasForeignKey(s => s.UserId) // Указываем, что UserId является внешним ключом
            .HasPrincipalKey(u => u.Id) // Указываем, что Id в User является соответствующим первичным ключом
            .OnDelete(DeleteBehavior.Cascade); // Настройка каскадного удаления
    }
}