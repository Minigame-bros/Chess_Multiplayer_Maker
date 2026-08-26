using System;

namespace ChessBot.Bots.CaHeo
{
    public class CaHeoBot : BaseBot
    {
        public override string Name => "Cá Heo";
        public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Ca_Heo.png";
        public override string Description => "Một chú cá heo thông minh, thân thiện. Khá giỏi và biết nhiều chiến thuật.";
        public override int Elo => 1600;

        public override string GetSpeech(GameState state)
        {
            return state switch
            {
                GameState.Start => "Éc éc! Xin chào, cùng tạo ra một ván cờ đẹp nhé!",
                GameState.Thinking => "Éc éc... Nước đi này có vẻ thú vị.",
                GameState.Moved => "Đỡ nước này xem nào! Éc éc!",
                GameState.Checked => "Ui chao, cậu tấn công rát quá!",
                GameState.Captured => "Éc éc... Tổn thất này nằm trong tính toán.",
                GameState.Won => "Trận đấu tuyệt vời! Éc éc!",
                GameState.Lost => "Éc éc! Cậu chơi quá đỉnh, tôi thua tâm phục khẩu phục.",
                GameState.Draw => "Một ván đấu cân não, éc éc!",
                _ => "Éc éc!"
            };
        }
    }
}
