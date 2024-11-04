namespace chARpack
{
    public interface IUndoableAction
    {
        public void Execute();
        public void Undo();
    }
}
