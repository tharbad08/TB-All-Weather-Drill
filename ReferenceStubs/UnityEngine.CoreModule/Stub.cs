namespace UnityEngine { public class Object {} public class Component : Object { public T GetComponent<T>() where T : Component => null; } public class MonoBehaviour : Component {} }
