namespace Optimizer.Core.Storage;

/// <summary>Coarse file groups shown in the storage analysis.</summary>
public enum FileGroup { Video, Audio, Images, Archives, Programs, Documents, System, Other }

/// <summary>Maps a file extension to its <see cref="FileGroup"/>; unknown extensions are Other.</summary>
public static class FileTypes
{
    private static readonly Dictionary<string, FileGroup> Map = Build();
    private static readonly Dictionary<string, FileGroup>.AlternateLookup<ReadOnlySpan<char>> SpanMap = Map.GetAlternateLookup<ReadOnlySpan<char>>();

    private static Dictionary<string, FileGroup> Build()
    {
        var map = new Dictionary<string, FileGroup>(StringComparer.OrdinalIgnoreCase);
        void Add(FileGroup group, string extensions)
        {
            foreach (var e in extensions.Split(' ')) map[e] = group;
        }
        Add(FileGroup.Video, "mp4 mkv avi mov wmv webm m4v ts mpg mpeg");
        Add(FileGroup.Audio, "mp3 flac wav aac ogg m4a wma opus");
        Add(FileGroup.Images, "jpg jpeg png gif bmp webp heic tif tiff raw cr2 nef dng psd svg ico");
        Add(FileGroup.Archives, "zip 7z rar tar gz bz2 xz zst iso img vhd vhdx vmdk cab wim esd");
        Add(FileGroup.Programs, "exe msi msix msixbundle appx appxbundle dll bat cmd ps1 jar");
        Add(FileGroup.Documents, "pdf doc docx xls xlsx ppt pptx odt ods odp txt md rtf csv epub");
        Add(FileGroup.System, "sys etl evtx dmp log mui cat pnf inf manifest mum pf");
        return map;
    }

    /// <summary>Group of an extension, with or without the leading dot, in any case.</summary>
    public static FileGroup GroupOf(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return FileGroup.Other;
        var key = extension[0] == '.' ? extension[1..] : extension;
        return Map.TryGetValue(key, out var group) ? group : FileGroup.Other;
    }

    /// <summary>The same without a string: the treemap asks for every file it draws.</summary>
    public static FileGroup GroupOf(ReadOnlySpan<char> extension)
    {
        if (extension.IsEmpty) return FileGroup.Other;
        if (extension[0] == '.') extension = extension[1..];
        return SpanMap.TryGetValue(extension, out var group) ? group : FileGroup.Other;
    }
}
