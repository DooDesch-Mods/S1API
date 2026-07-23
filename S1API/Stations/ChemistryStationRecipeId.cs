using System;

namespace S1API.Stations
{
    internal static class ChemistryStationRecipeId
    {
        internal static StringComparer Comparer { get; } = StringComparer.OrdinalIgnoreCase;

        internal static string Resolve(
            bool hasExplicitId,
            string? explicitId,
            int productQuantity,
            string productItemId)
        {
            if (!hasExplicitId)
                return $"{productQuantity}x{productItemId}";

            string recipeId = (explicitId ?? string.Empty).Trim();
            if (recipeId.Length == 0)
            {
                throw new InvalidOperationException(
                    "Explicit Chemistry Station recipe IDs cannot be empty or whitespace. " +
                    "Use a namespaced ID such as 'my-mod:alternate-route'.");
            }

            int separatorIndex = recipeId.IndexOf(':');
            if (separatorIndex <= 0 ||
                separatorIndex != recipeId.LastIndexOf(':') ||
                separatorIndex == recipeId.Length - 1 ||
                !IsValidSegment(recipeId, 0, separatorIndex) ||
                !IsValidSegment(recipeId, separatorIndex + 1, recipeId.Length))
            {
                throw new InvalidOperationException(
                    $"Explicit Chemistry Station recipe ID '{recipeId}' is invalid. " +
                    "Use '<namespace>:<recipe>' with ASCII letters, numbers, '.', '_', or '-' in each segment " +
                    "(for example 'my-mod:alternate-route').");
            }

            return recipeId;
        }

        private static bool IsValidSegment(string value, int startIndex, int endIndex)
        {
            bool hasAlphaNumericCharacter = false;
            for (int i = startIndex; i < endIndex; i++)
            {
                char character = value[i];
                bool isAsciiLetter =
                    character >= 'a' && character <= 'z' ||
                    character >= 'A' && character <= 'Z';
                bool isDigit = character >= '0' && character <= '9';
                bool isSeparator = character == '.' || character == '_' || character == '-';

                if (!isAsciiLetter && !isDigit && !isSeparator)
                    return false;

                if (isAsciiLetter || isDigit)
                    hasAlphaNumericCharacter = true;
            }

            return hasAlphaNumericCharacter;
        }
    }
}
