// Small, dependency-free helpers for tallying occurrences into a dictionary.
// Replaces the repeated "if (!dict.ContainsKey(k)) dict[k] = 0; dict[k]++"
// idiom that appeared across the patches (and was the source of two nullable
// warnings, because Dictionary throws on a null key).
//
// This file is intentionally pure C# (no Unity/BepInEx references) so it can be
// linked directly into the test project and unit-tested in isolation.

using System.Collections.Generic;

namespace SeaPowerCrunchatizer.Utilities
{
    /// <summary>
    /// Extension methods for counting occurrences keyed by string (e.g. ammunition
    /// filenames) into an <see cref="IDictionary{TKey,TValue}"/>.
    /// </summary>
    public static class CountExtensions
    {
        /// <summary>
        /// Adds <paramref name="amount"/> to the running total for <paramref name="key"/>,
        /// creating the entry (starting from zero) if it does not yet exist.
        /// </summary>
        /// <param name="counts">The tally dictionary to update.</param>
        /// <param name="key">
        /// The key to increment. Null or empty keys are ignored so callers can funnel
        /// possibly-unnamed items (e.g. ammunition with no filename) through without a
        /// null-key crash or a phantom entry.
        /// </param>
        /// <param name="amount">How much to add. Defaults to 1.</param>
        /// <returns>
        /// True if the count was incremented; false if the key was null/empty and skipped.
        /// </returns>
        public static bool Increment(this IDictionary<string, int> counts, string? key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            counts.TryGetValue(key!, out var current);
            counts[key!] = current + amount;
            return true;
        }
    }
}
