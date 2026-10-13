using System;
namespace Bindito.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ContextAttribute : Attribute { public ContextAttribute(string name) {} }
    public interface IExportAssignee {}
    public interface IScopeAssignee { IExportAssignee AsSingleton(); }
    public interface ISingleBindingBuilder<T> : IScopeAssignee {}
    public abstract class Configurator
    {
        protected virtual void Configure() {}
        protected ISingleBindingBuilder<T> Bind<T>() => default(ISingleBindingBuilder<T>);
    }
}
