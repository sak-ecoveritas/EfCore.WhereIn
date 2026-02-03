# EfCore.WhereIn

[![NuGet version](https://img.shields.io/nuget/v/EfCore.WhereIn.svg)](https://www.nuget.org/packages/EfCore.WhereIn)
[![NuGet downloads](https://img.shields.io/nuget/dt/EfCore.WhereIn.svg)](https://www.nuget.org/packages/EfCore.WhereIn)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../LICENSE)
[![Build Status](https://github.com/sak-ecoveritas/EfCore.WhereIn/actions/workflows/ci.yml/badge.svg)](https://github.com/sak-ecoveritas/EfCore.WhereIn/actions)
![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-blue)

---

**EfCore.WhereIn** provides two extension classes:
- `WhereInExtensions`: Standard `WhereIn` and `WhereNotIn` methods (enforce SQL Server parameter limit).
- `WhereInBatchedExtensions`: `WhereInBatched` method (supports arbitrarily large collections via batching, may impact performance).

---

## Installation

Install via NuGet Package Manager:

```shell
# .NET CLI
 dotnet add package EfCore.WhereIn

# Or with Package Manager
 PM> Install-Package EfCore.WhereIn
```

---

## Usage

Add the namespace:

```csharp
using EfCore.WhereIn;
```

Use the extension methods in your LINQ queries:

```csharp
var ids = new[] { 1, 2, 3 };
var result = dbContext.Entities.WhereIn(e => e.Id, ids).ToList();

// Or for NOT IN
var excluded = dbContext.Entities.WhereNotIn(e => e.Id, ids).ToList();

// For arbitrarily large collections (batching, may impact performance)
var largeIds = Enumerable.Range(1, 5000).ToArray();
var largeResult = dbContext.Entities.WhereInBatched(e => e.Id, largeIds).ToList();
```

### SQL Output Example

```sql
SELECT ... FROM [Entities] WHERE [Id] IN (1, 2, 3)
```

---

## How It Works

EF Core (with SQL Server) normally translates `Where(x => values.Contains(x.Id))` to use `OPENJSON`, which can be less efficient and harder to optimize. This library uses expression trees and `HashSet<T>` to force EF Core to generate inline `IN (...)` SQL, ensuring better performance and compatibility.

- **Without EfCore.WhereIn:**
  ```sql
  WHERE [Id] IN (SELECT [value] FROM OPENJSON(@__ids))
  ```
- **With EfCore.WhereIn:**
  ```sql
  WHERE [Id] IN (1, 2, 3)
  ```
- **With EfCore.WhereInBatched:**
  ```sql
  WHERE [Id] IN (1, 2, ..., 2100) OR [Id] IN (2101, ..., 4200) OR ...
  ```

---

## API Reference

- `IQueryable<T> WhereIn<T, TValue>(this IQueryable<T>, Expression<Func<T, TValue>>, IEnumerable<TValue>)`
- `IQueryable<T> WhereNotIn<T, TValue>(this IQueryable<T>, Expression<Func<T, TValue>>, IEnumerable<TValue>)`
- `IQueryable<T> WhereInBatched<T, TValue>(this IQueryable<T>, Expression<Func<T, TValue>>, IEnumerable<TValue>)`

---

## Compatibility

| EF Core | SQL Server | .NET Target Frameworks           |
|---------|------------|----------------------------------|
| 5.0+    | 2016+      | netstandard2.0, net8.0, net10.0  |

---

## Testing

- Includes xUnit tests verifying:
  - SQL contains `IN (...)`
  - SQL does **not** contain `OPENJSON`
  - `WhereIn`, `WhereNotIn`, and `WhereInBatched` return correct results

### Running Tests

To run tests locally:

```shell
cd tests/EfCore.WhereIn.Tests
# .NET CLI
 dotnet test
```

---

## Contributing

Contributions, issues, and feature requests are welcome! Please open an issue or submit a pull request.

---

## License

This project is licensed under the [MIT License](../LICENSE).

---

## More Information

- [NuGet Package](https://www.nuget.org/packages/EfCore.WhereIn)
- [GitHub Repository](https://github.com/sak-ecoveritas/EfCore.WhereIn)

---

## Further Reading

- [EF Core 10.0: Improved translation for parameterized collection](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew#improved-translation-for-parameterized-collection)
- [EF Core Issue #32365: Inline IN clause for small collections](https://github.com/dotnet/efcore/issues/32365)
- [EF Core Issue #34347: Always inline IN clause](https://github.com/dotnet/efcore/issues/34347)

---

### Why not just use `Contains`?

EF Core's default translation for `Contains` with SQL Server uses `OPENJSON`, which can be less efficient and less compatible with some SQL Server versions and query plans. This library ensures you always get the most efficient inline `IN (...)` clause, and with batching support, you can handle arbitrarily large collections (with a performance tradeoff).

---

### Example: Inline `IN` vs. `OPENJSON`

| Approach         | SQL Output Example                                 |
|------------------|---------------------------------------------------|
| Default Contains | `WHERE [Id] IN (SELECT [value] FROM OPENJSON(...))`|
| EfCore.WhereIn   | `WHERE [Id] IN (1, 2, 3)`                         |
| EfCore.WhereInBatched | `WHERE [Id] IN (1, ..., 2100) OR [Id] IN (2101, ...)` |
