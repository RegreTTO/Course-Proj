using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Crypto;
using ReactiveUI;
using FileFunc;

namespace AvaloniaProj.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _fileName = "";

    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    private ObservableCollection<string> _fileNames = new ObservableCollection<string>();

    public ObservableCollection<string> FileNames
    {
        get => _fileNames;
        set => this.RaiseAndSetIfChanged(ref _fileNames, value);
    }

    private object _selectedItem = new();

    public object SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    private string _exceptionText = "";

    public string ExceptionText
    {
        get => _exceptionText;
        set => this.RaiseAndSetIfChanged(ref _exceptionText, value);
    }

    public MainWindowViewModel()
    {
        var alreadySaved = Directory.GetFiles(FileFuncs.Path);
        if (alreadySaved.Length > 0)
        {
            foreach (var filePath in alreadySaved)
            {
                string name = new FileInfo(filePath).Name;
                FileNames.Add(Regex.Replace(name, @"\.ciphered$", string.Empty));
            }
        }

        Process thisProc = Process.GetCurrentProcess();
        thisProc.PriorityClass = ProcessPriorityClass.BelowNormal;
    }


    public async void UploadButton_Click()
    {
        try
        {
            if (FileName.Length == 0)
            {
                var topLevel =
                    TopLevel.GetTopLevel(
                        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                        ?.MainWindow);
                if (topLevel is null)
                {
                    throw new Exception("Ooops, something went wrong :(");
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "I HATE MY LIFE",
                    AllowMultiple = false
                });
                if (files.Count < 1)
                {
                    return;
                }

                foreach (var file in files)
                {
                    if (FileFuncs.SaveFile(new FileInfo(HttpUtility.UrlDecode(file.Path.AbsolutePath))))
                        FileNames.Add(file.Name);
                }
            }
        }
        catch (Exception e)
        {
            ExceptionText = e.Message;
        }
    }

    public void ListItem_DeleteSelected()
    {
        var fileToDelete = SelectedItem as string ?? string.Empty;
        if (fileToDelete == "") return;
        if (FileFuncs.DeleteSavedFile(fileToDelete))
            FileNames.Remove(fileToDelete);
    }

    public async void DownloadButton_Click()
    {
        try
        {
            var fileToDownload = SelectedItem as string + FileFuncs.Extension;
            if (fileToDownload == "" || !FileFuncs.IsFileSaved(fileToDownload)) return;
            var topLevel = TopLevel.GetTopLevel(
                (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            if (topLevel is null)
            {
                throw new Exception("Ooops, something went wrong :(");
            }

            var dialog = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Save your file",
                AllowMultiple = false
            });
            var newFilePath = $"{dialog[0].Path.AbsolutePath}/{fileToDownload}";
            newFilePath = Regex.Replace(newFilePath, @"\.ciphered$", string.Empty);
            if (!File.Exists(newFilePath))
            {
                var msg = Crypto.Cipher.Decode(await File.ReadAllBytesAsync(FileFuncs.Path + fileToDownload));
                await File.WriteAllBytesAsync(newFilePath, msg);
            }
        }
        catch (Exception e)
        {
            ExceptionText = e.Message;
        }
    }
}