using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OminousMenu
{
    public partial class MainWindow : Window
    {
        private readonly Random _r = new Random();
        private readonly Ash[] _pts;
        private int _activeAshCount = 0;
        private DrawingGroup _dg = new DrawingGroup();
        private DrawingImage _di;

        private readonly Stopwatch _sw = new Stopwatch();
        private double _lastTime;
        private double _lastSpawnTime = 0;

        private const int MaxAshCount = 120;

        private bool _skipCredits = false;
        private bool _fastCredits = false;
        private bool _isRendering = false;

        private readonly SolidColorBrush[] _ashBrushes;

        public MainWindow()
        {
            InitializeComponent();
            _di = new DrawingImage(_dg);
            AshImg.Source = _di;
            RenderOptions.SetEdgeMode(_dg, EdgeMode.Aliased);

            _ashBrushes = new SolidColorBrush[15];
            for (int i = 0; i < _ashBrushes.Length; i++)
            {
                SolidColorBrush b = new SolidColorBrush(Color.FromArgb((byte)(L(0.1, 0.45) * 255), 230, 228, 225));
                b.Freeze();
                _ashBrushes[i] = b;
            }

            _pts = new Ash[MaxAshCount];
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout();

            GameTitle.Text = "The Return of Damareen";

            _activeAshCount = 0;
            _sw.Start();
            _lastTime = _sw.Elapsed.TotalSeconds;

            if (!_isRendering)
            {
                CompositionTarget.Rendering += OnRenderFrame;
                _isRendering = true;
            }

            await ((Storyboard)Resources["Intro"]).BeginAsync(this);
        }

        private void SpawnSingleAsh()
        {
            if (_activeAshCount >= MaxAshCount) return;

            double h = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;
            double sz = L(1.2, 3.8);

            _pts[_activeAshCount].Brush = _ashBrushes[_r.Next(_ashBrushes.Length)];
            _pts[_activeAshCount].Radius = sz / 2.0;
            _pts[_activeAshCount].X = -20;
            _pts[_activeAshCount].Y = L(0, h);
            _pts[_activeAshCount].TargetSpeed = L(350, 750);
            _pts[_activeAshCount].Speed = _pts[_activeAshCount].TargetSpeed * 0.5;
            _pts[_activeAshCount].Drift = L(-5, 10);
            _pts[_activeAshCount].PhaseOffset = _r.NextDouble() * Math.PI * 2;
            _pts[_activeAshCount].WaveSpeed = L(1.0, 3.0);
            _pts[_activeAshCount].WaveHeight = L(10, 30);

            _activeAshCount++;
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            double now = _sw.Elapsed.TotalSeconds;
            double dt = now - _lastTime;
            _lastTime = now;

            if (dt > 0.05) dt = 0.05;

            double w = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
            double h = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

            if (now > 1.0 && _activeAshCount < MaxAshCount)
            {
                if (now - _lastSpawnTime > 0.02)
                {
                    SpawnSingleAsh();
                    _lastSpawnTime = now;
                }
            }

            using (DrawingContext dc = _dg.Open())
            {
                for (int i = 0; i < _activeAshCount; i++)
                {
                    if (now > 1.0)
                    {
                        if (_pts[i].Speed < _pts[i].TargetSpeed)
                        {
                            _pts[i].Speed += 300 * dt;
                        }
                    }

                    _pts[i].X += _pts[i].Speed * dt;

                    double waveOffset = Math.Sin((now * _pts[i].WaveSpeed) + _pts[i].PhaseOffset) * _pts[i].WaveHeight;
                    double currentY = _pts[i].Y + (_pts[i].Drift * now) + waveOffset;

                    if (_pts[i].X > w + 20)
                    {
                        _pts[i].X = -20;
                        _pts[i].Y = L(0, h);
                    }

                    dc.DrawEllipse(_pts[i].Brush, null, new Point(_pts[i].X, currentY), _pts[i].Radius, _pts[i].Radius);
                }
            }
        }

        private async void Credits_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            _skipCredits = false;
            _fastCredits = false;

            this.Focus();
            this.KeyDown += Credits_KeyDown;
            this.KeyUp += Credits_KeyUp;

            DoubleAnimation da = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(2));
            da.Completed += (s, ev) => OutCover.Opacity = 1;
            OutCover.BeginAnimation(UIElement.OpacityProperty, da);

            await Task.Delay(2500);

            Grid creditsGrid = new Grid { Background = Brushes.Transparent };
            Grid.SetRowSpan(creditsGrid, 10);
            Grid.SetColumnSpan(creditsGrid, 10);
            Panel.SetZIndex(creditsGrid, 9999);
            ((Grid)this.Content).Children.Add(creditsGrid);

            string[,] shorts = {
                { "Rendező", "Bálint Vince" },
                { "Vezető Programozó", "Rezák Kevin" },
                { "Hangmérnök", "Bálint Vince" },
                { "Művészeti Vezető", "Rezák Kevin" },
                { "Producer", "Bálint Vince" }
            };

            for (int i = 0; i < shorts.GetLength(0); i++)
            {
                if (_skipCredits) break;
                await ShowShort(creditsGrid, shorts[i, 0], shorts[i, 1]);
            }

            if (!_skipCredits)
            {
                await ShowLong(creditsGrid);
            }

            OutCover.BeginAnimation(UIElement.OpacityProperty, null);
            OutCover.Opacity = 0;

            ((Grid)this.Content).Children.Remove(creditsGrid);

            this.KeyDown -= Credits_KeyDown;
            this.KeyUp -= Credits_KeyUp;

            ResetState();

            if (!_isRendering)
            {
                CompositionTarget.Rendering += OnRenderFrame;
                _isRendering = true;
            }

            await ((Storyboard)Resources["Intro"]).BeginAsync(this);
            IsEnabled = true;
        }

        private async Task ShowShort(Grid cg, string title, string name)
        {
            StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0 };
            sp.Children.Add(new TextBlock { Text = title, Foreground = Brushes.LightGray, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10), FontFamily = new FontFamily("Palatino Linotype") });
            sp.Children.Add(new TextBlock { Text = name, Foreground = Brushes.White, FontSize = 40, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Palatino Linotype") });
            cg.Children.Add(sp);

            double sec = 1.0;
            double actSec = _fastCredits ? sec / 3.0 : sec;

            DoubleAnimation da = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(actSec));
            da.Completed += (s, e) => sp.Opacity = 1;
            sp.BeginAnimation(UIElement.OpacityProperty, da);

            await MyDelay(1.0);

            if (_skipCredits)
            {
                cg.Children.Remove(sp);
                return;
            }

            await MyDelay(1.5);

            if (_skipCredits)
            {
                cg.Children.Remove(sp);
                return;
            }

            actSec = _fastCredits ? sec / 3.0 : sec;
            DoubleAnimation daOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(actSec));
            daOut.Completed += (s, e) => sp.Opacity = 0;
            sp.BeginAnimation(UIElement.OpacityProperty, daOut);

            await MyDelay(1.0);
            cg.Children.Remove(sp);
        }

        private async Task ShowLong(Grid cg)
        {
            Canvas c = new Canvas();
            cg.Children.Add(c);

            StackPanel sp = new StackPanel { Width = this.ActualWidth > 0 ? this.ActualWidth : SystemParameters.PrimaryScreenWidth };
            Canvas.SetTop(sp, this.ActualHeight > 0 ? this.ActualHeight : SystemParameters.PrimaryScreenHeight);

            Action<string, int, Brush, Thickness> AddTitle = (txt, fs, b, m) =>
            {
                sp.Children.Add(new TextBlock { Text = txt, FontSize = fs, Foreground = b, Margin = m, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Palatino Linotype") });
            };

            AddTitle("Bálint Vince", 40, Brushes.White, new Thickness(0, 0, 0, 20));
            string[] bRoles = { "Támadó Biztonsági Főigazgató (COSO)", "Vezető Etikus Hacker és Behatolásvizsgáló", "Visual Studio Enterprise Munkafolyamat-Architekt", "Full-Stack Hibakeresési Maestro", "Neurális Hálózati Sebezhetőség-Kutató", "Haladó Titkosítási Szabvány Szakértő", "Kiberfenyegetettségi Elemző és Stratéga", "Kritikus Infrastruktúra-védelmi Főtanácsadó", "Kvantum-rezisztens Algoritmus-tervező", "Digitális Kártevő-izolációs Specialista", "Vezeték Nélküli Adatátviteli Biztonsági Felügyelő", "Kormányzati Szintű Rendszer-auditőr" };
            foreach (var r in bRoles) AddTitle(r, 24, Brushes.LightGray, new Thickness(0, 0, 0, 5));

            AddTitle(" ", 40, Brushes.Transparent, new Thickness(0, 50, 0, 50));

            AddTitle("Rezák Kevin", 40, Brushes.White, new Thickness(0, 0, 0, 20));
            string[] rRoles = { "Digitális Törvényszéki Főarchitekt (CDFA)", "Senior Szoftver-törvényszéki Szakértő", "Adatstruktúra Optimalizálási Guru", "Futásidejű Biztonsági és Rendszermag-Auditor", "Kódminőségi Főfelügyelő (CQO)", "Alacsony Szintű Memória-integritás Analitikus", "Osztott Rendszerek Védelmi Stratégája", "Katasztrófaelhárítási Adatvisszaállítási Mérnök", "Bináris Kód-visszafejtési Főszakértő", "Vállalati Adatvagyon-védelmi Főbiztos", "Blokklánc-alapú Integritás-ellenőr", "Rendszerlogikai Ellentmondás-feloldó Specialist" };
            foreach (var r in rRoles) AddTitle(r, 24, Brushes.LightGray, new Thickness(0, 0, 0, 5));

            AddTitle(" ", 40, Brushes.Transparent, new Thickness(0, 200, 0, 0));
            c.Children.Add(sp);

            sp.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double h = sp.DesiredSize.Height;
            if (h < 100) h = 3000;

            double y = this.ActualHeight > 0 ? this.ActualHeight : SystemParameters.PrimaryScreenHeight;

            Stopwatch creditSw = Stopwatch.StartNew();
            double lastCreditTime = creditSw.Elapsed.TotalSeconds;

            while (y > -h && !_skipCredits)
            {
                double now = creditSw.Elapsed.TotalSeconds;
                double dt = now - lastCreditTime;
                lastCreditTime = now;

                if (dt > 0.1) dt = 0.1;

                y -= (_fastCredits ? 600 : 120) * dt;
                Canvas.SetTop(sp, y);
                await Task.Delay(1);
            }

            creditSw.Stop();
            cg.Children.Remove(c);
        }

        private async Task MyDelay(double sec)
        {
            double s = 0;
            while (s < sec && !_skipCredits)
            {
                await Task.Delay(50);
                s += _fastCredits ? 0.15 : 0.05;
            }
        }

        private void Credits_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) _skipCredits = true;
            else if (e.Key == Key.Space) _fastCredits = true;
        }

        private void Credits_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) _fastCredits = false;
        }

        private void ResetState()
        {
            AshMaskStop1.BeginAnimation(GradientStop.OffsetProperty, null);
            AshMaskStop1.Offset = -0.3;
            AshMaskStop2.BeginAnimation(GradientStop.OffsetProperty, null);
            AshMaskStop2.Offset = 0.0;
            TitleMaskStop1.BeginAnimation(GradientStop.OffsetProperty, null);
            TitleMaskStop1.Offset = -0.3;
            TitleMaskStop2.BeginAnimation(GradientStop.OffsetProperty, null);
            TitleMaskStop2.Offset = 0.0;
            AshMaskTop.BeginAnimation(GradientStop.OffsetProperty, null);
            AshMaskTop.Offset = 0.4;
            AshMaskBottom.BeginAnimation(GradientStop.OffsetProperty, null);
            AshMaskBottom.Offset = 0.6;

            Cover.BeginAnimation(UIElement.OpacityProperty, null);
            Cover.Opacity = 1.0;
            Menu.BeginAnimation(UIElement.OpacityProperty, null);
            Menu.Opacity = 0.0;
            OutCover.BeginAnimation(UIElement.OpacityProperty, null);
            OutCover.Opacity = 0.0;

            TitlePos.BeginAnimation(TranslateTransform.YProperty, null);
            TitlePos.Y = 0.0;
            MTx.BeginAnimation(TranslateTransform.XProperty, null);
            MTx.X = -60.0;

            TitleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            TitleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            TitleScale.ScaleX = 1.1;
            TitleScale.ScaleY = 1.1;
        }

        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            CompositionTarget.Rendering -= OnRenderFrame;
            _isRendering = false;
            await ((Storyboard)Resources["Outro"]).BeginAsync(this);
            Application.Current.Shutdown();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Játék elindítása...");
        }

        private double L(double a, double b)
        {
            return a + (b - a) * _r.NextDouble();
        }

        private struct Ash
        {
            public Brush Brush;
            public double Radius;
            public double X;
            public double Y;
            public double Speed;
            public double TargetSpeed;
            public double Drift;
            public double PhaseOffset;
            public double WaveSpeed;
            public double WaveHeight;
        }
    }
}