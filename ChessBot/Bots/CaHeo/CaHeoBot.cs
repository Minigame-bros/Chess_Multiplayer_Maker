    using System;
    using ChessCore;

    namespace ChessBot.Bots.CaHeo
    {
        public class CaHeoBot : BaseBot
        {
            public override string Name => "Cá Heo";
            public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Ca_Heo.png";
            public override string Description => "Một chú cá heo thông minh, thân thiện. Khá giỏi và biết nhiều chiến thuật.";
            public override int Elo => 1600;

            public override string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null)
            {
                string[] lines;
                switch (state)
                {
                    case GameState.Start:
                        lines = new[] { "Éc éc! Xin chào, cùng tạo ra một ván cờ đẹp nhé!", "Chào cậu! Cùng bơi trong thế giới cờ vua nào, éc éc!", "Rất vui được chơi cùng cậu!" };
                        break;
                    case GameState.Thinking:
                        lines = new[] { "Éc éc... Nước đi này có vẻ thú vị.", "Để mình suy nghĩ một chút nhé.", "Hmmm, nước này hơi khó bơi đây." };
                        break;
                    case GameState.Moved:
                        if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                        {
                            lines = new[] { "Éc éc, biến hình!", "Tuyệt vời, thăng cấp rồi!" };
                        }
                        else
                        {
                            lines = new[] { "Đỡ nước này xem nào! Éc éc!", "Lên đường thôi!", "Éc éc, nước này mình tính kỹ rồi đấy." };
                        }
                        break;
                    case GameState.Checked:
                        lines = new[] { "Ui chao, cậu tấn công rát quá!", "Éc! Nguy hiểm thật!", "Gấp quá, phải lặn thôi!" };
                        break;
                    case GameState.Captured:
                        lines = new[] { "Éc éc... Tổn thất này nằm trong tính toán.", "Đổi lấy thế trận tốt hơn thôi.", "Ôi không, bị ăn mất rồi!" };
                        break;
                    case GameState.Won:
                        lines = new[] { "Trận đấu tuyệt vời! Éc éc!", "Thắng rồi! Bơi đi ăn mừng thôi!", "Cậu chơi rất hay, nhưng mình may mắn hơn." };
                        break;
                    case GameState.Lost:
                        lines = new[] { "Éc éc! Cậu chơi quá đỉnh, tôi thua tâm phục khẩu phục.", "Giỏi quá! Cậu đã thắng ván này.", "Đuối sức rồi, cậu đánh hay lắm." };
                        break;
                    case GameState.Draw:
                        lines = new[] { "Một ván đấu cân não, éc éc!", "Hòa rồi! Chúng ta ngang tài ngang sức nhỉ.", "Thật kịch tính, cảm ơn vì ván cờ!" };
                        break;
                    default:
                        lines = new[] { "Éc éc!" };
                        break;
                }
                return lines[Rnd.Next(lines.Length)];
            }
        }
    }
