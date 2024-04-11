using System.Diagnostics.CodeAnalysis;
using System.Text;
using Crypto;

namespace FileFunc;

public static class FileFuncs
{
    public static readonly string Path = Directory.GetCurrentDirectory() + "/saved_files/";
    public static readonly string Extension = ".ciphered";

    public static bool IsFileSaved(string name)
    {
        if (!File.Exists(Path + name)) return false;
        return true;
    }

    public static bool DeleteSavedFile(string name)
    {
        if (!IsFileSaved(name + Extension)) return false;
        new FileInfo(Path + name + Extension).IsReadOnly = false;
        File.Delete(Path + name + Extension);
        return true;
    }

    /// <summary>
    /// Creates file 'filename'.ciphered
    /// </summary>
    async private static void EncodeFile(FileInfo file)
    {
        byte[] msg = await File.ReadAllBytesAsync(file.FullName);
        byte[] encoded = await Cipher.Encode(msg);
        string cipheredFileName = Path + file.Name + Extension;
        await File.WriteAllBytesAsync(cipheredFileName, encoded);
    }

    public static void SaveFile(FileInfo filePath)
    {
        if (IsFileSaved(filePath.Name))
        {
            throw new InvalidDataException("Файл уже зашифрован и сохранен!");
        }

        if (File.GetAttributes(filePath.FullName) == FileAttributes.Directory)
        {
            throw new InvalidDataException("Вы не можете выбрать директорию!");
        }

        EncodeFile(filePath);
    }
}