// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using System.Security.Cryptography;
using System.Text.Json;
namespace RoguePrince.CheatMenu.Core;

public static class SaveBackup
{
    public static string Create(string sourceRoot, string backupRoot)
    {
        sourceRoot = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
        backupRoot = Path.GetFullPath(backupRoot).TrimEnd(Path.DirectorySeparatorChar);
        if (backupRoot.Equals(sourceRoot, StringComparison.OrdinalIgnoreCase) || backupRoot.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(MenuText.Get("备份目录不能放在源存档目录内"));
        RejectLinkedAncestors(sourceRoot);
        RejectLinkedAncestors(backupRoot);
        var files = new List<string>();
        Walk(sourceRoot, files);
        if (files.Count == 0) throw new InvalidDataException(MenuText.Get("没有找到存档文件，未执行修改"));
        var destination = Path.Combine(backupRoot, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(destination);
        var manifest = new List<object>();
        foreach (var source in files)
        {
            var relative = Path.GetRelativePath(sourceRoot, source);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            // Deny concurrent writers while copying; an in-progress save causes the operation to fail closed.
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) input.CopyTo(output);
            input.Position = 0;
            using var hasher = SHA256.Create();
            var originalHash = Convert.ToHexString(hasher.ComputeHash(input));
            using var copy = File.OpenRead(target);
            var copiedHash = Convert.ToHexString(hasher.ComputeHash(copy));
            if (originalHash != copiedHash) throw new IOException(MenuText.Get("存档备份校验失败"));
            manifest.Add(new { file = relative, length = input.Length, sha256 = originalHash });
        }
        File.WriteAllText(Path.Combine(destination, "manifest.json"), JsonSerializer.Serialize(new { createdUtc = DateTime.UtcNow, sourceRoot, files = manifest }, new JsonSerializerOptions { WriteIndented = true }));
        return destination;
    }

    private static void Walk(string directory, List<string> files)
    {
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(MenuText.Get("存档目录不存在：") + directory);
        RejectLink(directory);
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            RejectLink(entry);
            if (Directory.Exists(entry)) Walk(entry, files);
            else if (Path.GetExtension(entry).Equals(".sav", StringComparison.OrdinalIgnoreCase)) files.Add(entry);
        }
    }
    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException(MenuText.Get("存档路径包含链接，未执行修改：") + path);
    }
    private static void RejectLinkedAncestors(string path)
    {
        for (var current = new DirectoryInfo(path); current != null; current = current.Parent)
            if (current.Exists) RejectLink(current.FullName);
    }
}
