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

        public override string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null)
        {
            int balance = GetMaterialBalance(game, PlayerColor.Black);

            string[] lines;
            switch (state)
            {
                case GameState.Start:
                    lines = new[]
                    {
                        "Lũ sâu bọ các ngươi lại dám quỳ dưới chân trẫm để thách thức sao?",
                        "Ngươi sẽ sớm phải hối hận vì đã dám ngồi đối diện với đấng tối cao.",
                        "Một trận chiến không cân sức bắt đầu. Hãy dâng mạng ngươi lên cho trẫm.",
                        "Trước mặt trẫm, mọi mưu mô tầm thường của ngươi đều vô nghĩa."
                    };
                    break;

                case GameState.Thinking:
                    if (game != null && game.IsCheck)
                    {
                        lines = new[]
                        {
                            "Hừ! Kẻ hạ đẳng dám mạo phạm long thể sao?",
                            "Một mũi tên lạc vô hại, ngươi tưởng đụng chạm được tới trẫm ư?",
                            "Sự ngạo mạn này sẽ được trừng phạt bằng cái giá đắt nhất!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Cái quái gì thế này?! Thế trận của trẫm sao lại có vết rạn nứt?!",
                            "Không thể nào... Ngươi giở trò quỷ quyệt gì sau lưng trẫm?",
                            "Câm miệng! Bậc đế vương sẽ tự tay lật ngược càn khôn này!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Số phận của ngươi đã an bài dưới gót chân trẫm.",
                            "Trẫm đang cân nhắc xem nên ban cho ngươi kết cục thế nào cho hoành tráng.",
                            "Kết cục đã điểm, ngươi đã cảm nhận được cái lạnh của lưỡi gươm chưa?"
                        };
                    }
                    else if (IsEndgame(game))
                    {
                        lines = new[]
                        {
                            "Màn đêm buông xuống, tàn quân của ngươi sắp bị chôn vùi vĩnh viễn.",
                            "Một tàn cuộc định mệnh dưới sự thống trị của đấng quân vương."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Một ván cờ quá tẻ nhạt... Trẫm đã nhìn thấu 30 nước tiếp theo của ngươi.",
                            "Mọi giãy giụa của ngươi chỉ là những nét vẽ vô vọng trên bức tranh thất bại.",
                            "Chỉ là vấn đề thời gian trước khi vương triều của ngươi sụp đổ hoàn toàn.",
                            "Hãy tận hưởng những hơi thở tự do cuối cùng đi."
                        };
                    }
                    break;

                case GameState.Moved:
                    if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                    {
                        lines = new[]
                        {
                            "Trẫm phong thánh cho bầy tôi trung dũng! Một Hoàng Hậu mới tái sinh!",
                            "Sự trỗi dậy tàn khốc của đế chế vĩ đại!",
                            "Hai Hoàng Hậu ngự trị, ngày tàn của ngươi đã tới!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.IsCastle)
                    {
                        lines = new[]
                        {
                            "Long ngai đã được phong ấn kiên cố. Ngươi vô phương chạm tới!",
                            "Trẫm điều binh khiển tướng từ hoàng cung an toàn tuyệt đối."
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Một bước tiến quân dũng mãnh, đè bẹp mọi sự kháng cự!",
                            "Thiên mệnh đã định, sự hủy diệt đang ập xuống đầu ngươi!",
                            "Vương triều của trẫm đang nghiền nát tất cả!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Bậc đế vương không bao giờ lùi bước trước nghịch cảnh!",
                            "Trẫm sẽ cho ngươi thấy thế nào là sức mạnh của đấng tối cao!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Quỳ xuống trước uy quyền của trẫm!",
                            "Đây là nước cờ định đoạt số phận của cả vương quốc.",
                            "Chống cự chỉ làm tăng thêm nỗi nhục nhã và đau đớn cho ngươi mà thôi.",
                            "Mỗi bước đi của trẫm đều là chân lý bất diệt!"
                        };
                    }
                    break;

                case GameState.BotDeliveredCheck:
                    lines = new[]
                    {
                        "CHIẾU TƯỚNG! Quỳ xuống tạ tội trước mặt đấng quân vương!",
                        "Lưỡi gươm hoàng gia đã kề sát cổ ngươi! Trốn đi đâu?",
                        "Chiếu! Hãy run rẩy trước cơn thịnh nộ của đế vương!",
                        "Trẫm tuyên án tử hình cho quân vương của ngươi!"
                    };
                    break;

                case GameState.BotCaptured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "HẬU CỦA NGƯƠI ĐÃ BỊ XÓA SỔ KHỎI BẢN ĐỒ! Quỳ xuống khóc lóc đi!",
                            "Cột trụ sụp đổ, vương triều của ngươi chính thức diệt vong từ giây phút này!",
                            "Bắt được Hậu rồi! Sự sỉ nhục này ngươi sẽ phải mang theo suốt đời!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Xe của ngươi đã tan thành tro bụi dưới đại bác của trẫm!",
                            "Mất đi cánh tay đắc lực, ngươi lấy gì chống lại thiên mệnh?"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Một tên lính quèn bị nghiền nát dưới bánh xe lịch sử.",
                            "Quân cờ hạ đẳng này không xứng đáng hiện diện trên chiến trường của trẫm!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Giẫm đạp lên từng tấc đất của ngươi.",
                            "Không một hạt cát nào của ngươi thoát khỏi sự cai trị của trẫm."
                        };
                    }
                    break;

                case GameState.Checked:
                    lines = new[]
                    {
                        "Hỗn xược! Dám chỉ gươm vào long nhan sao?",
                        "Một hành động ngông cuồng vô nghĩa của kẻ sắp chết!",
                        "Trẫm chỉ cần lùi một bước để ngươi bước vào cõi chết!"
                    };
                    break;

                case GameState.Captured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "KHÔNG THỂ NÀO! Ngươi dám hạ sát Hoàng Hậu tôn quý của trẫm?!",
                            "NGƯƠI PHẢI ĐỀN MẠNG! Trẫm sẽ biến ngươi thành tro bụi!",
                            "Một sai sót không thể tha thứ... Nhưng trẫm sẽ tự tay xé xác ngươi!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Khá khen cho sự liều lĩnh của ngươi, nhưng ngươi sẽ phải trả bằng máu!",
                            "Một tổn thất nhỏ cho đế chế, ngươi chớ vội đắc ý!"
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Thí một con cờ phế để giăng bẫy tử thần cho ngươi, đồ ngu xuẩn!",
                            "Sự hy sinh anh dũng vì vinh quang vĩnh cửu của đế quốc!"
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Ngươi tưởng ăn được một con tốt là thắng được vương triều sao?",
                            "Tên tốt đó đã hoàn thành sứ mệnh làm mồi nhử cho ngươi."
                        };
                    }
                    break;

                case GameState.Castled:
                    lines = new[]
                    {
                        "Trốn vào xó tối như một con chuột nhắt! Trẫm sẽ san phẳng tất cả!",
                        "Thành trì đất cát của ngươi không ngăn nổi vó ngựa hoàng gia!"
                    };
                    break;

                case GameState.Promotion:
                    lines = new[]
                    {
                        "Ngươi dám tự xưng vương phong hậu trước mặt trẫm sao? Hỗn xược!",
                        "Một con tốt hạ đẳng đổi đời cũng không thay đổi được kết cục diệt vong!"
                    };
                    break;

                case GameState.DrawRefused:
                    lines = new[]
                    {
                        "Một kẻ sâu bọ lại dám mở miệng xin hòa với bậc đế vương sao? CÂM MIỆNG!",
                        "Cầu hòa là sự sỉ nhục đối với trẫm! Ngươi phải chết trên bàn cờ này!",
                        "Trẫm chỉ chấp nhận chiến thắng tuyệt đối, không có thỏa hiệp!"
                    };
                    break;

                case GameState.PlayerResigned:
                    lines = new[]
                    {
                        "Biết quỳ gối đầu hàng là chút trí khôn sót lại của loài sâu bọ!",
                        "Rút lui trong nhục nhã! Ngươi không bao giờ xứng làm đối thủ của trẫm!",
                        "Trẫm chấp nhận sự quy phục hèn hạ của ngươi!"
                    };
                    break;

                case GameState.Won:
                    lines = new[]
                    {
                        "HẾT CỜ! Kẻ yếu đuối muôn đời vẫn là kẻ hạ đẳng dưới chân trẫm!",
                        "TRẪM LÀ BẤT BẠI! Lịch sử sẽ khắc ghi sự thảm bại ê chề này của ngươi!",
                        "Ngươi thậm chí không xứng đáng để trẫm ghi nhớ tên trong biên niên sử!"
                    };
                    break;

                case GameState.Lost:
                    lines = new[]
                    {
                        "KHÔNG... KHÔNG THỂ NÀO!! Ngươi dùng tà thuật gì?! ĐÂY LÀ BỊP BỢM!",
                        "Sự sụp đổ của một đế chế... Trẫm không thể tin nổi vào mắt mình...",
                        "Một ngày đen tối nhất của lịch sử... Ngươi... ngươi đã làm được điều không tưởng..."
                    };
                    break;

                case GameState.Draw:
                    lines = new[]
                    {
                        "Hừ, coi như hôm nay trẫm rủ lòng từ bi tha cho ngươi giữ lấy cái mạng hèn mọn.",
                        "Một trận chiến tạm đình chỉ. Hãy tạ ơn sự khoan dung của trẫm đi!"
                    };
                    break;

                case GameState.TimeoutWon:
                    lines = new[]
                    {
                        "Thời gian của ngươi đã cạn kiệt cùng với sinh mệnh của ngươi!",
                        "Kẻ chần chừ do dự không xứng đáng đứng trước mặt trẫm! Hết giờ!"
                    };
                    break;

                case GameState.TimeoutLost:
                    lines = new[]
                    {
                        "Chiếc đồng hồ chết tiệt này dám phản bội trẫm sao?! Trẫm sẽ chém đầu kẻ chế tạo ra nó!"
                    };
                    break;

                default:
                    lines = new[] { "Hừm." };
                    break;
            }
            return PickRandom(lines);
        }
    }
}
