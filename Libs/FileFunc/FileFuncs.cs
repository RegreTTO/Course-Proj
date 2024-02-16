namespace FileFunc;

public static class FileFuncs
{
    public static readonly string Path = Directory.GetCurrentDirectory() + "/saved_files/";

    public static bool IsFileSaved(string name)
    {
        return File.Exists(Path + name);
    }

    public static bool DeleteSavedFile(string name)
    {
        if (!IsFileSaved(name)) return false;
        new FileInfo(Path + name).IsReadOnly = false;
        File.Delete(Path + name);
        return true;
    }

    public static bool SaveFile(FileInfo filePath)
    {
        if (IsFileSaved(filePath.Name) ||
            File.GetAttributes(filePath.FullName) == FileAttributes.Directory)
        {
            return false;
        }

        File.Copy(filePath.FullName, Path + filePath.Name);
        new FileInfo(Path + filePath.Name).IsReadOnly = true;
        return true;
    }
}