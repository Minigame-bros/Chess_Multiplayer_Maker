using System;

namespace ChessBot.Bots.Noob
{
    public class NoobBot : BaseBot
    {
        public override string Name => "Noob";
        public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Noob.png";
        public override string Description => "Một đứa trẻ trâu mới tập chơi. Đánh bừa, hay gáy to và khóc nhè khi thua.";
        public override int Elo => 400;

        public override string GetSpeech(GameState state)
        {
            return state switch
            {
                GameState.Start => "Mày tuổi gì đánh với tao? Vào việc!",
                GameState.Thinking => "Chờ tí tao đang xem kịch bản...",
                GameState.Moved => "Nước đi thần thánh! Sợ chưa?",
                GameState.Checked => "Ơ kìa... Chơi ăn gian à!",
                GameState.Captured => "Mất cờ! Đền tao con xe!",
                GameState.Won => "Gà! Bảo rồi mà không nghe.",
                GameState.Lost => "Máy lag thôi! Đánh lại không?",
                GameState.Draw => "Hòa là tao nhường mày đấy.",
                _ => "..."
            };
        }
    }
}
