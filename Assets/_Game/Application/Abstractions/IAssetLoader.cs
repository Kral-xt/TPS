// 文件说明：资源加载器抽象接口。
// 定义按地址加载与释放游戏资源的统一契约，供 Application 层解耦具体资源加载实现（如 Addressables）。
namespace TPS.Application.Abstractions
{
    /// <summary>
    /// 资源加载器接口，定义资源的加载与释放行为。
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// 根据地址加载资源。
        /// </summary>
        /// <param name="address">资源的地址标识。</param>
        /// <returns>加载得到的资源对象。</returns>
        object Load(string address);

        /// <summary>
        /// 释放已加载的资源。
        /// </summary>
        /// <param name="asset">需要释放的资源对象。</param>
        void Release(object asset);
    }
}