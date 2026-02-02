# EfCore.WhereIn

[![NuGet version](https://img.shields.io/nuget/v/EfCore.WhereIn.svg)](https://www.nuget.org/packages/EfCore.WhereIn)
[![NuGet downloads](https://img.shields.io/nuget/dt/EfCore.WhereIn.svg)](https://www.nuget.org/packages/EfCore.WhereIn)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../LICENSE)
[![Build Status](https://github.com/sak/EfCore.WhereIn/actions/workflows/build.yml/badge.svg)](https://github.com/sak/EfCore.WhereIn/actions)
![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-blue)

---

**EfCore.WhereIn** is a lightweight extension library for Entity Framework Core that forces SQL Server to generate inline `IN (...)` clauses instead of using `OPENJSON` when filtering by collections. This improves query performance and compatibility, especially for large or frequently-used `IN` queries.

---

## ?? Installation

Install via NuGet Package Manager:

```shell
# .NET CLI
 dotnet add package EfCore.WhereIn

# Or with Package Manager
 PM> Install-Package EfCore.WhereIn
```

---

## ?? Usage

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
```

### SQL Output Example

```sql
SELECT ... FROM [Entities] WHERE [Id] IN (1, 2, 3)
```

---

## ?? How It Works

EF Core (with SQL Server) normally translates `Where(x => values.Contains(x.Id))` to use `OPENJSON`, which can be less efficient and harder to optimize. This library uses expression trees and `HashSet<T>` to force EF Core to generate inline `IN (...)` SQL, ensuring better performance and compatibility.

- **Without EfCore.WhereIn:**
  ```sql
  WHERE [Id] IN (SELECT [value] FROM OPENJSON(@__ids))
  ```
- **With EfCore.WhereIn:**
  ```sql
  WHERE [Id] IN (1, 2, 3)
  ```

---

## ?? API Reference

- `IQueryable<T> WhereIn<T, TValue>(this IQueryable<T>, Expression<Func<T, TValue>>, IEnumerable<TValue>)`
- `IQueryable<T> WhereNotIn<T, TValue>(this IQueryable<T>, Expression<Func<T, TValue>>, IEnumerable<TValue>)`

---

## ??? Compatibility

| EF Core | SQL Server | .NET Target Frameworks           |
|---------|------------|----------------------------------|
| 5.0+    | 2016+      | netstandard2.0, net8.0, net10.0  |

---

## ?? Testing

- Includes xUnit tests verifying:
  - SQL contains `IN (...)`
  - SQL does **not** contain `OPENJSON`
  - `WhereIn` and `WhereNotIn` return correct results

---

## ?? Contributing

Contributions, issues, and feature requests are welcome! Please open an issue or submit a pull request.

---

## ?? License

This project is licensed under the [MIT License](../LICENSE).

---

## ?? More Information

- [NuGet Package](https://www.nuget.org/packages/EfCore.WhereIn)
- [GitHub Repository](https://github.com/sak/EfCore.WhereIn)

---

### Why not just use `Contains`?

EF Core's default translation for `Contains` with SQL Server uses `OPENJSON`, which can be less efficient and less compatible with some SQL Server versions and query plans. This library ensures you always get the most efficient inline `IN (...)` clause.

---

### Example: Inline `IN` vs. `OPENJSON`

| Approach         | SQL Output Example                                 |
|------------------|---------------------------------------------------|
| Default Contains | `WHERE [Id] IN (SELECT [value] FROM OPENJSON(...))`|
| EfCore.WhereIn   | `WHERE [Id] IN (1, 2, 3)`                         |
