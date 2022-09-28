using EpdCurveFittingTest.LMSolver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private const string _unitType = "OceanOptics";

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

        private string _filenameModel = "ModelDataSiO2(low).dat";

        public string FilenameModel
        {
            get => _filenameModel; 
            set
            {
                _filenameModel = value;
                OnPropertyChanged("FilenameModel");
            }
        }

        private string _filenameMeasured = "MeasuredDataSiO2(low).dat";

        public string FilenameMeasured
        {
            get => _filenameMeasured; 
            set
            {
                _filenameMeasured = value;
                OnPropertyChanged("FilenameMeasured");
            }
        }

        private double _tOffset = 0;

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

        private double _tGain = 0; //9.34162529713385;
        public double TGain
        {
            get => _tGain;
            set
            {
                _tGain = Clamp(value, 0, 25);
                OnPropertyChanged("TGain");
            }
        }

        private double _yOffset = 0; //8043.10597079514 / 650.0;
        public double YOffset
        {
            get => _yOffset;
            set
            {
                _yOffset = Clamp(value, -100.0, 100.0);
                OnPropertyChanged("YOffset");
            }
        }

        private double _yGain = 0.0;
        public double YGain
        {
            get => _yGain;
            set
            {
                _yGain = Clamp(value, 0.0, 1.0);
                OnPropertyChanged("YGain");
            }
        }

        private double _calculatedYOffset = 0.0;
        public double CalculatedYOffset
        {
            get => _calculatedYOffset;
            set
            {
                _calculatedYOffset = Clamp(value, -100.0, 100.0);
                OnPropertyChanged("CalculatedYOffset");
            }
        }

        private double _calculatedYGain = 1.0;
        public double CalculatedYGain
        {
            get => _calculatedYGain;
            set
            {
                _calculatedYGain = Clamp(value, 0.0, 1.0);
                OnPropertyChanged("CalculatedYGain");
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

        private double _requiredThickness = 1000.0;
        public double RequiredThickness
        {
            get => _requiredThickness;
            set
            {
                _requiredThickness = value;
                OnPropertyChanged("RequiredThickness");
            }
        }

        private double _expectedDepthRate = 560.497517828031;
        public double ExpectedDepthRate
        {
            get => _expectedDepthRate;
            set
            {
                _expectedDepthRate = value;
                OnPropertyChanged("ExpectedDepthRate");
            }
        }

        private double _depthRate = 0.0;
        public double DepthRate
        {
            get => _depthRate;
            set
            {
                _depthRate = value;
                OnPropertyChanged("DepthRate");
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

        private bool _replotAll = false;
        public bool ReplotAll
        {
            get => _replotAll;
            set
            {
                _replotAll = value;
                OnPropertyChanged("ReplotAll");
            }
        }

        private bool _keepFitParams = false;
        public bool KeepFitParams
        {
            get => _keepFitParams;
            set
            {
                _keepFitParams = value;
                OnPropertyChanged("KeepFitParams");
            }
        }

        private double _rmsError = 0.0;
        public double RMSError
        {
            get => _rmsError;
            set
            {
                _rmsError = value;
                OnPropertyChanged("RMSError");
            }
        }

        private double _rmsErrorAt = 100.0;
        public double RMSErrorAt
        {
            get => _rmsErrorAt;
            set
            {
                _rmsErrorAt = value;
                OnPropertyChanged("RMSErrorAt");
            }
        }

        private double _bestNorm = 0.0;
        public double BestNorm
        {
            get => _bestNorm;
            set
            {
                _bestNorm = value;
                OnPropertyChanged("BestNorm");
            }
        }

        private bool _useSuggestion1a = true;
        public bool UseSuggestion1a
        {
            get => _useSuggestion1a;
            set
            {
                _useSuggestion1a = value;
                LoadModel(FilenameModel);
                LoadActualData(FilenameMeasured);
                OnPropertyChanged("UseSuggestion1a");
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

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static double Clamp(double value, double min, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private class ModelPoint
        {
            public ulong Thickness { get; set; }
            public double Intensity { get; set; }
        }

        private class DataPoint
        {
            public double Time { get; set; }
            public double Intensity { get; set; }
        }
        
        BackgroundWorker backgroundWorker;
        private bool working = false;

        private readonly List<ModelPoint> _modelpoints = new List<ModelPoint>();

        private List<double> _intensityData = new List<double>();
        private List<double> _modelData = new List<double>();
        private List<DataPoint> _measuredData = new List<DataPoint>();
        private int dataIndex = 0;
        private int timeCount;

        private double _averageModelIntensity = 0.0;
        private double _minModelIntensity = 65000.0;
        private double _maxModelIntensity = 0.0;
        private double _minSignalIntensity = 65000.0;
        private double _maxSignalIntensity = 0.0;
        private bool _hadFirstFitting = false;
        private double _averageSignalIntensity = 0.0;
        private int _averageSignalCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Log.Suffix = "EpdCurveFittingTest";
            Log.AddEntry(_unitType, "MPFit ver: {0}", MPFit.MPFIT_VERSION);

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
            myChart.XMax = 240;
            myChart.YMax = 100;
            //myChart.YMin = -50;

            myChart.XMinorTick = 5;
            myChart.YMajorTick = 10;
            myChart.XMajorTick = 15;
            myChart.YMinorTick = 5;

            myChart.GraphOuterColor = Colors.LightPink;
            myChart.ShowGridlines = true;

            myChart.ShowToolTip = true;

            LoadModel(FilenameModel);
            LoadActualData(FilenameMeasured);
        }

        private readonly List<DataPoint> _datapoints = new List<DataPoint>();
        private List<DataPoint> _considerDatapoints = new List<DataPoint>();
        private int GiveModelData(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            for (int i = 0; i < dy.Length; i++)
            {
                // Get point for analysis
                DataPoint data = _considerDatapoints[i];
                double scaledThickness = 0.0;
                double scaledIntensity = CalculateScaledIntensity(data, p, ref scaledThickness);
                if (scaledIntensity == double.PositiveInfinity)
                    dy[i] = double.PositiveInfinity; // Off the end of the model
                else
                {
                    double error = scaledIntensity - data.Intensity;
                    dy[i] = error;
                }
            }

            return 0;
        }

        double scaledThickness = 0.0;
        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            double scaledIntensity = 0;
            var p = new[] { TGain, YOffset * 650.0, YGain * 650.0};
            while (!worker.CancellationPending)
            {
                if (working)
                {
                    Thread.Sleep(SpeedDelay);
                    timeCount++;

                    double intensity = _datapoints[dataIndex].Intensity;
                    _measuredData.Add(new DataPoint { Time = timeCount, Intensity = intensity });
                    Log.AddEntry(_unitType, "Measured Data: intensity({0})", intensity);

                    p[0] = TGain;
                    p[1] = YOffset * 650.0;
                    p[2] = YGain * 650.0;

                    if (timeCount >= IgnoreBefore)
                    {
                        if (UseSuggestion1a)
                        {
                            double scaledWave = intensity / 650.0;

                            _minSignalIntensity = Math.Min(_minSignalIntensity, scaledWave);
                            _maxSignalIntensity = Math.Max(_maxSignalIntensity, scaledWave);

                            if (!_hadFirstFitting)  
                            {
                                _averageSignalIntensity += scaledWave;
                                _averageSignalCount++;
                            }
                        }

                        scaledIntensity = CalculateScaledIntensity(new DataPoint { Time = timeCount, Intensity = intensity }, p, ref scaledThickness);
                        Log.AddEntry(_unitType, "CalculateScaledIntensity: scaledThickness({0}) = (dp.Time({1}) * p[0]({2})) + TOffset({3})", scaledThickness, timeCount, p[0], TOffset);

                        if (timeCount >= IgnoreBefore + FittingRange && timeCount % FitEvery == 0)
                        { // Time has exceeded Ignore before time and time necessary to get the fitting range
                            _considerDatapoints = _measuredData.Skip(Math.Max(0, _measuredData.Count() - FittingRange)).Take(FittingRange).ToList();

                            if (!_hadFirstFitting)
                            {
                                YGain = (_maxSignalIntensity - _minSignalIntensity) / (_maxModelIntensity - _minModelIntensity);
                                YOffset = (_averageSignalIntensity / _averageSignalCount) - (YGain * _averageModelIntensity);
                                CalculatedYGain = YGain;
                                CalculatedYOffset = YOffset;
                                p[0] = ExpectedDepthRate / 60.0;
                                p[1] = YOffset * 650;
                                p[2] = YGain * 650;
                            }
                            else
                                p[0] = TGain;

                            _hadFirstFitting = true;
                            if (_considerDatapoints.Count == FittingRange)
                            { // Just double check we do have FittingRange points in list
                                mp_result result = new mp_result(3);

                                Log.AddEntry(_unitType, "Pre MPFit: _timeCount({0}), TGain({1}), YOffset({2}), YGain({3}), TOffset({4}), _scaledThickness({5}), _scaledIntensity({6}), _hadFirstFitting({7})", timeCount, TGain, YOffset, YGain, TOffset, scaledThickness, scaledIntensity, _hadFirstFitting);

                                var r = MPFit.Solve(GiveModelData, _considerDatapoints.Count, 3, p, null, null, null, ref result);

                                Log.AddEntry(_unitType, "bestnorm {0}, covar {1}, nfev {2}, nfree {3}, nfunc {4}, niter {5}, npar {6}, npegged {7}, orignorm {8}, resid {9}, status {10}, version {11}, xerror[0] {12}, xerror[1] {13}, xerror[2] {14}",
                                    result.bestnorm, result.covar, result.nfev, result.nfree, result.nfunc, result.niter, result.npar, result.npegged, result.orignorm, result.resid, result.status, result.version, result.xerror[0], result.xerror[1], result.xerror);

                                BestNorm = result.bestnorm;
                                RMSError = Math.Sqrt(BestNorm / _considerDatapoints.Count) / 650.0;

                                if (!KeepFitParams)
                                {
                                    TGain = p[0];
                                    YOffset = p[1] / 650.0; 
                                    YGain = p[2] / 650.0;
                                    if (YGain < 0.01)
                                        YGain = 0.01;
                                }

                                Log.AddEntry(_unitType, "Post MPFit: _timeCount({0}), TGain({1}), YOffset({2}), YGain({3}), TOffset({4}), _scaledThickness({5}), _scaledIntensity({6}), _hadFirstFitting({7})", timeCount, TGain, YOffset, YGain, TOffset, scaledThickness, scaledIntensity, _hadFirstFitting);

                                if (ReplotAll)
                                {
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        myChart.ClearGraph(true, 0);
                                        for (int i = 0; i < _measuredData.Count - 1; i++)
                                        {
                                            scaledIntensity = CalculateScaledIntensity(_measuredData[i], p, ref scaledThickness);
                                            myChart.AddDataPoint(i+1, scaledIntensity / 650, 0);
                                            myChart.ClearGraph(false, 0);
                                        }
                                    }));
                                }
                            }
                        }
                    }

                    Thickness = scaledThickness;

                    DepthRate = (TGain * 60);

                    Dispatcher.Invoke(new Action(() =>
                    {
                        // Add measured data to graph
                        myChart.AddDataPoint(dataIndex, intensity / 650, 1);
                        myChart.ClearGraph(false, 1);

                        // Add scaled intensity to graph
                        myChart.AddDataPoint(dataIndex, scaledIntensity / 650, 0);
                        myChart.ClearGraph(false, 0);
                    }));

                    if (++dataIndex >= _datapoints.Count)
                    {
                        working = false;
                    }

                    if (RequiredThickness > 0 && Thickness >= RequiredThickness)
                    {
                        Log.AddEntry(_unitType, "Thickness met: {0}", scaledThickness);
                        MessageBox.Show( "Thickness reached after " + timeCount.ToString() + " secs", "Endpoint!");
                        working = false;
                    }

                    if (RMSErrorAt > 0 && RMSError > RMSErrorAt)
                    {
                        MessageBox.Show("Process stopped, RMS vale too high after " + timeCount.ToString() + " secs", "Endpoint!");
                        working = false;
                    }
                }
            }
        }

        private string ResultToString(int result)
        {
            switch (result)
            {
                case MPFit.MP_OK_CHI: return "Convergence in chi-square value";
                case MPFit.MP_OK_PAR: return "Convergence in parameter value";
                case MPFit.MP_OK_BOTH: return "Both PAR and CHI hold";
                case MPFit.MP_OK_DIR: return "Convergence in orthogonality";
                case MPFit.MP_MAXITER: return "Max iterations reached";
                case MPFit.MP_FTOL: return "ftol is too small";
                case MPFit.MP_XTOL: return "xtol is too small";
                case MPFit.MP_GTOL: return "gtol is too small";
                default: return "Unknown";
            }
        }

        private double CalculateRMS(double[] dy)
        {
            double square = 0;
            double mean = 0;

            // Calculate the Square.
            for (int i = 0; i < dy.Length; i++)
            {
                square += Math.Pow(dy[i], 2);
            }

            // Calculate the Mean.
            mean = square / dy.Length;

            // Calculate and return the Root.
            return Math.Sqrt(mean);
        }

        private double CalculateScaledIntensity(DataPoint dp, double[] p, ref double scaledThickness)
        {
            scaledThickness = (dp.Time * p[0]) + TOffset;
            int st = (int)Math.Floor(scaledThickness);
            double diff = scaledThickness - st;
            if (st >= 0 && st < _modelpoints.Count - 1)
            {
                // Get the points for the model thickness and the next entry so we can do a linear interpolation
                ModelPoint x1 = _modelpoints[st];
                ModelPoint x2 = _modelpoints[st + 1];

                // Perform the adjustment of the intensities using the provided potential fit parameters p
                double x1Intensity = p[1] + (x1.Intensity * p[2]);
                double x2Intensity = p[1] + (x2.Intensity * p[2]);

                // Do the linear interpolation between the intensities and return intensity
                return x1Intensity + diff * (x2Intensity - x1Intensity);
            }

            return double.PositiveInfinity;
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
            working = false;
            Thread.Sleep(100);
            Application.Current.Shutdown();
        }

        #endregion

        private void btnLoadMeasured_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Model";
            dlg.DefaultExt = ".dat"; // Default file extension 
            dlg.Filter = "Dat documents (.dat)|*.dat"; // Filter files by extension 

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                FilenameMeasured = dlg.FileName;
                LoadActualData(FilenameMeasured);
            }
        }
        
        private void btnLoadModel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Model";
            dlg.DefaultExt = ".dat"; // Default file extension 
            dlg.Filter = "Dat documents (.dat)|*.dat"; // Filter files by extension 

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                FilenameModel = dlg.FileName;
                LoadModel(FilenameModel);
            }
        }

        private void LoadModel(string filename)
        {
            _averageModelIntensity = 0.0;
            _minModelIntensity = 65000.0;
            _maxModelIntensity = 0.0;

            if (!File.Exists(filename))
                return;

            Log.AddEntry(_unitType, "Loaded curve model '{0}'", filename);

            _modelpoints.Clear();

            var lines = File.ReadAllLines(filename);
            int points = 0;
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length == 2)
                {
                    if (ulong.TryParse(parts[0], out var thickness) && double.TryParse(parts[1], out var intensity))
                    {
                        _modelpoints.Add(new ModelPoint { Thickness = thickness, Intensity = intensity });
                        _averageModelIntensity += intensity;
                        points++;
                        if (UseSuggestion1a)
                        {
                            _minModelIntensity = Math.Min(_minModelIntensity, intensity);
                            _maxModelIntensity = Math.Max(_maxModelIntensity, intensity);
                        }
                    }
                }
            }

            _averageModelIntensity /= points;
        }

        private void LoadActualData(string filename)
        {
            if (!File.Exists(filename))
                return;

            Log.AddEntry(_unitType, "Loaded measured data '{0}'", filename);

            _datapoints.Clear();

            var lines = File.ReadAllLines(filename);
            int t = 0;
            double intensityCopy = 0;
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out var time) && double.TryParse(parts[1], out var intensity))
                    {
                        intensityCopy = intensity;
                        _datapoints.Add(new DataPoint {Time = time, Intensity = intensity});
                        _intensityData.Add(intensity);
                    }
                }
                else
                {
                    if (double.TryParse(parts[0], out var intensity))
                    {
                        intensityCopy = intensity;
                        _datapoints.Add(new DataPoint { Time = ++t, Intensity = intensity });
                        _intensityData.Add(intensity);
                    }
                }

                if (UseSuggestion1a)
                {
                    _minSignalIntensity = 65000.0; 
                    _maxSignalIntensity = 0.0; 
                }
            }
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.Content.ToString() == "Start")
            {
                btn.Content = "Stop";
                dataIndex = 0;
                timeCount = 0;
                _measuredData.Clear();
                myChart.ClearGraph(true, 0);
                myChart.ClearGraph(true, 1);
                //TOffset = 2;
                TGain = ExpectedDepthRate / 60;
                UseSuggestion1a = false;
                //YOffset = 8043.10597079514 / 650.0;
                //YGain = 29.8905073144862 / 650.0;
                //if (UseSuggestion1a)
                if (YOffset == 0.0 && YGain == 0.0)
                {
                    UseSuggestion1a = true;
                    //YOffset = 0.0;
                    YGain = 1.0;
                    CalculatedYOffset = 0.0;
                    CalculatedYGain = 0.0;
                    _minSignalIntensity = 65000.0;
                    _maxSignalIntensity = 0.0;
                    _averageSignalIntensity = 0.0;
                    _averageSignalCount = 0;
                }
                RMSError = 0;
                BestNorm = 0;
                Thickness = 0;
                working = true;
                _hadFirstFitting = false;
                scaledThickness = 0.0;
            }
            else
            {
                btn.Content = "Start";
                working = false;
            }
        }
    }

    public class IntegerRangeRule : ValidationRule
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public IntegerRangeRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int _value = 0;

            try
            {
                if (((string)value).Length > 0)
                    _value = Int32.Parse((String)value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, $"Illegal characters or {e.Message}");
            }

            if ((_value < Min) || (_value > Max))
            {
                return new ValidationResult(false, $"Please enter an age in the range: {Min}-{Max}.");
            }
            return ValidationResult.ValidResult;
        }
    }

    public class YOffsetValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string text = value.ToString();
            double yOffset;
            double.TryParse(text, out yOffset);
            if (yOffset < -100.0 || yOffset > 100.0)
                return new ValidationResult(false, "Invalid age.");

            return ValidationResult.ValidResult;
        }
    }
}