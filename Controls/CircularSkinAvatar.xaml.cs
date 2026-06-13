using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SusanooLauncher.Services;

namespace SusanooLauncher.Controls
{
    public partial class CircularSkinAvatar : UserControl
    {
        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(CircularSkinAvatar),
                new PropertyMetadata(88.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty RingBrushProperty =
            DependencyProperty.Register(nameof(RingBrush), typeof(Brush), typeof(CircularSkinAvatar),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF))));

        public static readonly DependencyProperty RingThicknessProperty =
            DependencyProperty.Register(nameof(RingThickness), typeof(double), typeof(CircularSkinAvatar),
                new PropertyMetadata(2.0));

        public static readonly DependencyProperty BackgroundFillProperty =
            DependencyProperty.Register(nameof(BackgroundFill), typeof(Brush), typeof(CircularSkinAvatar),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x14, 0x18, 0x24))));

        public CircularSkinAvatar()
        {
            InitializeComponent();
            Loaded += (_, __) => UpdateCircularClip();
            SizeChanged += (_, __) => UpdateCircularClip();
        }

        public double Diameter
        {
            get => (double)GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public Brush RingBrush
        {
            get => (Brush)GetValue(RingBrushProperty);
            set => SetValue(RingBrushProperty, value);
        }

        public double RingThickness
        {
            get => (double)GetValue(RingThicknessProperty);
            set => SetValue(RingThicknessProperty, value);
        }

        public Brush BackgroundFill
        {
            get => (Brush)GetValue(BackgroundFillProperty);
            set => SetValue(BackgroundFillProperty, value);
        }

        public Task ApplySkinAsync(string? iconUrl, string? templateId = null) =>
            SkinImageHelper.ApplyToAsync(SkinImage, iconUrl, templateId);

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularSkinAvatar avatar)
                avatar.UpdateCircularClip();
        }

        private void UpdateCircularClip() => SkinImageHelper.EnsureCircularClip(SkinImage, Diameter);
    }
}
