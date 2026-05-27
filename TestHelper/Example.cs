using ADO_Helper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Metadata;

internal class Example
{
    static async Task Main(string[] args)
    {
        string ConnString = "Server=.;DataBase=School Management System;Integrated Security=True;TrustServerCertificate=True;";

        ADOHelper helper = new ADOHelper(ConnString);


        // - DataTable -

        DataTable dt1 = await helper.ExecuteDataTableAsync("Select * from People");

        DataTable dt2 = await helper.ExecuteDataTableAsync("sp_People_GetAll", CommandType.StoredProcedure);

        DataTable dt3 = await helper.ExecuteDataTableAsync("Select * from People Where FirstName = @FirstName",
            cmd => cmd.Parameters.AddWithValue("@FirstName", "Jeff"));


        // Method 1

        DataTable dt4 = await helper.ExecuteDataTableAsync("sp_People_GetAll",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@PageNumber", 1);
                cmd.Parameters.AddWithValue("@RowsPerPage", 10);
            }
            , CommandType.StoredProcedure);

        // Method 2

        void Parameters1(SqlCommand command)
        {
            command.Parameters.AddWithValue("@PageNumber", 1);
            command.Parameters.AddWithValue("@RowsPerPage", 10);
        }

        DataTable dt5 = await helper.ExecuteDataTableAsync("sp_People_GetAll", Parameters1, CommandType.StoredProcedure);


        //  - NonQuery -

        int RowsEffected1 = await helper.ExecuteNonQueryAsync("Delete from People Where PersonID = @PersonID",
            cmd => cmd.Parameters.AddWithValue("@PersonID", 2000));


        int RowsEffected2 = await helper.ExecuteNonQueryAsync("sp_People_Delete",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@FirstName", "Ali");
                cmd.Parameters.AddWithValue("@LastName", "Ahmed");
            },
            CommandType.StoredProcedure);



        // - Execute Scalar -

        string? FirstName = await helper.ExecuteScalarAsync<string>("Select FirstName From People Where PersonID = @PersonID",
            cmd => cmd.Parameters.AddWithValue("@PersonID", 12));



        // - Reader -
        // Note: This function return a single record or null.

        // Method 1
        Person FromDataReader(IDataReader reader)
        {
            return new Person
            {
                FirstName = reader.GetString(reader.GetOrdinal(nameof(Person.FirstName))),
                LastName = reader.GetString(reader.GetOrdinal(nameof(Person.LastName)))
            };
        }

        Person person1 = await helper.ExecuteReaderAsync("Select FirstName, LastName from People Where PersonID = @PersonID",
            cmd => cmd.Parameters.AddWithValue("@PersonID", 1), FromDataReader);


        // Method 2
        Person person2 = await helper.ExecuteReaderAsync("Select FirstName, LastName from People Where PersonID = @PersonID",
            cmd => cmd.Parameters.AddWithValue("@PersonID", 1),
           reader =>
           {
               return new Person
               {
                   FirstName = reader.GetString(reader.GetOrdinal(nameof(Person.FirstName))),
                   LastName = reader.GetString(reader.GetOrdinal(nameof(Person.LastName)))
               };
           });



        // --- List Reader ---

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

    }
}


class Person
{
    public int PersonID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}