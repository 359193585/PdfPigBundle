//ObservableObject.cs

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PdfPigBundle.Infrastructure
{
    /// <summary>
    /// base class implementing INotifyPropertyChanged, providing the basic functionality for property change notification.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///set  Sets the property value and raises the PropertyChanged event if the value changes.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">A reference to the backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property (automatically filled by the compiler).</param>
        /// <returns>Returns true if the value changes, otherwise false.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        /// <summary>
        /// manually triggers the property change notification (useful for computed properties that depend on other properties).
        /// </summary>
        /// <param name="propertyName">The name of the property (automatically filled by the compiler).</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
