
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using pigbrain.core.Utility;

namespace pigbrain.core.Collections
{
    public class ObjectReflector
    {
        readonly object target;
        Dictionary<string, FieldInfo> fields;
        public ObjectReflector(object target)
        {
            this.target = target;
            fields = target.GetType()
                .GetFields(ReflectionUtility.DefaultBindings)
                .ToDictionary(f => f.Name, f => f);
        }

        public object this[string name]
        {
            get => fields.TryGetValue(name, out FieldInfo field) ? field.GetValue(target) : default;
            set
            {
                try
                {
                    if (fields.TryGetValue(name, out FieldInfo field))
                    {
                        if (value != null && !field.FieldType.IsAssignableFrom(value.GetType()))
                        {
                            if (field.FieldType.IsEnum) value = Enum.Parse(field.FieldType, value.ToString(), true);
                            else value = Convert.ChangeType(value, field.FieldType, CultureInfo.InvariantCulture);
                        }
                        field.SetValue(target, value);
                    }
                }
                catch (Exception)
                {
                    // UnityEngine.Debug.LogError(x);
                }
            }
        }

        public void SetValue(string name, object value)
        {
            var field = fields[name];
            var type = field.FieldType;

            if (value != null && !type.IsAssignableFrom(value.GetType()))
            {
                if (type.IsEnum) value = Enum.Parse(type, value.ToString(), true);
                else value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            field.SetValue(target, value);
        }

        // static T ParseValue<T>(string value) where T : struct
        // {
        //     var type = typeof(T);
        //     if (type == typeof(int))
        //         return (T)(object)(int.TryParse(value, out int v) ? v : default);
        //     if (type == typeof(float))
        //         return (T)(object)(float.TryParse(value, out float v) ? v : default);
        //     if (type.IsEnum)
        //         return Enum.TryParse(value, true, out T v) ? v : default;
        //     throw new NotSupportedException($"Unsupported field type: {type.Name}");
        // }
    }
}