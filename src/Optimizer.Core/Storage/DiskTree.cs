using System.Diagnostics;

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
    /// <summary>Removed by <see cref="Remove"/> (itself or a folder above it): the page drops what it showed of it.</summary>
    public bool IsRemoved(int i) => _removed[i];
    public FileGroup Group(int i) => IsFolder(i) ? FileGroup.Other : FileTypes.GroupOf(Path.GetExtension(_name[i].AsSpan()));

    public IEnumerable<int> Children(int i) => _children[i] is { } list ? list.ToArray() : [];

    /// <summary>Number of children, without the copy <see cref="Children"/> makes (the treemap reads them on every layout).</summary>
    public int ChildCount(int i) => _children[i]?.Count ?? 0;

    /// <summary>The k-th child, largest first; k below <see cref="ChildCount"/>.</summary>
    public int Child(int i, int k) => _children[i]![k];

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
        private string[] _name;
        private int[] _parent;
        private long[] _size;
        private long[] _allocated;
        private DateTime[] _lastWrite;
        private EntryFlags[] _flags;
        private int _count;

        /// <param name="capacity">Entries expected, the root included (the file table reader knows them in advance).</param>
        public Builder(string rootPath, int capacity = 1024)
        {
            _rootPath = rootPath;
            capacity = Math.Max(capacity, 1);
            _name = new string[capacity];
            _parent = new int[capacity];
            _size = new long[capacity];
            _allocated = new long[capacity];
            _lastWrite = new DateTime[capacity];
            _flags = new EntryFlags[capacity];
            Append(-1, rootPath, 0, 0, default, EntryFlags.Folder);
        }

        public int Root => 0;

        public int AddFolder(int parent, string name, DateTime lastWriteUtc, EntryFlags flags) =>
            Append(parent, name, 0, 0, lastWriteUtc, flags | EntryFlags.Folder);

        /// <param name="allocated">Space on disk, -1 when unknown.</param>
        public int AddFile(int parent, string name, long size, long allocated, DateTime lastWriteUtc, EntryFlags flags) =>
            Append(parent, name, size, allocated, lastWriteUtc, flags & ~EntryFlags.Folder);

        /// <summary>Sums sizes, allocated space and file counts bottom-up and orders children by size, largest first. Once only.</summary>
        public DiskTree Build(ScanReader reader, TimeSpan elapsed, int lost = 0) => Build(reader, () => elapsed, lost);

        /// <summary>As <see cref="Build(ScanReader, TimeSpan, int)"/>, with the time read when the tree is done, so it includes the summing and sorting.</summary>
        public DiskTree Build(ScanReader reader, Stopwatch watch, int lost = 0) => Build(reader, () => watch.Elapsed, lost);

        private DiskTree Build(ScanReader reader, Func<TimeSpan> elapsed, int lost)
        {
            var n = _count;
            var size = Fit(_size, n);
            var allocated = Fit(_allocated, n);
            var files = new int[n];
            var children = new List<int>?[n];
            // Each folder's list made once at its final size: millions of entries leave no grown-out lists behind.
            var childCount = new int[n];
            for (var i = 1; i < n; i++) childCount[_parent[i]]++;
            for (var i = 0; i < n; i++)
            {
                if ((_flags[i] & EntryFlags.Folder) != 0) { children[i] = new List<int>(childCount[i]); size[i] = 0; allocated[i] = 0; }
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
            // Largest first, then in the order added; sorted in place.
            Comparison<int> bySize = (a, b) => size[a] != size[b] ? size[b].CompareTo(size[a]) : a.CompareTo(b);
            foreach (var list in children)
                if (list is { Count: > 1 }) list.Sort(bySize);
            return new DiskTree(_rootPath, n, Fit(_name, n), Fit(_parent, n), size, allocated, files, Fit(_lastWrite, n),
                Fit(_flags, n), children, reader, elapsed(), lost);
        }

        /// <summary>
        /// The array itself when it is no more than an eighth too long (presized from a count known in advance), else a
        /// copy of the used part: no copy of millions of entries just to drop a few unused slots.
        /// </summary>
        private static T[] Fit<T>(T[] array, int n) => array.Length - n <= n / 8 ? array : array[..n];

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
