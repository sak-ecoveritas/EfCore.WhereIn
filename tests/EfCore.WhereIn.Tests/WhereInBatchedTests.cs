using EfCore.WhereIn;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace EfCore.WhereIn.Tests
{
    public class WhereInBatchedTests
    {
        public class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
            public DbSet<Entity> Entities { get; set; } = null!;
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Entity>().HasData(
                    new Entity { Id = 1, Name = "A" },
                    new Entity { Id = 2, Name = "B" },
                    new Entity { Id = 3, Name = "C" },
                    new Entity { Id = 4, Name = "D" }
                );
            }
        }

        [Fact]
        public void WhereInBatched_WorksForLargeCollections()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True;")
                .LogTo(Console.WriteLine)
                .EnableSensitiveDataLogging()
                .Options;
            using var db = new TestDbContext(options);
            var ids = Enumerable.Range(1, 5000).ToArray();
            var query = db.Entities.WhereInBatched(e => e.Id, ids).ToQueryString();
            Assert.Contains("IN (1, 2, 3, 4", query); // At least the first batch should be present
            Assert.DoesNotContain("OPENJSON", query);
        }

        [Fact]
        public void WhereInBatched_EmptyCollection_ReturnsNoResults()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True;")
                .Options;
            using var db = new TestDbContext(options);
            var ids = Array.Empty<int>();
            var query = db.Entities.WhereInBatched(e => e.Id, ids).ToQueryString();
            Assert.Contains("WHERE 0 = 1", query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
