// Minimal MVVM base for toolchains that cannot use CommunityToolkit.Mvvm
// (Visual Studio 2012, .NET Framework 4.5, C# 5).
// Member names match CommunityToolkit.Mvvm.ComponentModel.ObservableObject so that a
// later move to the toolkit only changes the using directive.
// Replace the namespace with <RootNamespace>.Mvvm when copying into a project.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyApp.Mvvm
{
    /// <summary>
    /// Base class for objects that notify listeners when a property value changes.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises PropertyChanged. The caller's member name is used when no name is given.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises PropertyChanged with the given arguments.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Sets the field and raises PropertyChanged when the value changed.
        /// Returns true when the value changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
