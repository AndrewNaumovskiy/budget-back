using Budget.API.Models.DbModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Helpers;

public class AdminDbContext : DbContext
{
    public DbSet<UserDbModel> Users { get; set; }

    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}
