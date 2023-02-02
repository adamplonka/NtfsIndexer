namespace NtfsIndexer;

public class MftScanner
{
    private readonly IDictionary<Guid, FolderFrnWithName> folders = new Dictionary<Guid, FolderFrnWithName>();
    private readonly IDictionary<Guid, List<UsnRecord>> orphansByParentId = new Dictionary<Guid, List<UsnRecord>>();
    private const int RecursionLimit = 200;

    private readonly MftReader reader;

    public MftScanner()
    {
        reader = new MftReader();
    }

    public IEnumerable<string> EnumerateFiles(string drive)
    {
        var rootEntry = reader.GetRootFrnEntry(drive);
        folders[rootEntry] = new FolderFrnWithName { FileReferenceNumber = rootEntry, FullName = drive };

        foreach (var info in reader.EnumerateVolume(drive))
        {
            if (info.FileAttributes.HasFlag(FileAttributes.Directory))
            {
                folders[info.Guid] = new FolderFrnWithName
                {
                    FileReferenceNumber = info.Guid,
                    Name = info.FileName,
                    ParentFrn = info.ParentGuid
                };
            }

            if (IsAttachedToRoot(info))
            {
                foreach (var name in FlushWithChildren(info))
                    yield return name;
            }
            else
            {
                AddOrphanedChild(info);
            }
        }
    }

    /// <summary>
    /// Checks if the entity is attached all way up to the parent.
    /// </summary>
    /// <returns>True if we know the parent folder and it's not orphaned itself</returns>
    private bool IsAttachedToRoot(UsnRecord info)
    {
        return folders.TryGetValue(info.ParentGuid, out var parent)
               && !orphansByParentId.ContainsKey(parent.ParentFrn);
    }

    /// <summary>
    /// Adds orphaned entity to the ID of parent that is not yet known.
    /// Those children will be flushed along with the parent once it's available.
    /// </summary>
    private void AddOrphanedChild(UsnRecord info)
    {
        if (!orphansByParentId.TryGetValue(info.ParentGuid, out var orphans))
        {
            orphans = new List<UsnRecord>(1);
            orphansByParentId[info.ParentGuid] = orphans;
        }

        orphans.Add(info);
    }

    /// <summary>
    /// Recursively checks for item's fullpath.
    /// Stores all partial informations in the item and item's parent FullName properties to reduce complexity.
    /// </summary>
    public string GetFullName(FolderFrnWithName info, int limit = RecursionLimit)
    {
        return info.FullName ??= GetFullName(folders[info.ParentFrn], limit - 1) + "\\" + info.Name;
    }

    private string GetFullName(UsnRecord usnRecord)
    {
        return GetFullName(folders[usnRecord.ParentGuid]) + "\\" + usnRecord.FileName;
    }

    /// <summary>
    /// Enumerates item with all its formerly orphaned children
    /// </summary>
    private IEnumerable<string> FlushWithChildren(UsnRecord info, int limit = RecursionLimit)
    {
        yield return GetFullName(info);

        if (limit > 0 && orphansByParentId.TryGetValue(info.Guid, out var children))
        {
            orphansByParentId.Remove(info.Guid); // adopted
            foreach (var child in children)
                foreach (var childName in FlushWithChildren(child, limit - 1))
                    yield return childName;
        }
    }
}


public class FolderFrnWithName
{
    public Guid FileReferenceNumber;
    public Guid ParentFrn;
    public string? Name;
    public string? FullName;
}