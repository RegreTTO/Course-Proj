using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DynamicData;
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

    public MainWindowViewModel()
    {
        var alreadySaved = Directory.GetFiles(FileFuncs.Path);
        if (alreadySaved.Length > 0)
        {
            foreach (var filePath in alreadySaved)
            {
                FileNames.Add(new FileInfo(filePath).Name);
            }
        }
    }

    public async void UploadButton_Click()
    {
        if (FileName.Length == 0)
        {
            var topLevel =
                TopLevel.GetTopLevel(
                    (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
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
                throw new InvalidDataException("You must pick a file!");
            }

            foreach (var file in files)
            {
                if (FileFuncs.SaveFile(new FileInfo(HttpUtility.UrlDecode(file.Path.AbsolutePath))))
                    FileNames.Add(file.Name);
            }
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
        var fileToDownload = SelectedItem as string ?? string.Empty;
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
        if (!File.Exists(newFilePath))
            File.Copy(FileFuncs.Path + fileToDownload, newFilePath);
        else
        {
            
        }
    }
}