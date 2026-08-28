    using System;
    using ChessCore;

    namespace ChessBot.Bots.ConLon
    {
        public class ConLonBot : BaseBot
        {
            public override string Name => "Con Lợn";
            public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Con_Lon.png";
            public override string Description => "Lười biếng nhưng cực kì đáng gờm. Đánh lỳ lợm và rất tham ăn quân.";
            public override int Elo => 2100;

            public override string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null)
            {
                string[] lines;
                switch (state)
                {
                    case GameState.Start:
                        lines = new[] { "Ụt ịt... Đánh nhanh đi tao còn đi ăn cám.", "Vào việc lẹ lên, đói quá rồi ụt ịt.", "Mày chắc chứ? Tao đang buồn ngủ đây... khò khò." };
                        break;
                    case GameState.Thinking:
                        lines = new[] { "Mày suy nghĩ lâu thế, tao ngủ gật bây giờ... khò khò.", "Zzz... Ụt ịt... tới lượt tao à?", "Đang nhai cám, đợi tí." };
                        break;
                    case GameState.Moved:
                        if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                        {
                            lines = new[] { "Ụt ịt, tao hóa lợn rừng rồi đây!", "Lên chức rồi, ăn chục bát cám cũng được." };
                        }
                        else
                        {
                            lines = new[] { "Cho tao xin miếng thịt này nhé, ụt ịt.", "Đẩy con này lên cho mày sợ.", "Ụt ịt, nước này chắc cốp đấy." };
                        }
                        break;
                    case GameState.Checked:
                        lines = new[] { "Hú hồn! Suýt thì bị quay lợn.", "Mày làm tao giật cả mình, rớt mếng cám!", "Làm gì căng thế, ụt ịt!" };
                        break;
                    case GameState.Captured:
                        lines = new[] { "Á à, mày dám cắn trộm tao à?", "Miếng ngon bị nẫng tay trên, ụt ịt cay thế!", "Để xem mày no được bao lâu." };
                        break;
                    case GameState.Won:
                        lines = new[] { "Ụt ịt! Kém tắm, về nhà bú tí mẹ đi.", "Tao vừa ngủ gật vừa đánh vẫn thắng mày.", "Xong rồi, đi kiếm máng cám thôi." };
                        break;
                    case GameState.Lost:
                        lines = new[] { "Hôm nay tao chưa ăn no nên thế thôi, ụt ịt.", "Lợn ốm nên mới thua, mai đánh lại!", "Tại cái máng cám làm tao phân tâm." };
                        break;
                    case GameState.Draw:
                        lines = new[] { "Đói quá, hòa đi kiếm gì bỏ bụng nào.", "Thôi mệt rồi, hòa nhé ụt ịt.", "Đánh mãi không xong, đi ngủ cho khỏe." };
                        break;
                    default:
                        lines = new[] { "Ụt ịt..." };
                        break;
                }
                return lines[Rnd.Next(lines.Length)];
            }
        }
    }
