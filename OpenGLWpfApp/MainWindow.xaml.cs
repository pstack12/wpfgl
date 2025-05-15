using System.Diagnostics.Metrics;
using System.Windows;

namespace OpenGLWpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    //public Measurement? ActiveMeasurement {
    //    get;
    //    set;
    //}
    
    public MainWindow()
    {
        InitializeComponent();    
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XYZ File|*.xyz|TMD Files|*.tmd",
        };
        if (dialog.ShowDialog() != true)
            return;

        var filename = dialog.FileName;

        var measurement = new Measurement();
        if (!measurement.Initialize(filename))
        {
            return;
        }

        SurfaceControl.ActiveMeasurement = measurement;
        SurfaceControl.ComputeAo();
        SurfaceControl.RequestRender();
    }

}
