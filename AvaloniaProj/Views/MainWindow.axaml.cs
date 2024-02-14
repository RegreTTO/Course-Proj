using System.Diagnostics;
using Avalonia.Controls;

namespace AvaloniaProj.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (Debugger.IsAttached)
        {
            Debug.WriteLine("SUCKYEA");
        }
        else
        {
            Debug.WriteLine(":(");
        }
    }
}