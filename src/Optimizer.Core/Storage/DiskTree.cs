namespace Optimizer.Core.Storage;

[Flags]
public enum EntryFlags : byte { None = 0, Folder = 1, System = 2, CloudOnly = 4, ReparsePoint = 8, Unreadable = 16 }

/// <summary>Which reader produced a tree: the file table (fast, needs admin) or a plain folder walk.</summary>
public enum ScanReader { FileTable, Folders }

/// <summary>Bytes and file count of one extension (lower case, no dot) across the tree.</summary>
public sealed record TypeTotal(string Extension, FileGroup Group, long Bytes, int Count);

/// <summary>
/// Result of a storage scan. Entries are indexes into parallel arrays (a drive can hold millions of files, so no
/// object per entry); a removed entry keeps its slot and is skipped. Created through <see cref="Builder"/>.
/// </summary>
public sealed class DiskTree
{
    private readonly string[] _name;
    private readonly int[] _parent;
    private readonly long[] _size;
    private readonly long[] _allocated;
    private readonly int[] _files;
    private readonly DateTime[] _lastWrite;
    private readonly EntryFlags[] _flags;
    private readonly bool[] _removed;
    private readonly List<int>?[] _children;

    private DiskTree(string rootPath, int count, string[] name, int[] parent, long[] size, long[] allocated, int[] files,
        DateTime[] lastWrite, EntryFlags[] flags, List<int>?[] children, ScanReader reader, TimeSpan elapsed, int lost)
    {
        RootPath = rootPath;
        Count = count;
        _name = name; _parent = parent; _size = size; _allocated = allocated; _files = files;
        _lastWrite = lastWrite; _flags = flags; _children = children;
        _removed = new bool[name.Length];
        Reader = reader;
        Elapsed = elapsed;
        Lost = lost;
    }

    public string RootPath { get; }
    public ScanReader Reader { get; }
    public TimeSpan Elapsed { get; }
    /// <summary>Entries the reader could not see (access denied, vanished while scanning).</summary>
    public int Lost { get; }
    /// <summary>Slots used, including removed entries.</summary>
    public int Count { get; }
    public int Root => 0;

    public string Name(int i) => _name[i];
    public int Parent(int i) => _parent[i];
    public long Size(int i) => _size[i];
    /// <summary>Space on disk; -1 when unknown for this entry or any entry below it.</summary>
    public long Allocated(int i) => _allocated[i];
    /// <summary>1 for a file; for a folder the number of files below it (folders are not counted).</summary>
    public int Files(int i) => _files[i];
    public DateTime LastWriteUtc(int i) => _lastWrite[i];
    public EntryFlags Flags(int i) => _flags[i];
    public bool IsFolder(int i) => (_flags[i] & EntryFlags.Folder) != 0;
    public FileGroup Group(int i) => IsFolder(i) ? FileGroup.Other : FileTypes.GroupOf(ExtensionOf(i));

    public IEnumerable<int> Children(int i) => _children[i] is { } list ? list.ToArray() : [];

    public string FullPath(int i)
    {
        if (i == Root) return RootPath;
        var parts = new Stack<string>();
        for (var n = i; n != Root; n = _parent[n]) parts.Push(_name[n]);
        return Path.Combine(RootPath, string.Join('\\', parts));
    }

    /// <summary>Unlinks an entry (and everything below it) and subtracts it from every ancestor.</summary>
    public void Remove(int i)
    {
        if (i == Root || _removed[i]) return;
        var size = _size[i];
        var allocated = _allocated[i];
        var files = _files[i];
        for (var p = _parent[i]; p >= 0; p = _parent[p])
        {
            _size[p] -= size;
            _files[p] -= files;
            // An unknown ancestor stays unknown: the other unknown entries are not tracked.
            if (_allocated[p] >= 0 && allocated >= 0) _allocated[p] -= allocated;
        }
        _children[_parent[i]]!.Remove(i);
        MarkRemoved(i);
    }

    public IReadOnlyList<int> LargestFiles(int count) =>
        Files().OrderByDescending(i => _size[i]).Take(Math.Max(0, count)).ToArray();

    public IReadOnlyList<TypeTotal> TypeTotals() =>
        Files().GroupBy(ExtensionOf)
            .Select(g => new TypeTotal(g.Key, FileTypes.GroupOf(g.Key), g.Sum(i => _size[i]), g.Count()))
            .OrderByDescending(t => t.Bytes)
            .ToArray();

    public IReadOnlyList<int> LargestFilesOfType(string extension, int count)
    {
        var key = (extension ?? "").TrimStart('.').ToLowerInvariant();
        return Files().Where(i => ExtensionOf(i) == key).OrderByDescending(i => _size[i])
            .Take(Math.Max(0, count)).ToArray();
    }

    private IEnumerable<int> Files()
    {
        for (var i = 1; i < Count; i++)
            if (!_removed[i] && !IsFolder(i)) yield return i;
    }

    private string ExtensionOf(int i) => Path.GetExtension(_name[i]).TrimStart('.').ToLowerInvariant();

    private void MarkRemoved(int i)
    {
        var stack = new Stack<int>();
        stack.Push(i);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            _removed[n] = true;
            if (_children[n] is { } list) foreach (var c in list) stack.Push(c);
        }
    }

    /// <summary>Collects entries during a scan; parents must be added before their children. Not thread safe.</summary>
    public sealed class Builder
    {
        private readonly string _rootPath;
        private string[] _name = new string[1024];
        private int[] _parent = new int[1024];
        private long[] _size = new long[1024];
        private long[] _allocated = new long[1024];
        private DateTime[] _lastWrite = new DateTime[1024];
        private EntryFlags[] _flags = new EntryFlags[1024];
        private int _count;

        public Builder(string rootPath)
        {
            _rootPath = rootPath;
            Append(-1, rootPath, 0, 0, default, EntryFlags.Folder);
        }

        public int Root => 0;

        public int AddFolder(int parent, string name, DateTime lastWriteUtc, EntryFlags flags) =>
            Append(parent, name, 0, 0, lastWriteUtc, flags | EntryFlags.Folder);

        /// <param name="allocated">Space on disk, -1 when unknown.</param>
        public int AddFile(int parent, string name, long size, long allocated, DateTime lastWriteUtc, EntryFlags flags) =>
            Append(parent, name, size, allocated, lastWriteUtc, flags & ~EntryFlags.Folder);

        /// <summary>Sums sizes, allocated space and file counts bottom-up and orders children by size, largest first.</summary>
        public DiskTree Build(ScanReader reader, TimeSpan elapsed, int lost = 0)
        {
            var n = _count;
            var size = _size[..n];
            var allocated = _allocated[..n];
            var files = new int[n];
            var children = new List<int>?[n];
            for (var i = 0; i < n; i++)
            {
                if ((_flags[i] & EntryFlags.Folder) != 0) { children[i] = new List<int>(); size[i] = 0; allocated[i] = 0; }
                else files[i] = 1;
            }
            // Parents precede children, so one reverse pass finishes every folder before it is added to its own parent.
            for (var i = n - 1; i > 0; i--)
            {
                var p = _parent[i];
                children[p]!.Add(i);
                size[p] += size[i];
                files[p] += files[i];
                allocated[p] = allocated[p] < 0 || allocated[i] < 0 ? -1 : allocated[p] + allocated[i];
            }
            for (var i = 0; i < n; i++)
                if (children[i] is { } list) children[i] = list.OrderByDescending(c => size[c]).ThenBy(c => c).ToList();
            return new DiskTree(_rootPath, n, _name[..n], _parent[..n], size, allocated, files, _lastWrite[..n],
                _flags[..n], children, reader, elapsed, lost);
        }

        private int Append(int parent, string name, long size, long allocated, DateTime lastWrite, EntryFlags flags)
        {
            if (_count == _name.Length)
            {
                var cap = _count * 2;
                Array.Resize(ref _name, cap); Array.Resize(ref _parent, cap); Array.Resize(ref _size, cap);
                Array.Resize(ref _allocated, cap); Array.Resize(ref _lastWrite, cap); Array.Resize(ref _flags, cap);
            }
            var i = _count++;
            _name[i] = name; _parent[i] = parent; _size[i] = size; _allocated[i] = allocated;
            _lastWrite[i] = lastWrite; _flags[i] = flags;
            return i;
        }
    }
}
