    using System;
    using ChessCore;

    namespace ChessBot.Bots.Noob
    {
        public class NoobBot : BaseBot
        {
            public override string Name => "Noob";
            public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Noob.png";
            public override string Description => "Một đứa trẻ trâu mới tập chơi. Đánh bừa, hay gáy to và khóc nhè khi thua.";
            public override int Elo => 400;

            public override string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null)
            {
                string[] lines;
                switch (state)
                {
                    case GameState.Start:
                        lines = new[] { "Mày tuổi gì đánh với tao? Vào việc!", "Thích thì nhích, tao chấp mày 1 nước!", "Khởi động tí cho nóng máy nào." };
                        break;
                    case GameState.Thinking:
                        lines = new[] { "Chờ tí tao đang xem kịch bản...", "Mạng lag quá đợi xíu nha...", "Đang nghĩ nước chiếu bí mày đây." };
                        break;
                    case GameState.Moved:
                        if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                        {
                            lines = new[] { "Thấy gì chưa, tao có hoàng hậu thứ hai rồi!", "Bơm máu nè, sợ chưa con gà?" };
                        }
                        else
                        {
                            lines = new[] { "Nước đi thần thánh! Sợ chưa?", "Nhìn kĩ đi, IQ vô cực đấy!", "Chơi đi, cấm đi lại nha." };
                        }
                        break;
                    case GameState.Checked:
                        lines = new[] { "Ơ kìa... Chơi ăn gian à!", "Hên thôi con gà!", "Chiếu thì chiếu, tao chạy là xong!" };
                        break;
                    case GameState.Captured:
                        lines = new[] { "Mất cờ! Đền tao con xe!", "Mày chơi dơ quá nha, trả lại đây!", "Thí quân lấy lợi thế thôi, tao không gà đâu." };
                        break;
                    case GameState.Won:
                        lines = new[] { "Gà! Bảo rồi mà không nghe.", "Quá dễ, tao còn chưa dùng hết sức.", "Thắng dễ như ăn kẹo, xóa game đi em!" };
                        break;
                    case GameState.Lost:
                        lines = new[] { "Máy lag thôi! Đánh lại không?", "Do tao bấm nhầm phím thôi, ván này không tính!", "Mày hên thôi, solo lại không gà?" };
                        break;
                    case GameState.Draw:
                        lines = new[] { "Hòa là tao nhường mày đấy.", "Đang bận nên tao tha cho đấy.", "Hòa thôi, tí tao quay lại ăn mày sau." };
                        break;
                    default:
                        lines = new[] { "..." };
                        break;
                }
                return lines[Rnd.Next(lines.Length)];
            }
        }
    }
