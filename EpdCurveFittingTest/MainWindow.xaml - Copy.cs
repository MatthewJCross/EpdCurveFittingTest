using MPFitLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EpdCurveFittingTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const double TOLERANCE = 0.001;

        #region DllImports

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        #endregion

        #region Bindings

        private string _maximizeRestoreStr = "Maximise";

        public string MaximizeRestoreStr
        {
            get => _maximizeRestoreStr; 
            set
            {
                _maximizeRestoreStr = value;
                OnPropertyChanged("MaximizeRestoreStr");
            }
        }

        private string _filenameModel = "ModelData.dat";

        public string FilenameModel
        {
            get => _filenameModel; 
            set
            {
                _filenameModel = value;
                OnPropertyChanged("FilenameModel");
            }
        }

        private string _filenameMeasured = "MeasuredData.dat";

        public string FilenameMeasured
        {
            get => _filenameMeasured; 
            set
            {
                _filenameMeasured = value;
                OnPropertyChanged("FilenameMeasured");
            }
        }

        private double _tOffset = 2;

        public double TOffset
        {
            get => _tOffset;
            set
            {
                if (Math.Abs(value - _tOffset) > TOLERANCE)
                {
                    _tOffset = value;
                    OnPropertyChanged("TOffset");
                }
            }
        }

        private double _tGain = 9.34162529713385;

        public double TGain
        {
            get => _tGain;
            set
            {
                if (Math.Abs(value - _tGain) > TOLERANCE)
                {
                    _tGain = value;
                    OnPropertyChanged("TGain");
                }
            }
        }

        private double _yOffset = 8043.10597079514;

        public double YOffset
        {
            get => _yOffset;
            set
            {
                if (Math.Abs(value - _yOffset) > TOLERANCE)
                {
                    _yOffset = value;
                    OnPropertyChanged("YOffset");
                }
            }
        }

        private double _yGain = 29.8905073144862;
        public double YGain
        {
            get => _yGain;
            set
            {
                if (Math.Abs(value - _yGain) > TOLERANCE)
                {
                    _yGain = value;
                    OnPropertyChanged("YGain");
                }
            }
        }

        private double _ySlope = 0;
        public double YSlope
        {
            get => _ySlope;
            set
            {
                if (Math.Abs(value - _ySlope) > TOLERANCE)
                {
                    _ySlope = value;
                    OnPropertyChanged("YSlope");
                }
            }
        }

        private int _ignoreBefore = 10;
        public int IgnoreBefore
        {
            get => _ignoreBefore;
            set
            {
                _ignoreBefore = value;
                OnPropertyChanged("IgnoreBefore");
            }
        }

        private int _fitEvery = 10;
        public int FitEvery
        {
            get => _fitEvery;
            set
            {
                _fitEvery = value;
                OnPropertyChanged("FitEvery");
            }
        }

        private int _fittingRange = 30;
        public int FittingRange
        {
            get => _fittingRange;
            set
            {
                _fittingRange = value;
                OnPropertyChanged("FittingRange");
            }
        }

        private double _thickness = 0;
        public double Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                OnPropertyChanged("Thickness");
            }
        }

        private int _speedDelay = 1000;
        public int SpeedDelay
        {
            get => _speedDelay;
            set
            {
                _speedDelay = value;
                OnPropertyChanged("SpeedDelay");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        BackgroundWorker backgroundWorker;



        private bool working = false;

        private List<double> intensityData = new List<double>();
        private List<double> modelData = new List<double>();
        private List<double> errorData = new List<double>();
        private List<double> measuredData = new List<double>();
        private List<double> calculatedData = new List<double>();
        private List<double> scaledThickness = new List<double>();
        private int dataIndex = 0;
        private int timeCount;
        private int startFitTime;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            backgroundWorker.RunWorkerAsync();

            myChart.GraphTitle = "EPD Curve Matching";
            myChart.YLabel = "Intensity";
            myChart.XLabel = "Time (s)";
            myChart.BorderSize = 2;
            myChart.XMax = 180;
            myChart.YMax = 30;
            //myChart.ShowToolTip = true;

            myChart.GraphOuterColor = Colors.LightPink;
            myChart.ShowGridlines = true;

            LoadData(FilenameMeasured, ref intensityData, 0, 650);
            LoadData(FilenameModel, ref modelData, 1);
        }

        private int GetModelData(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            for (int i = 0; i < dy.Length; i++)
            {
                // Work out the error for this point using the given fit parameters
                double error = calculatedData[i] - measuredData[i];
                int st = (int)Math.Floor(scaledThickness[i]);
                if (st < 0)
                    dy[i] = double.PositiveInfinity; // Off the end of the model
                else
                    dy[i] = error + ((startFitTime+i) - scaledThickness[i]);
            }
            return 0;
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker) sender;
            double scaledIntensity = 0;
            while (!worker.CancellationPending)
            {
                if (working)
                {
                    Thread.Sleep(SpeedDelay);
                    timeCount++;

                    double intensity = intensityData[dataIndex];
                    double newIntensity;
                    //if (timeCount >= IgnoreBefore)
                    {
                        Thickness = (timeCount * TGain) + TOffset;
                        int st = (int)Math.Floor(Thickness);
                        double diff = Thickness - st;
                        double x1Intensity = YOffset + (modelData[st] * YGain) + st;
                        double x2Intensity = YOffset + (modelData[st + 1] * YGain) + st;
                        double x0Intensity = YOffset + (modelData[st - 1] * YGain) + st;

                        double scaledTime = (Thickness - TOffset) / TGain;

                        // Do the linear interpolation between the intensities
                        double scaledIntensity1 = (x1Intensity + diff * (x2Intensity - x1Intensity)) / 650;
                        scaledIntensity = (x0Intensity + diff * (x2Intensity - x0Intensity)) / 650;



                        // new code
                        //double thickness = timeCount * TGain + TOffset;
                        int exp = (int)Math.Floor(Thickness);

                        double slope = (modelData[exp+1] - modelData[exp]) / 1;
                        
                        double d = Thickness - st;
                        newIntensity = 9; // find intensity from thickness model find nm thickness intensity, first column model data
                        // Have to interpolate because we only have whole numbers in model table and we may need 58.6 etc, so find the intensity for thickness between 58 and 59

                        d *= slope;
                        newIntensity = ((modelData[exp] + d) * YGain + YOffset)/ 650;

                        ////// end new





                        // Work out the error for this point using the given fit parameters
                        double error = scaledIntensity - intensity;
                        errorData.Add(error + (timeCount - Thickness));

                        measuredData.Add(intensity);
                        calculatedData.Add(newIntensity);//scaledIntensity);
                        scaledThickness.Add(Thickness);

                        if (measuredData.Count > FittingRange)
                        {
                            measuredData.RemoveAt(0);
                            calculatedData.RemoveAt(0);
                            scaledThickness.RemoveAt(0);
                            startFitTime++;
                        }

                        if ((timeCount-IgnoreBefore) % FitEvery == 0 && timeCount > IgnoreBefore)
                        {
                            mp_result result = new mp_result(5);
                            var p = new[] { TOffset, TGain, YOffset, YGain, YSlope };
                            MPFit.Solve(GetModelData, measuredData.Count, 5, p, null, null, null, ref result);
                        }


                    }

                    Dispatcher.Invoke(new Action(() =>
                    {
                        myChart.AddDataPoint(dataIndex, intensity, 0);
                        myChart.ClearGraph(false, 0);
                        //if (timeCount >= IgnoreBefore)
                        {
                            //myChart.AddDataPoint(dataIndex, scaledIntensity, 1);
                            myChart.AddDataPoint(dataIndex, newIntensity, 1);
                            myChart.ClearGraph(false, 1);
                        }
                    }));

                    if (++dataIndex >= intensityData.Count)
                        working = false;
                }
            }
        }

        #region WindowDrag

        //Attach this to the MouseDown event of your drag control to move the window in place of the title bar
        private void WindowDrag(object sender, MouseButtonEventArgs e) // MouseDown
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, 0xA1, (IntPtr) 0x2, (IntPtr) 0);
        }

        //Attach this to the PreviewMousLeftButtonDown event of the grip control in the lower right corner of the form to resize the window
        private void WindowResize(object sender, MouseButtonEventArgs e) //PreviewMousLeftButtonDown
        {
            HwndSource hwndSource = PresentationSource.FromVisual((Visual) sender) as HwndSource;
            SendMessage(hwndSource.Handle, 0x112, (IntPtr) 61448, IntPtr.Zero);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition((IInputElement) sender);
            if (e.LeftButton == MouseButtonState.Pressed && pt.Y <= 40)
            {
                DragMove();
            }
        }

        #endregion

        #region WindowSize

        private void btnMinimise_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMaximise_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                imgMaximised.Source = new BitmapImage(new Uri(@"/Resources/Restore.png", UriKind.Relative));
                MaximizeRestoreStr = "Restore down";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                imgMaximised.Source = new BitmapImage(new Uri(@"/Resources/Maximise.png", UriKind.Relative));
                MaximizeRestoreStr = "Maximise";
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            //udp.Stop();
            Application.Current.Shutdown();
        }

        #endregion

        private void btnLoadMeasured_Click(object sender, RoutedEventArgs e)
        {
            string Filename = "";
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Model";
            dlg.DefaultExt = ".dat"; // Default file extension 
            dlg.Filter = "Dat documents (.dat)|*.dat"; // Filter files by extension 

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                FilenameMeasured = dlg.FileName;
                LoadData(FilenameMeasured, ref intensityData, 0, 650);
            }
        }
        
        private void btnLoadModel_Click(object sender, RoutedEventArgs e)
        {
            string Filename = "";
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Model";
            dlg.DefaultExt = ".dat"; // Default file extension 
            dlg.Filter = "Dat documents (.dat)|*.dat"; // Filter files by extension 

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                FilenameModel = dlg.FileName; 
                LoadData(FilenameModel, ref modelData, 1);
            }
        }

        private void LoadData(string filename, ref List<double> data, int series, int divisor=1)
        {
            var lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                if (double.TryParse(line, out var value))
                    data.Add(value / divisor);
            }

            dataIndex = 0;
            myChart.ClearGraph(true, series);
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.Content.ToString() == "Start")
            {
                btn.Content = "Stop";
                dataIndex = 0;
                timeCount = 0;
                errorData.Clear();
                measuredData.Clear();
                calculatedData.Clear();
                scaledThickness.Clear();
                startFitTime = IgnoreBefore;
                myChart.ClearGraph(true, 0);
                myChart.ClearGraph(true, 1);
                working = true;
            }
            else
            {
                btn.Content = "Start";
                working = false;
            }
        }
    }
}