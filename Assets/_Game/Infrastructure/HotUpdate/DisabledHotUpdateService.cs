using System;
using TPS.Application.Abstractions;

namespace TPS.Infrastructure.HotUpdate
{
    /// <summary>禁用的热更新服务（空实现），直接返回就绪状态</summary>
    public sealed class DisabledHotUpdateService : IHotUpdateService
    {
        public bool IsReady => true;

        public void CheckForUpdates(Action<bool> completed)
        {
            completed?.Invoke(true);
        }
    }
}

