using AquaModelLibrary.Data.BillyHatcher.SetData;
using Godot;
using OverEasy;

namespace OverEasy.Editor
{
    public class BillyMoveObjectCommand : IEditorCommand
    {
        private readonly EditingType editingType;
        private readonly int fromIndex;
        private readonly int toIndex;
        private readonly TreeItem categoryTreeItem;

        public BillyMoveObjectCommand(EditingType type, int from, int to, TreeItem categoryNode)
        {
            editingType = type;
            fromIndex = from;
            toIndex = to;
            categoryTreeItem = categoryNode;
        }

        public void Execute() => MoveAndSelect(fromIndex, toIndex);
        public void Undo() => MoveAndSelect(toIndex, fromIndex);

        private void MoveAndSelect(int from, int to)
        {
            OverEasyGlobals.ClearObjectSelection();

            switch (editingType)
            {
                case EditingType.BillySetObj:
                {
                    var item = OverEasyGlobals.loadedBillySetObjects.setObjs[from];
                    OverEasyGlobals.loadedBillySetObjects.setObjs.RemoveAt(from);
                    OverEasyGlobals.loadedBillySetObjects.setObjs.Insert(to, item);
                    break;
                }
                case EditingType.BillySetDesign:
                {
                    var item = OverEasyGlobals.loadedBillySetDesignObjects.setObjs[from];
                    OverEasyGlobals.loadedBillySetDesignObjects.setObjs.RemoveAt(from);
                    OverEasyGlobals.loadedBillySetDesignObjects.setObjs.Insert(to, item);
                    break;
                }
                case EditingType.BillySetEnemy:
                {
                    var item = OverEasyGlobals.loadedBillySetEnemies.setEnemies[from];
                    OverEasyGlobals.loadedBillySetEnemies.setEnemies.RemoveAt(from);
                    OverEasyGlobals.loadedBillySetEnemies.setEnemies.Insert(to, item);
                    break;
                }
            }

            OverEasyGlobals.RebuildBillyObjectCategory(categoryTreeItem, editingType);

            var newItem = categoryTreeItem.GetChild(to);
            if (newItem != null && GodotObject.IsInstanceValid(newItem))
            {
                OverEasyGlobals.setDataTree.SetSelected(newItem, 0);
                OverEasyGlobals.HandleTreeNodeSelected();
            }
        }
    }
}
