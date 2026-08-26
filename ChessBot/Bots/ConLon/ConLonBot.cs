using System;

namespace ChessBot.Bots.ConLon
{
    public class ConLonBot : BaseBot
    {
        public override string Name => "Con Lợn";
        public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Con_Lon.png";
        public override string Description => "Lười biếng nhưng cực kì đáng gờm. Đánh lỳ lợm và rất tham ăn quân.";
        public override int Elo => 2100;

        public override string GetSpeech(GameState state)
        {
            return state switch
            {
                GameState.Start => "Ụt ịt... Đánh nhanh đi tao còn đi ăn cám.",
                GameState.Thinking => "Mày suy nghĩ lâu thế, tao ngủ gật bây giờ... khò khò.",
                GameState.Moved => "Cho tao xin miếng thịt này nhé, ụt ịt.",
                GameState.Checked => "Hú hồn! Suýt thì bị quay lợn.",
                GameState.Captured => "Á à, mày dám cắn trộm tao à?",
                GameState.Won => "Ụt ịt! Kém tắm, về nhà bú tí mẹ đi.",
                GameState.Lost => "Hôm nay tao chưa ăn no nên thế thôi, ụt ịt.",
                GameState.Draw => "Đói quá, hòa đi kiếm gì bỏ bụng nào.",
                _ => "Ụt ịt..."
            };
        }
    }
}
