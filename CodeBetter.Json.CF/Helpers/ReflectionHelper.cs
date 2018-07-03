namespace CodeBetter.Json.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal static class ReflectionHelper
    {
        private readonly static Type _includeBaseAttributeType = typeof (SerializeIncludingBaseAttribute);
        private static readonly Type _nonSerializableAttributeType = typeof(NonSerializedAttribute);
        private static readonly Type _nullableType = typeof (Nullable<>);
        private static readonly IDictionary<Type, IList<FieldInfo>> _typeFields = new Dictionary<Type, IList<FieldInfo>>();

        public static IList<FieldInfo> GetSerializableFields(Type type)
        {
            if (_typeFields.ContainsKey(type))
            {
                return _typeFields[type];
            }
            
            var fields = new List<FieldInfo>(10);
            fields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            RemoveNonSerializableFields(fields);
            if (type.BaseType != null && type.GetCustomAttributes(_includeBaseAttributeType, false).Length > 0)
            {
                fields.AddRange(GetSerializableFields(type.BaseType));
            }
            _typeFields[type] = fields;
            return fields;
        }

        private static void RemoveNonSerializableFields(IList<FieldInfo> fields)
        {
            for(int i = 0; i < fields.Count; ++i)
            {
                if (!ShouldSerializeField(fields[i]))
                {
                    fields.RemoveAt(i--);
                }
            }
        }

        public static bool ShouldSerializeField(FieldInfo field)
        {
            if (field.GetCustomAttributes(_nonSerializableAttributeType, true).Length > 0)
            {
                return false;
            }
            return (field.Attributes & FieldAttributes.NotSerialized) != FieldAttributes.NotSerialized;
        }

        public static FieldInfo FindField(Type type, string name)
        {
            var field = FindFieldThroughoutHierarchy(type, name);
            if (field == null)
            {
                throw new ArgumentException(type.FullName + " doesn't have a field named: " + name);
            }
            return field;
        }
        public static FieldInfo FindFieldThroughoutHierarchy(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null && type.GetCustomAttributes(_includeBaseAttributeType, false).Length > 0)
            {
                field = FindFieldThroughoutHierarchy(type.BaseType, name);
            }
            return field;
        }

        public static object GetValue(FieldInfo field, object @object)
        {
            var value = field.GetValue(@object);
            var isEnum = field.FieldType.IsEnum;
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == _nullableType && value != null)
            {
                isEnum = Nullable.GetUnderlyingType(field.FieldType).IsEnum;               
            }
            return isEnum && value != null ? int.Parse(((Enum)value).ToString("d")) : value;
        }

        public static ConstructorInfo GetDefaultConstructor(Type type)
        {
            ConstructorInfo constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
            if (constructor == null)
            {
                throw new JsonException(type.FullName + " must have a parameterless constructor");
            }
            return constructor;
        }
    }
}
