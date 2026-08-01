using System.Collections.Generic;

namespace OverEasy.Editor
{
    public interface IEditorCommand
    {
        void Execute();
        void Undo();
    }

    public static class EditorHistory
    {
        private static readonly Stack<IEditorCommand> undoStack = new Stack<IEditorCommand>();
        private static readonly Stack<IEditorCommand> redoStack = new Stack<IEditorCommand>();

        public static bool CanUndo => undoStack.Count > 0;
        public static bool CanRedo => redoStack.Count > 0;

        public static void Execute(IEditorCommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }

        public static void Undo()
        {
            if (undoStack.Count == 0) return;
            var cmd = undoStack.Pop();
            cmd.Undo();
            redoStack.Push(cmd);
        }

        public static void Redo()
        {
            if (redoStack.Count == 0) return;
            var cmd = redoStack.Pop();
            cmd.Execute();
            undoStack.Push(cmd);
        }

        public static void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
