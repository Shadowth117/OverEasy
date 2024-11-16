using Godot;

namespace OverEasy
{
    partial class OverEasyGlobals
    {

        public static void CreateVector2Schema(string key, Vector2 value)
        {
            var newSchema = (VBoxContainer)Vector3SchemaTemplate.Duplicate();

            var paramX = (SpinBox)newSchema.GetChild(1).GetChild(0).GetChild(1);
            paramX.Value = value.X;
            var paramY = (SpinBox)newSchema.GetChild(1).GetChild(1).GetChild(1);
            paramY.Value = value.Y;
            newSchema.Visible = true;

            objectScrollContainer.GetChild(0).AddChild(newSchema);
            activeObjectEditorObjects[key] = newSchema;
        }

        public static void CreateVector3Schema(string key, Vector3 value)
        {
            var newSchema = (VBoxContainer)Vector3SchemaTemplate.Duplicate();

            var paramX = (SpinBox)newSchema.GetChild(1).GetChild(0).GetChild(1);
            paramX.Value = value.X;
            var paramY = (SpinBox)newSchema.GetChild(1).GetChild(1).GetChild(1);
            paramY.Value = value.Y;
            var paramZ = (SpinBox)newSchema.GetChild(1).GetChild(2).GetChild(1);
            paramZ.Value = value.Z;
            newSchema.Visible = true;

            objectScrollContainer.GetChild(0).AddChild(newSchema);
            activeObjectEditorObjects[key] = newSchema;
        }

        public static void CreateVector4Schema(string key, Vector4 value)
        {
            var newSchema = (VBoxContainer)Vector3SchemaTemplate.Duplicate();

            var paramX = (SpinBox)newSchema.GetChild(1).GetChild(0).GetChild(1);
            paramX.Value = value.X;
            var paramY = (SpinBox)newSchema.GetChild(1).GetChild(1).GetChild(1);
            paramY.Value = value.Y;
            var paramZ = (SpinBox)newSchema.GetChild(1).GetChild(2).GetChild(1);
            paramZ.Value = value.Z;
            var paramW = (SpinBox)newSchema.GetChild(1).GetChild(3).GetChild(1);
            paramW.Value = value.W;
            newSchema.Visible = true;

            objectScrollContainer.GetChild(0).AddChild(newSchema);
            activeObjectEditorObjects[key] = newSchema;
        }

        public static void CreateFloatSchema(string key, double value)
        {
            CreateFloatingPointSchema(key, value);
        }

        public static void CreateDoubleSchema(string key, double value)
        {
            CreateFloatingPointSchema(key, value);
        }

        public static void CreateFloatingPointSchema(string key, double value)
        {
            var newSchema = (VBoxContainer)FloatSchemaTemplate.Duplicate();

            var paramValue = (SpinBox)newSchema.GetChild(1);
            paramValue.Value = value;
            newSchema.Visible = true;

            objectScrollContainer.GetChild(0).AddChild(newSchema);
            activeObjectEditorObjects[key] = newSchema;
        }

        public static void CreateByteSchema(string key, long value)
        {
            CreateIntegerSchema(key, value, byte.MinValue, byte.MaxValue);
        }

        public static void CreateShortSchema(string key, long value)
        {
            CreateIntegerSchema(key, value, short.MinValue, short.MaxValue);
        }

        public static void CreateUShortSchema(string key, long value)
        {
            CreateIntegerSchema(key, value, ushort.MinValue, ushort.MaxValue);
        }

        public static void CreateIntSchema(string key, long value)
        {
            CreateIntegerSchema(key, value, int.MinValue, int.MaxValue);
        }

        public static void CreateUIntSchema(string key, long value)
        {
            CreateIntegerSchema(key, value, uint.MinValue, uint.MaxValue);
        }

        public static void CreateLongSchema(string key, long value)
        {
            CreateIntegerSchema(key, value, long.MinValue, long.MaxValue);
        }

        public static void CreateIntegerSchema(string key, long value, long min, long max)
        {
            var newSchema = (VBoxContainer)IntSchemaTemplate.Duplicate();
            newSchema.SetMeta("Min", min);
            newSchema.SetMeta("Max", max);

            var paramValue = (SpinBox)newSchema.GetChild(1);
            paramValue.Value = value;
            newSchema.Visible = true;

            objectScrollContainer.GetChild(0).AddChild(newSchema);
            activeObjectEditorObjects[key] = newSchema;
        }

        public static double GetSpinBoxValue(string key)
        {
            var vecObj = activeObjectEditorObjects[key];
            var spinBox = (SpinBox)vecObj.GetChild(1);

            return spinBox.Value;
        }

        public static void SetSpinBoxValue(string key, double newValue)
        {
            var vecObj = activeObjectEditorObjects[key];
            var spinBox = (SpinBox)vecObj.GetChild(1);

            spinBox.Value = newValue;
        }

        public static System.Numerics.Vector2 GetVec2SchemaValues(string key)
        {
            var vecObj = activeObjectEditorObjects[key];
            var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
            var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);

            return new System.Numerics.Vector2((float)boxX.Value, (float)boxY.Value);
        }

        public static System.Numerics.Vector3 GetVec3SchemaValues(string key)
        {
            var vecObj = activeObjectEditorObjects[key];
            var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
            var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);
            var boxZ = (SpinBox)vecObj.GetChild(1).GetChild(2).GetChild(1);

            return new System.Numerics.Vector3((float)boxX.Value, (float)boxY.Value, (float)boxZ.Value);
        }

        public static System.Numerics.Vector4 GetVec4SchemaValues(string key)
        {
            var vecObj = activeObjectEditorObjects[key];
            var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
            var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);
            var boxZ = (SpinBox)vecObj.GetChild(1).GetChild(2).GetChild(1);
            var boxW = (SpinBox)vecObj.GetChild(1).GetChild(3).GetChild(1);

            return new System.Numerics.Vector4((float)boxX.Value, (float)boxY.Value, (float)boxZ.Value, (float)boxW.Value);
        }

        public static void SetVec2SchemaValues(string key, Godot.Vector2 vec2)
        {
            var vecObj = activeObjectEditorObjects[key];
            var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
            var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);

            boxX.Value = vec2.X;
            boxY.Value = vec2.Y;
        }

        public static void SetVec3SchemaValues(string key, Godot.Vector3 vec3)
        {
            var vecObj = activeObjectEditorObjects[key];
            var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
            var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);
            var boxZ = (SpinBox)vecObj.GetChild(1).GetChild(2).GetChild(1);

            boxX.Value = vec3.X;
            boxY.Value = vec3.Y;
            boxZ.Value = vec3.Z;
        }

        public static void SetVec4SchemaValues(string key, Godot.Vector4 vec4)
        {
            var vecObj = activeObjectEditorObjects[key];
            var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
            var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);
            var boxZ = (SpinBox)vecObj.GetChild(1).GetChild(2).GetChild(1);
            var boxW = (SpinBox)vecObj.GetChild(1).GetChild(3).GetChild(1);

            boxX.Value = vec4.X;
            boxY.Value = vec4.Y;
            boxZ.Value = vec4.Z;
            boxW.Value = vec4.W;
        }

        public static void LoadVec2SchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName)
        {
            LoadVecSchemaTemplateInfo(key, objName, objNameDefault, objHint, xName, yName, "", "");
        }

        public static void LoadVec3SchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName, string zName)
        {
            LoadVecSchemaTemplateInfo(key, objName, objNameDefault, objHint, xName, yName, zName, "");
        }

        public static void LoadVec4SchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName, string zName, string wName)
        {
            LoadVecSchemaTemplateInfo(key, objName, objNameDefault, objHint, xName, yName, zName, wName);
        }

        public static void LoadSchemaTemplateInfo(string key, string name, string nameDefault, string hint)
        {
            if (!activeObjectEditorObjects.ContainsKey(key))
            {
                return;
            }
            var schObj = activeObjectEditorObjects[key];
            schObj.TooltipText = hint;

            var objText = (RichTextLabel)schObj.GetChild(0);
            var objTextText = name != "" && name != null ? name : nameDefault;
            objText.Text = objTextText;
        }

        private static void LoadVecSchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName, string zName, string wName)
        {
            if (!activeObjectEditorObjects.ContainsKey(key))
            {
                return;
            }
            var vecObj = activeObjectEditorObjects[key];
            vecObj.TooltipText = objHint;

            var objText = (RichTextLabel)vecObj.GetChild(0);
            var objTextText = objName != "" && objName != null ? objName : objNameDefault;
            var vecSize = vecObj.GetChild(1).GetChildCount();

            switch (vecSize)
            {
                case 0:
                    return;
                case 1:
                    return;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                default:
                    vecSize = 4;
                    break;
            }

            objText.Text = $"{objTextText}";

            if (vecSize >= 1)
            {
                var vecObjXText = (RichTextLabel)vecObj.GetChild(1).GetChild(0).GetChild(0);
                var vecObjXName = xName != "" && xName != null ? xName : "X";
                vecObjXText.Text = $"[color=fab293]{vecObjXName}: [/color]";
            }

            if (vecSize >= 2)
            {
                var vecObjYText = (RichTextLabel)vecObj.GetChild(1).GetChild(1).GetChild(0);
                var vecObjYName = yName != "" && yName != null ? yName : "Y";
                vecObjYText.Text = $"[color=93fab2]{vecObjYName}: [/color]";
            }

            if (vecSize >= 3)
            {
                var vecObjZText = (RichTextLabel)vecObj.GetChild(1).GetChild(2).GetChild(0);
                var vecObjZName = zName != "" && zName != null ? zName : "Z";
                vecObjZText.Text = $"[color=b293fa]{vecObjZName}: [/color]";
            }

            if (vecSize >= 4)
            {
                var vecObjWText = (RichTextLabel)vecObj.GetChild(1).GetChild(3).GetChild(0);
                var vecObjWName = wName != "" && wName != null ? wName : "W";
                vecObjWText.Text = $"[color=b200b2]{vecObjWName}: [/color]";
            }
        }
    }
}
