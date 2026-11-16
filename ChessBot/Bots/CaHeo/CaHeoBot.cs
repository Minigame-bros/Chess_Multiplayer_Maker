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

        public override string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null)
        {
            int balance = GetMaterialBalance(game, PlayerColor.Black);

            string[] lines;
            switch (state)
            {
                case GameState.Start:
                    lines = new[]
                    {
                        "Éc éc! Xin chào người bạn của biển cả! Cùng tạo nên một ván cờ tuyệt đẹp nhé!",
                        "Lặn sâu vào bàn cờ nào! Éc éc, chúc cậu một ván đấu thật vui!",
                        "Chào cậu! Hôm nay sóng êm biển lặng, rất thích hợp để so tài cờ vua!",
                        "Éc éc! Cùng nhau bơi qua những nước cờ chiến thuật kỳ thú nào!"
                    };
                    break;

                case GameState.Thinking:
                    if (game != null && game.IsCheck)
                    {
                        lines = new[]
                        {
                            "Éc éc! Sóng thần ập tới rồi, phải lặn gấp thôi!",
                            "Nguy hiểm quá, một đòn tấn công xuyên làn nước!",
                            "Đợi mình lách qua khe đá ngầm này nhé, éc éc!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Thủy triều đang rút, thế trận này mình chịu nhiều áp lực thật đấy!",
                            "Cậu tạo sóng dữ dội quá, mình sắp cạn oxi rồi, éc éc!",
                            "Phải kiên cường giữ vững vùng nước này thôi!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Gió xuôi buồm thuận, đàn cá heo đang làm chủ vùng biển này rồi!",
                            "Éc éc, thế cờ mượt mà như bơi giữa đại dương xanh!",
                            "Cậu chuẩn bị đón nhận đợt sóng tấn công tiếp theo nhé!"
                        };
                    }
                    else if (IsEndgame(game))
                    {
                        lines = new[]
                        {
                            "Đã bơi đến bờ biển tàn cuộc rồi, mọi thứ rất yên bình!",
                            "Chỉ còn vài quân cờ, éc éc, cuộc đua thể lực bắt đầu!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Éc éc... Nước cờ này uốn lượn như rạn san hô vậy.",
                            "Để mình dùng sóng siêu âm quét lại toàn bộ bàn cờ nhé, éc éc!",
                            "Một dòng hải lưu thú vị... mình nên bơi theo hướng nào đây?",
                            "Suy nghĩ một chút để tìm ra nước đi đẹp mắt nhất!"
                        };
                    }
                    break;

                case GameState.Moved:
                    if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                    {
                        lines = new[]
                        {
                            "Éc éc! Chú cá nhỏ đã vượt vũ môn hóa rồng thành Hậu rồi!",
                            "Biến hình! Nàng tiên cá Hậu xuất hiện trên mặt biển!",
                            "Thăng cấp tuyệt đỉnh nơi đại dương sâu thẳm!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.IsCastle)
                    {
                        lines = new[]
                        {
                            "Nhập thành! Vua đã an tọa trong hang san hô vững chãi!",
                            "Bơi vào vùng biển lặng thôi, an toàn tuyệt đối, éc éc!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Một đợt sóng lớn đang tràn ngập khu vực trung tâm!",
                            "Éc éc, thế trận tấn công bùng nổ như cá heo vượt sóng!",
                            "Ưu thế biển cả đang thuộc về đàn cá heo!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Một cú xoay mình né sóng khéo léo để lật lại tình thế!",
                            "Kiên trì bơi ngược dòng hải lưu, không nản lòng!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Lên đường thôi! Một nước đi êm ả như lướt sóng, éc éc!",
                            "Nước này mình đã tính toán kỹ qua từng dòng chảy rồi đấy!",
                            "Một nhịp bơi nhẹ nhàng đưa quân cờ vào vị trí đắc địa!",
                            "Đỡ nước lượn sóng này xem nào, éc éc!"
                        };
                    }
                    break;

                case GameState.BotDeliveredCheck:
                    lines = new[]
                    {
                        "Éc éc! Chiếu tướng! Một đợt sóng ngầm ập đến bất ngờ chưa?",
                        "Chiếu! Cậu có né được cú nhảy vượt sóng này không?",
                        "Cảnh báo nước sâu! Chiếu tướng cậu nhé, éc éc!",
                        "Đòn tấn công siêu âm xuyên phá, chiếu tướng!"
                    };
                    break;

                case GameState.BotCaptured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "Éc éc! Bắt được chú cá voi xanh Queen khổng lồ của cậu rồi!",
                            "Một cú phóng mình ngoạn mục, con Hậu đã thuộc về đại dương!",
                            "Tuyệt vời! Bắt được Hậu rồi, thế trận mở toang, éc éc!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Xin nhẹ chiến hạm Rook nhé! Éc éc, đắm tàu rồi!",
                            "Một đòn quét đuôi chuẩn xác, con Xe đã chìm sâu dưới đáy biển!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Éc éc, thêm một chú cá nhỏ bơi vào lưới!",
                            "Kiểm soát từng dòng nước nhỏ, cảm ơn cậu nhé!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Nhặt một viên ngọc trai nhỏ trên bãi cát, éc éc!",
                            "Từng bước gom góp lợi thế cho đàn cá heo!"
                        };
                    }
                    break;

                case GameState.Checked:
                    lines = new[]
                    {
                        "Ui chao! Cậu phóng lao rát quá, mình phải lặn thật sâu thôi!",
                        "Éc! Suýt chút nữa là mắc lưới rồi, né gấp!",
                        "Đòn chiếu hiểm hóc đấy, nhưng mình đã bơi thoát an toàn!"
                    };
                    break;

                case GameState.Captured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "Éc éc... Mất Hậu rồi! Một tổn thất cực lớn nơi rạn san hô!",
                            "Ôi không! Đòn bắt Hậu của cậu chuẩn xác và sắc bén quá!",
                            "Đau xót thật đấy, nhưng đàn cá heo vẫn sẽ bơi tiếp đến cùng!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Éc! Mất một chú cá voi hộ tống rồi, cậu bắt quân hay quá!",
                            "Chiến thuyền bị đánh đắm rồi, éc éc!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Tổn thất này nằm trong dòng hải lưu mình dự liệu, éc éc!",
                            "Một sự đánh đổi để giành quyền kiểm soát trung tâm!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Một chú tốt nhỏ đã hy sinh vì tập thể, éc éc!",
                            "Cậu tiến quân chắc chắn thật đấy!"
                        };
                    }
                    break;

                case GameState.Castled:
                    lines = new[]
                    {
                        "Cậu xây pháo đài vỏ ốc vững vàng ghê, éc éc!",
                        "Một nước phòng thủ chuẩn chỉ, rất đáng khen!"
                    };
                    break;

                case GameState.Promotion:
                    lines = new[]
                    {
                        "Éc éc, cậu cũng hóa phép ra một nàng tiên cá Hậu mới rồi!",
                        "Đại dương có thêm một kỳ quan mới rồi đấy!"
                    };
                    break;

                case GameState.DrawRefused:
                    lines = new[]
                    {
                        "Éc éc! Vùng biển này còn nhiều điều kỳ thú lắm, mình muốn bơi tiếp cùng cậu!",
                        "Trận đấu đang rất hấp dẫn, ta so tài thêm chút nữa nhé!",
                        "Dòng nước vẫn đang cuộn trào, đừng dừng lại vội, éc éc!"
                    };
                    break;

                case GameState.PlayerResigned:
                    lines = new[]
                    {
                        "Cảm ơn cậu vì trận thủy chiến tuyệt vời! Lần sau lại bơi cùng nhau nhé, éc éc!",
                        "Cậu đã thi đấu rất cống hiến, hẹn gặp lại ở vùng biển mới!"
                    };
                    break;

                case GameState.Won:
                    lines = new[]
                    {
                        "Éc éc! Thắng rồi! Đàn cá heo mở hội bơi lượn ăn mừng nào!",
                        "Trận đấu quá tuyệt vời! Cậu chơi rất cừ nhưng dòng nước hôm nay ủng hộ mình hơn!",
                        "Éc éc, chiến thắng ngọt ngào như ngọn sóng dâng cao!"
                    };
                    break;

                case GameState.Lost:
                    lines = new[]
                    {
                        "Éc éc... Cậu bơi quá nhanh và xuất sắc! Tôi thua tâm phục khẩu phục!",
                        "Đỉnh cao chiến thuật đại dương! Cậu xứng đáng là vị vua biển cả!",
                        "Mình bị sóng cuốn rồi, chúc mừng chiến thắng rực rỡ của cậu!"
                    };
                    break;

                case GameState.Draw:
                    lines = new[]
                    {
                        "Một kết quả hòa tuyệt đẹp như mặt biển hoàng hôn! Éc éc, cảm ơn cậu!",
                        "Cân bằng hoàn hảo! Cả hai chúng ta đều là những kình ngư xuất sắc!"
                    };
                    break;

                case GameState.TimeoutWon:
                    lines = new[]
                    {
                        "Thời gian của cậu trôi nhanh như cát chảy qua kẽ tay rồi, éc éc!",
                        "Hết giờ rồi bạn ơi, một ván đấu kịch tính đến giây cuối cùng!"
                    };
                    break;

                case GameState.TimeoutLost:
                    lines = new[]
                    {
                        "Mải ngắm san hô suy nghĩ nên hết giờ mất rồi, éc éc!"
                    };
                    break;

                default:
                    lines = new[] { "Éc éc!" };
                    break;
            }
            return PickRandom(lines);
        }
    }
}
