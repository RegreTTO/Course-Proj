using System.IO;
using Avalonia.Controls;
using Crypto;
using FileFunc;

namespace AvaloniaProj.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Directory.CreateDirectory(FileFuncs.Path);
        Directory.CreateDirectory(Crypto.Cipher.ConfigDir);
        Cipher.ReadKeys();
    }
}