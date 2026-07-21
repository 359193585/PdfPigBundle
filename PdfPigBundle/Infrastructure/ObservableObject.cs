//ObservableObject.cs

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PdfPigBundle.Infrastructure
{
    /// <summary>
    /// 实现 INotifyPropertyChanged 的基类，提供属性变更通知的基础功能。
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 设置属性值，并在值发生变化时引发 PropertyChanged 事件。
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">引用字段</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称（由编译器自动填充）</param>
        /// <returns>如果值发生变化返回 true，否则返回 false。</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        /// <summary>
        /// 手动触发属性变更通知（适用于依赖其他属性的计算属性）。
        /// </summary>
        /// <param name="propertyName">属性名称（由编译器自动填充）</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
