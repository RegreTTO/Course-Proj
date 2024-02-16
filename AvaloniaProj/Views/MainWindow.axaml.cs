using System.IO;
using Avalonia.Controls;
using FileFunc;

namespace AvaloniaProj.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (!Directory.Exists(FileFuncs.Path))
        {
            Directory.CreateDirectory(FileFuncs.Path);
        }

    }
}