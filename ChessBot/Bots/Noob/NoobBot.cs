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

        public override string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null)
        {
            int balance = GetMaterialBalance(game, PlayerColor.Black);

            string[] lines;
            switch (state)
            {
                case GameState.Start:
                    lines = new[]
                    {
                        "Mày tuổi gì đánh với tao? Vào việc!",
                        "Thích thì nhích, tao chấp mày 3 nước đầu!",
                        "Khởi động tí cho nóng máy nào con gà!",
                        "Thua thì đừng có khóc nhè gọi mẹ nhé!",
                        "Đánh nhanh thắng nhanh, tao còn đi đánh Liên Quân!"
                    };
                    break;

                case GameState.Thinking:
                    if (game != null && game.IsCheck)
                    {
                        lines = new[]
                        {
                            "Ủa ai cho chiếu đấy? Chờ tí tao né ra!",
                            "Chơi bẩn thế nhờ! Đợi tao xem chạy đường nào...",
                            "Lag tí thôi làm gì căng, chiếu không ăn thua đâu!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Game bị bug à? Sao bàn cờ tao rụng hết quân thế này?",
                            "Mày hack map đúng không? Khai mau!",
                            "Bình tĩnh, tao đang dụ mày vào bẫy 500 IQ đấy!",
                            "Đang nhả ván này cho mày vui thôi, tí biết tay tao!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Đánh với gà chán thật sự, bấm đại cũng thắng.",
                            "Chuẩn bị nhấn nút đầu hàng đi là vừa em ơi!",
                            "Ez game quá! Đang tính nước chiếu bí lộng lẫy đây.",
                            "Có thế thôi à? Cố tí nữa xem nào!"
                        };
                    }
                    else if (IsEndgame(game))
                    {
                        lines = new[]
                        {
                            "Tàn cuộc rồi, vua tao ra trận đập mày này!",
                            "Đợi tí tao đẩy tốt lên phong hậu là hết phim!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Chờ tí tao đang xem kịch bản trên mạng...",
                            "Mạng lag 999ms quá đợi xíu nha...",
                            "Đang tính nước cờ IQ vô cực chiếu bí mày đây.",
                            "Đừng có giục! Thiên tài cần thời gian suy nghĩ."
                        };
                    }
                    break;

                case GameState.Moved:
                    if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                    {
                        lines = new[]
                        {
                            "Thấy gì chưa, tao đẻ thêm con Hậu nữa rồi nè!",
                            "Bơm máu thành công! Hai con Hậu lùa mày chạy tụt quần!",
                            "Phong Hậu rồi! Quỳ xuống xin tha chưa?"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.IsCastle)
                    {
                        lines = new[]
                        {
                            "Nhập thành giấu vua kĩ chưa? Bắn tỉa mày từ xa luôn!",
                            "Xây boong-ke chống gà xong, vào đây mà ăn!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Nước đi ép sân toàn diện! Thở oxy chưa con gà?",
                            "Nhìn thế cờ này mà khóc đi em!",
                            "Tao áp đảo hoàn toàn rồi, giãy giụa làm gì!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Một nước cờ lật kèo đỉnh cao! Run sợ đi!",
                            "Bẫy đấy, ngon thì lao vào ăn đi con gà!",
                            "Tao đi nước này là mày sắp toang rồi đấy."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Nước đi thần thánh! Sợ chưa con gà?",
                            "Nhìn kĩ đi, IQ vô cực đấy!",
                            "Chơi đi, cấm đi lại nha!",
                            "Đỉnh cao chiến thuật của thần đồng cờ vua!",
                            "Nước này trong sách giáo khoa cũng không có đâu!"
                        };
                    }
                    break;

                case GameState.BotDeliveredCheck:
                    lines = new[]
                    {
                        "Chiếu! Giật mình chưa con gà?",
                        "Chiếu tướng nè! Chạy đằng trời nhé!",
                        "Ơ kìa chiếu kìa, chạy mau không mất vua bây giờ!",
                        "Né đi em ơi, chạy đâu cho thoát khỏi tay tao!",
                        "Chiếu phát cho mày tỉnh ngủ!"
                    };
                    break;

                case GameState.BotCaptured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "HÚP CON HẬU RỒI! Xóa game đi em ơi!",
                            "Mất Hậu rồi còn đánh đấm gì nữa, đầu hàng lẹ!",
                            "Haha con gà sơ hở mất Hậu! Cay nổ mắt chưa?"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Xin nhẹ con Xe! Cụt một cánh rồi nhé!",
                            "Mất Xe là toang rồi em ơi, khóc đi!",
                            "Húp con Xe quá ngọt, ez vãi!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Ăn con Mã của mày ngọt xớt!",
                            "Đánh đấm sơ hở thế, tao húp luôn không tha!",
                            "Một quân nữa lên bảng đếm số!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Ăn con Tốt lót dạ tí!",
                            "Một con Tốt cũng không tha cho mày!",
                            "Húp gọn, một điểm về chỗ!"
                        };
                    }
                    break;

                case GameState.Checked:
                    lines = new[]
                    {
                        "Ơ kìa chiếu cái gì? Hên thôi con gà!",
                        "Chiếu thì tao chạy, làm gì được tao nào?",
                        "Chơi ăn gian à, cấm chiếu liên tục nha!",
                        "Hú hồn, ai cho mày chiếu bất ngờ thế!"
                    };
                    break;

                case GameState.Captured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "Ơ ơ con Hậu của tao! Mày chơi dơ thế!",
                            "Trả Hậu đây! Tao bấm nhầm chuột thôi ván này không tính!",
                            "Tao cố tình thí Hậu tạo thế trận đấy, không phải tao gà đâu!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Cay thế nhờ! Đền tao con Xe mau lên!",
                            "Mày dám ăn Xe tao á? Coi chừng tao đấy!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Mất quân rồi! Mày đợi đấy lát tao đập lại!",
                            "Thí quân lấy lợi thế thôi, tao tính cả rồi!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Ăn được con tốt mà tưởng bở à con gà?",
                            "Tao nhường mày con tốt đấy, đừng có gáy!",
                            "Tí nữa tao đòi lại cả vốn lẫn lời!"
                        };
                    }
                    break;

                case GameState.Castled:
                    lines = new[]
                    {
                        "Trốn vào góc làm rùa rụt cổ à?",
                        "Tưởng nhập thành là sống sót được với tao à?",
                        "Nhập thành hả? Tí tao dồn quân sang đập nát!"
                    };
                    break;

                case GameState.Promotion:
                    lines = new[]
                    {
                        "Ơ sao mày cũng phong Hậu được thế? Bịp à!",
                        "Hậu giả tạo thôi, chuẩn bị tinh thần bị tao xơi đi!"
                    };
                    break;

                case GameState.DrawRefused:
                    lines = new[]
                    {
                        "Hòa cái gì mà hòa? Sợ thua rồi chứ gì, đánh tiếp!",
                        "Tuổi gì đòi hòa với tao? Đang chơi hay đòi hòa!",
                        "Không hòa! Tao phải đập mày cho bõ ghét!"
                    };
                    break;

                case GameState.PlayerResigned:
                    lines = new[]
                    {
                        "Haha! Mới thế đã rén rồi à con gà?",
                        "Đầu hàng là đúng rồi, đỡ mất thời gian của thiên tài!",
                        "Out trình quá mà, ez win 1 sao!"
                    };
                    break;

                case GameState.Won:
                    lines = new[]
                    {
                        "Gà! Bảo rồi mà không nghe, xóa game đi em!",
                        "Quá dễ! Tao còn chưa dùng đến 10% công lực!",
                        "Thắng dễ như ăn kẹo, khóc to lên xem nào!",
                        "Lần sau luyện tập thêm 10 năm nữa rồi hẵng gặp tao nhé!"
                    };
                    break;

                case GameState.Lost:
                    lines = new[]
                    {
                        "Máy tao lag 999ms thôi! Đánh lại không dám solo à?",
                        "Chuột bị kẹt nút nên bấm nhầm, ván này không tính!",
                        "Mày hên thôi, solo lại tao chấp 1 Xe luôn!",
                        "Do tao nhường mày ván này đấy, đừng có tưởng bở!"
                    };
                    break;

                case GameState.Draw:
                    lines = new[]
                    {
                        "Hòa là tao thương tình nhường mày đấy.",
                        "Đang bận nên tao tha cho đấy, tí quay lại xử sau.",
                        "Thôi hòa, coi như mày may mắn thoát chết!"
                    };
                    break;

                case GameState.TimeoutWon:
                    lines = new[]
                    {
                        "Hết giờ rồi con gà ơi! Đánh chậm rì như rùa bò!",
                        "Suy nghĩ cả buổi cháy đồng hồ, ez win!"
                    };
                    break;

                case GameState.TimeoutLost:
                    lines = new[]
                    {
                        "Ơ hết giờ á? Màn hình tao lag không hiện giờ! Bịp thật sự!"
                    };
                    break;

                default:
                    lines = new[] { "..." };
                    break;
            }
            return PickRandom(lines);
        }
    }
}
