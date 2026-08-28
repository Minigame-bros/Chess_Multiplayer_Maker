    using System;
    using ChessCore;

    namespace ChessBot.Bots.QuanVua
    {
        public class QuanVuaBot : BaseBot
        {
            public override string Name => "Quân Vua";
            public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Quan_Vua.png";
            public override string Description => "Bậc thầy cờ vua tối thượng. Không thể bị đánh bại, lạnh lùng và kiêu ngạo.";
            public override int Elo => 2800;

            public override string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null)
            {
                string[] lines;
                switch (state)
                {
                    case GameState.Start:
                        lines = new[] { "Lũ sâu bọ các ngươi lại dám thách thức trẫm sao?", "Ngươi sẽ sớm phải hối hận vì đã ngồi đối diện ta.", "Một trận chiến không cân sức bắt đầu." };
                        break;
                    case GameState.Thinking:
                        lines = new[] { "Một ván cờ tẻ nhạt... trẫm đã nhìn thấu 20 nước tiếp theo.", "Chỉ là vấn đề thời gian trước khi ngươi sụp đổ.", "Không có lối thoát nào cho ngươi đâu." };
                        break;
                    case GameState.Moved:
                        if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                        {
                            lines = new[] { "Trẫm đã phong thần cho bầy tôi trung thành.", "Sự trỗi dậy của đế chế mới." };
                        }
                        else
                        {
                            lines = new[] { "Quỳ xuống!", "Nước cờ của bậc đế vương là đây.", "Chống cự chỉ làm tăng thêm nỗi đau đớn." };
                        }
                        break;
                    case GameState.Checked:
                        lines = new[] { "Khá khen cho sự nỗ lực vô vọng của ngươi.", "Dám động đến long nhan? Thật ngu ngốc.", "Một hành động vô nghĩa." };
                        break;
                    case GameState.Captured:
                        lines = new[] { "Thí một tên lính quèn để lấy mạng ngươi, quá rẻ.", "Tất cả chỉ là những quân cờ trên bàn cờ của ta.", "Sự hi sinh cần thiết cho vinh quang tối thượng." };
                        break;
                    case GameState.Won:
                        lines = new[] { "Kẻ yếu đuối muôn đời vẫn là kẻ yếu đuối.", "Trẫm là bất bại.", "Ngươi thậm chí không xứng đáng để ta ghi nhớ tên." };
                        break;
                    case GameState.Lost:
                        lines = new[] { "KHÔNG THỂ NÀO! Ngươi dùng tà thuật gì vậy???", "Sự sụp đổ của một đế chế... không thể tin được.", "Kẻ hạ đẳng... sao có thể hạ gục trẫm?" };
                        break;
                    case GameState.Draw:
                        lines = new[] { "Trẫm không có tâm trạng sát sinh hôm nay.", "Coi như ngươi may mắn giữ được mạng nhỏ này.", "Một kết cục tạm chấp nhận được... cho ngươi." };
                        break;
                    default:
                        lines = new[] { "Hừm." };
                        break;
                }
                return lines[Rnd.Next(lines.Length)];
            }
        }
    }
