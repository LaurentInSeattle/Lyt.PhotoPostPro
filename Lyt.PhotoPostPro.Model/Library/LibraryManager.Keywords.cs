namespace Lyt.PhotoPostPro.Model.Library;

public sealed partial class LibraryManager
{
    // Lookup the index, can provide multiple keywords, if so this is a AND operation 
    // LATER: Add OR and NOT operators 
    public HashSet<string> KeywordsLookup(string keywordsString)
    {
        string[] operators = ["AND", "OR", "NOT"];

        bool IsOperator(string token) => (from op in operators where token == op select op).Any();

        char[] separators = [' ', '\n', '\r', ',', ';'];
        string[] tokens =
            keywordsString.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens is null || tokens.Length == 0)
        {
            return [];
        }

        // Find first non empty hash 
        HashSet<string>? firstHash = null;
        int currentIndex = 0;
        for (int tokenIndex = 0; tokenIndex < tokens.Length; ++tokenIndex)
        {
            string token = tokens[tokenIndex];
            if (IsOperator(token))
            {
                continue;
            }

            if (!this.KeywordsIndex.TryGetValue(token, out var hash))
            {
                continue;
            }

            if (hash.Count == 0)
            {
                continue;
            }

            // We need to deep clone or else we are going to corrupt the master index when 
            // performing the intersects in the next steps.
            HashSet<string> deepClone = new(hash.Count);
            foreach (string path in hash)
            {
                _ = deepClone.Add(path);
            }

            firstHash = deepClone;
            currentIndex = tokenIndex;
            break;
        }

        if (firstHash is null || firstHash.Count == 0)
        {
            return [];
        }

        for (int tokenIndex = currentIndex + 1; tokenIndex < tokens.Length; ++tokenIndex)
        {
            string token = tokens[tokenIndex];
            if (IsOperator(token))
            {
                continue;
            }

            if (!this.KeywordsIndex.TryGetValue(token, out var hash))
            {
                continue;
            }

            if (hash.Count == 0)
            {
                continue;
            }

            firstHash.IntersectWith(hash);
        }

        return firstHash;
    }

    public void UpdateKeywordsMasterIndex(Metadata metadata)
    {
        string path = metadata.MetadataFullPath();

        // Completely remove 'path' from every entry in the dictionary  
        foreach (HashSet<string> hash in this.KeywordsIndex.Values)
        {
            _ = hash.Remove(path);
        }

        // Add new keywords and add again keywords that were removed 
        foreach (string keyword in metadata.Keywords)
        {
            string key = keyword.ToLowerInvariant();
            if (this.KeywordsIndex.TryGetValue(key, out var hash))
            {
                _ = hash.Add(path);
            }
            else
            {
                this.KeywordsIndex.Add(key, [path]);
            }
        }

        new LibraryKeywordsUpdateMessage().Publish();
    }
}
