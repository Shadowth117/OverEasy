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

                    switch (set.Key)
                    {
                        case "ar_ene_am02.arc":
                            nj = arc.models[1];
                            njtl = arc.texList[0];
                            break;
                        default:
                            nj = arc.models[0];
                            njtl = arc.texList[0];
                            break;
                    }
                    var modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
                    CreateObjectCollision(modelNode);

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

            if(obj.objectId == 11)
            {
                name += $"_{obj.intProperty1}";
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
                        CreateObjectCollision(modelNode);
                        break;
                }

                if (!OverEasyGlobals.modelDictionary.ContainsKey(name))
                {
                    OverEasyGlobals.modelDictionary.Add(name, modelNode);
                }
            }

            return modelNode;
        }

        public static void CleanModelNode(Node3D modelNode)
        {
            foreach (var child in modelNode.GetChildren())
            {
                //Do not kill the camera
                if(child == OverEasyGlobals.TransformGizmo)
                {
                    continue;
                }
                //Set visibility so this appears more immediately seamless
                if (child is Node3D child3d)
                {
                    child3d.Visible = false;
                }
                child.QueueFree();
            }
        }

        public static void CreateObjectCollision(Node3D modelNode)
        {
            List<MeshInstance3D> meshInstances = new List<MeshInstance3D>();
            GetObjectMeshInstances(modelNode, meshInstances);

            foreach (var meshInst in meshInstances)
            {
                meshInst.CreateTrimeshCollision();
                var staticBody = (StaticBody3D)meshInst.GetChild(0);
                var collChild = ((CollisionShape3D)staticBody.GetChild(0));
                collChild.Disabled = false;
                staticBody.CollisionLayer = 1;
                staticBody.CollisionMask = 1;
            }
        }

        public static void GetObjectMeshInstances(Node modelNode, List<MeshInstance3D> meshInstances)
        {
            var children = modelNode.GetChildren();
            foreach (var nodeChild in children)
            {
                if (nodeChild is MeshInstance3D meshChild)
                {
                    meshInstances.Add(meshChild);
                }
                GetObjectMeshInstances(nodeChild, meshInstances);
            }
        }

        public static void CacheObjectModelsPC()
        {
            var commonObjectsPath = OverEasyGlobals.GetAssetPath("geobj_common.arc");
            var commGeobj = new GEObj_Stage(File.ReadAllBytes(commonObjectsPath));
            CacheGeobjCommon(commGeobj);
        }

        public static void CacheGeobjCommon(GEObj_Stage commonGeo)
        {
            //List<int> diffuseAsAlphaList = new List<int> {  };
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

            CreateObjectCollision(apple);
            CreateObjectCollision(banana);
            CreateObjectCollision(cherry);
            CreateObjectCollision(melon);
            CreateObjectCollision(pineapple);
            CreateObjectCollision(strawberry);
            CreateObjectCollision(watermelon);

            OverEasyGlobals.modelDictionary["object_11_0"] = apple;
            OverEasyGlobals.modelDictionary["object_11_1"] = banana;
            OverEasyGlobals.modelDictionary["object_11_2"] = cherry;
            OverEasyGlobals.modelDictionary["object_11_3"] = melon;
            OverEasyGlobals.modelDictionary["object_11_4"] = pineapple;
            OverEasyGlobals.modelDictionary["object_11_5"] = strawberry;
            OverEasyGlobals.modelDictionary["object_11_6"] = watermelon;
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

            BillyModelIO.CreateObjectCollision(modelNode);
            if (forceAdd || !OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                OverEasyGlobals.modelDictionary[name] = modelNode;
            }

            return modelNode;
        }

        public static Node3D CacheModel(string name, NJSObject nj, NJTextureList njtl, PuyoFile gvm, bool forceAdd)
        {
            ModelConversion.LoadGVM(name, gvm, out var gvmTextures, out var gvrAlphaTypes);
            var modelNode = ModelConversion.NinjaToGDModel(name, nj, gvmTextures, gvrAlphaTypes);
            BillyModelIO.CreateObjectCollision(modelNode);
            if (forceAdd || !OverEasyGlobals.modelDictionary.ContainsKey(name))
            {
                OverEasyGlobals.modelDictionary[name] = modelNode;
            }

            return modelNode;
        }
    }
}
