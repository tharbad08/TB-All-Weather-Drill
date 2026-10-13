namespace Timberborn.BaseComponentSystem
{
    public class BaseComponent
    {
        public T GetComponent<T>() => default(T);
    }
}
