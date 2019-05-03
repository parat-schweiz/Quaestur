using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace Publicus
{
    public class PostgresTransaction : ITransaction
    {
        public NpgsqlTransaction Transaction { get; private set; }

        public bool Valid { get { return Transaction != null; } }

        public PostgresTransaction()
        {
            Transaction = null;
        }

        public PostgresTransaction(NpgsqlTransaction transaction)
        {
            Transaction = transaction;
        }

        public void Rollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
            }
        }

        public void Commit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
            }
        }

        public void Dispose()
        {
            if (Transaction != null)
            {
                Transaction.Dispose();
                Transaction = null;
            }
        }
    }

    public class PostgresDatabase : IDatabase
    {
        private NpgsqlConnection _connection;
        private PostgresTransaction _transaction;

        public PostgresDatabase(Config config)
        {
            var connectionString = string.Format(
                "Server={0};Port={1};Database={2};User Id={3};Password={4};",
                config.DatabaseServer,
                config.DatabasePort,
                config.DatabaseName,
                config.DatabaseUsername,
                config.DatabasePassword);
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
            _transaction = new PostgresTransaction();
        }

        private string ColumnPreTypeDefinition(Field field)
        {
            if (field is StringField)
            {
                if (((StringField)field).Size > 1024)
                {
                    return "text";
                }
                else
                {
                    return string.Format("varchar({0})", ((StringField)field).Size);
                }
            }
            else if (field is StringNullField)
            {
                if (((StringNullField)field).Size > 1024)
                {
                    return "text";
                }
                else
                {
                    return string.Format("varchar({0})", ((StringNullField)field).Size);
                }
            }
            else if (field is StringListField)
            {
                return "text";
            }
            else if (field is MultiLanguageStringField)
            {
                return "text";
            }
            else if (field is DecimalField)
            {
                return string.Format("numeric({0}, {1})", ((DecimalField)field).Precision, ((DecimalField)field).Scale);
            }
            else if (field is DecimalNullField)
            {
                return string.Format("numeric({0}, {1})", ((DecimalNullField)field).Precision, ((DecimalNullField)field).Scale);
            }

            switch (field.BaseType.FullName)
            {
                case "System.Guid":
                    return "uuid";
                case "System.Int32":
                    return "integer";
                case "System.Int64":
                    return "bigint";
                case "System.String":
                    throw new InvalidOperationException("Type string should not arrive here.");
                case "System.DateTime":
                    return "timestamp";
                case "System.Decimal":
                    throw new InvalidOperationException("Type decimal should not arrive here.");
                case "System.Byte[]":
                    return "bytea";
                case "System.Boolean":
                    return "boolean";
                default:
                    throw new NotSupportedException("Data type " + field.BaseType.FullName + " not supported");
            }
        }

        private string ColumnTypeDefinition(Field field)
        {
            var preType = ColumnPreTypeDefinition(field);

            if (!field.Nullable)
            {
                preType += " NOT NULL";
            }

            if (field.ReferencedType != null)
            {
                preType += string.Format(" REFERENCES {0}(id)", TableName(field.ReferencedType));
            }

            return preType;
        }

        private string ColumnDefinition(Field field)
        {
            var fullType = ColumnTypeDefinition(field);

            if (field.ColumnName == "id")
            {
                fullType += " PRIMARY KEY";
            }

            return string.Format("{0} {1}", field.ColumnName, fullType);
        }

        public bool TableExists(string name)
        {
            using (var transaction = EnsureTransaction())
            {
                var command = Command("SELECT count(1) FROM pg_catalog.pg_tables WHERE schemaname = 'public' AND tablename = @tablename");
                command.AddParam("@tablename", name);
                return (long)command.ExecuteScalar() == 1;
            }
        }

        public void CreateTable<T>() where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                if (TableExists(TableName<T>()))
                {
                    return;
                }

                var temp = new T();
                var columns = string.Join(", ",
                    temp.Fields.Select(f => ColumnDefinition(f)));

                using (var command = Command("CREATE TABLE {0} ({1})", TableName<T>(), columns))
                {
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void Delete(DatabaseObject obj)
        {
            using (var transaction = EnsureTransaction())
            {
                using (var command = Command("DELETE FROM {0} WHERE id = @id", TableName(obj.GetType())))
                {
                    command.AddParam("@id", obj.Id.Value);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        private void Insert(DatabaseObject obj)
        {
            if (!obj.NewlyCreated)
            {
                throw new InvalidOperationException("Insert of object not newly created."); 
            }

            obj.Validate();

            var columns = string.Join(", ", obj.Fields.Select(f => f.ColumnName));
            var variables = string.Join(", ", obj.Fields.Select(f => f.VariableName));

            using (var command = Command("INSERT INTO {0} ({1}) VALUES ({2})", TableName(obj.GetType()), columns, variables))
            {
                foreach (var f in obj.Fields)
                {
                    f.AddValue(command); 
                }

                command.ExecuteNonQuery();
            }

            obj.Updated();
        }

        private void CascaseLoad(DatabaseObject source, Dictionary<Guid, DatabaseObject> loaded)
        {
            foreach (var cascade in source.Cascades)
            {
                var assingList = new List<DatabaseObject>();

                foreach (var o in cascade.QueryLoad(this))
                {
                    if (!loaded.ContainsKey(o.Id))
                    {
                        loaded.Add(o.Id, o);
                        CascaseLoad(o, loaded);
                        assingList.Add(o);
                    }
                    else
                    {
                        assingList.Add(loaded[o.Id]);
                    }
                }

                cascade.AssignLoad(assingList);
            }

            foreach (var field in source.Fields)
            {
                foreach (var cascade in field.CascadeLoad())
                {
                    if (loaded.ContainsKey(cascade.Id))
                    {
                        cascade.AssignLoad(loaded[cascade.Id]);
                    }
                    else
                    {
                        var o = cascade.QueryLoad(this);

                        if (o != null && !loaded.ContainsKey(o.Id))
                        {
                            loaded.Add(o.Id, o);
                            CascaseLoad(o, loaded);
                        }
                    }
                }
            }

            source.Updated();
        }

        public IEnumerable<T> Query<T>(DataCondition condition) where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                var sources = SubQuery<T>(condition);
                var loaded = new Dictionary<Guid, DatabaseObject>();

                foreach (var source in sources)
                {
                    loaded.Add(source.Id, source);
                }

                foreach (var source in sources)
                {
                    CascaseLoad(source, loaded);
                }

                return sources;
            }
        }

        public IEnumerable<T> Query<T>() where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                return Query<T>(DC.True());
            }
        }

        public IEnumerable<T> SubQuery<T>(DataCondition condition) where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                var list = new List<T>();

                using (var command = Command("SELECT * FROM {0} WHERE {1}", TableName<T>(), condition.Text))
                {
                    foreach (var value in condition.Values)
                    {
                        command.AddParam(value.Item1, value.Item2);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ret = new T();

                            foreach (var field in ret.Fields)
                            {
                                field.Read(reader);
                            }

                            list.Add(ret);
                        }
                    }
                }

                return list;
            }
        }

        public T Query<T>(Guid id) where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                var source = SubQuery<T>(id);
                if (source != null)
                {
                    var loaded = new Dictionary<Guid, DatabaseObject>();
                    loaded.Add(source.Id, source);
                    CascaseLoad(source, loaded);
                }
                return source;
            }
        }

        public T SubQuery<T>(Guid id) where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                var list = new List<T>();

                using (var command = Command("SELECT * FROM {0} WHERE id = @id", TableName<T>()))
                {
                    command.AddParam("@id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ret = new T();

                            foreach (var field in ret.Fields)
                            {
                                field.Read(reader);
                            }

                            list.Add(ret);
                        }
                    }
                }

                return list.FirstOrDefault();
            }
        }

        public T Query<T>(string idString) where T : DatabaseObject, new()
        {
            using (var transaction = EnsureTransaction())
            {
                if (Guid.TryParse(idString, out Guid id))
                {
                    return Query<T>(id);
                }
                else
                {
                    return null;
                }
            }
        }

        public string TableName<T>()
        {
            return TableName(typeof(T)); 
        }

        public string TableName(Type t)
        {
            var name = t.Name.ToLowerInvariant();

            switch (name)
            {
                case "role":
                case "group":
                case "user":
                    name += "s";
                    break;
            }

            return name;
        }

        private void Update(DatabaseObject obj)
        {
            if (obj.NewlyCreated)
            {
                throw new InvalidOperationException("Update of object newly created.");
            }

            if (!obj.Dirty)
            {
                return;
            }

            obj.Validate();

            var updates = string.Join(", ",
                obj.Fields.Where(f => f.Dirty)
                    .Select(f => f.ColumnName + " = " + f.VariableName));

            using (var command = Command("UPDATE {0} SET {1} WHERE id = @id", TableName(obj.GetType()), updates))
            {
                command.AddParam("@id", obj.Id.Value);

                foreach (var f in obj.Fields)
                {
                    if (f.Dirty)
                    {
                        f.AddValue(command);
                    }
                }

                command.ExecuteNonQuery();
            }

            obj.Updated();
        }

        private NpgsqlCommand Command(string text, params string[] parts)
        {
            var command = _connection.CreateCommand();
            command.Transaction = _transaction.Transaction;
            command.CommandText = string.Format(text, parts);
            return command;
        }

        public void Dispose()
        {
            _transaction.Dispose();

            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null; 
            }
        }

        private void AddCascade(DatabaseObject source, List<DatabaseObject> list)
        {
            foreach (var x in source.Cascades.SelectMany(c => c.GetObjects()))
            {
                if (!list.Contains(x))
                {
                    list.Add(x);
                    AddCascade(x, list); 
                }
            }

            foreach (var f in source.Fields)
            {
                foreach (var x in f.CascadeUpdate())
                {
                    if (!list.Contains(x))
                    {
                        list.Add(x);
                        AddCascade(x, list);
                    }
                } 
            }
        }

        public void Save(DatabaseObject obj)
        {
            using (var transaction = EnsureTransaction())
            {
                var list = new List<DatabaseObject>();
                list.Add(obj);
                AddCascade(obj, list);

                foreach (var o in list)
                {
                    if (o.Dirty)
                    {
                        if (o.NewlyCreated)
                        {
                            Insert(o);
                        }
                        else
                        {
                            Update(o);
                        }
                    }
                }

                transaction.Commit();
            }
        }

        private ITransaction EnsureTransaction()
        {
            if (_transaction.Valid)
            {
                return new PostgresTransaction();
            }
            else
            {
                return BeginTransaction(); 
            }
        }

        public ITransaction BeginTransaction()
        {
            if (_transaction.Valid)
            {
                throw new InvalidOperationException("Transaction already in progress."); 
            }

            _transaction = new PostgresTransaction(_connection.BeginTransaction());

            return _transaction;
        }

        public void AddColumn<T>(Func<T, Field> getField) where T : DatabaseObject, new()
        {
            var prototype = new T();
            var field = getField(prototype);

            using (var transaction = EnsureTransaction())
            {
                if (field.Nullable)
                {
                    var add = Command("ALTER TABLE {0} ADD COLUMN {1}", TableName<T>(), ColumnDefinition(field));
                    add.ExecuteNonQuery();
                }
                else
                {
                    var add = Command("ALTER TABLE {0} ADD COLUMN {1}", TableName<T>(), ColumnDefinition(field).Replace(" NOT NULL", string.Empty));
                    add.ExecuteNonQuery();

                    var update = Command("UPDATE {0} SET {1} = {2}", TableName<T>(), field.ColumnName, field.VariableName);
                    field.AddValue(update);
                    update.ExecuteNonQuery();

                    var notNull = Command("ALTER TABLE {0} ALTER COLUMN {1} SET NOT NULL", TableName<T>(), field.ColumnName);
                    notNull.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void ModifyColumnType<T>(Func<T, Field> getField) where T : DatabaseObject, new()
        {
            var prototype = new T();
            var field = getField(prototype);

            using (var transaction = EnsureTransaction())
            { 
                var command = Command("ALTER TABLE {0} ALTER COLUMN {1} TYPE {2}", TableName<T>(), field.ColumnName, ColumnPreTypeDefinition(field));
                command.ExecuteNonQuery();

                transaction.Commit();
            }
        }
    }

    public static class Extensions
	{
		public static void AddParam(this NpgsqlCommand command, string name, object value)
        {
            command.Parameters.Add(new NpgsqlParameter(name, value));
        }
	}
}
