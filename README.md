# ADOHelper
> A lightweight, high-performance Asynchronous ADO.NET Wrapper for .NET applications. This utility simplifies database interactions by automating connection management, reducing boilerplate > code, and providing a clean, generic interface for data mapping.

## Installation & Setup

Simply include the ADOHelper.cs class in your project and ensure you have the Microsoft.Data.SqlClient package installed.

```bash
string connString = "Your_Connection_String";
ADOHelper helper = new ADOHelper(connString);
```

## Examples
1. Execute List with Mapping
```bash
 List<Person> people = await helper.ExecuteListAsync("Select * from People Where FirstName = @FirstName",
     cmd => cmd.Parameters.AddWithValue("@FirstName", "jeff"),
     reader => 
     {
         return new Person
         {
             PersonID = reader.GetInt32(reader.GetOrdinal(nameof(Person.PersonID))),
             FirstName = reader.GetString(reader.GetOrdinal(nameof(Person.FirstName))),
             LastName = reader.GetString(reader.GetOrdinal(nameof(Person.LastName)))
         };
     });
```

2. Execute Scalar
```bash
object result = await helper.ExecuteScalarAsync("SELECT COUNT(*) FROM People");
int count = Convert.ToInt32(result);
```
