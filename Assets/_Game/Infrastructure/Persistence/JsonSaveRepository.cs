using System.IO;
using TPS.Application.Abstractions;
using UnityEngine;
using UnityApplication = UnityEngine.Application;

namespace TPS.Infrastructure.Persistence
{
    /// <summary>JSON 存档仓储实现，将存档以 JSON 文件形式存储到本地磁盘</summary>
    public sealed class JsonSaveRepository : ISaveRepository
    {
        private readonly string rootDirectory;

        public JsonSaveRepository(string rootDirectory = null)
        {
            this.rootDirectory = string.IsNullOrEmpty(rootDirectory)
                ? UnityApplication.persistentDataPath
                : rootDirectory;
        }

        /// <summary>检查指定槽位的存档文件是否存在</summary>
        public bool Exists(string slot)
        {
            return File.Exists(GetPath(slot));
        }

        /// <summary>加载指定槽位的存档数据</summary>
        public string Load(string slot)
        {
            string path = GetPath(slot);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>保存数据到指定槽位的 JSON 文件</summary>
        public void Save(string slot, string data)
        {
            Directory.CreateDirectory(rootDirectory);
            File.WriteAllText(GetPath(slot), data ?? string.Empty);
        }

        /// <summary>删除指定槽位的存档文件</summary>
        public void Delete(string slot)
        {
            string path = GetPath(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>根据槽位标识生成存档文件完整路径</summary>
        private string GetPath(string slot)
        {
            string safeSlot = string.IsNullOrEmpty(slot) ? "default" : slot;
            return Path.Combine(rootDirectory, safeSlot + ".json");
        }
    }
}

