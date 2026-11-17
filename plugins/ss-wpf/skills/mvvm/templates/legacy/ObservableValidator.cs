// Minimal validating base for toolchains that cannot use CommunityToolkit.Mvvm
// (Visual Studio 2012, .NET Framework 4.5, C# 5).
// Members match CommunityToolkit.Mvvm.ComponentModel.ObservableValidator: properties are
// validated with System.ComponentModel.DataAnnotations attributes and errors are
// reported through INotifyDataErrorInfo, which WPF bindings show by default.
// Requires a reference to System.ComponentModel.DataAnnotations.
// Replace the namespace with <RootNamespace>.Mvvm when copying into a project.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MyApp.Mvvm
{
    /// <summary>
    /// ObservableObject that validates its properties with data annotation attributes.
    /// </summary>
    public abstract class ObservableValidator : ObservableObject, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<ValidationResult>> _errors =
            new Dictionary<string, List<ValidationResult>>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors
        {
            get { return _errors.Count > 0; }
        }

        /// <summary>
        /// Returns the errors of one property, or of every property when the name is null or empty.
        /// </summary>
        public IEnumerable<ValidationResult> GetErrors(string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.Values.SelectMany(list => list).ToList();
            }

            List<ValidationResult> list;
            if (_errors.TryGetValue(propertyName, out list))
            {
                return list;
            }

            return Enumerable.Empty<ValidationResult>();
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return GetErrors(propertyName);
        }

        /// <summary>
        /// Sets the field, raises PropertyChanged, and validates the new value when validate is true.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T newValue, bool validate, [CallerMemberName] string propertyName = null)
        {
            bool changed = SetProperty(ref field, newValue, propertyName);
            if (changed && validate)
            {
                ValidateProperty(newValue, propertyName);
            }

            return changed;
        }

        /// <summary>
        /// Validates one property value against the attributes declared on the property.
        /// </summary>
        protected void ValidateProperty(object value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }

            ValidationContext context = new ValidationContext(this, null, null);
            context.MemberName = propertyName;

            List<ValidationResult> results = new List<ValidationResult>();
            Validator.TryValidateProperty(value, context, results);
            SetErrors(propertyName, results);
        }

        /// <summary>
        /// Validates every public property that declares at least one validation attribute.
        /// </summary>
        protected void ValidateAllProperties()
        {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (!property.GetCustomAttributes(typeof(ValidationAttribute), true).Any())
                {
                    continue;
                }

                ValidateProperty(property.GetValue(this, null), property.Name);
            }
        }

        /// <summary>
        /// Clears the errors of one property, or of every property when the name is null or empty.
        /// </summary>
        protected void ClearErrors(string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                List<string> names = _errors.Keys.ToList();
                _errors.Clear();
                foreach (string name in names)
                {
                    RaiseErrorsChanged(name);
                }

                return;
            }

            if (_errors.Remove(propertyName))
            {
                RaiseErrorsChanged(propertyName);
            }
        }

        private void SetErrors(string propertyName, List<ValidationResult> results)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);
            if (results.Count == 0)
            {
                if (hadErrors)
                {
                    _errors.Remove(propertyName);
                    RaiseErrorsChanged(propertyName);
                }

                return;
            }

            _errors[propertyName] = results;
            RaiseErrorsChanged(propertyName);
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            EventHandler<DataErrorsChangedEventArgs> handler = ErrorsChanged;
            if (handler != null)
            {
                handler(this, new DataErrorsChangedEventArgs(propertyName));
            }

            OnPropertyChanged("HasErrors");
        }
    }
}
