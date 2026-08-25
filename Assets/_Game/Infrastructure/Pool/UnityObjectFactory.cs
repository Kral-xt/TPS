using UnityEngine;

namespace Infrastructure
{
    /// <summary>对象工厂抽象接口，定义对象的创建与销毁</summary>
    public interface IObjectFactory
    {
        GameObject Create(GameObject prefab, Transform parent);
        void Destroy(GameObject obj);
    }
    
    /// <summary>Unity 对象工厂实现，使用 Instantiate 和 Destroy</summary>
    public class UnityObjectFactory : IObjectFactory
    {
        public GameObject Create(GameObject prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent);
        }

        public void Destroy(GameObject obj)
        {
            Object.Destroy(obj);
        }
    }
}
