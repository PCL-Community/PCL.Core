namespace PCL.Core.App.Tasks;

public delegate void PropertyChangedHandler<in TProperty>(object source, TProperty oldValue, TProperty newValue);