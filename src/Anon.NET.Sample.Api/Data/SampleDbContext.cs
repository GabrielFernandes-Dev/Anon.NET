using Anon.NET.Sample.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Anon.NET.Sample.Api.Data;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(u => u.Id);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "João Silva",
                Email = "joao@example.com",
                CPF = "790.840.080-90",
                Idade = 28,
                Salario = 4500,
                CEP = "01310-100"
            },
            new User
            {
                Id = 2,
                Name = "Maria Santos",
                Email = "maria@example.com",
                CPF = "286.993.070-47",
                Idade = 35,
                Salario = 6200,
                CEP = "04567-020"
            },
            new User
            {
                Id = 3,
                Name = "Pedro Oliveira",
                Email = "pedro@example.com",
                CPF = "583.760.150-36",
                Idade = 42,
                Salario = 7800,
                CEP = "22070-030"
            }
        );
    }
}
