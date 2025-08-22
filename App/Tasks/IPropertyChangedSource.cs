namespace PCL.Core.App.Tasks;

public delegate void PropertyChangedHandler<in TProperty>(object source, TProperty oldValue, TProperty newValue);

/// <summary>
/// 可观察的属性值改变模型。
/// </summary>
/// <typeparam name="TProperty">属性类型。</typeparam>
public interface IPropertyChangedSource<out TProperty>
{
    /// <summary>
    /// 属性值改变事件。
    /// 当你需要实现这个事件时，请保证只有在当前派生类型中可观察的一个或几个属性会触发这个事件，而不是像
    /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> 一样将所有属性的更改都传递过去。
    /// </summary>
    event PropertyChangedHandler<TProperty>? PropertyChanged;
}
