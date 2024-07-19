using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TomateTwitchBot.Data.Models;

namespace TomateTwitchBot.Data;

public class MyContext : DbContext
{
    public DbSet<UserDb> Users { get; set; }
    public DbSet<TimeoutDb> Timeouts { get; set; }

    protected MyContext()
    {
    }

    public MyContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserDb>()
            .HasMany<TimeoutDb>(t => t.Killed)
            .WithOne(a => a.Killer);

        modelBuilder.Entity<UserDb>()
            .HasMany<TimeoutDb>(t => t.Died)
            .WithOne(a => a.Victim);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>()
            .HaveConversion<DateTimeToBinaryConverter>();
        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<DateTimeToBinaryConverter>();

        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}