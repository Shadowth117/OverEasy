using Godot;
using System.Collections.Generic;
using Vector3 = System.Numerics.Vector3;

namespace OverEasy.Editor
{
    /// <summary>
    /// Adapted from https://github.com/soulsmods/DSMapStudio/blob/master/src/StudioCore/MsbEditor/Gizmos.cs
    /// </summary>
    public partial class Gizmo : Node3D
    {
        private TransformType transformType;

        public List<MeshInstance3D> XMeshInstList = new List<MeshInstance3D>();
        public List<MeshInstance3D> YMeshInstList = new List<MeshInstance3D>();
        public List<MeshInstance3D> ZMeshInstList = new List<MeshInstance3D>();
        public List<MeshInstance3D> XYMeshInstList = new List<MeshInstance3D>();
        public List<MeshInstance3D> XZMeshInstList = new List<MeshInstance3D>();
        public List<MeshInstance3D> YZMeshInstList = new List<MeshInstance3D>();

        public Color XActiveColor = Color.Color8(250, 127, 118);
        public Color YActiveColor = Color.Color8(0, 245, 99);
        public Color ZActiveColor = Color.Color8(90, 160, 255);
        public Color XYActiveColor = Color.Color8(90, 160, 255);
        public Color XZActiveColor = Color.Color8(0, 245, 99);
        public Color YZActiveColor = Color.Color8(250, 127, 118);
        public Color XInactiveColor = Color.Color8(229, 59, 59);
        public Color YInactiveColor = Color.Color8(0, 178, 70);
        public Color ZInactiveColor = Color.Color8(0, 122, 246);
        public Color XYInactiveColor = Color.Color8(0, 122, 246);
        public Color XZInactiveColor = Color.Color8(0, 178, 70);
        public Color YZInactiveColor = Color.Color8(229, 59, 59);

        public SelectionRegion currentHover = SelectionRegion.None;
        public enum SelectionRegion
        {
            None = 0,
            PositionX = 1,
            PositionY = 2,
            PositionZ = 3,
            RotationX = 4,
            RotationY = 5,
            RotationZ = 6,
            ScaleX = 7,
            ScaleY = 8,
            ScaleZ = 9,
            PositionXY = 10,
            PositionXZ = 11,
            PositionYZ = 12,
            ScaleXY = 13,
            ScaleXZ = 14,
            ScaleYZ = 15,
        }

        public enum Axis
        {
            None,
            PosX,
            PosY,
            PosZ,
            PosXY,
            PosYZ,
            PosXZ
        }

        public enum TransformType
        {
            None = -1,
            Basic = 0,
            Translation = 1,
            Rotation = 2,
            Scale = 3,
        }

        public override void _Ready()
        {
            OverEasyGlobals.TransformGizmo = this;
            SetCurrentTransformType(TransformType.None);

            XMeshInstList.Add((MeshInstance3D)GetNode("Translation/ArrowTipX"));
            XMeshInstList.Add((MeshInstance3D)GetNode("Translation/ArrowX"));
            XMeshInstList.Add((MeshInstance3D)GetNode("Rotation/RingXCollisionBody/RingX"));
            XMeshInstList.Add((MeshInstance3D)GetNode("Scale/ArrowTipX"));
            XMeshInstList.Add((MeshInstance3D)GetNode("Scale/ArrowX"));

            YMeshInstList.Add((MeshInstance3D)GetNode("Translation/ArrowTipY"));
            YMeshInstList.Add((MeshInstance3D)GetNode("Translation/ArrowY"));
            YMeshInstList.Add((MeshInstance3D)GetNode("Rotation/RingYCollisionBody/RingY"));
            YMeshInstList.Add((MeshInstance3D)GetNode("Scale/ArrowTipY"));
            YMeshInstList.Add((MeshInstance3D)GetNode("Scale/ArrowY"));

            ZMeshInstList.Add((MeshInstance3D)GetNode("Translation/ArrowTipZ"));
            ZMeshInstList.Add((MeshInstance3D)GetNode("Translation/ArrowZ"));
            ZMeshInstList.Add((MeshInstance3D)GetNode("Rotation/RingZCollisionBody/RingZ"));
            ZMeshInstList.Add((MeshInstance3D)GetNode("Scale/ArrowTipZ"));
            ZMeshInstList.Add((MeshInstance3D)GetNode("Scale/ArrowZ"));

            XYMeshInstList.Add((MeshInstance3D)GetNode("Translation/XYPlane"));
            XYMeshInstList.Add((MeshInstance3D)GetNode("Scale/XYPlane"));

            XZMeshInstList.Add((MeshInstance3D)GetNode("Translation/XZPlane"));
            XZMeshInstList.Add((MeshInstance3D)GetNode("Scale/XZPlane"));

            YZMeshInstList.Add((MeshInstance3D)GetNode("Translation/YZPlane"));
            YZMeshInstList.Add((MeshInstance3D)GetNode("Scale/YZPlane"));
        }

        public void SetCurrentTransformType(TransformType tfmType)
        {
            transformType = tfmType;
            var baseNode = (Node3D)GetNode("Basic");
            var transNode = (Node3D)GetNode("Translation");
            var rotNode = (Node3D)GetNode("Rotation");
            var scaleNode = (Node3D)GetNode("Scale");

            switch (tfmType)
            {
                case TransformType.None:
                    baseNode.Visible = false;
                    transNode.Visible = false;
                    rotNode.Visible = false;
                    scaleNode.Visible = false;
                    SetCollideableState(transNode, false);
                    SetCollideableState(rotNode, false);
                    SetCollideableState(scaleNode, false);
                    break;
                case TransformType.Basic:
                    baseNode.Visible = true;
                    transNode.Visible = false;
                    rotNode.Visible = false;
                    scaleNode.Visible = false;
                    SetCollideableState(transNode, false);
                    SetCollideableState(rotNode, false);
                    SetCollideableState(scaleNode, false);
                    break;
                case TransformType.Translation:
                    baseNode.Visible = false;
                    transNode.Visible = true;
                    rotNode.Visible = false;
                    scaleNode.Visible = false;
                    SetCollideableState(transNode, true);
                    SetCollideableState(rotNode, false);
                    SetCollideableState(scaleNode, false);
                    break;
                case TransformType.Rotation:
                    baseNode.Visible = false;
                    transNode.Visible = false;
                    rotNode.Visible = true;
                    scaleNode.Visible = false;
                    SetCollideableState(transNode, false);
                    SetCollideableState(rotNode, true);
                    SetCollideableState(scaleNode, false);
                    break;
                case TransformType.Scale:
                    baseNode.Visible = false;
                    transNode.Visible = false;
                    rotNode.Visible = false;
                    scaleNode.Visible = true;
                    SetCollideableState(transNode, false);
                    SetCollideableState(rotNode, false);
                    SetCollideableState(scaleNode, true);
                    break;
            }
        }

        public void SetCollideableState(Node3D node, bool enable)
        {
            switch (@node)
            {
                case CollisionPolygon3D cs3d:
                    cs3d.Disabled = !enable;
                    break;
                case CollisionShape3D cs3d:
                    cs3d.Disabled = !enable;
                    break;
                case CsgPrimitive3D cs3d:
                    cs3d.UseCollision = enable;
                    break;
            }
            foreach (var childNode in node.GetChildren())
            {
                SetCollideableState((Node3D)childNode, enable);
            }
        }

        /// <summary>
        /// Handles the visual for when a gizmo area is hovered
        /// </summary>
        public void SetHover(StaticBody3D collision = null)
        {
            SelectionRegion newHover = SelectionRegion.None;
            if (collision != null)
            {
                newHover = (SelectionRegion)collision.GetMeta("Region").AsInt32();
            }

            //If the selection is different than the previous selection, set previous selection back to its default color and change the new one to the highlight color
            if (newHover != currentHover)
            {
                SetHoverColor(currentHover, false);
                SetHoverColor(newHover, true);
                currentHover = newHover;
            }
        }

        public void SetHoverColor(SelectionRegion region, bool hovering)
        {
            switch (region)
            {
                case SelectionRegion.None:
                    break;
                case SelectionRegion.PositionX:
                case SelectionRegion.RotationX:
                case SelectionRegion.ScaleX:
                    foreach (var meshInst in XMeshInstList)
                    {
                        var color = hovering ? XActiveColor : XInactiveColor;
                        ((StandardMaterial3D)meshInst.GetSurfaceOverrideMaterial(0)).AlbedoColor = color;
                    }
                    break;
                case SelectionRegion.PositionY:
                case SelectionRegion.RotationY:
                case SelectionRegion.ScaleY:
                    foreach (var meshInst in YMeshInstList)
                    {
                        var color = hovering ? YActiveColor : YInactiveColor;
                        ((StandardMaterial3D)meshInst.GetSurfaceOverrideMaterial(0)).AlbedoColor = color;
                    }
                    break;
                case SelectionRegion.PositionZ:
                case SelectionRegion.RotationZ:
                case SelectionRegion.ScaleZ:
                    foreach (var meshInst in ZMeshInstList)
                    {
                        var color = hovering ? ZActiveColor : ZInactiveColor;
                        ((StandardMaterial3D)meshInst.GetSurfaceOverrideMaterial(0)).AlbedoColor = color;
                    }
                    break;
                case SelectionRegion.PositionXY:
                case SelectionRegion.ScaleXY:
                    foreach (var meshInst in XYMeshInstList)
                    {
                        var color = hovering ? XYActiveColor : XYInactiveColor;
                        ((StandardMaterial3D)meshInst.GetSurfaceOverrideMaterial(0)).AlbedoColor = color;
                    }
                    break;
                case SelectionRegion.PositionXZ:
                case SelectionRegion.ScaleXZ:
                    foreach (var meshInst in XZMeshInstList)
                    {
                        var color = hovering ? XZActiveColor : XZInactiveColor;
                        ((StandardMaterial3D)meshInst.GetSurfaceOverrideMaterial(0)).AlbedoColor = color;
                    }
                    break;
                case SelectionRegion.PositionYZ:
                case SelectionRegion.ScaleYZ:
                    foreach (var meshInst in YZMeshInstList)
                    {
                        var color = hovering ? YZActiveColor : YZInactiveColor;
                        ((StandardMaterial3D)meshInst.GetSurfaceOverrideMaterial(0)).AlbedoColor = color;
                    }
                    break;
            }
        }

        // Code referenced from
        // https://github.com/nem0/LumixEngine/blob/master/src/editor/gizmo.cpp
        // https://github.com/soulsmods/DSMapStudio/blob/master/src/StudioCore/MsbEditor/Gizmos.cs
        public static Vector3 GetSingleAxisProjection(Vector3 rayOrigin, Vector3 rayDirection, Vector3 tPos, System.Numerics.Quaternion tQuat, SelectionRegion axis)
        {
            Vector3 axisvec = Vector3.Zero;
            if (OverEasyGlobals.TransformGizmoWorld == true)
            {
                tQuat = new System.Numerics.Quaternion();
            }
            switch (axis)
            {
                case SelectionRegion.PositionX:
                case SelectionRegion.ScaleX:
                    axisvec = Vector3.Transform(new Vector3(1.0f, 0.0f, 0.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
                case SelectionRegion.PositionY:
                case SelectionRegion.ScaleY:
                    axisvec = Vector3.Transform(new Vector3(0.0f, 1.0f, 0.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
                case SelectionRegion.PositionZ:
                case SelectionRegion.ScaleZ:
                    axisvec = Vector3.Transform(new Vector3(0.0f, 0.0f, 1.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
            }

            Vector3 pos = tPos;
            Vector3 normal = Vector3.Cross(Vector3.Cross(rayDirection, axisvec), rayDirection);
            var d = Vector3.Dot(rayOrigin - pos, normal) / Vector3.Dot(axisvec, normal);
            return (axisvec * d);
        }

        public static Vector3 GetDoubleAxisProjection(Vector3 rayOrigin, Vector3 rayDirection, Node3D t, SelectionRegion axis)
        {
            Vector3 planeNormal = Vector3.Zero;
            var tPos = t.Position;
            var tQuat = t.Quaternion;
            switch (axis)
            {
                case SelectionRegion.PositionXY:
                case SelectionRegion.ScaleXY:
                    planeNormal = Vector3.Transform(new Vector3(0.0f, 0.0f, 1.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
                case SelectionRegion.PositionYZ:
                case SelectionRegion.ScaleYZ:
                    planeNormal = Vector3.Transform(new Vector3(1.0f, 0.0f, 0.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
                case SelectionRegion.PositionXZ:
                case SelectionRegion.ScaleXZ:
                    planeNormal = Vector3.Transform(new Vector3(0.0f, 1.0f, 0.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
            }

            float dist;
            Vector3 pos = new Vector3(tPos.X, tPos.Y, tPos.Z);
            Vector3 relorigin = rayOrigin - pos;
            if (RayPlaneIntersection(relorigin, rayDirection, Vector3.Zero, planeNormal, out dist))
            {
                return rayOrigin + (rayDirection * dist);
            }

            return rayOrigin;
        }

        public static Vector3 GetAxisPlaneProjection(Vector3 rayOrigin, Vector3 rayDirection, Node3D t, SelectionRegion axis)
        {
            Vector3 planeNormal = Vector3.Zero;
            var tPos = t.Position;
            var tQuat = t.Quaternion;
            switch (axis)
            {
                case SelectionRegion.RotationX:
                    planeNormal = Vector3.Transform(new Vector3(1.0f, 0.0f, 0.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
                case SelectionRegion.RotationY:
                    planeNormal = Vector3.Transform(new Vector3(0.0f, 1.0f, 0.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
                case SelectionRegion.RotationZ:
                    planeNormal = Vector3.Transform(new Vector3(0.0f, 0.0f, 1.0f), new System.Numerics.Quaternion(tQuat.X, tQuat.Y, tQuat.Z, tQuat.W));
                    break;
            }

            float dist;
            Vector3 pos = new Vector3(tPos.X, tPos.Y, tPos.Z);
            Vector3 relorigin = rayOrigin - pos;
            if (RayPlaneIntersection(relorigin, rayDirection, Vector3.Zero, planeNormal, out dist))
            {
                return rayOrigin + (rayDirection * dist);
            }

            return rayOrigin;
        }

        public static bool RayPlaneIntersection(Vector3 origin,
            Vector3 direction,
            Vector3 planePoint,
            Vector3 normal,
            out float dist)
        {
            var d = Vector3.Dot(direction, normal);
            if (d == 0)
            {
                dist = float.PositiveInfinity;
                return false;
            }

            dist = Vector3.Dot(planePoint - origin, normal) / d;
            return true;
        }
    }
}
