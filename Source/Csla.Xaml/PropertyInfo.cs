﻿//-----------------------------------------------------------------------
// <copyright file="PropertyInfo.cs" company="Marimer LLC">
//     Copyright (c) Marimer LLC. All rights reserved.
//     Website: http://www.lhotka.net/cslanet/
// </copyright>
// <summary>Expose metastate information about a property.</summary>
//-----------------------------------------------------------------------
using System;
using System.Linq;
using System.ComponentModel;
using Csla.Reflection;
using Csla.Core;
using Csla.Rules;
using System.Collections.ObjectModel;
using System.Reflection;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
#endif

namespace Csla.Xaml
{
  /// <summary>
  /// Expose metastate information about a property.
  /// </summary>
  public class PropertyInfo : FrameworkElement, INotifyPropertyChanged
  {
    private bool _loading = true;
    private const string _dependencyPropertySuffix = "Property";

    /// <summary>
    /// Creates an instance of the object.
    /// </summary>
    public PropertyInfo()
    {
      Visibility = Visibility.Collapsed;
      Height = 20;
      Width = 20;
      BrokenRules = new ObservableCollection<BrokenRule>();
      Loaded += (o, e) =>
      {
        _loading = false;
        UpdateState();
      };
    }

    /// <summary>
    /// Creates an instance of the object for testing.
    /// </summary>
    /// <param name="testing">Test mode parameter.</param>
    public PropertyInfo(bool testing)
      : this()
    {
      _loading = false;
      UpdateState();
    }

    #region BrokenRules property

    /// <summary>
    /// Gets the broken rules collection from the
    /// business object.
    /// </summary>
    public static readonly DependencyProperty BrokenRulesProperty = DependencyProperty.Register(
      "BrokenRules",
      typeof(ObservableCollection<BrokenRule>),
      typeof(PropertyInfo),
      null);

    /// <summary>
    /// Gets the broken rules collection from the
    /// business object.
    /// </summary>
    [Category("Property Status")]
    public ObservableCollection<BrokenRule> BrokenRules
    {
      get { return (ObservableCollection<BrokenRule>)GetValue(BrokenRulesProperty); }
      private set { SetValue(BrokenRulesProperty, value); }
    }

    #endregion

    #region MyDataContext Property
    /// <summary>
    /// Used to monitor for changes in the binding path.
    /// </summary>
    public static readonly DependencyProperty MyDataContextProperty =
    DependencyProperty.Register("MyDataContext",
                                typeof(Object),
                                typeof(PropertyInfo),
#if NETFX_CORE
                                new PropertyMetadata(null, MyDataContextPropertyChanged));
#else
                                new PropertyMetadata(MyDataContextPropertyChanged));
#endif

    private static void MyDataContextPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      ((PropertyInfo)sender).SetSource(true);
    }

    #endregion

    #region RelativeBinding Property

    /// <summary>
    /// Used to monitor for changes in the binding path.
    /// </summary>
    public static readonly DependencyProperty RelativeBindingProperty =
    DependencyProperty.Register("RelativeBinding",
                                typeof(Object),
                                typeof(PropertyInfo),
#if NETFX_CORE
                                new PropertyMetadata(null, RelativeBindingPropertyChanged));
#else
                                new PropertyMetadata(RelativeBindingPropertyChanged));
#endif

    private static void RelativeBindingPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      ((PropertyInfo)sender).SetSource(true);
    }

    #endregion

    #region Source property

    /// <summary>
    /// Gets or sets the source business
    /// property to which this control is bound.
    /// </summary>
    public static readonly DependencyProperty PropertyProperty = DependencyProperty.Register(
      "Property",
      typeof(object),
      typeof(PropertyInfo),
      new PropertyMetadata(new object(), (o, e) =>
      {
        bool changed = true;
        if (e.NewValue == null)
        {
          if (e.OldValue == null)
            changed = false;
        }
        else if (e.NewValue.Equals(e.OldValue))
        {
          changed = false;
        }
        ((PropertyInfo)o).SetSource(changed);
      }));

    /// <summary>
    /// Gets or sets the source business
    /// property to which this control is bound.
    /// </summary>
    [Category("Common")]
    public object Property
    {
      get { return GetValue(PropertyProperty); }
      set
      {
        SetValue(PropertyProperty, value);
        SetSource(false);
      }
    }

    private object _source = null;
    /// <summary>
    /// Gets or sets the Source.
    /// </summary>
    /// <value>The source.</value>
    protected object Source
    {
      get
      {
        return _source;
      }
      set
      {
        _source = value;
      }
    }

    private string _bindingPath = string.Empty;
    /// <summary>
    /// Gets or sets the binding path.
    /// </summary>
    /// <value>The binding path.</value>
    protected string BindingPath
    {
      get
      {
        return _bindingPath;
      }
      set
      {
        _bindingPath = value;
      }
    }

    //private object _oldDataContext;
    //private System.Windows.Data.BindingExpression _oldBinding;

    /// <summary>
    /// Checks a binding expression to see if it is a relative source binding used in a control template.
    /// </summary>
    /// <param name="sourceBinding">The binding expression to parse.</param>
    /// <returns>If the source binding is a relative source binding, this method 
    /// finds the proper dependency property on the parent control and returns
    /// the binding expression for that property.</returns>
    protected virtual BindingExpression ParseRelativeBinding(BindingExpression sourceBinding)
    {
      if (sourceBinding != null
        && sourceBinding.ParentBinding.RelativeSource != null
        && sourceBinding.ParentBinding.RelativeSource.Mode == RelativeSourceMode.TemplatedParent
        && sourceBinding.DataItem is FrameworkElement)
      {
        var control = (FrameworkElement)sourceBinding.DataItem;
        var path = sourceBinding.ParentBinding.Path.Path;

        var type = control.GetType();
        FieldInfo fi = null;
        while (type != null)
        {
#if NETFX_CORE
          fi = type.GetField(string.Format("{0}{1}", path, _dependencyPropertySuffix), BindingFlags.Instance | BindingFlags.Public);
#else
          fi = type.GetField(string.Format("{0}{1}", path, _dependencyPropertySuffix));
#endif

          if (fi != null)
          {
            DependencyProperty mappedDP = (DependencyProperty)fi.GetValue(control.GetType());
            return control.GetBindingExpression(mappedDP);
          }
          else
          {
#if NETFX_CORE
            type = type.GetTypeInfo().BaseType;
#else
            type = type.BaseType;
#endif
          }
        }

        return null;
      }

      return sourceBinding;
    }

    /// <summary>
    /// Sets the source binding and updates status.
    /// </summary>
    protected virtual void SetSource(bool propertyValueChanged)
    {
      var binding = GetBindingExpression(PropertyProperty);
      if (binding != null)
      {
        SetSource(binding.DataItem);
      }
    }

    /// <summary>
    /// Sets the source binding and updates status.
    /// </summary>
    protected virtual void SetSource(object dataItem)
    {
      bool isDataLoaded = true;

      SetBindingValues(GetBindingExpression(PropertyProperty));
      var newSource = GetRealSource(dataItem, BindingPath);

      // Check to see if PropertyInfo is inside a control template
      ClearValue(MyDataContextProperty);
      if (newSource != null && newSource is FrameworkElement)
      {
        var data = ((FrameworkElement)newSource).DataContext;
        SetBindingValues(ParseRelativeBinding(GetBindingExpression(PropertyProperty)));

        if (data != null && GetBindingExpression(RelativeBindingProperty) == null)
        {
          var relativeBinding = ParseRelativeBinding(GetBindingExpression(PropertyProperty));
          if (relativeBinding != null)
            SetBinding(RelativeBindingProperty, relativeBinding.ParentBinding);
        }

        newSource = GetRealSource(data, BindingPath);

        if (newSource != null)
        {
          Binding b = new Binding();
          b.Source = newSource;
          if (BindingPath.IndexOf('.') > 0)
          {
            var path = GetRelativePath(newSource, BindingPath);
            if (path != null)
              b.Path = path;
          }
          if (b.Path != null
              && !string.IsNullOrEmpty(b.Path.Path)
              && b.Path.Path != BindingPath.Substring(BindingPath.LastIndexOf('.') + 1))
          {
            SetBinding(MyDataContextProperty, b);
            isDataLoaded = false;
          }
        }
      }

      if (BindingPath.IndexOf('.') > 0)
        BindingPath = BindingPath.Substring(BindingPath.LastIndexOf('.') + 1);

      if (isDataLoaded)
      {
        if (!ReferenceEquals(Source, newSource))
        {
          var old = Source;
          Source = newSource;

          HandleSourceEvents(old, Source);
        }

        UpdateState();
      }
    }

    /// <summary>
    /// Sets the binding values for this instance.
    /// </summary>
    private void SetBindingValues(BindingExpression binding)
    {
      var bindingPath = string.Empty;

      if (binding != null)
      {
        if (binding.ParentBinding != null && binding.ParentBinding.Path != null)
          bindingPath = binding.ParentBinding.Path.Path;
        else
          bindingPath = string.Empty;
      }

      BindingPath = bindingPath;
    }

    /// <summary>
    /// Gets the real source helper method.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="bindingPath">The binding path.</param>
    /// <returns></returns>
    protected object GetRealSource(object source, string bindingPath)
    {
      var icv = source as ICollectionView;
      if (icv != null)
        source = icv.CurrentItem;
      if (source != null && bindingPath.IndexOf('.') > 0)
      {
        var firstProperty = bindingPath.Substring(0, bindingPath.IndexOf('.'));
        var p = MethodCaller.GetProperty(source.GetType(), firstProperty);
        if (p != null)
        {
         var rs = GetRealSource(
          MethodCaller.GetPropertyValue(source, p),
          bindingPath.Substring(bindingPath.IndexOf('.') + 1));

          if (rs != null)
            return rs;
        }
      }
        
      return source;
    }

    /// <summary>
    /// Gets the part of the binding path relevant to the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="bindingPath">The binding path.</param>
    /// <returns></returns>
    protected PropertyPath GetRelativePath(object source, string bindingPath)
    {
      if (source != null)
      {
        if (bindingPath.IndexOf('.') > 0)
        {
          var firstProperty = bindingPath.Substring(0, bindingPath.IndexOf('.'));
          var p = MethodCaller.GetProperty(source.GetType(), firstProperty);

          if (p != null)
            return new PropertyPath(firstProperty);
          else
            return GetRelativePath(source, bindingPath.Substring(bindingPath.IndexOf('.') + 1));
        }
        else
          return new PropertyPath(bindingPath);
      }

      return null;
    }

    private void HandleSourceEvents(object old, object source)
    {
      if (!ReferenceEquals(old, source))
      {
        DetachSource(old);
        AttachSource(source);
        BusinessBase bb = Source as BusinessBase;
        if (bb != null)
        {
          IsBusy = bb.IsPropertyBusy(BindingPath);
        }
      }
    }

    private void DetachSource(object source)
    {
      var p = source as INotifyPropertyChanged;
      if (p != null)
        p.PropertyChanged -= source_PropertyChanged;
      INotifyBusy busy = source as INotifyBusy;
      if (busy != null)
        busy.BusyChanged -= source_BusyChanged;
    }

    private void AttachSource(object source)
    {
      var p = source as INotifyPropertyChanged;
      if (p != null)
        p.PropertyChanged += source_PropertyChanged;
      INotifyBusy busy = source as INotifyBusy;
      if (busy != null)
        busy.BusyChanged += source_BusyChanged;
    }

    void source_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == BindingPath || string.IsNullOrEmpty(e.PropertyName))
        UpdateState();
    }

    void source_BusyChanged(object sender, BusyChangedEventArgs e)
    {
      if (e.PropertyName == BindingPath || string.IsNullOrEmpty(e.PropertyName))
      {
        bool busy = e.Busy;
        BusinessBase bb = Source as BusinessBase;
        if (bb != null)
          busy = bb.IsPropertyBusy(BindingPath);

        if (busy != IsBusy)
        {
          IsBusy = busy;
          UpdateState();
        }
      }
    }

    #endregion

    #region State properties

    private bool _canRead = true;
    /// <summary>
    /// Gets a value indicating whether the user
    /// is authorized to read the property.
    /// </summary>
    [Category("Property Status")]
    public bool CanRead
    {
      get { return _canRead; }
      protected set
      {
        if (value != _canRead)
        {
          _canRead = value;
          OnPropertyChanged("CanRead");
        }
      }
    }

    private bool _canWrite = true;
    /// <summary>
    /// Gets a value indicating whether the user
    /// is authorized to write the property.
    /// </summary>
    [Category("Property Status")]
    public bool CanWrite
    {
      get { return _canWrite; }
      protected set
      {
        if (value != _canWrite)
        {
          _canWrite = value;
          OnPropertyChanged("CanWrite");
        }
      }
    }

    private bool _isBusy = false;
    /// <summary>
    /// Gets a value indicating whether the property
    /// is busy with an asynchronous operation.
    /// </summary>
    [Category("Property Status")]
    public bool IsBusy
    {
      get { return _isBusy; }
      private set
      {
        if (value != _isBusy)
        {
          _isBusy = value;
          OnPropertyChanged("IsBusy");
        }
      }
    }

    private bool _isValid = true;
    /// <summary>
    /// Gets a value indicating whether the 
    /// property is valid.
    /// </summary>
    [Category("Property Status")]
    public bool IsValid
    {
      get { return _isValid; }
      private set
      {
        if (value != _isValid)
        {
          _isValid = value;
          OnPropertyChanged("IsValid");
        }
      }
    }

    private RuleSeverity _worst;
    /// <summary>
    /// Gets a valud indicating the worst
    /// severity of all broken rules
    /// for this property (if IsValid is
    /// false).
    /// </summary>
    [Category("Property Status")]
    public RuleSeverity RuleSeverity
    {
      get { return _worst; }
      private set
      {
        if (value != _worst)
        {
          _worst = value;
          OnPropertyChanged("RuleSeverity");
        }
      }
    }

    private string _ruleDescription = string.Empty;
    /// <summary>
    /// Gets the description of the most severe
    /// broken rule for this property.
    /// </summary>
    [Category("Property Status")]
    public string RuleDescription
    {
      get { return _ruleDescription; }
      private set
      {
        if (value != _ruleDescription)
        {
          _ruleDescription = value;
          OnPropertyChanged("RuleDescription");
        }
      }
    }

    #endregion

    #region State management

    /// <summary>
    /// Updates the state on control Property.
    /// </summary>
    protected virtual void UpdateState()
    {
      if (_loading) return;
      if (Source == null || string.IsNullOrEmpty(BindingPath)) return;

      var iarw = Source as Csla.Security.IAuthorizeReadWrite;
      if (iarw != null)
      {
        CanWrite = iarw.CanWriteProperty(_bindingPath);
        CanRead = iarw.CanReadProperty(_bindingPath);
      }

      BusinessBase businessObject = Source as BusinessBase;
      if (businessObject != null)
      {
        var allRules = (from r in businessObject.BrokenRulesCollection
                        where r.Property == BindingPath
                        select r).ToArray();

        var removeRules = (from r in BrokenRules
                           where !allRules.Contains(r)
                           select r).ToArray();

        var addRules = (from r in allRules
                        where !BrokenRules.Contains(r)
                        select r).ToArray();

        foreach (var rule in removeRules)
          BrokenRules.Remove(rule);
        foreach (var rule in addRules)
          BrokenRules.Add(rule);

        IsValid = BrokenRules.Count == 0;

        if (!IsValid)
        {
          BrokenRule worst = (from r in BrokenRules
                              orderby r.Severity
                              select r).FirstOrDefault();

          if (worst != null)
          {
            RuleSeverity = worst.Severity;
            RuleDescription = worst.Description;
          }
          else
            RuleDescription = string.Empty;
        }
        else
          RuleDescription = string.Empty;
      }
      else
      {
        BrokenRules.Clear();
        RuleDescription = string.Empty;
        IsValid = true;
      }
    }

    #endregion

    #region INotifyPropertyChanged Members

    /// <summary>
    /// Event raised when a property has changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">Name of the changed property.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
