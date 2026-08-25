using System;

// 文件说明：热更新服务抽象接口。
// 定义检测热更新就绪状态并触发更新检查的统一契约，供 Application 层解耦具体热更新实现。
namespace TPS.Application.Abstractions
{
    /// <summary>
    /// 热更新服务接口，定义更新就绪状态查询与更新检查行为。
    /// </summary>
    public interface IHotUpdateService
    {
        /// <summary>
        /// 获取热更新服务是否已就绪。
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 检查是否有可用更新。
        /// </summary>
        /// <param name="completed">检查完成后的回调，参数表示是否需要更新（或更新是否成功）。</param>
        void CheckForUpdates(Action<bool> completed);
    }
}