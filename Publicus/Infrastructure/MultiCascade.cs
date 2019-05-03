using System;
using System.Collections.Generic;

namespace Publicus
{
    public abstract class MultiCascade
    {
        public abstract IEnumerable<DatabaseObject> QueryLoad(IDatabase db);
        public abstract void AssignLoad(IEnumerable<DatabaseObject> list);
        public abstract IEnumerable<DatabaseObject> GetObjects();
    }

    public class MultiCascade<T> : MultiCascade where T : DatabaseObject, new()
    {
        private Func<List<T>> _getList;
        private string _columnName;
        private Guid _id;

        public MultiCascade(string columnName, Guid id, Func<List<T>> getList)
        {
            _columnName = columnName;
            _id = id;
            _getList = getList;
        }

        public override void AssignLoad(IEnumerable<DatabaseObject> list)
        {
            foreach (var o in list)
            {
                _getList().Add((T)o);
            }
        }

        public override IEnumerable<DatabaseObject> GetObjects()
        {
            return _getList();
        }

        public override IEnumerable<DatabaseObject> QueryLoad(IDatabase db)
        {
            var list = db.SubQuery<T>(DC.Equal(_columnName, _id));
            return list;
        }
    }
}
