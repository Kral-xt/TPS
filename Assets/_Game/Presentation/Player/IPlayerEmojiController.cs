namespace TPS.Player.Presentation
{
    public interface IPlayerEmojiController
    {
        bool IsPlayingEmoji { get; }

        void PlayEmoji(string emojiName);

        void StopEmoji();
    }
}
