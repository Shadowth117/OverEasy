using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.ARCData;
using AquaModelLibrary.Data.BillyHatcher.SetData;
using AquaModelLibrary.Data.DataTypes;
using AquaModelLibrary.Data.Ninja;
using ArchiveLib;
using Godot;
using OverEasy.Billy;
using OverEasy.TextInfo;
using OverEasy.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OverEasy
{
    partial class OverEasyGlobals
    {
        public static StageDef stgDef = null;
        public static bool stgDefModified = false;
        public static SetEnemyList loadedBillySetEnemies = null;
        public static SetObjList loadedBillySetObjects = null;
        public static SetObjList loadedBillySetDesignObjects = null;
        public static Dictionary<int, SetObjDefinition> cachedBillySetObjDefinitions = new Dictionary<int, SetObjDefinition>();
        public static Dictionary<int, SetEnemyDefinition> cachedBillySetEnemyDefinitions = new Dictionary<int, SetEnemyDefinition>();
        public static Dictionary<int, string> cachedStageObjCommonNames = new Dictionary<int, string>();
        public static Dictionary<int, string> cachedStageObjLocalNames = new Dictionary<int, string>();
        public static bool isDay = true;
        public static Node3D daySkybox = null;
        public static Node3D nightSkybox = null;
        public static DirectionalLight3D billyDirectionalLight = null;

        public static PRD currentPRD = null;
        public static PRD currentCommonPRD = null;
        public static SetLightParam currentLightsParam = null;
        /// <summary>
        /// Stores the original stage limit so we can remove the test stages we add later
        /// </summary>
        public static int fakeDefEntryStart;

        public static Vector3? clipboardPosition = null;
        public static Vector3? clipboardRotation = null;
        public static SetObj? clipboardSetObj = null;
        public static SetEnemy? clipboardSetEnemy = null;

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

        private static void ResetBillyLoadedData()
        {
            clipboardPosition = null;
            clipboardRotation = null;
            clipboardSetObj = null;
            clipboardSetEnemy = null;

            currentLightsParam = null;
            billyDirectionalLight.Dispose();
            billyDirectionalLight = null;
            stgDef = null;
            currentPRD = null;
            currentCommonPRD = null;
            loadedBillySetObjects = null;
            cachedBillySetObjDefinitions.Clear();
            cachedBillySetEnemyDefinitions.Clear();
        }

        public static void BillyCopyObjectData()
        {
            switch (currentEditorType)
            {
                case EditingType.BillySpawnPoint:
                    clipboardSetObj = null;
                    clipboardSetEnemy = null;
                    clipboardPosition = GetVec3SchemaValues("PlayerPosition").ToGVec3();
                    clipboardRotation = new Vector3(0, (float)GetSpinBoxValue("PlayerRotation"), 0);
                    break;
                case EditingType.BillySetObj:
                    clipboardSetEnemy = null;
                    clipboardPosition = GetVec3SchemaValues("ObjectPosition").ToGVec3();
                    clipboardRotation = GetVec3SchemaValues("ObjectRotation").ToGVec3();
                    clipboardSetObj = loadedBillySetObjects.setObjs[currentObjectId];
                    break;
                case EditingType.BillySetDesign:
                    clipboardSetEnemy = null;
                    clipboardPosition = GetVec3SchemaValues("ObjectPosition").ToGVec3();
                    clipboardRotation = GetVec3SchemaValues("ObjectRotation").ToGVec3();
                    clipboardSetObj = loadedBillySetDesignObjects.setObjs[currentObjectId];
                    break;
                case EditingType.BillySetEnemy:
                    clipboardSetObj = null;
                    clipboardPosition = GetVec3SchemaValues("ObjectPosition").ToGVec3();
                    clipboardRotation = GetVec3SchemaValues("ObjectRotation").ToGVec3();
                    clipboardSetEnemy = loadedBillySetEnemies.setEnemies[currentObjectId];
                    break;
                case EditingType.None:
                    break;
            }
        }

        public static void BillyPasteTransformData()
        {
            if (clipboardPosition != null && clipboardRotation != null)
            {
                switch (currentEditorType)
                {
                    case EditingType.BillySpawnPoint:
                        SetVec3SchemaValues("PlayerPosition", clipboardPosition.Value);
                        SetSpinBoxValue("PlayerRotation", clipboardRotation.Value.Y);
                        break;
                    case EditingType.BillySetObj:
                        SetVec3SchemaValues("ObjectPosition", clipboardPosition.Value);
                        SetVec3SchemaValues("ObjectRotation", clipboardRotation.Value);
                        break;
                    case EditingType.BillySetDesign:
                        SetVec3SchemaValues("ObjectPosition", clipboardPosition.Value);
                        SetVec3SchemaValues("ObjectRotation", clipboardRotation.Value);
                        break;
                    case EditingType.BillySetEnemy:
                        SetVec3SchemaValues("ObjectPosition", clipboardPosition.Value);
                        SetVec3SchemaValues("ObjectRotation", clipboardRotation.Value);
                        break;
                    case EditingType.None:
                        break;
                }
            }
        }

        public static void BillyPasteNonTransformData()
        {
            if (clipboardSetObj != null || clipboardSetEnemy != null)
            {
                System.Numerics.Vector3 tempPos;
                Vector3Int.Vec3Int tempRot;
                switch (currentEditorType)
                {
                    case EditingType.BillySpawnPoint:
                        break;
                    case EditingType.BillySetObj:
                        if (clipboardSetObj != null)
                        {
                            tempPos = loadedBillySetObjects.setObjs[currentObjectId].Position;
                            tempRot = loadedBillySetObjects.setObjs[currentObjectId].BAMSRotation;
                            SetObj newData = clipboardSetObj.Value;
                            newData.Position = tempPos;
                            newData.BAMSRotation = tempRot;
                            loadedBillySetObjects.setObjs[currentObjectId] = newData;
                            LoadSetObject();

                            var parentNode = (Node3D)TransformGizmo.GetParent();
                            BillyModelIO.LoadBillyObjectModel(newData, false, parentNode);
                            parentNode.GlobalPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
                            parentNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.X),
                                (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Z));
                        }
                        break;
                    case EditingType.BillySetDesign:
                        if (clipboardSetObj != null)
                        {
                            tempPos = loadedBillySetDesignObjects.setObjs[currentObjectId].Position;
                            tempRot = loadedBillySetDesignObjects.setObjs[currentObjectId].BAMSRotation;
                            SetObj newData = clipboardSetObj.Value;
                            newData.Position = tempPos;
                            newData.BAMSRotation = tempRot;
                            loadedBillySetDesignObjects.setObjs[currentObjectId] = newData;
                            LoadSetObject();

                            var parentNode = (Node3D)TransformGizmo.GetParent();
                            BillyModelIO.LoadBillyObjectModel(newData, true, parentNode);
                            parentNode.GlobalPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
                            parentNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.X),
                                (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Z));
                        }
                        break;
                    case EditingType.BillySetEnemy:
                        if (clipboardSetEnemy != null)
                        {
                            tempPos = loadedBillySetEnemies.setEnemies[currentObjectId].Position;
                            tempRot = loadedBillySetEnemies.setEnemies[currentObjectId].BAMSRotation;
                            SetEnemy newData = clipboardSetEnemy.Value;
                            newData.Position = tempPos;
                            newData.BAMSRotation = tempRot;
                            loadedBillySetEnemies.setEnemies[currentObjectId] = newData;
                            LoadBillyEnemy();

                            var parentNode = (Node3D)TransformGizmo.GetParent();
                            BillyModelIO.LoadBillySetEnemyModel(newData, parentNode);
                            parentNode.GlobalPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
                            parentNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.X),
                                (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Z));
                        }
                        break;
                    case EditingType.None:
                        break;
                }
            }
        }

        public static void BillyPasteFullObjectData()
        {
            if (clipboardPosition != null && clipboardRotation != null)
            {
                switch (currentEditorType)
                {
                    case EditingType.BillySpawnPoint:
                        SetVec3SchemaValues("PlayerPosition", clipboardPosition.Value);
                        SetSpinBoxValue("PlayerRotation", clipboardRotation.Value.Y);
                        break;
                    case EditingType.BillySetObj:
                        if (clipboardSetObj != null)
                        {
                            SetObj newData = clipboardSetObj.Value;
                            loadedBillySetObjects.setObjs[currentObjectId] = newData;
                            LoadSetObject();

                            var parentNode = (Node3D)TransformGizmo.GetParent();
                            BillyModelIO.LoadBillyObjectModel(newData, false, parentNode);
                            parentNode.GlobalPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
                            parentNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.X),
                                (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Z));
                        }
                        else
                        {
                            SetVec3SchemaValues("ObjectPosition", clipboardPosition.Value);
                            SetVec3SchemaValues("ObjectRotation", clipboardRotation.Value);
                        }
                        break;
                    case EditingType.BillySetDesign:
                        if (clipboardSetObj != null)
                        {
                            SetObj newData = clipboardSetObj.Value;
                            loadedBillySetDesignObjects.setObjs[currentObjectId] = newData;

                            var parentNode = (Node3D)TransformGizmo.GetParent();
                            BillyModelIO.LoadBillyObjectModel(newData, true, parentNode);
                            parentNode.GlobalPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
                            parentNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.X),
                                (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Z));
                            LoadSetObject();
                        }
                        else
                        {
                            SetVec3SchemaValues("ObjectPosition", clipboardPosition.Value);
                            SetVec3SchemaValues("ObjectRotation", clipboardRotation.Value);
                        }
                        break;
                    case EditingType.BillySetEnemy:
                        if (clipboardSetEnemy != null)
                        {
                            SetEnemy newData = clipboardSetEnemy.Value;
                            loadedBillySetEnemies.setEnemies[currentObjectId] = newData;
                            LoadSetObject();

                            var parentNode = (Node3D)TransformGizmo.GetParent();
                            BillyModelIO.LoadBillySetEnemyModel(newData, parentNode);
                            parentNode.GlobalPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
                            parentNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.X),
                                (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * newData.BAMSRotation.Z));
                        }
                        else
                        {
                            SetVec3SchemaValues("ObjectPosition", clipboardPosition.Value);
                            SetVec3SchemaValues("ObjectRotation", clipboardRotation.Value);
                        }
                        break;
                    case EditingType.None:
                        break;
                }
            }
        }

        public static void BillyDropObjToNearestSolid()
        {
            Vector3 currentPosition = new Vector3();
            switch (currentEditorType)
            {
                case EditingType.BillySpawnPoint:
                    currentPosition = GetVec3SchemaValues("PlayerPosition").ToGVec3();
                    break;
                case EditingType.BillySetObj:
                case EditingType.BillySetDesign:
                case EditingType.BillySetEnemy:
                    currentPosition = GetVec3SchemaValues("ObjectPosition").ToGVec3();
                    break;
                case EditingType.None:
                    return;
            }
            var node = (Node3D)TransformGizmo.GetParent();
            Vector3 end = new Vector3(currentPosition.X, currentPosition.Y - 10000, currentPosition.Z);
            PhysicsRayQueryParameters3D objQuery = PhysicsRayQueryParameters3D.Create(currentPosition, end, 1);
            PhysicsRayQueryParameters3D mc2Query = PhysicsRayQueryParameters3D.Create(currentPosition, end, 4);
            var spaceState = modelRoot.GetWorld3D().DirectSpaceState;
            var objResult = spaceState.IntersectRay(objQuery);
            Vector3? finalPos = null;

            if (terrainCollision.Count > 0)
            {
                var spaceState2 = terrainCollision[0].GetWorld3D().DirectSpaceState;
                var mc2Result = spaceState.IntersectRay(mc2Query);

                if (mc2Result.Count > 0)
                {
                    finalPos = ((Vector3)mc2Result["position"]);
                }
            }
            
            if (objResult.Count > 0)
            {
                var objResultPos = ((Vector3)objResult["position"]);

                if(finalPos == null || finalPos.Value.Y < objResultPos.Y)
                {
                    finalPos = objResultPos;
                }
            }

            if(finalPos != null)
            {
                switch (currentEditorType)
                {
                    case EditingType.BillySpawnPoint:
                        SetVec3SchemaValues("PlayerPosition", finalPos.Value);
                        break;
                    case EditingType.BillySetObj:
                    case EditingType.BillySetDesign:
                    case EditingType.BillySetEnemy:
                        SetVec3SchemaValues("ObjectPosition", finalPos.Value);
                        break;
                    case EditingType.None:
                        break;
                }
            }
        }

        private static void LoadInitialDataBilly()
        {
            billyDirectionalLight = new DirectionalLight3D();
            modelRoot.GetParent().AddChild(billyDirectionalLight);
            LoadMapNames("TextInfo\\BillyMapNames.txt");
            LoadSetObjTemplates("TextInfo\\BillyObjDefinitions\\");
            LoadSetEnemyTemplates("TextInfo\\BillyEnemyDefinitions\\");
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

        public static void SetBillyLighting()
        {
            var def = stgDef.defs[currentMissionId];

            int lightingId = 0;
            if (LightingMapping.LightIdMapping.ContainsKey(def.missionName))
            {
                lightingId = isDay ? LightingMapping.LightIdMapping[def.missionName][0] : LightingMapping.LightIdMapping[def.missionName][1];
            }
            else if (LightingMapping.LightIdMapping.ContainsKey(def.worldName))
            {
                lightingId = isDay ? LightingMapping.LightIdMapping[def.worldName][0] : LightingMapping.LightIdMapping[def.worldName][1];
            }

            billyDirectionalLight.LightColor = new Color(0, 0, 0, 0);
            var param = currentLightsParam.lightParams[lightingId];

            RenderingServer.SetDefaultClearColor(new Color(param.ambientLightingColor[0] / 255f, param.ambientLightingColor[1] / 255f, param.ambientLightingColor[2] / 255f, (param.ambientLightingColor[3] / 255f)));

            modelRoot.GetWorld3D().Environment.AmbientLightEnergy = 1.0f;
            modelRoot.GetWorld3D().Environment.AmbientLightSkyContribution = 0;
            billyDirectionalLight.LightColor = new Color(param.directionalLightingColor[0] / 255f, param.directionalLightingColor[1] / 255f, param.directionalLightingColor[2] / 255f, (param.directionalLightingColor[3] / 255f));
            var tfm = Transform3D.Identity;
            var lightDir = -param.lightDirection;
            var lookAtTfm = tfm.LookingAt(lightDir.ToGVec3(), Godot.Vector3.Up, true);

            billyDirectionalLight.Quaternion = lookAtTfm.Basis.GetRotationQuaternion();

        }

        private static void LazyLoadBillyPC()
        {
            var def = stgDef.defs[currentMissionId];
            daySkybox = null;
            nightSkybox = null;
            SettingsBtn.GetPopup().SetItemChecked(0, true);
            isDay = true;
            SetCameraSettingsBilly();
            ClearModelAndTextureData();

            //Load Player Models
            BillyModelIO.CachePlayerModelsPC();

            //Load Enemy Models
            BillyModelIO.CacheEnemyModelsPC();

            //Load Object Models
            BillyModelIO.CacheObjectModelsPC(def);

            if(def.worldName == "title")
            {
                string titleObjPath = GetAssetPath("ar_obj_title.arc");
                string titleObjTexPath = GetAssetPath("obj_title.GVM");
                if(File.Exists(titleObjPath) && File.Exists(titleObjTexPath))
                {
                    BillyModelIO.CacheTitleObj(new ArEnemy(File.ReadAllBytes(titleObjPath)), new PuyoFile(File.ReadAllBytes(titleObjTexPath)));
                }
            }

            //Load Set Design
            string setDesignFilePath = GetAssetPath(def.setDesignFilename);
            if (File.Exists(setDesignFilePath))
            {
                loadedBillySetDesignObjects = new SetObjList(File.ReadAllBytes(setDesignFilePath));
            }
            else
            {
                loadedBillySetDesignObjects = null;
            }

            //Load Set Objects
            string setObjFilePath = GetAssetPath(def.setObjFilename);
            if (File.Exists(setObjFilePath))
            {
                loadedBillySetObjects = new SetObjList(File.ReadAllBytes(setObjFilePath));
            }
            else
            {
                loadedBillySetObjects = null;
            }

            //Load Set Enemies
            string setEnemyFilePath = GetAssetPath(def.setEnemyFilename);
            if (File.Exists(setEnemyFilePath))
            {
                loadedBillySetEnemies = new SetEnemyList(File.ReadAllBytes(setEnemyFilePath));
            }
            else
            {
                loadedBillySetEnemies = null;
            }

            //Load Light Param
            string setLightParamFilePath = GetAssetPath("set_light_param.bin");
            if (File.Exists(setLightParamFilePath))
            {
                currentLightsParam = new SetLightParam(File.ReadAllBytes(setLightParamFilePath));
            }
            SetBillyLighting();

            string lndPath = GetAssetPath(def.lndFilename);
            if (File.Exists(lndPath))
            {
                var lnd = Billy.ModelConversion.LNDToGDModel(def.lndFilename, new LND(File.ReadAllBytes(lndPath)));
                lnd.Visible = DisplayBtn.GetPopup().IsItemChecked(0);
                terrainModels.Add(lnd);
                modelRoot.AddChild(lnd);
            }
            string mc2Path = GetAssetPath(def.mc2Filename);
            if (File.Exists(mc2Path))
            {
                var mc2 = Billy.ModelConversion.MC2ToGDModel(def.mc2Filename, new MC2(File.ReadAllBytes(mc2Path)));
                mc2.Visible = DisplayBtn.GetPopup().IsItemChecked(1);
                terrainCollision.Add(mc2);
                modelRoot.AddChild(mc2);
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
            SettingsBtn.GetPopup().SetItemChecked(0, true);
            isDay = true;
            SetCameraSettingsBilly();
            ClearModelAndTextureData();
            cachedStageObjCommonNames.Clear();
            cachedStageObjLocalNames.Clear();
            var prdMissionname = GetBillyMissionName(def.missionName);
            currentArhiveFilename = $"k_{prdMissionname}.prd";
            currentPRD = new PRD(File.ReadAllBytes(GetAssetPath(currentArhiveFilename)));

            var battleCommonPRD = new PRD(File.ReadAllBytes(GetAssetPath("k_battle_common.prd")));
            var stageCommonPRD = new PRD(File.ReadAllBytes(GetAssetPath("k_stage_common.prd")));
            var bossCommonPRD = new PRD(File.ReadAllBytes(GetAssetPath("k_boss_common.prd")));

            //Load common object models, enemies, items. May want to do this in the initial load step
            PuyoFile eyeTextures = null;
            for (int i = 0; i < battleCommonPRD.files.Count; i++)
            {
                switch (battleCommonPRD.fileNames[i])
                {
                    case "ge_player1l.arc":
                        var billy = new GEPlayer(battleCommonPRD.files[i]);
                        BillyModelIO.CachePlayerModel("player_1", billy, false);
                        break;
                    case "ge_player2l.arc":
                        var rolly = new GEPlayer(battleCommonPRD.files[i]);
                        BillyModelIO.CachePlayerModel("player_2", rolly, false);
                        break;
                    case "ge_player3l.arc":
                        var chick = new GEPlayer(battleCommonPRD.files[i]);
                        BillyModelIO.CachePlayerModel("player_3", chick, false);
                        break;
                    case "ge_player4l.arc":
                        var bantam = new GEPlayer(battleCommonPRD.files[i]);
                        BillyModelIO.CachePlayerModel("player_4", bantam, false);
                        break;
                    case "ene_eye.gvm":
                        eyeTextures = new PuyoFile(battleCommonPRD.files[i]);
                        break;
                }
            }

            //Certain files use certain common PRDs
            if (prdMissionname.Contains("battle"))
            {
                currentCommonPRD = battleCommonPRD;
            }
            else if (prdMissionname.Contains("boss"))
            {
                currentCommonPRD = bossCommonPRD;
            }
            else
            {
                currentCommonPRD = stageCommonPRD;
            }
            for (int i = 0; i < currentCommonPRD.files.Count; i++)
            {
                switch (currentCommonPRD.fileNames[i])
                {
                    case "stgobj_common.arc":
                        var commonStgobj = new StageObj(currentCommonPRD.files[i]);
                        for (int j = 0; j < commonStgobj.objEntries.Count; j++)
                        {
                            var obj = commonStgobj.objEntries[j];
                            if (obj.model2Id0 != ushort.MaxValue)
                            {
                                cachedStageObjCommonNames.Add(j, obj.objName);
                            }
                        }
                        break;
                    case "geobj_common.arc":
                        var commonGeobj = new GEObj_Stage(currentCommonPRD.files[i]);
                        BillyModelIO.CacheGeobjCommon(commonGeobj);
                        break;
                    case "set_light_param.bin":
                        currentLightsParam = new SetLightParam(currentCommonPRD.files[i]);
                        break;

                }
            }
            SetBillyLighting();

            Dictionary<string, ArEnemy> enemyArchiveDict = new Dictionary<string, ArEnemy>();
            Dictionary<string, PuyoFile> enemyGVMDict = new Dictionary<string, PuyoFile>();
            ArEnemy titleObj = null;
            PuyoFile titleObjTex = null;
            GEObj_Stage localGeobj = null;
            StageObj localStgobj = null;
            for (int i = 0; i < currentPRD.files.Count; i++)
            {
                //Hold Enemy GVM
                if (ObjectVariants.enemyFileMap.ContainsKey(currentPRD.fileNames[i].ToLower()))
                {
                    enemyArchiveDict.Add(Path.GetFileNameWithoutExtension(currentPRD.fileNames[i].ToLower().Replace("ar_", "")), new ArEnemy(currentPRD.files[i]));
                }
                else if (currentPRD.fileNames[i].ToLower().StartsWith("ene_") && Path.GetExtension(currentPRD.fileNames[i].ToLower()) == ".gvm")
                {
                    enemyGVMDict.Add(Path.GetFileNameWithoutExtension(currentPRD.fileNames[i].ToLower()), new PuyoFile(currentPRD.files[i]));
                }
                else //Load High Quality Player Model
                if (currentPRD.fileNames[i] == "ge_player1.arc")
                {
                    var billy = new GEPlayer(currentPRD.files[i]);
                    BillyModelIO.CachePlayerModel("player_1", billy, true);
                }
                else if (currentPRD.fileNames[i] == "ge_player2.arc")
                {
                    var rolly = new GEPlayer(currentPRD.files[i]);
                    BillyModelIO.CachePlayerModel("player_2", rolly, true);
                }
                else if (currentPRD.fileNames[i] == "ge_player3.arc")
                {
                    var chick = new GEPlayer(currentPRD.files[i]);
                    BillyModelIO.CachePlayerModel("player_3", chick, true);
                }
                else if (currentPRD.fileNames[i] == "ge_player4.arc")
                {
                    var bantam = new GEPlayer(currentPRD.files[i]);
                    BillyModelIO.CachePlayerModel("player_4", bantam, true);
                }
                else

                //Load Set Camera

                //Load Set Design
                if (currentPRD.fileNames[i] == def.setDesignFilename)
                {
                    loadedBillySetDesignObjects = new SetObjList(currentPRD.files[i]);
                }
                else

                //Load Set Enemeies
                if (currentPRD.fileNames[i] == def.setEnemyFilename)
                {
                    loadedBillySetEnemies = new SetEnemyList(currentPRD.files[i]);
                }
                else

                //Load Set Objects
                if (currentPRD.fileNames[i] == def.setObjFilename)
                {
                    loadedBillySetObjects = new SetObjList(currentPRD.files[i]);
                }
                else

                //Load Stage Model
                if (currentPRD.fileNames[i] == def.lndFilename)
                {
                    var lnd = Billy.ModelConversion.LNDToGDModel(def.lndFilename, new LND(currentPRD.files[i]));
                    lnd.Visible = DisplayBtn.GetPopup().IsItemChecked(0);
                    terrainModels.Add(lnd);
                    modelRoot.AddChild(lnd);
                }

                //Load Title objects (if this is the title screen prd)
                //Title screen scenery objects are special and show up in an arc closer to what enemies use
                if (currentPRD.fileNames[i] == "ar_obj_title.arc")
                {
                    titleObj = new ArEnemy(currentPRD.files[i]);
                }
                if (currentPRD.fileNames[i] == "obj_title.GVM")
                {
                    titleObjTex = new PuyoFile(currentPRD.files[i]);
                }

                //Load Stage Object Models 
                if (currentPRD.fileNames[i] == def.commonData.objectData)
                {
                    localGeobj = new GEObj_Stage(currentPRD.files[i]);
                }

                //Load Stage Object Definitions
                if (currentPRD.fileNames[i] == def.commonData.objectDefinition)
                {
                    localStgobj = new StageObj(currentPRD.files[i]);
                    for (int j = 0; j < localStgobj.objEntries.Count; j++)
                    {
                        var obj = localStgobj.objEntries[j];
                        if (obj.model2Id0 != ushort.MaxValue)
                        {
                            cachedStageObjLocalNames.Add(j, obj.objName);
                        }
                    }
                }

                //Load Stage Collision Model
                if (currentPRD.fileNames[i] == def.mc2Filename)
                {
                    var mc2 = Billy.ModelConversion.MC2ToGDModel(def.mc2Filename, new MC2(currentPRD.files[i]));
                    mc2.Visible = DisplayBtn.GetPopup().IsItemChecked(1);
                    terrainCollision.Add(mc2);
                    modelRoot.AddChild(mc2);
                }

                //Load Stage Path

                //Load Stage Event Camera

                //Load stage event file
            }

            if(titleObj != null && titleObjTex != null)
            {
                BillyModelIO.CacheTitleObj(titleObj, titleObjTex);
            }

            //Load Stage Object Models
            if(localStgobj != null && localGeobj != null)
            {
                BillyModelIO.CacheGeobjLocal(localStgobj, localGeobj);
            }

            //Load enemies
            foreach (var pair in enemyArchiveDict)
            {
                for (int i = 0; i < pair.Value.texList[0].texNames.Count; i++)
                {
                    if (pair.Value.texList[0].texNames[i] == "am064_e00bstex01")
                    {
                        enemyGVMDict[pair.Key].Entries[i] = eyeTextures.Entries[0];
                    }
                }
                var model = pair.Value.models[0];

                switch (pair.Key)
                {
                    case "ene_am02":
                    case "ene_blue_boss":
                    case "ene_orange_boss":
                        model = pair.Value.models[1];
                        BillyModelIO.CacheModel($"enemy_{ObjectVariants.enemyFileMap[$"ar_{pair.Key}.arc"]}", model, pair.Value.texList[0], enemyGVMDict[pair.Key], false);
                        break;
                    case "ene_purple_boss":
                        model = pair.Value.models[32];
                        BillyModelIO.CacheModel($"enemy_{ObjectVariants.enemyFileMap[$"ar_{pair.Key}.arc"]}", model, pair.Value.texList[0], enemyGVMDict[pair.Key], false);
                        break;
                    case "ene_last_ex_boss":
                        var modelRef = $"enemy_{ObjectVariants.enemyFileMap[$"ar_{pair.Key}.arc"]}";
                        ModelConversion.LoadGVM(modelRef, enemyGVMDict[pair.Key], out var gvmTextures, out var gvrAlphaTypes);
                        var modelNode = ModelConversion.NinjaToGDModel(modelRef, pair.Value.models[5], gvmTextures, gvrAlphaTypes);
                        modelNode = ModelConversion.NinjaToGDModel(modelRef, pair.Value.models[6], gvmTextures, gvrAlphaTypes, null, null, modelNode);
                        ModelConversion.CreateObjectCollision(modelNode);
                        if (!modelDictionary.ContainsKey(modelRef))
                        {
                            modelDictionary[modelRef] = modelNode;
                        }
                        break;
                    default:
                        BillyModelIO.CacheModel($"enemy_{ObjectVariants.enemyFileMap[$"ar_{pair.Key}.arc"]}", model, pair.Value.texList[0], enemyGVMDict[pair.Key], false);
                        break;
                }

            }

            //Set skybox setting
            if (daySkybox != null)
            {
                daySkybox.Visible = isDay;
            }
            if (nightSkybox != null)
            {
                nightSkybox.Visible = !isDay;
            }
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
                            fakeDefEntryStart = stgDef.defs.Count;
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
                    fakeDefEntryStart = stgDef.defs.Count;

                    //Add unused maps
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_battle_red",
                        lndFilename = "stg_battle_red.lnd",
                        mc2Filename = "stg_battle_red.mc2",
                        setCameraFilename = "set_cam_battle_red.bin",
                        setDesignFilename = "set_design_battle_red.bin",
                        setEnemyFilename = "set_ene_battle_red.bin",
                        setObjFilename = "set_obj_battle_red.bin",
                        commonData = stgDef.defsDict["red1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_blue_night",
                        lndFilename = "stg_blue_night.lnd",
                        setDesignFilename = "set_design_blue_night.bin",
                        commonData = stgDef.defsDict["blue1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_pur_boss",
                        lndFilename = "stg_pur_boss.lnd",
                        mc2Filename = "stg_pur_boss.mc2",
                        commonData = stgDef.defsDict["purple1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_purple_n",
                        lndFilename = "stg_purple_n.lnd",
                        setDesignFilename = "set_design_purple_n.bin",
                        commonData = stgDef.defsDict["purple1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_purple_night",
                        lndFilename = "stg_purple_night.lnd",
                        setDesignFilename = "set_design_purple_night.bin",
                        commonData = stgDef.defsDict["purple1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_test",
                        lndFilename = "stg_test.lnd",
                        mc2Filename = "stg_test.mc2",
                        setCameraFilename = "set_cam_test01.bin",
                        setDesignFilename = "set_design_test.bin",
                        setEnemyFilename = "set_ene_test01.bin",
                        setObjFilename = "set_obj_test01.bin",
                        commonData = stgDef.defsDict["red1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_test2",
                        lndFilename = "stg_test2.lnd",
                        mc2Filename = "stg_test2.mc2",
                        setCameraFilename = "set_cam_test2.bin",
                        setDesignFilename = "set_design_test2.bin",
                        setEnemyFilename = "set_ene_test02.bin",
                        setObjFilename = "set_obj_test02.bin",
                        commonData = stgDef.defsDict["blue1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_test2_n",
                        lndFilename = "stg_test2_n.lnd",
                        setDesignFilename = "set_design_test2_n.bin",
                        commonData = stgDef.defsDict["blue1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_test07",
                        lndFilename = "stg_test07.lnd",
                        setCameraFilename = "set_cam_test07.bin",
                        setEnemyFilename = "set_ene_test07.bin",
                        setObjFilename = "set_obj_test07.bin",
                        commonData = stgDef.defsDict["purple1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);
                    stgDef.defs.Add(new StageDef.StageDefinition()
                    {
                        missionName = "stg_test08",
                        lndFilename = "stg_test08.lnd",
                        setCameraFilename = "set_cam_test08.bin",
                        setEnemyFilename = "set_ene_test08.bin",
                        setObjFilename = "set_obj_test08.bin",
                        commonData = stgDef.defsDict["blue1"].commonData
                    });
                    stgDef.defsDict.Add(stgDef.defs[^1].missionName, stgDef.defs[^1]);

                    return true;
                }
            }

            return false;
        }

        public static void LoadBillyEnemy()
        {
            allowedToUpdate = false;
            foreach (var objSet in activeObjectEditorObjects)
            {
                objSet.Value.Free();
            }
            activeObjectEditorObjects.Clear();

            //Load in enemy data
            LoadBillySetEnemyGui();
            LoadBillySetEnemyTemplateInfo();
            ToggleObjectScrollContainerCollision();
            allowedToUpdate = true;
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
            LoadBillySetObjectGui(setObjList);
            LoadBillySetObjectTemplateInfo(setObjList);
            ToggleObjectScrollContainerCollision();
            allowedToUpdate = true;
        }

        public static void LoadBillySpawn()
        {
            allowedToUpdate = false;
            foreach (var objSet in activeObjectEditorObjects)
            {
                objSet.Value.Free();
            }
            activeObjectEditorObjects.Clear();

            //Load in object data
            LoadBillySpawnPoint();
            LoadBillySpawnPointInfo();
            ToggleObjectScrollContainerCollision();
            allowedToUpdate = true;
        }

        public static void LoadBillySpawnPointInfo()
        {
            LoadVec3SchemaTemplateInfo("PlayerPosition", "Player Position", "Position", "The position of the player at spawn", "", "", "");
            LoadSchemaTemplateInfo("PlayerRotation", "Player Rotation", "Rotation", "The rotation of the player at spawn");
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
                var templatePath = Path.Combine(editorRootDirectory, "TextInfo\\BillyObjDefinitions", $"{setObj.objectId}.json");
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

        public static void LoadBillySetEnemyTemplateInfo()
        {
            var setObj = loadedBillySetEnemies.setEnemies[currentObjectId];
            //Check if there's a template for this object type, else we use the defaults
            var template = new SetEnemyDefinition();
            var key = setObj.enemyId;

            //Check if we've loaded this template already
            if (cachedBillySetEnemyDefinitions.ContainsKey(key))
            {
                template = cachedBillySetEnemyDefinitions[key];

            }
            else
            {
                var templatePath = Path.Combine(editorRootDirectory, "TextInfo\\BillyEnemyDefinitions", $"{setObj.enemyId}.json");
                if (File.Exists(templatePath))
                {
                    template = JsonSerializer.Deserialize<SetEnemyDefinition>(File.ReadAllText(templatePath));
                    cachedBillySetEnemyDefinitions.Add(key, template);
                }
            }

            if (activeObjectEditorObjects.ContainsKey("ObjectId"))
            {
                var objName = activeObjectEditorObjects["ObjectId"];
                var objNameText = (RichTextLabel)objName.GetChild(0);
                objNameText.Text = GetEnemyName(false, setObj);
                currentObjectTreeItem.SetText(0, GetEnemyName(true, setObj));
            }

            LoadVec3SchemaTemplateInfo("ObjectPosition", template.Position, "Position", template.PositionHint, template.PositionX, template.PositionY, template.PositionZ);
            LoadVec3SchemaTemplateInfo("ObjectRotation", template.Rotation, "Rotation", template.RotationHint, template.RotationX, template.RotationY, template.RotationZ);
            LoadSchemaTemplateInfo("Int_1C", template.Int_1C, "Int_1C", template.Int_1CHint);
            LoadSchemaTemplateInfo("Int_20", template.Int_20, "Int_20", template.Int_20Hint);
            LoadSchemaTemplateInfo("Int_24", template.Int_24, "Int_24", template.Int_24Hint);
            LoadSchemaTemplateInfo("Int_28", template.Int_28, "Int_28", template.Int_28Hint);
            LoadSchemaTemplateInfo("Int_2C", template.Int_2C, "Int_2C", template.Int_2CHint);
            LoadSchemaTemplateInfo("Int_30", template.Int_30, "Int_30", template.Int_30Hint);
            LoadSchemaTemplateInfo("Int_34", template.Int_34, "Int_34", template.Int_34Hint);
            LoadSchemaTemplateInfo("Int_38", template.Int_38, "Int_38", template.Int_38Hint);
            LoadSchemaTemplateInfo("Int_3C", template.Int_3C, "Int_3C", template.Int_3CHint);
            LoadSchemaTemplateInfo("Flt_40", template.Flt_40, "Flt_40", template.Flt_40Hint);
            LoadSchemaTemplateInfo("Flt_44", template.Flt_44, "Flt_44", template.Flt_44Hint);
            LoadSchemaTemplateInfo("Flt_48", template.Flt_48, "Flt_48", template.Flt_48Hint);
            LoadSchemaTemplateInfo("Flt_4C", template.Flt_4C, "Flt_4C", template.Flt_4CHint);
        }

        public static void LoadBillySpawnPoint()
        {
            var def = stgDef.defs[currentMissionId];
            StageDef.PlayerStart start;
            switch (currentObjectId)
            {
                case 0:
                    start = def.player1Start;
                    break;
                case 1:
                    start = def.player2Start;
                    break;
                case 2:
                    start = def.player3Start;
                    break;
                case 3:
                    start = def.player4Start;
                    break;
                default:
                    throw new System.Exception("Bad player spawn");
            }
            CreateVector3Schema("PlayerPosition", new Vector3(start.playerPosition.X, start.playerPosition.Y, start.playerPosition.Z));
            CreateFloatSchema("PlayerRotation", start.rotation);
        }

        public static void LoadBillySetEnemyGui()
        {
            var setEne = loadedBillySetEnemies.setEnemies[currentObjectId];
            CreateIntSchema("ObjectId", setEne.enemyId);
            CreateVector3Schema("ObjectPosition", new Vector3(setEne.Position.X, setEne.Position.Y, setEne.Position.Z));
            CreateVector3Schema("ObjectRotation", new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * setEne.BAMSRotation.X),
                (float)(NinjaConstants.FromBAMSValueToDegrees * setEne.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * setEne.BAMSRotation.Z)));
            CreateIntSchema("Int_1C", setEne.int_1C);
            CreateIntSchema("Int_20", setEne.int_20);
            CreateIntSchema("Int_24", setEne.int_24);
            CreateIntSchema("Int_28", setEne.int_28);
            CreateIntSchema("Int_2C", setEne.int_2C);
            CreateIntSchema("Int_30", setEne.int_30);
            CreateIntSchema("Int_34", setEne.int_34);
            CreateIntSchema("Int_38", setEne.int_38);
            CreateIntSchema("Int_3C", setEne.int_3C);
            CreateFloatSchema("Flt_40", setEne.flt_40);
            CreateFloatSchema("Flt_44", setEne.flt_44);
            CreateFloatSchema("Flt_48", setEne.flt_48);
            CreateFloatSchema("Flt_4C", setEne.flt_4C);
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

        public static void UpdateBillySpawnPoint(int spawnId)
        {
            //Gather some initial values
            var objPosition = GetVec3SchemaValues("PlayerPosition");
            var objRotation = GetSpinBoxValue("PlayerRotation");

            //Update 3d representation
            var parentNode = (Node3D)TransformGizmo.GetParent();
            parentNode.GlobalPosition = new Vector3(objPosition.X, objPosition.Y, objPosition.Z);
            parentNode.RotationDegrees = new Vector3(0, (float)objRotation, 0);

            //Gather current object values
            var spawn = new StageDef.PlayerStart();
            spawn.playerPosition = objPosition;
            spawn.rotation = (float)objRotation;

            var objRaw = stgDef.defs[currentMissionId];
            switch (spawnId)
            {
                case 0:
                    objRaw.player1Start = spawn;
                    break;
                case 1:
                    objRaw.player2Start = spawn;
                    break;
                case 2:
                    objRaw.player3Start = spawn;
                    break;
                case 3:
                    objRaw.player4Start = spawn;
                    break;
            }

            stgDefModified = true;
        }

        public static void UpdateBillySetObjects(SetObjList setObjList, int objectId, bool isDesignObject)
        {
            //Gather some initial values
            var objPosition = GetVec3SchemaValues("ObjectPosition");
            var objRotation = GetVec3SchemaValues("ObjectRotation");

            //Gather current object values
            bool shouldReloadModel = false;
            var objRaw = setObjList.setObjs[objectId];
            foreach (var objSet in activeObjectEditorObjects)
            {
                switch (objSet.Key)
                {
                    case "ObjectId":
                        var objIdValue = (int)GetSpinBoxValue("ObjectId");
                        if (objRaw.objectId != objIdValue)
                        {
                            shouldReloadModel = true;
                            objRaw.objectId = objIdValue;
                        }
                        break;
                    case "ObjectPosition":
                        objRaw.Position = objPosition;
                        break;
                    case "ObjectRotation":
                        objRaw.BAMSRotation = new AquaModelLibrary.Data.DataTypes.Vector3Int.Vec3Int((int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.X),
                            (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Y), (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Z));
                        break;
                    case "IntProperty1":
                        var intProperty1Value = (int)GetSpinBoxValue("IntProperty1");
                        if ((objRaw.objectId == 11 || objRaw.objectId == 10) && objRaw.intProperty1 != intProperty1Value)
                        {
                            shouldReloadModel = true;
                        }
                        objRaw.intProperty1 = intProperty1Value;
                        break;
                    case "IntProperty2":
                        objRaw.intProperty2 = (int)GetSpinBoxValue("IntProperty2");
                        break;
                    case "IntProperty3":
                        var intProperty3Value = (int)GetSpinBoxValue("IntProperty3");
                        if (objRaw.objectId == 11 && objRaw.intProperty3 != intProperty3Value)
                        {
                            shouldReloadModel = true;
                        }
                        objRaw.intProperty3 = intProperty3Value;
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

            //Update 3d representation
            var parentNode = (Node3D)TransformGizmo.GetParent();
            if (shouldReloadModel)
            {
                BillyModelIO.LoadBillyObjectModel(objRaw, isDesignObject, parentNode);
            }
            parentNode.GlobalPosition = new Vector3(objPosition.X, objPosition.Y, objPosition.Z);
            parentNode.RotationDegrees = new Vector3(objRotation.X, objRotation.Y, objRotation.Z);

            //Insert over original file object values
            setObjList.setObjs[objectId] = objRaw;
        }

        public static void UpdateBillySetEnemies(int objectId)
        {
            //Gather some initial values
            var objPosition = GetVec3SchemaValues("ObjectPosition");
            var objRotation = GetVec3SchemaValues("ObjectRotation");

            //Gather current object values
            bool shouldReloadModel = false;
            var objRaw = loadedBillySetEnemies.setEnemies[objectId];
            foreach (var objSet in activeObjectEditorObjects)
            {
                switch (objSet.Key)
                {
                    case "ObjectId":
                        var idValue = (int)GetSpinBoxValue("ObjectId");
                        if (objRaw.enemyId != idValue)
                        {
                            shouldReloadModel = true;
                        }
                        objRaw.enemyId = idValue;
                        break;
                    case "ObjectPosition":
                        objRaw.Position = objPosition;
                        break;
                    case "ObjectRotation":
                        objRaw.BAMSRotation = new AquaModelLibrary.Data.DataTypes.Vector3Int.Vec3Int((int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.X),
                            (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Y), (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Z));
                        break;
                    case "Int_1C":
                        objRaw.int_1C = (int)GetSpinBoxValue("Int_1C");
                        break;
                    case "Int_20":
                        objRaw.int_20 = (int)GetSpinBoxValue("Int_20");
                        break;
                    case "Int_24":
                        objRaw.int_24 = (int)GetSpinBoxValue("Int_24");
                        break;
                    case "Int_28":
                        objRaw.int_28 = (int)GetSpinBoxValue("Int_28");
                        break;
                    case "Int_2C":
                        objRaw.int_2C = (int)GetSpinBoxValue("Int_2C");
                        break;
                    case "Int_30":
                        objRaw.int_30 = (int)GetSpinBoxValue("Int_30");
                        break;
                    case "Int_34":
                        objRaw.int_34 = (int)GetSpinBoxValue("Int_34");
                        break;
                    case "Int_38":
                        var int38Value = (int)GetSpinBoxValue("Int_38");
                        if (objRaw.int_38 != int38Value)
                        {
                            shouldReloadModel = true;
                        }
                        objRaw.int_38 = int38Value;
                        break;
                    case "Int_3C":
                        objRaw.int_3C = (int)GetSpinBoxValue("Int_3C");
                        break;
                    case "Flt_40":
                        objRaw.flt_40 = (int)GetSpinBoxValue("Flt_40");
                        break;
                    case "Flt_44":
                        objRaw.flt_44 = (int)GetSpinBoxValue("Flt_44");
                        break;
                    case "Flt_48":
                        objRaw.flt_48 = (int)GetSpinBoxValue("Flt_48");
                        break;
                    case "Flt_4C":
                        objRaw.flt_4C = (int)GetSpinBoxValue("Flt_4C");
                        break;
                }
            }

            //Update 3d representation
            var parentNode = (Node3D)TransformGizmo.GetParent();
            if (shouldReloadModel)
            {
                BillyModelIO.LoadBillySetEnemyModel(objRaw, parentNode);
            }
            parentNode.GlobalPosition = new Vector3(objPosition.X, objPosition.Y, objPosition.Z);
            parentNode.RotationDegrees = new Vector3(objRotation.X, objRotation.Y, objRotation.Z);

            //Insert over original file object values
            loadedBillySetEnemies.setEnemies[objectId] = objRaw;
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
            string setEnemyName = stgDef.defs[currentMissionId].setEnemyFilename;
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
            File.WriteAllBytes(Path.Combine(modFolderLocation, setEnemyName), loadedBillySetEnemies.GetBytes());

            //StageDef
            if (stgDefModified)
            {
                backupFileName = Path.Combine(backupFolderLocation, "ge_stagedef.bin");
                if (!File.Exists(backupFileName))
                {
                    File.Copy(Path.Combine(gameFolderLocation, "ge_stagedef.bin"), backupFileName);
                }
                File.WriteAllBytes(Path.Combine(modFolderLocation, "ge_stagedef.bin"), BuildSavableStageDef().GetBytes());
                stgDefModified = false;
            }
        }

        private static StageDef BuildSavableStageDef()
        {
            //Get rid of the test stages that shouldn't be there
            StageDef newStageDef = new StageDef();
            for (int i = 0; i < fakeDefEntryStart; i++)
            {
                newStageDef.defs.Add(stgDef.defs[i]);
            }
            newStageDef.definition = stgDef.definition;
            return newStageDef;
        }

        private static void SaveDataBillyGC()
        {
            string backupFileName;
            string setName = stgDef.defs[currentMissionId].setObjFilename;
            string setDesignName = stgDef.defs[currentMissionId].setDesignFilename;
            string setEnemyName = stgDef.defs[currentMissionId].setEnemyFilename;
            int setFileId = -1;
            int setDesignId = -1;
            int setEnemyId = -1;

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
                else if (currentPRD.fileNames[i] == setEnemyName)
                {
                    setEnemyId = i;
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
            if (setEnemyId != -1)
            {
                currentPRD.files[setEnemyId] = loadedBillySetEnemies.GetBytes();
            }

            //StageDef
            string stgDefLocation = null;
            if (File.Exists(Path.Combine(modFolderLocation, "k_boot.prd")))
            {
                stgDefLocation = Path.Combine(modFolderLocation, "k_boot.prd");

            }
            else if (File.Exists(Path.Combine(gameFolderLocation, "k_boot.prd")))
            {
                stgDefLocation = Path.Combine(gameFolderLocation, "k_boot.prd");
            }

            PRD bootPrd = null;
            if (stgDefModified == true && stgDefLocation != null)
            {
                bootPrd = new PRD(stgDefLocation);
                for (int i = 0; i < bootPrd.files.Count; i++)
                {
                    var fName = bootPrd.fileNames[i];
                    if (fName == "ge_stagedef.bin")
                    {
                        bootPrd.files[i] = BuildSavableStageDef().GetBytes();
                    }
                }

                //Make sure we have a backup if it's not there already
                backupFileName = Path.Combine(backupFolderLocation, "k_boot.prd");
                if (!File.Exists(backupFileName))
                {
                    File.Copy(Path.Combine(gameFolderLocation, "k_boot.prd"), backupFileName);
                }
                File.WriteAllBytes(Path.Combine(modFolderLocation, "k_boot.prd"), bootPrd.GetBytes());
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

            //We will ALWAYS have 4 spawnpoints. 
            temp = activeNode.CreateChild();
            temp.SetText(0, "Player Spawnpoints");
            temp.SetMetadata(0, 2);

            Node3D p1Node = null;
            for (int i = 0; i < 4; i++)
            {
                StageDef.PlayerStart start;
                switch (i)
                {
                    case 0:
                        start = stgDef.defs[currentMissionId].player1Start;
                        break;
                    case 1:
                        start = stgDef.defs[currentMissionId].player2Start;
                        break;
                    case 2:
                        start = stgDef.defs[currentMissionId].player3Start;
                        break;
                    case 3:
                        start = stgDef.defs[currentMissionId].player4Start;
                        break;
                    default:
                        throw new System.Exception("Bad player spawn point");
                }

                var objNode = temp.CreateChild();
                objNode.SetText(0, $"Player Spawn {i + 1}");

                //Node type
                objNode.SetMetadata(0, 3);
                //Node's original object id
                objNode.SetMetadata(1, i);
                //Node's object category
                objNode.SetMetadata(2, 3);
                //Attach a model instance
                var modelNode = BillyModelIO.LoadBillySpawnModel(i);
                modelNode.SetMeta("treeItem", objNode);
                objNode.SetMetadata(3, modelNode);
                modelNode.RotationDegrees = new Vector3(0, start.rotation, 0);
                modelNode.Position = new Vector3(start.playerPosition.X, start.playerPosition.Y, start.playerPosition.Z);
                modelRoot.AddChild(modelNode);

                if (i == 0)
                {
                    p1Node = modelNode;
                }
            }
            temp.Collapsed = true;
            HandleEditorObjectsOnselection(p1Node);

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
                    var modelNode = BillyModelIO.LoadBillyObjectModel(obj, false);
                    modelNode.SetMeta("treeItem", objNode);
                    objNode.SetMetadata(3, modelNode);

                    modelNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
                    modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
                    modelRoot.AddChild(modelNode);
                }
                temp.Collapsed = true;
            }

            if (loadedBillySetDesignObjects != null && loadedBillySetDesignObjects?.setObjs?.Count != 0)
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
                    var modelNode = BillyModelIO.LoadBillyObjectModel(obj, true);
                    modelNode.SetMeta("treeItem", objNode);
                    objNode.SetMetadata(3, modelNode);

                    modelNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
                    modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
                    modelRoot.AddChild(modelNode);
                }
                temp.Collapsed = true;
            }

            if (loadedBillySetEnemies != null && loadedBillySetEnemies?.setEnemies?.Count != 0)
            {
                temp = activeNode.CreateChild();
                temp.SetText(0, "Set Enemy Objects");
                temp.SetMetadata(0, 2);
                for (int i = 0; i < loadedBillySetEnemies.setEnemies.Count; i++)
                {
                    var obj = loadedBillySetEnemies.setEnemies[i];
                    var objNode = temp.CreateChild();
                    objNode.SetText(0, GetEnemyName(true, obj));

                    //Node type
                    objNode.SetMetadata(0, 3);
                    //Node's original object id
                    objNode.SetMetadata(1, i);
                    //Node's object category
                    objNode.SetMetadata(2, 4);
                    //Attach a model instance
                    var modelNode = BillyModelIO.LoadBillySetEnemyModel(obj);
                    modelNode.SetMeta("treeItem", objNode);
                    objNode.SetMetadata(3, modelNode);

                    modelNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
                    modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
                    modelRoot.AddChild(modelNode);
                }
                temp.Collapsed = true;
            }
        }



        public static string GetObjectName(bool isNode, SetObj obj)
        {
            string nodeAddition = "";
            if (isNode)
            {
                nodeAddition = "Type ";
            }

            switch (obj.objectId)
            {
                case 0xA:
                    //Use stgobj names if we have them for this
                    if(obj.intProperty2 == 1 && cachedStageObjCommonNames.ContainsKey(obj.intProperty1))
                    {
                        return cachedBillySetObjDefinitions[obj.objectId].ObjectName + $" {cachedStageObjCommonNames[obj.intProperty1]}";
                    }
                    else if (obj.intProperty2 == 0 && cachedStageObjLocalNames.ContainsKey(obj.intProperty1))
                    {
                        return cachedBillySetObjDefinitions[obj.objectId].ObjectName + $" {cachedStageObjLocalNames[obj.intProperty1]}";
                    }
                    return cachedBillySetObjDefinitions[obj.objectId].ObjectName + $" {obj.intProperty1}";
                case 0xB:
                    string fruit;
                    switch (obj.intProperty1)
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
                default:
                    if (cachedBillySetObjDefinitions.ContainsKey(obj.objectId))
                    {
                        return cachedBillySetObjDefinitions[obj.objectId].ObjectName;
                    }
                    else
                    {
                        return $"Object {nodeAddition}{obj.objectId} 0x{obj.objectId:X}";
                    }

            }
        }

        public static string GetEnemyName(bool isNode, SetEnemy obj)
        {
            string nodeAddition = "";
            if (isNode)
            {
                nodeAddition = "Type ";
            }

            if (obj.enemyId == 0x101)
            {
                switch (obj.int_38)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return $"Zako - {ObjectVariants.zakoCrows[obj.int_38]}";
                    default:
                        return $"Zako - Out of range";
                }
            }

            if (cachedBillySetEnemyDefinitions.ContainsKey(obj.enemyId))
            {
                return cachedBillySetEnemyDefinitions[obj.enemyId].EnemyName;
            }
            else
            {
                return $"Enemy {nodeAddition}0x{obj.enemyId:X}";
            }
        }

        public static void TransformFromGizmoBilly(Vector3? pos, Quaternion? rot, Vector3? scale)
        {
            switch (currentEditorType)
            {
                case EditingType.BillySetObj:
                case EditingType.BillySetDesign:
                case EditingType.BillySetEnemy:
                    if (pos != null)
                    {
                        SetVec3SchemaValues("ObjectPosition", (Vector3)pos);
                    }
                    if (rot != null)
                    {
                        var eulRot = ((Quaternion)rot).GetEuler() * 180 / Mathf.Pi;
                        SetVec3SchemaValues("ObjectRotation", eulRot);
                    }
                    if (scale != null)
                    {
                        //SetVec3SchemaValues("ObjectScale", (Vector3)scale);
                    }
                    break;
                case EditingType.BillySpawnPoint:
                    if (pos != null)
                    {
                        SetVec3SchemaValues("PlayerPosition", (Vector3)pos);
                    }
                    if (rot != null)
                    {
                        var eulRot = ((Quaternion)rot).GetEuler() * 180 / Mathf.Pi;
                        SetSpinBoxValue("PlayerRotation", eulRot.Z);
                    }
                    break;
            }
        }
    }
}
