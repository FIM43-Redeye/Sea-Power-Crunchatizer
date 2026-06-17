// Centralized, fail-loud wrappers around Harmony's AccessTools reflection lookups.
//
// The mod reaches into the game's private fields, methods, and properties by name.
// When the game updates and a member is renamed or removed, a raw AccessTools call
// silently returns null - and the failure only surfaces later as an opaque
// NullReferenceException deep inside patch logic (or as a patch that quietly does
// nothing). These wrappers instead log a clear, named error at the point of lookup
// ("the game may have updated"), so a broken patch announces itself at load time
// and points straight at the member that moved.
//
// Each method returns null on failure (after logging) so callers can decide whether
// to skip a patch gracefully - mirroring the pattern already used in
// MultiAutoAttackPatch, now shared across all patch files.

using System;
using System.Reflection;
using HarmonyLib;
using Sea_Power_Crunchatizer;

namespace SeaPowerCrunchatizer.Utilities
{
    /// <summary>
    /// Fail-loud wrappers over <see cref="AccessTools"/> lookups. On a miss, each
    /// method logs a descriptive error naming the type and member, then returns null.
    /// </summary>
    public static class Reflect
    {
        /// <summary>Resolves a (possibly private) field, logging if it is missing.</summary>
        public static FieldInfo? Field(Type type, string name)
        {
            var info = AccessTools.Field(type, name);
            if (info == null)
            {
                LogMissing(type, $"field '{name}'");
            }
            return info;
        }

        /// <summary>
        /// Resolves a method, optionally disambiguating by parameter types, logging if missing.
        /// </summary>
        public static MethodInfo? Method(Type type, string name, Type[]? parameters = null)
        {
            var info = AccessTools.Method(type, name, parameters);
            if (info == null)
            {
                var signature = parameters == null ? $"method '{name}'" : $"method '{name}' with the expected signature";
                LogMissing(type, signature);
            }
            return info;
        }

        /// <summary>Resolves a property's getter, logging if the property or getter is missing.</summary>
        public static MethodInfo? PropertyGetter(Type type, string name)
        {
            var info = AccessTools.PropertyGetter(type, name);
            if (info == null)
            {
                LogMissing(type, $"property getter '{name}'");
            }
            return info;
        }

        /// <summary>Resolves a property's setter, logging if the property or setter is missing.</summary>
        public static MethodInfo? PropertySetter(Type type, string name)
        {
            var info = AccessTools.PropertySetter(type, name);
            if (info == null)
            {
                LogMissing(type, $"property setter '{name}'");
            }
            return info;
        }

        /// <summary>Resolves a constructor by parameter types, logging if missing.</summary>
        public static ConstructorInfo? Constructor(Type type, Type[]? parameters = null)
        {
            var info = AccessTools.Constructor(type, parameters);
            if (info == null)
            {
                LogMissing(type, "constructor with the expected signature");
            }
            return info;
        }

        /// <summary>Resolves a type by its (namespace-qualified) name, logging if missing.</summary>
        public static Type? TypeByName(string name)
        {
            var type = AccessTools.TypeByName(name);
            if (type == null)
            {
                CrunchatizerCore.Log.LogError(
                    $"[Reflect] Type '{name}' not found - the game may have updated. The dependent patch will be skipped.");
            }
            return type;
        }

        private static void LogMissing(Type type, string member)
        {
            CrunchatizerCore.Log.LogError(
                $"[Reflect] {type.FullName}.{member} not found - the game may have updated. The dependent patch may not work.");
        }
    }
}
