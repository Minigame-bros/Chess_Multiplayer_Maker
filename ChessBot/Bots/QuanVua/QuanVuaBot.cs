using System;

namespace ChessBot.Bots.QuanVua
{
    public class QuanVuaBot : BaseBot
    {
        public override string Name => "Quân Vua";
        public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Quan_Vua.png";
        public override string Description => "Bậc thầy cờ vua tối thượng. Không thể bị đánh bại, lạnh lùng và kiêu ngạo.";
        public override int Elo => 2800;

        public override string GetSpeech(GameState state)
        {
            return state switch
            {
                GameState.Start => "Lũ sâu bọ các ngươi lại dám thách thức trẫm sao?",
                GameState.Thinking => "Một ván cờ tẻ nhạt... trẫm đã nhìn thấu 20 nước tiếp theo.",
                GameState.Moved => "Quỳ xuống!",
                GameState.Checked => "Khá khen cho sự nỗ lực vô vọng của ngươi.",
                GameState.Captured => "Thí một tên lính quèn để lấy mạng ngươi, quá rẻ.",
                GameState.Won => "Kẻ yếu đuối muôn đời vẫn là kẻ yếu đuối.",
                GameState.Lost => "KHÔNG THỂ NÀO! Ngươi dùng tà thuật gì vậy???",
                GameState.Draw => "Trẫm không có tâm trạng sát sinh hôm nay.",
                _ => "Hừm."
            };
        }
    }
}
