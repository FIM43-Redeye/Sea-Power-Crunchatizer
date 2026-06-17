// Utility for cloning game parameter objects
// Used by the side-swap system to cache original state

using System;
using System.Reflection;

namespace SeaPowerCrunchatizer.Utilities
{
    /// <summary>
    /// Provides shallow cloning for game parameter objects.
    /// Copies all fields from source to a new instance of the same type.
    /// </summary>
    public static class ParameterCloner
    {
        /// <summary>
        /// Creates a shallow clone of an object by copying all fields.
        /// Works with any class that has a parameterless constructor.
        /// </summary>
        public static T? ShallowClone<T>(T? source) where T : class
        {
            if (source == null)
            {
                return null;
            }

            var type = source.GetType();
            var clone = Activator.CreateInstance(type);
            if (clone == null)
            {
                return null;
            }

            CopyFields(source, clone, type);
            return (T)clone;
        }

        /// <summary>
        /// Restores all fields from a cached clone back to the target object.
        /// </summary>
        public static void RestoreFrom<T>(T target, T? cached) where T : class
        {
            if (target == null || cached == null)
            {
                return;
            }

            var type = target.GetType();
            CopyFields(cached, target, type);
        }

        private static void CopyFields(object source, object target, Type type)
        {
            // Copy all fields including inherited ones
            while (type != null && type != typeof(object))
            {
                var fields = type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    // Skip readonly fields - they can't be restored anyway
                    if (field.IsInitOnly)
                    {
                        continue;
                    }

                    try
                    {
                        var value = field.GetValue(source);
                        field.SetValue(target, value);
                    }
                    catch
                    {
                        // Skip fields that fail to copy (rare edge cases)
                    }
                }

                type = type.BaseType!;
            }
        }
    }
}
