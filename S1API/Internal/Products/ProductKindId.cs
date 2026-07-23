using System;

namespace S1API.Internal.Products
{
    internal static class ProductKindId
    {
        internal static string Normalize(string id, string parameterName)
        {
            if (id == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            string normalized = id.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Product-kind IDs cannot be empty or whitespace.", parameterName);
            }

            int separatorIndex = normalized.IndexOf(':');
            if (separatorIndex <= 0
                || separatorIndex == normalized.Length - 1
                || separatorIndex != normalized.LastIndexOf(':'))
            {
                throw new ArgumentException(
                    "Product-kind IDs must use '<namespace>:<name>' with exactly one colon and non-empty parts.",
                    parameterName);
            }

            string namespacePart = normalized.Substring(0, separatorIndex);
            string namePart = normalized.Substring(separatorIndex + 1);
            ValidatePart(namespacePart, allowSlash: false, "namespace", parameterName);
            ValidatePart(namePart, allowSlash: true, "name", parameterName);
            return normalized;
        }

        private static void ValidatePart(
            string value,
            bool allowSlash,
            string partName,
            string parameterName)
        {
            bool hasLetterOrDigit = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (IsAsciiLetterOrDigit(character))
                {
                    hasLetterOrDigit = true;
                    continue;
                }

                if (character == '.'
                    || character == '_'
                    || character == '-'
                    || (allowSlash && character == '/'))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Product-kind ID {partName} contains invalid character '{character}' at position {i}. "
                    + "Use ASCII letters, digits, '.', '_', '-'"
                    + (allowSlash ? ", or '/'." : "."),
                    parameterName);
            }

            if (!hasLetterOrDigit)
            {
                throw new ArgumentException(
                    $"Product-kind ID {partName} must contain at least one ASCII letter or digit.",
                    parameterName);
            }
        }

        private static bool IsAsciiLetterOrDigit(char character)
        {
            return character >= 'a' && character <= 'z'
                   || character >= 'A' && character <= 'Z'
                   || character >= '0' && character <= '9';
        }
    }
}
