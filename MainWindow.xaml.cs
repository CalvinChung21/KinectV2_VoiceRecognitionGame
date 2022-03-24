using System.Windows;
using Microsoft.Kinect;
using NUI3D;

namespace FromNoToYes
{
    /// The whole framework is brrowed from T11_VoiceControl 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor; 
        private BodyFrameManager bodyFrameManager;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            bodyFrameManager = new BodyFrameManager();
            bodyFrameManager.Init(sensor, skeletonImg, recognizedCommand, gameWin, gameLose, cover);       
        }        

        
    }
}
