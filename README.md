# ADOHelper

A lightweight async ADO.NET wrapper for .NET applications. Automates connection management, reduces boilerplate, and provides a clean generic interface for SQL Server data access.

---

## Installation

Add `ADOHelper.cs` to your project, Then instantiate with your connection string:

```csharp
var helper = new ADOHelper("Your_Connection_String");
```

---

## API Reference

### `ExecuteListAsync`
Returns a list of mapped objects from a SELECT query.

```csharp
Task<List<T>> ExecuteListAsync<T>(
    string query,
    Action<SqlCommand> configure,
    Func<SqlDataReader, T> mapper,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

### `ExecuteReaderAsync`
Returns a single mapped object (first row only).

```csharp
Task<T> ExecuteReaderAsync<T>(
    string query,
    Action<SqlCommand> configure,
    Func<SqlDataReader, T> mapper,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default) where T : class
```

### `ExecuteNonQueryAsync`
Executes INSERT / UPDATE / DELETE. Returns rows affected.

```csharp
Task<int> ExecuteNonQueryAsync(
    string query,
    Action<SqlCommand> configure = null,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

### `ExecuteScalarAsync`
Returns a single value from a query with type-safe casting.

```csharp
Task<T?> ExecuteScalarAsync<T>(
    string query,
    Action<SqlCommand> configure = null,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

### `ExecuteDataTableAsync`
Returns query results as a `DataTable`.

```csharp
Task<DataTable> ExecuteDataTableAsync(
    string query,
    Action<SqlCommand> configure = null,
    CommandType commandType = CommandType.Text,
    CancellationToken cancellationToken = default)
```

---

## Examples

### Query a list with parameters

```csharp
List<Person> people = await helper.ExecuteListAsync(
    "SELECT * FROM People WHERE FirstName = @FirstName",
    cmd => cmd.Parameters.AddWithValue("@FirstName", "Jeff"),
    reader => new Person
    {
        PersonID  = reader.GetInt32(reader.GetOrdinal(nameof(Person.PersonID))),
        FirstName = reader.GetString(reader.GetOrdinal(nameof(Person.FirstName))),
        LastName  = reader.GetString(reader.GetOrdinal(nameof(Person.LastName)))
    });
```

### Get a scalar value

```csharp
int count = await helper.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM People");
```

### Insert a record

```csharp
int rows = await helper.ExecuteNonQueryAsync(
    "INSERT INTO People (FirstName, LastName) VALUES (@First, @Last)",
    cmd =>
    {
        cmd.Parameters.AddWithValue("@First", "Jane");
        cmd.Parameters.AddWithValue("@Last", "Doe");
    });
```

### Call a stored procedure

```csharp
List<Order> orders = await helper.ExecuteListAsync(
    "sp_GetOrdersByCustomer",
    cmd => cmd.Parameters.AddWithValue("@CustomerID", 42),
    reader => new Order
    {
        OrderID    = reader.GetInt32(reader.GetOrdinal(nameof(Order.OrderID))),
        TotalPrice = reader.GetDecimal(reader.GetOrdinal(nameof(Order.TotalPrice)))
    },
    commandType: CommandType.StoredProcedure);
```

### Cancellation support (ASP.NET Core)

```csharp
// HttpContext.RequestAborted يُلغى تلقائياً إذا أغلق المستخدم المتصفح
List<Product> products = await helper.ExecuteListAsync(
    "SELECT * FROM Products",
    null,
    reader => new Product { ... },
    cancellationToken: HttpContext.RequestAborted);
```

