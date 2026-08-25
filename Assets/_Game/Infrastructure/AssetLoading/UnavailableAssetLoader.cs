using TPS.Application.Abstractions;

namespace TPS.Infrastructure.AssetLoading
{
    /// <summary>不可用的资源加载器（空实现），用于资源系统未接入时的占位</summary>
    public sealed class UnavailableAssetLoader : IAssetLoader
    {
        public object Load(string address)
        {
            return null;
        }

        public void Release(object asset)
        {
        }
    }
}

