using AquaModelLibrary.Data.BillyHatcher.ARCData;
using AquaModelLibrary.Data.BillyHatcher.SetData;
using AquaModelLibrary.Data.Ninja.Model;
using AquaModelLibrary.Data.Ninja;
using ArchiveLib;
using Godot;
using OverEasy.TextInfo;
using System.Collections.Generic;
using System.IO;
using AquaModelLibrary.Data.PSO2.Aqua;
using AquaModelLibrary.Data.BillyHatcher;
using VrSharp.Gvr;

namespace OverEasy.Billy
{
    public class BillyModelIO
    {
        public static void CacheEnemyModelsPC()
        {
            foreach (var set in ObjectVariants.enemyFileMap)
            {
                //Load textures
                string texturePath = null;
                switch (set.Key)
                {
                    case "ar_ene_yellow_boss_green.arc":
                        texturePath = OverEasyGlobals.GetAssetPath("ene_yellow_boss.gvm");
                        break;
                    case "ar_ene_red_boss.arc":
                        texturePath = OverEasyGlobals.GetAssetPath("ene_red_boss_dino.gvm");
                        break;
                    default:
                        texturePath = OverEasyGlobals.GetAssetPath(set.Key.Replace(".arc", ".gvm").Replace("ar_", ""));
                        break;

                }
                ModelConversion.LoadGVM(set.Key, new PuyoFile(File.ReadAllBytes(texturePath)), out var gvmTextures, out var gvrAlphaTypes);
                if (gvmTextures[0].ResourceName == "am064_e00bstex01.gvr")
                {
                    string eyespath = OverEasyGlobals.GetAssetPath("ene_eye.gvm");
                    if (File.Exists(eyespath))
                    {
                        ModelConversion.LoadGVM(set.Key, new PuyoFile(File.ReadAllBytes(eyespath)), out var eyeTextures, out var eyeAlphaTypes);
                        gvmTextures[0] = eyeTextures[0];
                    }

                }

                //Load models
                var modelPath = OverEasyGlobals.GetAssetPath(set.Key);
                if (File.Exists(modelPath))
                {
                    var arc = new ArEnemy(File.ReadAllBytes(modelPath));
                    NJSObject nj = null;
                    NJTextureList njtl = null;

                    Node3D modelNode;
                    switch (set.Key)
                    {
                        case "ar_ene_am02.arc":
                        case "ar_ene_blue_boss.arc":
                        case "ar_ene_orange_boss.arc":
                            nj = arc.models[1];
                            njtl = arc.texList[0];
                            modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
                            break;
                        case "ar_ene_purple_boss.arc":
                            nj = arc.models[32];
                            njtl = arc.texList[0];
                            modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
                            break;
                        case "ar_ene_last_ex_boss.arc":
                            modelNode = ModelConversion.NinjaToGDModel(set.Key, arc.models[5], gvmTextures, gvrAlphaTypes);
                            modelNode = ModelConversion.NinjaToGDModel(set.Key, arc.models[6], gvmTextures, gvrAlphaTypes, null, null, modelNode);
                            break;
                        default:
                            nj = arc.models[0];
                            njtl = arc.texList[0];
                            modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
                            break;
                    }
                    ModelConversion.CreateObjectCollision(modelNode);

                    string enemyRef = $"enemy_{set.Value}";
                    if (!OverEasyGlobals.modelDictionary.ContainsKey(enemyRef))
                    {
                        OverEasyGlobals.modelDictionary.Add(enemyRef, modelNode);
                    }
                }

            }
        }

        public static Node3D LoadBillySpawnModel(int spawnId)
        {
            var name = $"player_{spawnId + 1}";

            Node3D modelNode;
            if (OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                modelNode = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary[name]);
            }
            else
            {
                Color color = new Color(1, 0, 0, 1);
                modelNode = ModelConversion.CreateDefaultObjectModel(name, color);
                ((MeshInstance3D)modelNode.GetChild(0)).CreateTrimeshCollision();
                var staticBody = ((StaticBody3D)modelNode.GetChild(0).GetChild(0));
                var child = ((CollisionShape3D)staticBody.GetChild(0));
                child.Disabled = false;
                staticBody.CollisionLayer = 1;
                staticBody.CollisionMask = 1;
                OverEasyGlobals.modelDictionary.Add(name, modelNode);
            }

            return modelNode;
        }

        public static Node3D LoadBillySetEnemyModel(SetEnemy ene, Node3D modelNode = null)
        {
            var name = $"enemy_{ene.enemyId.ToString("X")}";
            if (ene.enemyId == 0x101)
            {
                name += $"_{ene.int_38}";
            }

            //If it's not null, we clean up the node.
            if (modelNode != null)
            {
                CleanModelNode(modelNode);
            }
            if (OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                modelNode = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary[name], modelNode);
            }
            else if (ene.enemyId == 0)
            {
                if (modelNode == null)
                {
                    modelNode = new Node3D();
                }
            }
            else
            {
                Color color = new Color(1, 1, 0, 1);
                modelNode = ModelConversion.CreateDefaultObjectModel(name, color, modelNode);
                ((MeshInstance3D)modelNode.GetChild(0)).CreateTrimeshCollision();
                var staticBody = ((StaticBody3D)modelNode.GetChild(0).GetChild(0));
                var child = ((CollisionShape3D)staticBody.GetChild(0));
                child.Disabled = false;
                staticBody.CollisionLayer = 1;
                staticBody.CollisionMask = 1;
                OverEasyGlobals.modelDictionary.Add(name, modelNode);
            }

            return modelNode;
        }

        public static Node3D LoadBillyObjectModel(SetObj obj, bool designObj, Node3D modelNode = null)
        {
            string name = $"object_{obj.objectId}";

            switch(obj.objectId)
            {
                case 10:
                    string objectBank = "Local";
                    if(obj.intProperty2 == 1)
                    {
                        objectBank = "Common";
                    }
                    name = $"commGeoM2{objectBank}_{obj.intProperty1}";
                    break;
                case 11:
                    name += $"_{obj.intProperty1}";
                    break;
                case 25:
                    name = $"egg_{obj.intProperty1}";
                    if(!OverEasyGlobals.modelDictionary.ContainsKey(name))
                    {
                        name = "egg_0";
                    }
                    break;
                case 50:
                    name = $"segg_{obj.intProperty1}";
                    if (!OverEasyGlobals.modelDictionary.ContainsKey(name))
                    {
                        name = "segg_0";
                    }
                    break;
            }

            //If it's not null, we clean up the node.
            if(modelNode != null)
            {
                CleanModelNode(modelNode);
            }
            if (OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                modelNode = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary[name], modelNode);
            }
            else if (obj.objectId == 0)
            {
                if(modelNode == null)
                {
                    modelNode = new Node3D();
                }
            }
            else
            {
                switch (obj.objectId)
                {
                    default:
                        name = "blueDefaultBox";
                        Color color = new Color(0, 0, 1, 1);
                        if (designObj)
                        {
                            color = new Color(0, 1, 0, 1);
                            name = "greenDefaultBox";
                        }
                        modelNode = ModelConversion.CreateDefaultObjectModel(name, color, modelNode);

                        //Set up collision
                        ModelConversion.CreateObjectCollision(modelNode);
                        break;
                }

                if (!OverEasyGlobals.modelDictionary.ContainsKey(name))
                {
                    OverEasyGlobals.modelDictionary.Add(name, modelNode);
                }
            }

            //Handle object scale
            switch (obj.objectId)
            {
                case 11:
                    modelNode.Scale = obj.intProperty3 == 1 ? new Vector3(2, 2, 2) : new Vector3(1, 1, 1);
                    break;
                default:
                    modelNode.Scale = new Vector3(1, 1, 1);
                    break;
            }

            return modelNode;
        }

        public static void CleanModelNode(Node3D modelNode)
        {
            bool reAddCamera = false;
            bool reAddTransform = false;
            if(modelNode == OverEasyGlobals.ViewCamera.targetNode.GetParent())
            {
                reAddCamera = true;
                OverEasyGlobals.ViewCamera.ToggleMode();
            }
            if(modelNode == OverEasyGlobals.TransformGizmo.GetParent())
            {
                reAddTransform = true;
                OverEasyGlobals.TransformGizmo.Reparent(OverEasyGlobals.TransformGizmo.GetTree().Root, true);
            }
            foreach (var child in modelNode.GetChildren())
            {
                //Set visibility so this appears more immediately seamless
                if (child is Node3D child3d)
                {
                    child3d.Visible = false;
                }
                child.QueueFree();
            }
            if(reAddTransform)
            {
                OverEasyGlobals.TransformGizmo.Reparent(modelNode, true);
            }
            if (reAddCamera)
            {
                OverEasyGlobals.ViewCamera.ToggleMode();
            }
        }

        public static void CacheTitleObj(ArEnemy titleObj, PuyoFile gvm)
        {
            ModelConversion.LoadGVM("titleObj", gvm, out var gvmTextures, out var gvrAlphaTypes);
            //Small flowers
            CacheModel("object_4610", titleObj.models[1], titleObj.texList[0], gvm, false);
            //Big flowers
            CacheModel("object_4609", titleObj.models[3], titleObj.texList[0], gvm, false);
            //Tree
            CacheModel("object_4611", titleObj.models[4], titleObj.texList[0], gvm, false);
            //Bush
            CacheModel("object_4608", titleObj.models[2], titleObj.texList[0], gvm, false);
            //Waterfall
            //CacheModel("object_4616", titleObj.models[7], titleObj.texList[0], gvm, false);
            //Water
            //CacheModel("object_4615", titleObj.models[8], titleObj.texList[0], gvm, false);
            //Rainbow
            CacheModel("object_4617", titleObj.models[9], titleObj.texList[0], gvm, false);
            //Chick
            CacheModel("object_4612", titleObj.models[0], titleObj.texList[0], gvm, false);
            //Crow
            CacheModel("object_4612_1", titleObj.models[5], titleObj.texList[0], gvm, false);
        }

        public static void CacheEggContentData(GEEGG gegg, List<Texture2D> gplTextures, List<int> gplAlphaTypes)
        {
            //Transform to translate items above eggs
            var tfm = System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 20, 0));
            var tfmEgg = System.Numerics.Matrix4x4.CreateScale(1.5f, 1.5f, 1.5f) * System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 7.5f, 0));

            //Default Egg - Blue Speckled
            CacheEggModel(gegg, gplTextures, gplAlphaTypes, null, null, null, 0, tfm, tfmEgg);

            for(int i = 1; i < 66; i++)
            {
                CacheEggEntity(gegg, gplTextures, gplAlphaTypes, tfm, tfmEgg, ObjectVariants.eggFileNames[i], i);
            }
        }

        private static void CacheEggEntity(GEEGG gegg, List<Texture2D> gplTextures, List<int> gplAlphaTypes, System.Numerics.Matrix4x4 tfm, System.Numerics.Matrix4x4 tfmEgg, string file, int id)
        {
            var path = OverEasyGlobals.GetAssetPath(file);
            if (path != "" && File.Exists(path))
            {
                if(file == "obj_ms_bomb.arc")
                {
                    var item = new ObjMsBomb(File.ReadAllBytes(path));
                    CacheEggModel(gegg, gplTextures, gplAlphaTypes, item.model, item.texLists[0], item.gvm, id, tfm, tfmEgg);
                } else if (file.StartsWith("ani_model"))
                {
                    var item = new AniModel(File.ReadAllBytes(path));
                    CacheEggModel(gegg, gplTextures, gplAlphaTypes, item.models[0], item.texList, item.gvm, id, tfm, tfmEgg);
                }
                else
                {
                    var item = new ItemLibModel(File.ReadAllBytes(path));
                    CacheEggModel(gegg, gplTextures, gplAlphaTypes, item.model, item.texList, item.gvm, id, tfm, tfmEgg);
                }
            }
        }

        public static void CacheEggModel(GEEGG gegg, List<Texture2D> gplTextures, List<int> gplAlphaTypes, NJSObject itemNj, NJTextureList texList, PuyoFile gvm, int eggId, System.Numerics.Matrix4x4 tfm, System.Numerics.Matrix4x4 tfmEgg)
        {
            Node3D itemModel = null;
            Node3D itemModel2 = null;
            if(itemNj != null)
            {
                ModelConversion.LoadGVM($"egg_{eggId}_item", gvm, out var gvmTextures, out var gvrAlphaTypes);
                var textureSubSet = ModelConversion.GetTextureSubset(gvmTextures, texList, gvrAlphaTypes, out var itemAlphaTypes);
                itemModel = ModelConversion.NinjaToGDModel($"egg_{eggId}", itemNj, textureSubSet, itemAlphaTypes, null, null, null, tfm, false, 0.5f);
                itemModel2 = ModelConversion.NinjaToGDModel($"segg_{eggId}", itemNj, textureSubSet, itemAlphaTypes, null, null, null, tfm, false, 0.5f);
            }
            itemModel = ModelConversion.NinjaToGDModel($"egg_{eggId}", gegg.models[0], new List<Texture2D>() { gplTextures[eggId] }, new List<int>() { gplAlphaTypes[eggId] }, null, null, itemModel, tfmEgg);
            ModelConversion.CreateObjectCollision(itemModel);
            itemModel2 = ModelConversion.NinjaToGDModel($"segg_{eggId}", gegg.models[0], new List<Texture2D>() { gplTextures[eggId] }, new List<int>() { gplAlphaTypes[eggId] }, null, null, itemModel2, tfmEgg, false, 0.5f);
            ModelConversion.CreateObjectCollision(itemModel2);
            OverEasyGlobals.modelDictionary[$"egg_{eggId}"] = itemModel;
            OverEasyGlobals.modelDictionary[$"segg_{eggId}"] = itemModel2;
        }

        public static void CacheObjectModelsPC(StageDef.StageDefinition def)
        {
            //Clear prior data
            OverEasyGlobals.cachedStageObjCommonNames.Clear();
            OverEasyGlobals.cachedStageObjLocalNames.Clear();

            //Load egg data
            var amemBootPath = OverEasyGlobals.GetAssetPath("amem_boot.nrc");
            var geEggPath = OverEasyGlobals.GetAssetPath("ge_egg.arc");
            if(amemBootPath != "" && geEggPath != "")
            {
                var nrc = new PRD(File.ReadAllBytes(amemBootPath), true);
                LoadGPLTextures(nrc, out var gplTextures, out var gplAlphaTypes);

                var geEgg = new GEEGG(File.ReadAllBytes(geEggPath));
                CacheEggContentData(geEgg, gplTextures, gplAlphaTypes);
            }

            //Load common geobj data
            var commonObjectsPath = OverEasyGlobals.GetAssetPath("geobj_common.arc");
            var commonObjectsDefPath = OverEasyGlobals.GetAssetPath("stgobj_common.arc");
            if(commonObjectsPath != "" && commonObjectsDefPath != "")
            {
                var stgobjCommon = new StageObj(File.ReadAllBytes(commonObjectsDefPath));
                for(int i = 0; i < stgobjCommon.objEntries.Count; i++)
                {
                    var obj = stgobjCommon.objEntries[i];
                    if(obj.model2Id0 != ushort.MaxValue)
                    {
                        OverEasyGlobals.cachedStageObjCommonNames.Add(i, obj.objName);
                    }
                }

                var commGeobj = new GEObj_Stage(File.ReadAllBytes(commonObjectsPath));
                CacheGeobjCommon(commGeobj);
            }

            //Load local world geobj data
            var objDataFile = def.commonData != null ? def.commonData.objectData : "";
            var objDefFile = def.commonData != null ? def.commonData.objectDefinition : "";
            var localObjectsPath = OverEasyGlobals.GetAssetPath(objDataFile);
            var localObjectsDefPath = OverEasyGlobals.GetAssetPath(objDefFile);
            if(localObjectsPath != "" && localObjectsDefPath != "")
            {
                var localStgobj = new StageObj(File.ReadAllBytes(localObjectsDefPath));
                for (int i = 0; i < localStgobj.objEntries.Count; i++)
                {
                    var obj = localStgobj.objEntries[i];
                    if (obj.model2Id0 != ushort.MaxValue)
                    {
                        OverEasyGlobals.cachedStageObjLocalNames.Add(i, obj.objName);
                    }
                }

                var localGeobj = new GEObj_Stage(File.ReadAllBytes(localObjectsPath));
                CacheGeobjLocal(localStgobj, localGeobj);
            }
        }

        public static void LoadGPLTextures(PRD nrc, out List<Texture2D> gplTextures, out List<int> gplAlphaTypes)
        {
            gplTextures = new List<Texture2D>();
            gplAlphaTypes = new List<int>();
            for (int i = 0; i < nrc.fileNames.Count; i++)
            {
                if (nrc.fileNames[i] == "egg.gpl")
                {
                    var gpl = new GPL(nrc.files[i]);
                    var gvrs = gpl.GetGVRs();
                    var gvrTextureList = new List<GvrTexture>();
                    List<string> names = new List<string>();
                    for (int t = 0; t < gvrs.Count; t++)
                    {
                        gvrTextureList.Add(new GvrTexture(gvrs[t]));
                        names.Add($"gpl_{t}");
                    }
                    ModelConversion.LoadGVRTextures("egg.gpl", gvrTextureList, names, out gplTextures, out gplAlphaTypes);
                }
            }
        }

        public static void CacheGeobjLocal(StageObj stgobj, GEObj_Stage stageGeo)
        {
            ModelConversion.LoadGVM("geobjStage", stageGeo.gvm, out var gvmTextures, out var gvrAlphaTypes);

            //Scenery
            for (int i = 0; i < stgobj.objEntries.Count; i++)
            {
                var objEntry = stgobj.objEntries[i];

                //Model2s all share the same texlist
                if(objEntry.model2Id0 == ushort.MaxValue)
                {
                    continue;
                }
                CacheModel($"commGeoM2Local_{i}", stageGeo.model2s[$"model2_{objEntry.model2Id0}"], stageGeo.texList2s["texList2_0"], stageGeo.gvm, false, true);
            }
        }

        public static void CacheGeobjCommon(GEObj_Stage commonGeo)
        {
            List<int> diffuseAsAlphaList = new List<int> { 15, 16, 27, 28 };
            ModelConversion.LoadGVM("geobjCommon", commonGeo.gvm, out var gvmTextures, out var gvrAlphaTypes, diffuseAsAlphaList);

            //Fruit balls
            var tfm = System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 5, 0));
            var appleBall = ModelConversion.NinjaToGDModel("fruitBall", commonGeo.models["model_24"], ModelConversion.GetTextureSubset(gvmTextures, commonGeo.texLists["texList_23"], gvrAlphaTypes, out var sphereAlphaTypes), sphereAlphaTypes, null, null, null, tfm);
            var bananaBall = ModelConversion.GDModelClone(appleBall);
            var cherryBall = ModelConversion.GDModelClone(appleBall);
            var melonBall = ModelConversion.GDModelClone(appleBall);
            var pineappleBall = ModelConversion.GDModelClone(appleBall);
            var strawberryBall = ModelConversion.GDModelClone(appleBall);
            var watermelonBall = ModelConversion.GDModelClone(appleBall);

            var texList24 = ModelConversion.GetTextureSubset(gvmTextures, commonGeo.texLists["texList_24"], gvrAlphaTypes, out var fruitAlphaTypes);
            var apple = ModelConversion.NinjaToGDModel("apple", commonGeo.models["model_25"], texList24, fruitAlphaTypes, null, null, appleBall, tfm);
            var banana = ModelConversion.NinjaToGDModel("banana", commonGeo.models["model_26"], texList24, fruitAlphaTypes, null, null, bananaBall, tfm);
            var cherry = ModelConversion.NinjaToGDModel("cherry", commonGeo.models["model_27"], texList24, fruitAlphaTypes, null, null, cherryBall, tfm);
            var melon = ModelConversion.NinjaToGDModel("melon", commonGeo.models["model_28"], texList24, fruitAlphaTypes, null, null, melonBall, tfm);
            var pineapple = ModelConversion.NinjaToGDModel("pineapple", commonGeo.models["model_29"], texList24, fruitAlphaTypes, null, null, pineappleBall, tfm);
            var strawberry = ModelConversion.NinjaToGDModel("strawberry", commonGeo.models["model_30"], texList24, fruitAlphaTypes, null, null, strawberryBall, tfm);
            var watermelon = ModelConversion.NinjaToGDModel("watermelon", commonGeo.models["model_31"], texList24, fruitAlphaTypes, null, null, watermelonBall, tfm);

            ModelConversion.CreateObjectCollision(apple);
            ModelConversion.CreateObjectCollision(banana);
            ModelConversion.CreateObjectCollision(cherry);
            ModelConversion.CreateObjectCollision(melon);
            ModelConversion.CreateObjectCollision(pineapple);
            ModelConversion.CreateObjectCollision(strawberry);
            ModelConversion.CreateObjectCollision(watermelon);

            OverEasyGlobals.modelDictionary["object_11_0"] = apple;
            OverEasyGlobals.modelDictionary["object_11_1"] = banana;
            OverEasyGlobals.modelDictionary["object_11_2"] = cherry;
            OverEasyGlobals.modelDictionary["object_11_3"] = melon;
            OverEasyGlobals.modelDictionary["object_11_4"] = pineapple;
            OverEasyGlobals.modelDictionary["object_11_5"] = strawberry;
            OverEasyGlobals.modelDictionary["object_11_6"] = watermelon;

            //Scenery
            for(int i = 0; i < commonGeo.model2s.Count; i++)
            {
                //Model2s all share the same texlist
                CacheModel($"commGeoM2Common_{i}", commonGeo.model2s[$"model2_{i}"], commonGeo.texList2s["texList2_0"], commonGeo.gvm, false, true);
            }
        }

        public static void CachePlayerModelsPC()
        {
            var billy = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player1.arc")));
            var rolly = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player2.arc")));
            var chick = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player3.arc")));
            var bantam = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player4.arc")));

            CachePlayerModel("player_1", billy, false);
            CachePlayerModel("player_2", rolly, false);
            CachePlayerModel("player_3", chick, false);
            CachePlayerModel("player_4", bantam, false);
        }
        public static Node3D CachePlayerModel(string name, GEPlayer player, bool forceAdd)
        {
            ModelConversion.LoadGVM(name, player.gvm, out var gvmTextures, out var gvrAlphaTypes);
            AquaNode playerAqn = new AquaNode();
            var modelNode = ModelConversion.NinjaToGDModel(name, player.models[0], gvmTextures, gvrAlphaTypes, playerAqn);
            var combNode = ModelConversion.NinjaToGDModel(name, player.models[1], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[55].GetInverseBindPoseMatrixInverted().Translation));
            var faceNode = ModelConversion.NinjaToGDModel(name, player.models[2], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[57].GetInverseBindPoseMatrixInverted().Translation));
            var leftHandNode = ModelConversion.NinjaToGDModel(name, player.models[3], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateRotationY(Mathf.Pi) * System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[47].GetInverseBindPoseMatrixInverted().Translation));
            var rightHandNode = ModelConversion.NinjaToGDModel(name, player.models[4], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[37].GetInverseBindPoseMatrixInverted().Translation));

            ModelConversion.CreateObjectCollision(modelNode);
            if (forceAdd || !OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                OverEasyGlobals.modelDictionary[name] = modelNode;
            }

            return modelNode;
        }

        public static Node3D CacheModel(string name, NJSObject nj, NJTextureList njtl, PuyoFile gvm, bool forceAdd, bool blockVertColors = false, float? forceOpacity = null)
        {
            ModelConversion.LoadGVM(name, gvm, out var gvmTextures, out var gvrAlphaTypes);
            var textureSubSet = ModelConversion.GetTextureSubset(gvmTextures, njtl, gvrAlphaTypes, out var fruitAlphaTypes);

            var modelNode = ModelConversion.NinjaToGDModel(name, nj, textureSubSet, fruitAlphaTypes, null, null, null, null, blockVertColors, forceOpacity);
            ModelConversion.CreateObjectCollision(modelNode);
            if (forceAdd || !OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                OverEasyGlobals.modelDictionary[name] = modelNode;
            }

            return modelNode;
        }
    }
}
