using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NimeVault.Models;

namespace NimeVault.Views
{
    public partial class AnimeCard : UserControl
    {
        public static readonly DependencyProperty AnimeDataProperty =
            DependencyProperty.Register(nameof(AnimeData), typeof(Anime), typeof(AnimeCard),
                new PropertyMetadata(null, OnAnimeDataChanged));

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(AnimeCard));

        public Anime? AnimeData
        {
            get => (Anime?)GetValue(AnimeDataProperty);
            set => SetValue(AnimeDataProperty, value);
        }

        public ICommand? SelectCommand
        {
            get => (ICommand?)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        public AnimeCard()
        {
            InitializeComponent();
        }

        private static void OnAnimeDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimeCard card)
                card.DataContext = e.NewValue;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (SelectCommand?.CanExecute(AnimeData) == true)
                SelectCommand.Execute(AnimeData);
        }
    }
}
