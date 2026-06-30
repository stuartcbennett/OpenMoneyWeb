using Microsoft.EntityFrameworkCore;
using OpenMoneyWeb.Core.Models;

namespace OpenMoneyWeb.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account>      Accounts       => Set<Account>();
    public DbSet<Investment>   Investments    => Set<Investment>();
    public DbSet<Transaction>  Transactions   => Set<Transaction>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Transaction>(e =>
        {
            e.Property(t => t.Quantity).HasPrecision(18, 6);
            e.Property(t => t.Price).HasPrecision(18, 6);
            e.Property(t => t.Total).HasPrecision(18, 4);
            e.HasOne(t => t.Account)
             .WithMany(a => a.Transactions)
             .HasForeignKey(t => t.AccountId);
            e.HasOne(t => t.Investment)
             .WithMany(i => i.Transactions)
             .HasForeignKey(t => t.InvestmentId);
        });

        mb.Entity<PriceHistory>(e =>
        {
            e.Property(p => p.Price).HasPrecision(18, 6);
            e.HasOne(p => p.Investment)
             .WithMany(i => i.PriceHistories)
             .HasForeignKey(p => p.InvestmentId);
        });

        mb.Entity<Investment>(e =>
        {
            e.Property(i => i.InitialPrice).HasPrecision(18, 6);
        });
    }
}
