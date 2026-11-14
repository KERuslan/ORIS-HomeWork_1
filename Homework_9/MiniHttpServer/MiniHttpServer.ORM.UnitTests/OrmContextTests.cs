using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Models;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public sealed class OrmContextTests
{
    [DataTestMethod]
    [DataRow("Users")]
    [DataRow("_Products2")]
    public void Quote_AcceptsSimpleIdentifiers(string name) =>
        Assert.AreEqual($"[{name}]", SqlCommands.Quote(name));

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("9Users")]
    [DataRow("dbo.Users")]
    [DataRow("User Names")]
    [DataRow("Users]")]
    [DataRow("Users\n")]
    public void Quote_RejectsInvalidIdentifiers(string name) =>
        Assert.ThrowsException<ArgumentException>(() => SqlCommands.Quote(name));

    [TestMethod]
    public void Insert_UsesParametersAndSkipsIdentity()
    {
        using var command = SqlCommands.Insert("Products", new Product { Id = 9, Name = "O'Brien", Price = 15.25m });
        Assert.AreEqual("INSERT INTO [Products] ([Name], [Price]) VALUES (@p0, @p1);", command.CommandText);
        Assert.AreEqual("O'Brien", command.Parameters["@p0"].Value);
        Assert.AreEqual(15.25m, command.Parameters["@p1"].Value);
        Assert.AreEqual(SqlDbType.Decimal, command.Parameters["@p1"].SqlDbType);
    }

    [TestMethod]
    public void Insert_AllowsAnExplicitlyAssignedId()
    {
        using var command = SqlCommands.Insert("CustomRows", new AssignedKey { Id = 8, Name = "Row" });
        Assert.AreEqual("INSERT INTO [CustomRows] ([Id], [Name]) VALUES (@p0, @p1);", command.CommandText);
        Assert.AreEqual(8, command.Parameters["@p0"].Value);
    }

    [TestMethod]
    public void Insert_SendsTypedNullAsDbNull()
    {
        using var command = SqlCommands.Insert("People", new NullableRow());
        Assert.AreEqual(DBNull.Value, command.Parameters[0].Value);
        Assert.AreEqual(SqlDbType.Int, command.Parameters[0].SqlDbType);
    }

    [TestMethod]
    public void Update_ChangesOnlyNonKeyProperties()
    {
        var user = new User { Id = 999, Name = "Oleg", Email = "oleg@example.com", Age = 23 };
        using var command = SqlCommands.Update("Users", 4, user);
        Assert.AreEqual("UPDATE [Users] SET [Name] = @p0, [Email] = @p1, [Age] = @p2 WHERE [Id] = @id;",
            command.CommandText);
        Assert.AreEqual(4, command.Parameters["@id"].Value);
        Assert.AreEqual("oleg@example.com", command.Parameters["@p1"].Value);
        Assert.AreEqual(999, user.Id);
    }

    [TestMethod]
    public void Delete_UsesParameterizedId()
    {
        using var command = SqlCommands.Delete("Products", 12);
        Assert.AreEqual("DELETE FROM [Products] WHERE [Id] = @id;", command.CommandText);
        Assert.AreEqual(12, command.Parameters["@id"].Value);
    }

    [TestMethod]
    public void Commands_RejectInvalidInput()
    {
        Assert.ThrowsException<ArgumentNullException>(() => SqlCommands.Insert("Users", null!));
        Assert.ThrowsException<ArgumentNullException>(() => SqlCommands.Update("Users", 4, null!));
        Assert.ThrowsException<ArgumentException>(() => SqlCommands.Update("Users", 4, new object()));
        Assert.ThrowsException<ArgumentException>(() => new OrmContext(""));
        Assert.ThrowsException<ArgumentException>(() => SqlCommands.Delete(null!, 1));
    }

    [TestMethod]
    public void Map_UsesColumnNamesAndConvertsSqlValues()
    {
        using var table = new DataTable();
        table.Columns.Add("price", typeof(int));
        table.Columns.Add("Extra", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("id", typeof(short));
        table.Rows.Add(125, "unused", "Notebook", (short)7);
        using var reader = table.CreateDataReader();
        var mapper = new RowMapper<Product>(reader);

        Assert.IsTrue(reader.Read());
        var product = mapper.Map(reader);
        Assert.AreEqual(7, product.Id);
        Assert.AreEqual("Notebook", product.Name);
        Assert.AreEqual(125m, product.Price);
    }

    [TestMethod]
    public void Map_HandlesNullableAndMissingColumns()
    {
        using var table = new DataTable();
        table.Columns.Add("Score", typeof(short));
        table.Rows.Add(DBNull.Value);
        table.Rows.Add((short)3);
        using var reader = table.CreateDataReader();
        var mapper = new RowMapper<NullableRow>(reader);

        Assert.IsTrue(reader.Read());
        Assert.IsNull(mapper.Map(reader).Score);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(3, mapper.Map(reader).Score);

        using var users = new DataTable();
        users.Columns.Add("Name", typeof(string));
        users.Rows.Add("Maria");
        using var userReader = users.CreateDataReader();
        Assert.IsTrue(userReader.Read());
        var user = new RowMapper<User>(userReader).Map(userReader);
        Assert.AreEqual("Maria", user.Name);
        Assert.AreEqual(string.Empty, user.Email);
    }

    public sealed class NullableRow
    {
        public int? Score { get; set; }
    }

    public sealed class AssignedKey
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
