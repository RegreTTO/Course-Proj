using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Crypto;
using ReactiveUI;
using FileFunc;

namespace AvaloniaProj.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<string> _fileNames = new ObservableCollection<string>();

    public ObservableCollection<string> FileNames
    {
        get => _fileNames;
        set => this.RaiseAndSetIfChanged(ref _fileNames, value);
    }

    private object _selectedItem = new object();

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

    private bool _isIndeterminate;

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => this.RaiseAndSetIfChanged(ref _isIndeterminate, value);
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
        thisProc.PriorityClass = ProcessPriorityClass.Normal;
    }


    public async void UploadButton_Click()
    {
        try
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
                Title = "Выберите файл",
                AllowMultiple = false
            });
            if (files.Count < 1)
            {
                return;
            }

            foreach (var file in files)
            {
                IsIndeterminate = true;
                try
                {
                    var task = Task.Run(() => FileFuncs.SaveFile(new FileInfo(file.Path.LocalPath)));
                    await task;
                }
                catch (InvalidDataException e)
                {
                    ExceptionText = e.Message;
                }

                FileNames.Add(file.Name);
                IsIndeterminate = false;
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
                Title = "Выберите куда сохранить файл",
                AllowMultiple = false
            });
            var newFilePath = $"{dialog[0].Path.LocalPath}/{fileToDownload}";
            newFilePath = Regex.Replace(newFilePath, @"\.ciphered$", string.Empty);
            if (!File.Exists(newFilePath))
            {
                IsIndeterminate = true;
                var bytes = await File.ReadAllBytesAsync(FileFuncs.Path + fileToDownload);
                var msg = await Task.Run(() => Cipher.Decode(bytes));
                await File.WriteAllBytesAsync(newFilePath, msg);
                IsIndeterminate = false;
            }
            else
            {
                throw new InvalidDataException("Файл в данной директории уже существует!");
            }
        }
        catch (Exception e)
        {
            ExceptionText = e.Message;
        }
    }
}