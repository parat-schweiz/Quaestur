using System;
namespace Publicus
{
    public abstract class SingleCascade
    {
        public abstract Guid Id { get; }
        public abstract DatabaseObject QueryLoad(IDatabase db);
        public abstract void AssignLoad(DatabaseObject obj);
    }

    public class SingleCascade<T> : SingleCascade where T : DatabaseObject, new()
    {
        private Action<T> _assign;
        private Guid _id;

        public SingleCascade(Guid id, Action<T> assign)
        {
            _id = id;
            _assign = assign;
        }

        public override Guid Id { get { return _id; } }

        public override void AssignLoad(DatabaseObject obj)
        {
            _assign((T)obj);
        }

        public override DatabaseObject QueryLoad(IDatabase db)
        {
            var o = db.SubQuery<T>(_id);
            _assign(o);
            return o;
        }
    }
}
