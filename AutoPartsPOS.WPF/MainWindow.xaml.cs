using AutoPartsPOS.WPF.ViewModels;
using System.Windows;

namespace AutoPartsPOS.WPF;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
