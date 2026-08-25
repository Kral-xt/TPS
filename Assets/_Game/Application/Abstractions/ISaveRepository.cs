// 文件说明：存档仓库抽象接口。
// 定义按存档槽位进行存在性判断、加载、保存与删除的统一契约，供 Application 层解耦具体存档存储实现。
namespace TPS.Application.Abstractions
{
    /// <summary>
    /// 存档仓库接口，定义存档的存在判断、加载、保存与删除行为。
    /// </summary>
    public interface ISaveRepository
    {
        /// <summary>
        /// 判断指定存档槽位是否存在存档数据。
        /// </summary>
        /// <param name="slot">存档槽位标识。</param>
        /// <returns>存在返回 true，否则返回 false。</returns>
        bool Exists(string slot);

        /// <summary>
        /// 加载指定存档槽位的数据。
        /// </summary>
        /// <param name="slot">存档槽位标识。</param>
        /// <returns>存档数据字符串。</returns>
        string Load(string slot);

        /// <summary>
        /// 保存数据到指定存档槽位。
        /// </summary>
        /// <param name="slot">存档槽位标识。</param>
        /// <param name="data">需要保存的存档数据。</param>
        void Save(string slot, string data);

        /// <summary>
        /// 删除指定存档槽位的存档数据。
        /// </summary>
        /// <param name="slot">存档槽位标识。</param>
        void Delete(string slot);
    }
}