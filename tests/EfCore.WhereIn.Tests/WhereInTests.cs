using EfCore.WhereIn;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EfCore.WhereIn.Tests
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

    public class WhereInTests
    {
        [Fact]
        public void WhereIn_GeneratesInlineInClause()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True;")
                .LogTo(Console.WriteLine)
                .EnableSensitiveDataLogging()
                .Options;

            using var db = new TestDbContext(options);
            var ids = new[] { 1, 2, 3 };
            var query = db.Entities.WhereIn(e => e.Id, ids).ToQueryString();
            Assert.Contains("IN (1, 2, 3)", query);
            Assert.DoesNotContain("OPENJSON", query);
        }

        [Fact]
        public void WhereNotIn_GeneratesInlineNotInClause()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True;")
                .LogTo(Console.WriteLine)
                .EnableSensitiveDataLogging()
                .Options;

            using var db = new TestDbContext(options);
            var ids = new[] { 1, 2 };
            var query = db.Entities.WhereNotIn(e => e.Id, ids).ToQueryString();
            Assert.Contains("NOT IN (1, 2)", query);
            Assert.DoesNotContain("OPENJSON", query);
        }

        [Fact]
        public void WhereIn_EmptyValues_ReturnsNoResults()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True;")
                .Options;
            using var db = new TestDbContext(options);
            var ids = Array.Empty<int>();
            var query = db.Entities.WhereIn(e => e.Id, ids).ToQueryString();
            Assert.Contains("WHERE 0 = 1", query, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WhereNotIn_EmptyValues_ReturnsAllResults()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True;")
                .Options;
            using var db = new TestDbContext(options);
            var ids = Array.Empty<int>();
            var query = db.Entities.WhereNotIn(e => e.Id, ids).ToQueryString();
            Assert.DoesNotContain("NOT IN", query);
        }
    }
}
