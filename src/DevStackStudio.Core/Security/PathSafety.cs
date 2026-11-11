namespace DevStackStudio.Core.Security;

public static class PathSafety
{
    public static bool IsSafeProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            return false;
        }

        if (name.Contains("..", StringComparison.Ordinal) || name.Contains('/', StringComparison.Ordinal) || name.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        return name.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
    }

    public static string CombineUnderRoot(string root, params string[] segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var fullRoot = Path.GetFullPath(root);
        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRoot += Path.DirectorySeparatorChar;
        }

        var combined = fullRoot;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                throw new InvalidOperationException("Path segment cannot be empty.");
            }

            var normalized = segment.Replace('\\', '/');
            foreach (var part in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part is "." or "..")
                {
                    throw new InvalidOperationException("Path traversal is not allowed.");
                }

                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new InvalidOperationException($"Invalid path segment '{part}'.");
                }

                combined = Path.GetFullPath(Path.Combine(combined, part));
            }
        }

        if (!combined.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(combined.TrimEnd(Path.DirectorySeparatorChar), fullRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved path escaped the output root.");
        }

        return combined;
    }
}
