﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace LiteDbExplorer.Modules
{
    public static class ArchiveExtensions
    {
        public static string EnsureUniqueFileName(this FileInfo fileInfo)
        {
            if (fileInfo?.DirectoryName == null)
            {
                throw new DirectoryNotFoundException();
            }

            var fileCount = 0;
            string newFileName;
            do
            {
                fileCount++;
                newFileName =
                    $"{Path.GetFileNameWithoutExtension(fileInfo.Name)} {(fileCount > 0 ? "(" + fileCount + ")" : "")}{Path.GetExtension(fileInfo.Name)}";
            } while (File.Exists(Path.Combine(fileInfo.DirectoryName, newFileName)));

            return newFileName;
        }

        public static void EnsureFileDirectory([NotNull] string fileFullName)
        {
            if (string.IsNullOrEmpty(fileFullName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(fileFullName));
            }

            var dir = Path.GetDirectoryName(fileFullName);

            EnsureDirectory(dir);
        }

        public static void EnsureDirectory([NotNull] string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(directoryPath));
            }
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static string EnsureFileNameExtension(string fileName, string extension)
        {
            if (!Path.HasExtension(fileName) || !fileName.EndsWith(extension))
            {
                fileName = $"{fileName.TrimEnd('.')}.{extension.TrimStart('.')}";
            }

            return fileName;
        }

        public static void PrependTimeStamp(ref string fileName)
        {
            fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyy-MM-dd_HH-mmss}_{Path.GetExtension(fileName)}";
        }

        public static string EnsureFileName(string name, string fallbackName, string extension, bool prependTimestamp)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = fallbackName;
            }

            var result = Path.GetFileNameWithoutExtension(name);
            if (prependTimestamp)
            {
                result += $"_{DateTime.Now:yyyy-MM-dd_HH-mmss}";
            }

            result += $".{extension.TrimStart('.')}";

            return result;
        }

        public static DriveType GetDriveType(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return DriveType.Unknown;
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                // var di = new DirectoryInfo(dir);
                var isWinDrive = Regex.IsMatch(dir, @"[a-z]:\\");
                if (Path.IsPathRooted(dir) && isWinDrive)
                {
                    try
                    {
                        var rootPath = Path.GetPathRoot(dir);
                        var drive = new DriveInfo(rootPath);
                        return drive.DriveType;
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }

                if (new Uri(path).IsUnc)
                {
                    return DriveType.Network;
                }
            }

            return DriveType.Unknown;
        }

        public static T SafeDeserializeJsonFile<T>(string path,
            Action<(string recoveryPath, Exception exception)> errorCallback = null,
            JsonSerializerSettings serializerSettings = null)
        {
            if (serializerSettings == null)
            {
                serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new IgnoreParentPropertiesResolver(true),
                    Formatting = Formatting.Indented
                };
            }

            try
            {
                if (File.Exists(path))
                {
                    var value = File.ReadAllText(path);
                    var rawQueryHistories = JsonConvert.DeserializeObject<T>(value, serializerSettings);
                    return rawQueryHistories;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                string recoveryPath = null;
                if (File.Exists(path))
                {
                    recoveryPath = Path.Combine(
                        Path.GetDirectoryName(path), 
                        $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.UtcNow.Ticks}_fail.{Path.GetExtension(path).TrimStart('.')}"
                    );
                    File.Copy(path, recoveryPath);
                    File.WriteAllText(path, JsonConvert.SerializeObject(default(T)));
                }

                errorCallback?.Invoke((recoveryPath, e));
            }

            return default(T);
        }
    }
}