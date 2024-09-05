using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.SetData;
using AquaModelLibrary.Data.Ninja;
using AquaModelLibrary.Data.PSO2.Aqua.AquaObjectData;
using Godot;
using OverEasy.Billy;
using OverEasy.TextInfo;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OverEasy
{
    partial class OverEasyGlobals
    {
        public static void SetCameraSettingsBilly()
        {
            ViewerCamera.SCROLL_SPEED = 1000;
            ViewerCamera.ZOOM_SPEED = 500;
            ViewerCamera.SPIN_SPEED = 10;
            ViewerCamera.DEFAULT_DISTANCE = 2000;
            ViewerCamera.ROTATE_SPEED_X = 40;
            ViewerCamera.ROTATE_SPEED_Y = 40;
            ViewerCamera.TOUCH_ZOOM_SPEED = 4000;
            ViewerCamera.SHIFT_MULTIPLIER = 2.5;
            ViewerCamera.CTRL_MULTIPLIER = 0.4;
            ViewerCamera.FREECAM_ACCELERATION = 3000;
            ViewerCamera.FREECAM_DECELERATION = -10;
            ViewerCamera.FREECAM_VELOCITY_MULTIPLIER = 400;
        }

        private static void LoadInitialDataBilly()
        {
            LoadMapNames("TextInfo/BillyMapNames.txt");
            LoadSetObjTemplates("TextInfo/BillyObjDefinitions/");
            if (!ReadBillyGEStageDef(modFolderLocation))
            {
                ReadBillyGEStageDef(gameFolderLocation);
            }

            var tempList = stgDef.defs.ToList();

            var root = setDataTree.CreateItem();
            setDataTree.HideRoot = true;
            var defsDictKeys = stgDef.defsDict.Keys.ToList();
            foreach (var mission in mapKeyOrderList)
            {
                if (defsDictKeys.Contains(mission))
                {
                    var node = setDataTree.CreateItem(root);
                    string description = "";
                    if (mapNames.ContainsKey(mission))
                    {
                        description = $"{mapNames[mission]}";
                    }
                    node.SetText(0, $"{mission} - {description}");

                    //Store node type as int, store actual stageDef id for later use
                    node.SetMetadata(0, 1);
                    node.SetMetadata(1, stgDef.defs.IndexOf(stgDef.defsDict[mission]));

                    defsDictKeys.Remove(mission);
                }
            }

            //Catch unlisted missions
            foreach (var mission in defsDictKeys)
            {
                var node = setDataTree.CreateItem(root);
                node.SetText(0, $"{mission}");

                //Store node type as int, store actual stageDef id for later use
                node.SetMetadata(0, 1);
                node.SetMetadata(1, stgDef.defs.IndexOf(stgDef.defsDict[mission]));
            }
        }

        private static void LazyLoadBillyPC()
        {
            var def = stgDef.defs[currentMissionId];
            daySkybox = null;
            nightSkybox = null;
            dayNightToggle.ButtonPressed = true;
            isDay = true;
            SetCameraSettingsBilly();
            ClearModelAndTextureData();
            //Load Set Design
            string setDesignFilePath = GetAssetPath(def.setDesignFilename);
            if (File.Exists(setDesignFilePath))
            {
                loadedBillySetDesignObjects = new SetObjList(File.ReadAllBytes(setDesignFilePath));
            }

            //Load Set Objects
            string setObjFilePath = GetAssetPath(def.setObjFilename);
            if (File.Exists(setObjFilePath))
            {
                loadedBillySetObjects = new SetObjList(File.ReadAllBytes(setObjFilePath));
            }

            string lndPath = GetAssetPath(def.lndFilename);
            if (File.Exists(lndPath))
            {
                modelRoot.AddChild(Billy.ModelConversion.LNDToGDModel(def.lndFilename, new LND(File.ReadAllBytes(lndPath))));
            }

            if (daySkybox != null)
            {
                daySkybox.Visible = isDay;
            }
            if (nightSkybox != null)
            {
                nightSkybox.Visible = !isDay;
            }
        }

        private static void LazyLoadBillyGC()
        {
            var def = stgDef.defs[currentMissionId];
            daySkybox = null;
            nightSkybox = null;
            dayNightToggle.ButtonPressed = true;
            isDay = true;
            SetCameraSettingsBilly();
            ClearModelAndTextureData();
            var prdMissionname = GetBillyMissionName(def.missionName);
            currentPRD = new PRD(File.ReadAllBytes(GetAssetPath($"k_{prdMissionname}.prd")));

            for (int i = 0; i < currentPRD.files.Count; i++)
            {
                //Load Set Camera

                //Load Set Design
                if (currentPRD.fileNames[i] == def.setDesignFilename)
                {
                    loadedBillySetDesignObjects = new SetObjList(currentPRD.files[i]);
                }

                //Load Set Enemeies

                //Load Set Objects
                if (currentPRD.fileNames[i] == def.setObjFilename)
                {
                    loadedBillySetObjects = new SetObjList(currentPRD.files[i]);
                }

                //Load Stage Model
                if (currentPRD.fileNames[i] == def.lndFilename)
                {
                    modelRoot.AddChild(Billy.ModelConversion.LNDToGDModel(def.lndFilename, new LND(currentPRD.files[i])));
                }

                //Load Stage Object models (We need to link these somehow...)

                //Load Stage Collision Model

                //Load Stage Path

                //Load Stage Event Camera

                //When figured out, load stage event file

                //Load stage bsp?
            }
            if (daySkybox != null)
            {
                daySkybox.Visible = isDay;
            }
            if (nightSkybox != null)
            {
                nightSkybox.Visible = !isDay;
            }

            //Load common object models, enemies, items. May want to do this in the initial load step
        }

        public static bool ReadBillyGEStageDef(string location)
        {
            if (gameType is GameType.BillyGC)
            {
                if (File.Exists(Path.Combine(location, "k_boot.prd")))
                {
                    var bootPrd = new PRD(Path.Combine(location, "k_boot.prd"));
                    for (int i = 0; i < bootPrd.files.Count; i++)
                    {
                        var fName = bootPrd.fileNames[i];
                        if (fName == "ge_stagedef.bin")
                        {
                            stgDef = new StageDef(bootPrd.files[i]);
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (File.Exists(Path.Combine(location, "ge_stagedef.bin")))
                {
                    stgDef = new StageDef(Path.Combine(location, "ge_stagedef.bin"));
                    return true;
                }
            }

            return false;
        }

        public static void LoadBillySetObject(SetObjList setObjList)
        {
            allowedToUpdate = false;
            foreach (var objSet in activeObjectEditorObjects)
            {
                objSet.Value.Free();
            }
            activeObjectEditorObjects.Clear();

            //Load in object data
            var setObj = setObjList.setObjs[currentObjectId];
            LoadBillySetObjectGui(setObjList);
            LoadBillySetObjectTemplateInfo(setObjList);
            ToggleObjectScrollContainerCollision();
            allowedToUpdate = true;
        }

        public static void LoadBillySetObjectTemplateInfo(SetObjList setObjList)
        {
            var setObj = setObjList.setObjs[currentObjectId];
            //Check if there's a template for this object type, else we use the defaults
            var template = new SetObjDefinition();
            var key = setObj.objectId;

            //Check if we've loaded this template already
            if (cachedBillySetObjDefinitions.ContainsKey(key))
            {
                template = cachedBillySetObjDefinitions[key];

            }
            else
            {
                var templatePath = Path.Combine(editorRootDirectory, @"BillyHatcher\\SetData\\SetObjDefinitions", $"{setObj.objectId}.json");
                if (File.Exists(templatePath))
                {
                    template = JsonSerializer.Deserialize<SetObjDefinition>(File.ReadAllText(templatePath));
                    cachedBillySetObjDefinitions.Add(key, template);
                }
            }

            if (activeObjectEditorObjects.ContainsKey("ObjectId"))
            {
                var objName = activeObjectEditorObjects["ObjectId"];
                var objNameText = (RichTextLabel)objName.GetChild(0);
                objNameText.Text = GetObjectName(false, setObj);
                currentObjectTreeItem.SetText(0, GetObjectName(true, setObj));
            }

            LoadVec3SchemaTemplateInfo("ObjectPosition", template.Position, "Position", template.PositionHint, template.PositionX, template.PositionY, template.PositionZ);
            LoadVec3SchemaTemplateInfo("ObjectRotation", template.Rotation, "Rotation", template.RotationHint, template.RotationX, template.RotationY, template.RotationZ);
            LoadSchemaTemplateInfo("IntProperty1", template.IntProperty1, "Int Property 1", template.IntProperty1Hint);
            LoadSchemaTemplateInfo("IntProperty2", template.IntProperty2, "Int Property 2", template.IntProperty2Hint);
            LoadSchemaTemplateInfo("IntProperty3", template.IntProperty3, "Int Property 3", template.IntProperty3Hint);
            LoadSchemaTemplateInfo("IntProperty4", template.IntProperty4, "Int Property 4", template.IntProperty4Hint);
            LoadSchemaTemplateInfo("FloatProperty1", template.FloatProperty1, "Float Property 1", template.FloatProperty1Hint);
            LoadSchemaTemplateInfo("FloatProperty2", template.FloatProperty2, "Float Property 2", template.FloatProperty2Hint);
            LoadSchemaTemplateInfo("FloatProperty3", template.FloatProperty3, "Float Property 3", template.FloatProperty3Hint);
            LoadSchemaTemplateInfo("FloatProperty4", template.FloatProperty4, "Float Property 4", template.FloatProperty4Hint);
            LoadSchemaTemplateInfo("ByteProperty1", template.ByteProperty1, "Byte Property 1", template.ByteProperty1Hint);
            LoadSchemaTemplateInfo("ByteProperty2", template.ByteProperty2, "Byte Property 2", template.ByteProperty2Hint);
            LoadSchemaTemplateInfo("ByteProperty3", template.ByteProperty3, "Byte Property 3", template.ByteProperty3Hint);
            LoadSchemaTemplateInfo("ByteProperty4", template.ByteProperty4, "Byte Property 4", template.ByteProperty4Hint);
        }

        public static void LoadBillySetObjectGui(SetObjList setObjList)
        {
            var setObj = setObjList.setObjs[currentObjectId];

            CreateIntSchema("ObjectId", setObj.objectId);
            CreateVector3Schema("ObjectPosition", new Vector3(setObj.Position.X, setObj.Position.Y, setObj.Position.Z));
            CreateVector3Schema("ObjectRotation", new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * setObj.BAMSRotation.X),
                (float)(NinjaConstants.FromBAMSValueToDegrees * setObj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * setObj.BAMSRotation.Z)));
            CreateIntSchema("IntProperty1", setObj.intProperty1);
            CreateIntSchema("IntProperty2", setObj.intProperty2);
            CreateIntSchema("IntProperty3", setObj.intProperty3);
            CreateIntSchema("IntProperty4", setObj.intProperty4);
            CreateFloatSchema("FloatProperty1", setObj.fltProperty1);
            CreateFloatSchema("FloatProperty2", setObj.fltProperty2);
            CreateFloatSchema("FloatProperty3", setObj.fltProperty3);
            CreateFloatSchema("FloatProperty4", setObj.fltProperty4);
            CreateByteSchema("ByteProperty1", setObj.btProperty1);
            CreateByteSchema("ByteProperty2", setObj.btProperty2);
            CreateByteSchema("ByteProperty3", setObj.btProperty3);
            CreateByteSchema("ByteProperty4", setObj.btProperty4);
        }

        public static void UpdateBillySetObjects(SetObjList setObjList, int objectId)
        {
            //Gather current object values
            var objRaw = setObjList.setObjs[objectId];
            foreach (var objSet in activeObjectEditorObjects)
            {
                switch (objSet.Key)
                {
                    case "ObjectId":
                        objRaw.objectId = (int)GetSpinBoxValue("ObjectId");
                        break;
                    case "ObjectPosition":
                        objRaw.Position = GetVec3SchemaValues("ObjectPosition");
                        break;
                    case "ObjectRotation":
                        var objRotation = GetVec3SchemaValues("ObjectRotation");
                        objRaw.BAMSRotation = new AquaModelLibrary.Data.DataTypes.Vector3Int.Vec3Int((int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.X),
                            (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Y), (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Z));
                        break;
                    case "IntProperty1":
                        objRaw.intProperty1 = (int)GetSpinBoxValue("IntProperty1");
                        break;
                    case "IntProperty2":
                        objRaw.intProperty2 = (int)GetSpinBoxValue("IntProperty2");
                        break;
                    case "IntProperty3":
                        objRaw.intProperty3 = (int)GetSpinBoxValue("IntProperty3");
                        break;
                    case "IntProperty4":
                        objRaw.intProperty4 = (int)GetSpinBoxValue("IntProperty4");
                        break;
                    case "FloatProperty1":
                        objRaw.fltProperty1 = (float)GetSpinBoxValue("FloatProperty1");
                        break;
                    case "FloatProperty2":
                        objRaw.fltProperty2 = (float)GetSpinBoxValue("FloatProperty2");
                        break;
                    case "FloatProperty3":
                        objRaw.fltProperty3 = (float)GetSpinBoxValue("FloatProperty3");
                        break;
                    case "FloatProperty4":
                        objRaw.fltProperty4 = (float)GetSpinBoxValue("FloatProperty4");
                        break;
                    case "ByteProperty1":
                        objRaw.btProperty1 = (byte)GetSpinBoxValue("ByteProperty1");
                        break;
                    case "ByteProperty2":
                        objRaw.btProperty2 = (byte)GetSpinBoxValue("ByteProperty2");
                        break;
                    case "ByteProperty3":
                        objRaw.btProperty3 = (byte)GetSpinBoxValue("ByteProperty3");
                        break;
                    case "ByteProperty4":
                        objRaw.btProperty4 = (byte)GetSpinBoxValue("ByteProperty4");
                        break;
                }
            }

            //Insert over original file object values
            setObjList.setObjs[objectId] = objRaw;
        }

        /// <summary>
        /// For some reason this one mission doesn't correlate to the actual mission name.
        /// Billy mission names are technically hardcoded, but in most cases we can infer them from the stagedef mission name anyways.
        /// </summary>
        private static string GetBillyMissionName(string missionName)
        {
            if (missionName == "last")
            {
                missionName = "last1";
            }

            return missionName;
        }

        private static void SaveDataBillyPC()
        {
            string backupFileName;
            string setName = stgDef.defs[currentMissionId].setObjFilename;
            string setDesignName = stgDef.defs[currentMissionId].setDesignFilename;
            //Make sure we have backups if they're not there already
            backupFileName = Path.Combine(backupFolderLocation, setName);
            if (!File.Exists(backupFileName))
            {
                File.Copy(Path.Combine(gameFolderLocation, setName), backupFileName);
            }
            backupFileName = Path.Combine(backupFolderLocation, setDesignName);
            if (!File.Exists(backupFileName))
            {
                File.Copy(Path.Combine(gameFolderLocation, setDesignName), backupFileName);
            }

            File.WriteAllBytes(Path.Combine(modFolderLocation, setName), loadedBillySetObjects.GetBytes());
            File.WriteAllBytes(Path.Combine(modFolderLocation, setDesignName), loadedBillySetDesignObjects.GetBytes());
        }

        private static void SaveDataBillyGC()
        {
            string backupFileName;
            string setName = stgDef.defs[currentMissionId].setObjFilename;
            string setDesignName = stgDef.defs[currentMissionId].setDesignFilename;
            int setFileId = -1;
            int setDesignId = -1;
            for (int i = 0; i < currentPRD.fileNames.Count; i++)
            {
                if (currentPRD.fileNames[i] == setName)
                {
                    setFileId = i;
                }
                else if (currentPRD.fileNames[i] == setDesignName)
                {
                    setDesignId = i;
                }
            }
            if (setFileId != -1)
            {
                currentPRD.files[setFileId] = loadedBillySetObjects.GetBytes();
            }
            if (setDesignId != -1)
            {
                currentPRD.files[setDesignId] = loadedBillySetDesignObjects.GetBytes();
            }
            //Make sure we have backups if they're not there already
            backupFileName = Path.Combine(backupFolderLocation, currentArhiveFilename);
            if (!File.Exists(backupFileName))
            {
                File.Copy(Path.Combine(gameFolderLocation, currentArhiveFilename), backupFileName);
            }
            File.WriteAllBytes(Path.Combine(modFolderLocation, currentArhiveFilename), currentPRD.GetBytes());
        }

        private static void PopulateSetObjectsBilly(TreeItem activeNode)
        {
            TreeItem temp;
            if (loadedBillySetObjects != null && loadedBillySetObjects?.setObjs?.Count != 0)
            {
                temp = activeNode.CreateChild();
                temp.SetText(0, "Set Objects");
                temp.SetMetadata(0, 2);
                for (int i = 0; i < loadedBillySetObjects.setObjs.Count; i++)
                {
                    var obj = loadedBillySetObjects.setObjs[i];
                    var objNode = temp.CreateChild();
                    objNode.SetText(0, GetObjectName(true, obj));

                    //Node type
                    objNode.SetMetadata(0, 3);
                    //Node's original object id
                    objNode.SetMetadata(1, i);
                    //Node's object category
                    objNode.SetMetadata(2, 1);
                    //Attach a model instance
                    var modelNode = LoadBillyObjectModel(obj, false);
                    modelNode.SetMeta("treeItem", objNode);
                    objNode.SetMetadata(3, modelNode);

                    modelNode.Rotation = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
                    modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
                    modelRoot.AddChild(modelNode);
                }
                temp.Collapsed = true;
            }

            if (loadedBillySetDesignObjects?.setObjs?.Count != 0)
            {
                temp = activeNode.CreateChild();
                temp.SetText(0, "Set Design Objects");
                temp.SetMetadata(0, 2);
                for (int i = 0; i < loadedBillySetDesignObjects.setObjs.Count; i++)
                {
                    var obj = loadedBillySetDesignObjects.setObjs[i];
                    var objNode = temp.CreateChild();
                    objNode.SetText(0, GetObjectName(true, obj));

                    //Node type
                    objNode.SetMetadata(0, 3);
                    //Node's original object id
                    objNode.SetMetadata(1, i);
                    //Node's object category
                    objNode.SetMetadata(2, 2);
                    //Attach a model instance
                    var modelNode = LoadBillyObjectModel(obj, true);
                    modelNode.SetMeta("treeItem", objNode);
                    objNode.SetMetadata(3, modelNode);

                    modelNode.Rotation = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
                    modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
                    modelRoot.AddChild(modelNode);
                }
                temp.Collapsed = true;
            }
        }

        private static Node3D LoadBillyObjectModel(SetObj obj, bool designObj)
        {
            var name = "blueDefaultBox";
            Color color = new Color(0, 0, 1, 1);
            if (designObj)
            {
                color = new Color(0, 1, 1, 1);
                name = "greenDefaultBox";
            }

            Node3D modelNode;
            if (modelDictionary.ContainsKey(name))
            {
                modelNode = ModelConversion.GDModelClone(modelDictionary[name]);
            }
            else
            {
                modelNode = ModelConversion.CreateDefaultObjectModel(obj.GetHashCode().ToString("X"), color);
                ((MeshInstance3D)modelNode.GetChild(0)).CreateTrimeshCollision();
                var staticBody = ((StaticBody3D)modelNode.GetChild(0).GetChild(0));
                var child = ((CollisionShape3D)staticBody.GetChild(0));
                child.Disabled = false;
                staticBody.CollisionLayer = 1;
                staticBody.CollisionMask = 1;
                modelDictionary.Add(name, modelNode);
            }

            return modelNode;
        }

        public static string GetObjectName(bool isNode, SetObj obj)
        {
            string nodeAddition = "";
            if (isNode)
            {
                nodeAddition = "Type ";
            }

            if (obj.objectId == 0xB)
            {
                string fruit;
                switch(obj.intProperty1)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        fruit = ObjectVariants.fruits[obj.intProperty1];
                        break;
                    default:
                        fruit = ObjectVariants.fruits[6];
                        break;
                }
                return cachedBillySetObjDefinitions[obj.objectId].ObjectName + $" {fruit}";
            }
            else if (cachedBillySetObjDefinitions.ContainsKey(obj.objectId))
            {
                return cachedBillySetObjDefinitions[obj.objectId].ObjectName;
            }
            else
            {
                return $"Object {nodeAddition}{obj.objectId} 0x{obj.objectId:X}";
            }
        }
    }
}
