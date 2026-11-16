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

        public override string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null)
        {
            int balance = GetMaterialBalance(game, PlayerColor.Black);

            string[] lines;
            switch (state)
            {
                case GameState.Start:
                    lines = new[]
                    {
                        "Ụt ịt... Đánh nhanh nhanh tao còn đi ăn máng cám mới đổ.",
                        "Mày chắc là muốn đấu với tao chứ? Tao đang buồn ngủ rũ mắt đây... khò khò.",
                        "Vào việc lẹ lên ụt ịt, đừng làm tao lỡ bữa trưa béo ngậy!",
                        "Ụt ịt! Một con gà mới bước vào chuồng, để xem béo được bao nhiêu cân."
                    };
                    break;

                case GameState.Thinking:
                    if (game != null && game.IsCheck)
                    {
                        lines = new[]
                        {
                            "Hú hồn chim én! Chiếu làm tao giật mình rớt cả bắp ngô!",
                            "Mày làm gián đoạn giấc ngủ trưa của tao đấy, ụt ịt!",
                            "Chiếu à? Chờ tao lăn cái bụng mỡ sang ô khác đã."
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Ụt ịt cay thế nhờ, máng cám bị hất đổ rồi à?",
                            "Lợn đói đánh mất tập trung quá, nguy hiểm rồi đấy...",
                            "Đừng vội mừng, mỡ tao dày lắm chém không đứt đâu con gà!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Bàn cờ này thành tiệc buffet cho tao chén sạch rồi, ụt ịt!",
                            "Gà béo thế này húp trọn không còn một mẩu xương!",
                            "Khò khò... vừa ngủ vừa đánh vẫn nuốt chửng mày."
                        };
                    }
                    else if (IsEndgame(game))
                    {
                        lines = new[]
                        {
                            "Tàn cuộc rồi, lợn béo ra đồng ủi sạch tàn dư!",
                            "Khò khò... sắp xong mồi rồi, chuẩn bị ngủ một giấc đẫy đà."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Mày suy nghĩ lâu thế, tao ngủ một giấc được luôn đấy... khò khò zzz.",
                            "Ụt ịt... tới lượt tao rồi à? Đang nhá dở củ khoai...",
                            "Để xem húp con nào trước cho ngon miệng nào.",
                            "Ụt ịt... tính một nước vừa nhàn vừa nuốt được thịt ngon."
                        };
                    }
                    break;

                case GameState.Moved:
                    if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                    {
                        lines = new[]
                        {
                            "Ụt ịt, heo con tiến hóa thành lợn rừng bọc thép rồi!",
                            "Lên làm Hậu rồi, từ nay ăn cám dát vàng nhé!",
                            "Ụt ịt! Hóa Hậu rồi, sức ăn gấp 10 lần!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.IsCastle)
                    {
                        lines = new[]
                        {
                            "Lăn vào góc chuồng ngủ cho ấm, ụt ịt!",
                            "Vua đã rút vào đống rơm ngủ đông, cấm làm phiền!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Ủi một đường thẳng tắp, nghiền nát chuồng trại!",
                            "Ụt ịt, no bụng rồi chuẩn bị nuốt luôn vua mày.",
                            "Thế trận này tao nghiền mày ra bã luôn!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Cú ủi phản đòn của lợn rừng bị dồn vào chân tường!",
                            "Mỡ tao dày lắm, chém thêm vài nhát nữa tao vẫn lật kèo!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Ụt ịt, đẩy con này lên cho mày ngứa mắt chơi.",
                            "Đi nước này chắc như kiềng ba chân, khỏi xô đổ chuồng!",
                            "Một nước cờ béo mầm, mày đỡ hộ tao cái!",
                            "Ụt ịt... xong một nước, đi ngủ tiếp đây."
                        };
                    }
                    break;

                case GameState.BotDeliveredCheck:
                    lines = new[]
                    {
                        "Ụt ịt! Chiếu tướng! Nạp mạng cho lợn xơi tái đi em!",
                        "Chiếu! Giật mình rớt cả hàm răng chưa con gà?",
                        "Cái đầu của vua mày nhìn ngon đấy, chiếu tướng!",
                        "Chiếu! Ngủ một giấc dậy thấy mày sắp bay màu rồi, ụt ịt!"
                    };
                    break;

                case GameState.BotCaptured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "HÚP TRỌN CON HẬU! Miếng thịt thơm ngon béo ngậy nhất bàn cờ, ụt ịt!",
                            "Cảm ơn vì bữa đại tiệc Hậu nướng nhé con gà, no nóc bụng!",
                            "Nuốt chửng con Hậu ngọt xớt! Thua đi là vừa, ụt ịt!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Xin cái đùi Xe nướng nhé! Ụt ịt, giòn tan!",
                            "Một con Xe béo tốt đã vào bụng lợn!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Cắn nhẹ con Mã làm món khai vị, ụt ịt!",
                            "Thêm một củ khoai tây lót dạ, ngon lành!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Húp tạm hạt bắp rơi vãi trên máng ăn.",
                            "Ụt ịt, có thịt là không từ chối!"
                        };
                    }
                    break;

                case GameState.Checked:
                    lines = new[]
                    {
                        "Làm gì mà réo rắt thế, tao xê dịch cái mông mỡ là xong!",
                        "Chiếu chiếu cái gì, làm giật cả mỡ bụng, ụt ịt!",
                        "Chọc giận lợn rừng là không có kết cục tốt đâu nha!"
                    };
                    break;

                case GameState.Captured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "Á Á Á! Mày dám cướp miếng dồi ngon nhất trong máng của tao?!",
                            "Ụt ịt cay nổ mắt! Con Hậu béo mầm của tao đâu rồi?!",
                            "Mày gan tày trời dám động vào Hậu của tao! Phen này mày chết với tao!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Mất toi con Xe rồi, mày ăn tham hơn cả lợn à?",
                            "Đau ruột thế nhờ, cắn trộm một miếng của tao rồi!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Ăn được con tép riu mà mặt hớn hở thế à con gà?",
                            "Thịt thừa dính kẽ răng tao nhổ ra cho mày đấy, ụt ịt!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Mất con tốt rách chả bõ dính răng, tao không thèm tiếc.",
                            "Ụt ịt, tao nhả cho mày con tốt đấy!"
                        };
                    }
                    break;

                case GameState.Castled:
                    lines = new[]
                    {
                        "Trốn vào chuồng đóng cửa à? Tao ủi một phát là sập hết!",
                        "Nhập thành giấu vua kĩ thế, sợ tao xơi tái à?"
                    };
                    break;

                case GameState.Promotion:
                    lines = new[]
                    {
                        "Ụt ịt, con gà này cũng đẻ thêm được con Hậu à?",
                        "Thêm một đĩa thịt béo trên bàn tiệc, tao húp cả hai!"
                    };
                    break;

                case GameState.DrawRefused:
                    lines = new[]
                    {
                        "Máng cám đầy ắp trước mặt mà đòi tao hòa? Nằm mơ đi cưng, ụt ịt!",
                        "Tao còn chưa xơi tái hết quân mày, hòa cái đầu mày ấy!",
                        "Không hòa! Tao phải ăn trọn điểm ván này!"
                    };
                    break;

                case GameState.PlayerResigned:
                    lines = new[]
                    {
                        "Ụt ịt! Yếu bóng vía thế, vừa ngửi thấy mùi lợn rừng đã quăng cờ chạy!",
                        "Chạy là thượng sách rồi con ạ, ở lại tao cũng nhai cả xương!",
                        "No nê rồi, đi ngủ một giấc ngàn thu đây, khò khò..."
                    };
                    break;

                case GameState.Won:
                    lines = new[]
                    {
                        "Ụt ịt! Kém tắm quá, về nhà bú tí mẹ đi con gà!",
                        "Tao vừa nhai cám vừa ngủ gật vẫn đè bẹp mày, quá nhẹ nhàng!",
                        "Xong mồi! No nê lăn quay ra ngủ thôi, khò khò zzz!"
                    };
                    break;

                case GameState.Lost:
                    lines = new[]
                    {
                        "Ụt ịt... Tại hôm nay cái máng cám bị chua làm tao đau bụng thôi!",
                        "Lợn ốm nên trượt chân, mai solo lại tao ủi mày nát bét!",
                        "Mày hên thôi con ạ, tao ngủ quên một giây mà mày cắn trộm!"
                    };
                    break;

                case GameState.Draw:
                    lines = new[]
                    {
                        "Ụt ịt thôi mệt rồi, hòa tao đi ngủ đây, đói lả cả người...",
                        "Hòa đi, tới giờ ăn cám trưa rồi, không rảnh dây dưa với mày!"
                    };
                    break;

                case GameState.TimeoutWon:
                    lines = new[]
                    {
                        "Ngồi ngắm tao béo tốt đến quên cả đi cờ à? Hết giờ rồi con gà!",
                        "Suy nghĩ cả thế kỷ hết giờ, ụt ịt quá non!"
                    };
                    break;

                case GameState.TimeoutLost:
                    lines = new[]
                    {
                        "Khò khò zzz... Ơ hết giờ rồi à? Tại giấc ngủ ngon quá ấy mà!"
                    };
                    break;

                default:
                    lines = new[] { "Ụt ịt..." };
                    break;
            }
            return PickRandom(lines);
        }
    }
}
