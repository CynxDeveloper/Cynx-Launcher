using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SusanooLauncher.Controls
{
    public partial class LiveBackground : UserControl
    {
        private readonly DispatcherTimer _timer = new();
        private readonly Random _rng = new();

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LiveBackground),
                new PropertyMetadata(false, OnIsActiveChanged));

        public static readonly DependencyProperty UseSquaresProperty =
            DependencyProperty.Register(nameof(UseSquares), typeof(bool), typeof(LiveBackground),
                new PropertyMetadata(false, OnUseSquaresChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public bool UseSquares
        {
            get => (bool)GetValue(UseSquaresProperty);
            set => SetValue(UseSquaresProperty, value);
        }

        public LiveBackground()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += (_, __) => Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += (_, __) => Tick();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsActive)
                Start();
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LiveBackground bg)
                return;

            if ((bool)e.NewValue)
                bg.Start();
            else
                bg.Stop();
        }

        private static void OnUseSquaresChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LiveBackground bg && bg.IsActive)
                bg.RebuildParticles();
        }

        public void Start()
        {
            Visibility = Visibility.Visible;
            Opacity = 1;
            RebuildParticles();
            if (!_timer.IsEnabled)
                _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            ParticleCanvas.Children.Clear();
        }

        private void RebuildParticles()
        {
            ParticleCanvas.Children.Clear();
            if (!IsActive)
                return;

            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 50 || h < 50)
                return;

            int count = UseSquares ? _rng.Next(3, 6) : _rng.Next(2, 5);

            for (int i = 0; i < count; i++)
                SpawnParticle(enterFromEdge: false);
        }

        private void SpawnParticle(bool enterFromEdge)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 50 || h < 50)
                return;

            bool isSquare = UseSquares;
            double size = isSquare ? _rng.Next(56, 120) : _rng.Next(48, 100);

            Shape shape = isSquare
                ? new Rectangle { RadiusX = 12, RadiusY = 12 }
                : new Ellipse();

            shape.Width = size;
            shape.Height = size;
            shape.Opacity = _rng.NextDouble() * 0.22 + 0.18;
            shape.Fill = new SolidColorBrush(Color.FromArgb(
                (byte)_rng.Next(45, 75),
                (byte)_rng.Next(100, 190),
                (byte)_rng.Next(90, 210),
                (byte)_rng.Next(210, 255)));
            shape.Effect = new BlurEffect { Radius = 16 };

            double speed = _rng.NextDouble() * 0.9 + 0.6;
            double angle = _rng.NextDouble() * Math.PI * 2;
            var state = new ParticleState
            {
                Vx = Math.Cos(angle) * speed,
                Vy = Math.Sin(angle) * speed,
            };

            shape.Tag = state;

            if (enterFromEdge)
                PlaceOnEdge(shape, state, w, h);
            else
            {
                Canvas.SetLeft(shape, _rng.NextDouble() * Math.Max(1, w - size));
                Canvas.SetTop(shape, _rng.NextDouble() * Math.Max(1, h - size));
            }

            ParticleCanvas.Children.Add(shape);
        }

        private void PlaceOnEdge(Shape shape, ParticleState state, double w, double h)
        {
            int edge = _rng.Next(4);
            double size = shape.Width;

            switch (edge)
            {
                case 0:
                    Canvas.SetLeft(shape, _rng.NextDouble() * Math.Max(1, w - size));
                    Canvas.SetTop(shape, -size - _rng.Next(20, 80));
                    state.Vy = Math.Abs(state.Vy);
                    break;
                case 1:
                    Canvas.SetLeft(shape, w + _rng.Next(20, 80));
                    Canvas.SetTop(shape, _rng.NextDouble() * Math.Max(1, h - size));
                    state.Vx = -Math.Abs(state.Vx);
                    break;
                case 2:
                    Canvas.SetLeft(shape, _rng.NextDouble() * Math.Max(1, w - size));
                    Canvas.SetTop(shape, h + _rng.Next(20, 80));
                    state.Vy = -Math.Abs(state.Vy);
                    break;
                default:
                    Canvas.SetLeft(shape, -size - _rng.Next(20, 80));
                    Canvas.SetTop(shape, _rng.NextDouble() * Math.Max(1, h - size));
                    state.Vx = Math.Abs(state.Vx);
                    break;
            }
        }

        private void Tick()
        {
            if (!IsActive)
                return;

            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 50 || h < 50)
                return;

            if (ParticleCanvas.Children.Count == 0)
                RebuildParticles();

            var toRespawn = new List<Shape>();

            foreach (UIElement child in ParticleCanvas.Children)
            {
                if (child is not Shape shape || shape.Tag is not ParticleState state)
                    continue;

                double x = Canvas.GetLeft(shape) + state.Vx;
                double y = Canvas.GetTop(shape) + state.Vy;

                Canvas.SetLeft(shape, x);
                Canvas.SetTop(shape, y);

                if (IsFullyOutside(shape, w, h))
                    toRespawn.Add(shape);
            }

            foreach (Shape shape in toRespawn)
            {
                ParticleCanvas.Children.Remove(shape);
                SpawnParticle(enterFromEdge: true);
            }
        }

        private static bool IsFullyOutside(Shape shape, double w, double h)
        {
            double x = Canvas.GetLeft(shape);
            double y = Canvas.GetTop(shape);
            double margin = 40;

            return x + shape.Width < -margin
                || x > w + margin
                || y + shape.Height < -margin
                || y > h + margin;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (IsActive && sizeInfo.NewSize.Width > 50)
                RebuildParticles();
        }

        private sealed class ParticleState
        {
            public double Vx;
            public double Vy;
        }
    }
}
