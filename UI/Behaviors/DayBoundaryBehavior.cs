using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Loggez.UI.Behaviors
{
    public enum Boundary
    {
        StartOfDay,
        EndOfDay
    }

    public class DayBoundaryBehavior : Behavior<CalendarDatePicker>
    {
        public static readonly StyledProperty<Boundary> ModeProperty =
            AvaloniaProperty.Register<DayBoundaryBehavior, Boundary>(
                nameof(Mode), Boundary.StartOfDay);

        public Boundary Mode
        {
            get => GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectedDateChanged += OnDateChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectedDateChanged -= OnDateChanged;
        }

        private void OnDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssociatedObject.SelectedDate is DateTime dt)
            {
                DateTime adjusted = Mode switch
                {
                    Boundary.StartOfDay => dt.Date,
                    Boundary.EndOfDay => dt.Date.AddDays(1).AddTicks(-1),
                    _ => dt
                };
                if (adjusted != dt)
                    AssociatedObject.SelectedDate = adjusted;
            }
        }
    }
}