using AquaModelLibrary.Data.BillyHatcher.SetData;
using Godot;
using OverEasy;

namespace OverEasy.Editor
{
    public class BillyDeleteObjectCommand : IEditorCommand
    {
        private readonly EditingType editingType;
        private readonly int objectIndex;
        private readonly SetObj? savedSetObj;
        private readonly SetEnemy? savedSetEnemy;
        private readonly TreeItem categoryTreeItem;

        public BillyDeleteObjectCommand(EditingType type, int index, TreeItem categoryNode)
        {
            editingType = type;
            objectIndex = index;
            categoryTreeItem = categoryNode;

            switch (type)
            {
                case EditingType.BillySetObj:
                    savedSetObj = OverEasyGlobals.loadedBillySetObjects.setObjs[index];
                    break;
                case EditingType.BillySetDesign:
                    savedSetObj = OverEasyGlobals.loadedBillySetDesignObjects.setObjs[index];
                    break;
                case EditingType.BillySetEnemy:
                    savedSetEnemy = OverEasyGlobals.loadedBillySetEnemies.setEnemies[index];
                    break;
            }
        }

        public void Execute()
        {
            switch (editingType)
            {
                case EditingType.BillySetObj:
                    OverEasyGlobals.loadedBillySetObjects.setObjs.RemoveAt(objectIndex);
                    break;
                case EditingType.BillySetDesign:
                    OverEasyGlobals.loadedBillySetDesignObjects.setObjs.RemoveAt(objectIndex);
                    break;
                case EditingType.BillySetEnemy:
                    OverEasyGlobals.loadedBillySetEnemies.setEnemies.RemoveAt(objectIndex);
                    break;
            }
            OverEasyGlobals.RebuildBillyObjectCategory(categoryTreeItem, editingType);
        }

        public void Undo()
        {
            switch (editingType)
            {
                case EditingType.BillySetObj:
                    OverEasyGlobals.loadedBillySetObjects.setObjs.Insert(objectIndex, savedSetObj.Value);
                    break;
                case EditingType.BillySetDesign:
                    OverEasyGlobals.loadedBillySetDesignObjects.setObjs.Insert(objectIndex, savedSetObj.Value);
                    break;
                case EditingType.BillySetEnemy:
                    OverEasyGlobals.loadedBillySetEnemies.setEnemies.Insert(objectIndex, savedSetEnemy.Value);
                    break;
            }
            OverEasyGlobals.RebuildBillyObjectCategory(categoryTreeItem, editingType);
        }
    }
}
