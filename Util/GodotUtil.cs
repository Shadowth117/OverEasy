using Godot;

namespace OverEasy.Util
{
    public static class GodotUtil
    {
        public static System.Numerics.Vector2 ToSNVec2(this Godot.Vector2 vec3)
        {
            return new System.Numerics.Vector2(vec3.X, vec3.Y);
        }
        public static Godot.Vector2 ToGVec2(this System.Numerics.Vector2 vec3)
        {
            return new Godot.Vector2(vec3.X, vec3.Y);
        }
        public static System.Numerics.Vector3 ToSNVec3(this Godot.Vector3 vec3)
        {
            return new System.Numerics.Vector3(vec3.X, vec3.Y, vec3.Z);
        }
        public static Godot.Vector3 ToGVec3(this System.Numerics.Vector3 vec3)
        {
            return new Godot.Vector3(vec3.X, vec3.Y, vec3.Z);
        }
        public static System.Numerics.Quaternion ToSNQuat(this Godot.Quaternion quat)
        {
            return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }
        public static Godot.Quaternion ToGQuat(this System.Numerics.Quaternion quat)
        {
            return new Godot.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static Godot.Aabb GetHierarchyAABB(Node3D node)
        {
            return GetHierarchyAABB(node, new Aabb());
        }

        private static Godot.Aabb GetHierarchyAABB(Node3D node, Godot.Aabb aabb)
        {
            foreach(var nodeChild in node.GetChildren())
            {
                aabb = aabb.Merge(GetHierarchyAABB((Node3D)nodeChild, aabb));
            }

            if(node is MeshInstance3D)
            {
                return aabb.Merge(((MeshInstance3D)node).GetAabb());
            }

            return aabb;
        }
    }
}
