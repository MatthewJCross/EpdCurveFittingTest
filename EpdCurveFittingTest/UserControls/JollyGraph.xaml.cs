using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EpdCurveFittingTest.UserControls
{
    /// <summary>
    /// Interaction logic for JollyGraph.xaml
    /// </summary>
    public partial class JollyGraph : UserControl, INotifyPropertyChanged
    {
        #region Bindings
        private string _graphTitle = "My Graph";
        public string GraphTitle
        {
            get
            {
                return _graphTitle;
            }
            set
            {
                _graphTitle = value;
                OnPropertyChanged("GraphTitle");
            }
        }

        private string _yLabel = "y-axis";
        public string YLabel
        {
            get
            {
                return _yLabel;
            }
            set
            {
                _yLabel = value;
                OnPropertyChanged("YLabel");
            }
        }

        private string _xLabel = "x-axis";
        public string XLabel
        {
            get
            {
                return _xLabel;
            }
            set
            {
                _xLabel = value;
                OnPropertyChanged("XLabel");
            }
        }

        private int _xMajorTick;
        public int XMajorTick
        {
            get
            {
                return _xMajorTick;
            }
            set
            {
                _xMajorTick = value;
                OnPropertyChanged("XMajorTick");
            }
        }

        private int _xMinorTick;
        public int XMinorTick
        {
            get
            {
                return _xMinorTick;
            }
            set
            {
                if (value <= (XMajorTick / 2))
                    _xMinorTick = value;
                else
                    _xMinorTick = 0;
                OnPropertyChanged("XMinorTick");
            }
        }

        private int _yMajorTick;
        public int YMajorTick
        {
            get
            {
                return _yMajorTick;
            }
            set
            {
                _yMajorTick = value;
                OnPropertyChanged("YMajorTick");
            }
        }

        private int _yMinorTick;
        public int YMinorTick
        {
            get
            {
                return _yMinorTick;
            }
            set
            {
                if (value <= (YMajorTick / 2))
                    _yMinorTick = value;
                else
                    _yMinorTick = 0;
                OnPropertyChanged("YMinorTick");
            }
        }

        private double _xMax = 100;
        public double XMax
        {
            get
            {
                return _xMax;
            }
            set
            {
                _xMax = value;
                OnPropertyChanged("XMax");
            }
        }

        private double _xMin = 0;
        public double XMin
        {
            get
            {
                return _xMin;
            }
            set
            {
                _xMin = value;
                OnPropertyChanged("XMin");
            }
        }

        private double _yMax = 100;
        public double YMax
        {
            get
            {
                return _yMax;
            }
            set
            {
                _yMax = value;
                OnPropertyChanged("YMax");
            }
        }

        private double _yMin = 0;
        public double YMin
        {
            get
            {
                return _yMin;
            }
            set
            {
                _yMin = value;
                OnPropertyChanged("YMin");
            }
        }

        private double _plotWidth;
        public double PlotWidth
        {
            get
            {
                return _plotWidth;
            }
            set
            {
                _plotWidth = value;
                OnPropertyChanged("PlotWidth");
            }
        }

        private double _plotHeight;
        public double PlotHeight
        {
            get
            {
                return _plotHeight;
            }
            set
            {
                _plotHeight = value;
                OnPropertyChanged("PlotHeight");
            }
        }

        private double _minZoom = 1;
        public double MinZoom
        {
            get
            {
                return _minZoom;
            }
            set
            {
                _minZoom = value;
                OnPropertyChanged("MinZoom");
            }
        }

        private double _maxZoom = 5;
        public double MaxZoom
        {
            get
            {
                return _maxZoom;
            }
            set
            {
                _maxZoom = value;
                OnPropertyChanged("MaxZoom");
            }
        }

        private double _zoom;
        public double Zoom
        {
            get
            {
                return _zoom;
            }
            set
            {
                _zoom = value;
                OnPropertyChanged("Zoom");
            }
        }

        private bool _showToolTip = false;
        public bool ShowToolTip
        {
            get
            {
                return _showToolTip;
            }
            set
            {
                _showToolTip = value;
                OnPropertyChanged("ShowToolTip");
            }
        }

        private bool _showGridlines = false;
        public bool ShowGridlines
        {
            get
            {
                return _showGridlines;
            }
            set
            {
                _showGridlines = value;
                OnPropertyChanged("ShowGridlines");
            }
        }
        
        private int _borderSize = 7;
        public int BorderSize
        {
            get
            {
                return _borderSize;
            }
            set
            {
                _borderSize = value;
                OnPropertyChanged("BorderSize");
            }
        }

        private Color _graphOuterColor = Colors.Bisque;
        public Color GraphOuterColor
        {
            get
            {
                return _graphOuterColor;
            }
            set
            {
                _graphOuterColor = value;
                OnPropertyChanged("GraphOuterColor");
            }
        }

        private Color _graphBackgroundrColor = Colors.Bisque;
        public Color GraphBackgroundrColor
        {
            get
            {
                return _graphBackgroundrColor;
            }
            set
            {
                _graphBackgroundrColor = value;
                OnPropertyChanged("GraphBackgroundrColor");
            }
        }

        private Color _graphGridlineColor = Colors.LightGray;
        public Color GraphGridlineColor
        {
            get
            {
                return _graphGridlineColor;
            }
            set
            {
                _graphGridlineColor = value;
                OnPropertyChanged("GraphGridlineColor");
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

        private const int MAX_SERIES = 10;
        private Brush[] lineBrushes = new Brush[MAX_SERIES] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Magenta, Brushes.Yellow, Brushes.RosyBrown, Brushes.DodgerBlue, Brushes.Teal, Brushes.Gold, Brushes.SandyBrown };

        public LineSeries[] ls = new LineSeries[MAX_SERIES];
        
        public JollyGraph()
        {
            DataContext = this;
            InitializeComponent();

            for (int i = 0; i < MAX_SERIES; i++)
            {
                ls[i] = new LineSeries();
            }

            XMajorTick = 10;
            XMinorTick = 0;
            YMajorTick = 10;
            YMinorTick = 0;

            Zoom = 1;
        }

        public void ClearGraph(bool clearData, int series = 0)
        {
            if (clearData)
                ls[series].GraphData.Clear();
            UpdateScreen();
        }

        public void AddDataPoint(double x, double y, int series = 0)
        {
            AddDataPoint(new Point(x, y), series);
        }

        public void AddDataPoint(Point pt, int series = 0)
        {
            if (pt.X > XMax)
                XMax = (int)pt.X;

            if (pt.Y < YMin)
                YMin = (int)pt.Y;

            if (pt.Y > YMax)
                YMax = (int)pt.Y;

            ls[series].GraphData.Add(pt);
        }

        public void SetSeriesBrush(int series, Brush b)
        {
            if (series < 0 || series >= MAX_SERIES)
                return;

            lineBrushes[series] = b;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            theCanvas.Children.Clear();

            Line x = new Line();
            x.Stroke = System.Windows.Media.Brushes.Black; ;
            x.StrokeThickness = 1;
            x.X1 = 0;
            x.Y1 = x.Y2 = theCanvas.ActualHeight;
            x.X2 = theCanvas.ActualWidth;

            Line y = new Line();
            y.Stroke = System.Windows.Media.Brushes.Black; ;
            y.StrokeThickness = 1;
            y.X1 = y.Y1 = y.X2 = 0;
            y.Y2 = theCanvas.ActualHeight;

            theCanvas.Children.Add(x);
            theCanvas.Children.Add(y);

            double tickGap = theCanvas.ActualWidth / (XMax / XMajorTick);
            double minorTickGap = 0;
            if (XMinorTick > 0)
                minorTickGap = tickGap / (XMajorTick / XMinorTick);
            Line tick;
            TextBlock tb;
            for (int i = 0; i < (XMax / XMajorTick); i++) 
            {
                if (_showGridlines && XMinorTick > 0)
                {
                    for (int ii = 0; ii < (XMajorTick / XMinorTick); ii++)
                    {
                        tick = new Line();
                        tick.Stroke = System.Windows.Media.Brushes.Black;
                        tick.StrokeThickness = 1;
                        tick.X1 = tick.X2 = (i * tickGap) + (ii * minorTickGap);
                        tick.Y1 = theCanvas.ActualHeight - 3;
                        tick.Y2 = theCanvas.ActualHeight;
                        theCanvas.Children.Add(tick);
                    }
                }

                tick = new Line();
                tick.Stroke = System.Windows.Media.Brushes.Black; 
                tick.StrokeThickness = 1;
                tick.X1 = tick.X2 = (i + 1) * tickGap;
                tick.Y1 = ((i + 1) % 10) == 0 ? theCanvas.ActualHeight - 10 : theCanvas.ActualHeight - 6;
                tick.Y2 = theCanvas.ActualHeight;
                theCanvas.Children.Add(tick);
                tb = new TextBlock();
                tb.Text = ((i + 1) * XMajorTick).ToString("G8");
                theCanvas.Children.Add(tb);
                int xx = (tb.Text.Length * 8) / 2;
                Canvas.SetTop(tb, theCanvas.ActualHeight);
                Canvas.SetLeft(tb, tick.X1 - xx);

                if (_showGridlines)
                {
                    tick = new Line();
                    tick.Stroke = new SolidColorBrush(_graphGridlineColor);
                    tick.StrokeThickness = 1;
                    tick.X1 = tick.X2 = (i + 1) * tickGap;
                    tick.Y1 = 0;
                    tick.Y2 = theCanvas.ActualHeight - ((i + 1) % 10) == 0 ? theCanvas.ActualHeight - 10 : theCanvas.ActualHeight - 5; ;
                    theCanvas.Children.Add(tick);
                }
            }

            int yRange = (int)(Math.Abs(YMin) + YMax);
            int noYTicks = yRange / YMajorTick;
            int a = yRange / YMajorTick;
            tickGap = theCanvas.ActualHeight / ((double)yRange / YMajorTick);
            if (YMinorTick > 0)
                minorTickGap = tickGap / (YMajorTick / YMinorTick);

            for (int i = 1; i <= noYTicks+1; i++)
            {
                tick = new Line();
                tick.Stroke = System.Windows.Media.Brushes.Black; ;
                tick.StrokeThickness = 1;
                tick.X1 = 0;
                tick.X2 = 10;
                tick.Y1 = tick.Y2 = theCanvas.ActualHeight - (i * tickGap);
                theCanvas.Children.Add(tick);
                tb = new TextBlock();
                int offset = yRange % YMajorTick;
                int val = (int)((YMax-offset) - ((noYTicks-i) * YMajorTick));
                tb.Text = val.ToString();
                theCanvas.Children.Add(tb);
                Canvas.SetTop(tb, tick.Y1 - 8);
                Canvas.SetLeft(tb, -(tb.Text.Length * 8));

                if (_showGridlines)
                {
                    tick = new Line();
                    tick.Stroke = new SolidColorBrush(_graphGridlineColor);
                    tick.StrokeThickness = 1;
                    tick.X1 = 10;
                    tick.X2 = theCanvas.ActualWidth;
                    tick.Y1 = tick.Y2 = theCanvas.ActualHeight - (i * tickGap);
                    theCanvas.Children.Add(tick);

                    double y1 = tick.Y1;
                    if (YMinorTick > 0)
                    {
                        for (int ii = 0; ii < (YMajorTick / YMinorTick); ii++)
                        {
                            tick = new Line();
                            tick.Stroke = System.Windows.Media.Brushes.Black;
                            tick.StrokeThickness = 1;
                            tick.X1 = 0;
                            tick.X2 = 5;
                            tick.Y1 = tick.Y2 = y1 + (ii * minorTickGap);
                            if (tick.Y1 >= 0)
                                theCanvas.Children.Add(tick);
                        }
                    }
                }
            }

            for (int j = 0; j < MAX_SERIES; j++)
            {
                DrawSeries(ls[j], yRange, lineBrushes[j]);
            }

            tb = new TextBlock();
            tb.Text = GraphTitle;
            tb.FontWeight = FontWeights.ExtraBold;
            tb.FontSize = 16;
            theCanvas.Children.Add(tb);
            Canvas.SetTop(tb, -24);
            Canvas.SetLeft(tb, (theCanvas.ActualWidth / 2) - (MeasureStringWidth(tb) / 2));

            tb = new TextBlock();
            tb.Text = XLabel;
            tb.FontWeight = FontWeights.Bold;
            theCanvas.Children.Add(tb);
            Canvas.SetTop(tb, theCanvas.ActualHeight + 12);
            Canvas.SetLeft(tb, (theCanvas.ActualWidth / 2) - (MeasureStringWidth(tb) / 2));

            tb = new TextBlock();
            tb.Text = YLabel;
            tb.FontWeight = FontWeights.Bold;
            tb.LayoutTransform = new RotateTransform(-90);
            theCanvas.Children.Add(tb);
            Canvas.SetTop(tb, (theCanvas.ActualHeight / 2) - (MeasureStringWidth(tb) / 2));
            Canvas.SetLeft(tb, -30);
        }

        private void DrawSeries(LineSeries ls, double yRange, Brush b)
        {
            double xUnit = theCanvas.ActualWidth / XMax;
            double yUnit = theCanvas.ActualHeight / yRange;

            Polyline pl = new Polyline();
            pl.StrokeThickness = 1;
            pl.Stroke = b;

            foreach (Point pt in ls.GraphData)
            {
                Point newPt = pt;
                newPt.X = pt.X * xUnit;
                newPt.Y = theCanvas.ActualHeight - ((pt.Y - YMin) * yUnit);
                pl.Points.Add(newPt);
            }
            theCanvas.Children.Add(pl);
        }

        private int MeasureStringWidth(TextBlock tb)
        {
            var formattedText = new FormattedText(tb.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch), tb.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            return (int)formattedText.Width;
        }

        Point popUpPt;
        double startLeft;
        double startTop;
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_showToolTip || popUpToolTip.IsOpen)
            {
                popUpToolTip.IsOpen = false;
                return;
            }

            popUpPt = e.GetPosition((IInputElement)sender);
            popUpToolTip.HorizontalOffset = 10;
            popUpToolTip.VerticalOffset = 10;
            popUpToolTip.IsOpen = true;

            var canvas = sender as Canvas;
            if (canvas == null)
                return;

            startLeft = popUpPt.X;
            startTop = popUpPt.Y;

            double xVal = startLeft / (theCanvas.ActualWidth / XMax);
            double yVal = (theCanvas.ActualHeight - (startTop - YMin)) / (theCanvas.ActualHeight / YMax);
            textBlock.Text = "X-Value: " + String.Format("{0:.##}", xVal) + "\n" + "Y-Value: " + String.Format("{0:.##}", yVal);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            popUpToolTip.IsOpen = false;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (popUpToolTip.IsOpen == true)
            {
                Point position = e.GetPosition((IInputElement)sender);
                double left = position.X;// - xOffset;
                double top = position.Y;// - yOffset;
                double xVal = left / (theCanvas.ActualWidth / XMax);
                double yVal = (theCanvas.ActualHeight - (top - YMin)) / (theCanvas.ActualHeight / YMax);

                popUpToolTip.HorizontalOffset = position.X - popUpPt.X + 10;
                popUpToolTip.VerticalOffset = position.Y - popUpPt.Y + 10;
                textBlock.Text = "X-Value: " + String.Format("{0:.##}", xVal) + "\n" + "Y-Value: " + String.Format("{0:.##}", yVal);
            }
        }
    }

    public class DataPoint : INotifyPropertyChanged
    {
        private double _frequency;
        public double Frequency
        {
            get
            {
                return _frequency;
            }
            set
            {
                _frequency = value;
                OnPropertyChanged("Frequency");
            }
        }

        private double _value = new double();
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LineSeries : INotifyPropertyChanged
    {
        private ObservableCollection<Point> _graphData = new ObservableCollection<Point>();
        public ObservableCollection<Point> GraphData
        {
            get
            {
                return _graphData;
            }
            set
            {
                _graphData = value;
                OnPropertyChanged("GraphData");
            }
        }

        private string _name = "";
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
