using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public interface IDatabase : IDisposable
    {
		IEnumerable<T> Query<T>() where T : DatabaseObject, new();
		IEnumerable<T> Query<T>(DataCondition condition) where T : DatabaseObject, new();
        IEnumerable<T> SubQuery<T>(DataCondition condition) where T : DatabaseObject, new();
        T Query<T>(Guid id) where T : DatabaseObject, new();
        T SubQuery<T>(Guid id) where T : DatabaseObject, new();
        T Query<T>(string idString) where T : DatabaseObject, new();
        void Save(DatabaseObject obj);
        void Delete(DatabaseObject obj);
        void CreateTable<T>() where T : DatabaseObject, new();
        string TableName(Type t);
        ITransaction BeginTransaction();
        void AddColumn<T>(Func<T, Field> getField) where T : DatabaseObject, new();
        void ModifyColumnType<T>(Func<T, Field> getField) where T : DatabaseObject, new();
    }

    public interface ITransaction : IDisposable
    {
        void Commit();
        void Rollback(); 
    }

    public enum DataOperator
    {
        Equal = 0,
        NotEqual = 1,
        Greater = 2,
        Smaller = 3,
        GreaterOrEqual = 4,
        SmallerOrEqual = 5, 
    }

    public static class DC
    {
        public static DataCondition True()
        {
            return new DataTrueCondition();
        }

        public static DataCondition Equal(string columnName, object value)
        {
            return new DataValueCondition(columnName, DataOperator.Equal, value); 
        }
    }

    public abstract class DataCondition
    {
        public abstract string Text { get; }
        public abstract IEnumerable<Tuple<string, object>> Values { get; }

        public DataAndCondition And(DataCondition b)
        {
            return new DataAndCondition(this, b);
        }
    }

    public class DataAndCondition : DataCondition
    {
        public DataCondition A { get; private set; }
        public DataCondition B { get; private set; }

        public DataAndCondition(DataCondition a, DataCondition b)
        {
            A = a;
            B = b; 
        }

        public override string Text
        {
            get
            {
                return string.Format("({0} AND {1})", A.Text, B.Text);
            }
        }

        public override IEnumerable<Tuple<string, object>> Values
        {
            get
            {
                foreach (var value in A.Values)
                {
                    yield return value; 
                }

                foreach (var value in B.Values)
                {
                    yield return value;
                }
            }
        }
    }

    public class DataTrueCondition : DataCondition
    {
        public override string Text
        {
            get { return "true"; } 
        }

        public override IEnumerable<Tuple<string, object>> Values
        {
            get
            {
                return new Tuple<string, object>[0];
            }
        }
    }

    public class DataValueCondition : DataCondition
    {
        public string ColumnName { get; private set; }
        public string VariableName { get { return "@" + ColumnName; } }
        public object Value { get; private set; }
        public DataOperator Operator { get; private set; }

        private string OperatorText
        {
            get 
            {
                switch (Operator)
                {
                    case DataOperator.Equal:
                        return "=";
                    case DataOperator.NotEqual:
                        return "!=";
                    case DataOperator.Smaller:
                        return "<";
                    case DataOperator.Greater:
                        return ">";
                    case DataOperator.SmallerOrEqual:
                        return "<=";
                    case DataOperator.GreaterOrEqual:
                        return ">=";
                    default:
                        throw new NotSupportedException();
                }
            } 
        }

        public override string Text
        {
            get
            {
                return string.Format("({0} {1} {2})", ColumnName, OperatorText, VariableName);
            }
        }

        public override IEnumerable<Tuple<string, object>> Values
        {
            get 
            {
                yield return new Tuple<string, object>(VariableName, Value);
            } 
        }

        public DataValueCondition(string columnName, DataOperator op, object value)
        {
            ColumnName = columnName;
            Operator = op;
            Value = value;
        }
    }
}
