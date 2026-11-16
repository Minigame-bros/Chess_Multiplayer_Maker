using System;
using ChessCore;

namespace ChessBot.Bots.DanLang
{
    public class DanLangBot : BaseBot
    {
        public override string Name => "Dân Làng";
        public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Dan_Lang.png";
        public override string Description => "Một nông dân thật thà, biết chút ít về cờ vua. Hay nhầm lẫn nhưng rất cố gắng.";
        public override int Elo => 1000;

        public override string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null)
        {
            int balance = GetMaterialBalance(game, PlayerColor.Black);

            string[] lines;
            switch (state)
            {
                case GameState.Start:
                    lines = new[]
                    {
                        "Chào cậu! Mới gặt xong ruộng lúa, làm ván cờ cho ấm người nhé!",
                        "Bác nông dân này đánh kém lắm, cậu nhẹ tay thôi nha.",
                        "Vừa pha ấm chè tươi, mời cậu một chén rồi ta vào việc.",
                        "Nông nhàn rảnh rỗi làm ván cờ giao lưu học hỏi là vui nhất trần đời."
                    };
                    break;

                case GameState.Thinking:
                    if (game != null && game.IsCheck)
                    {
                        lines = new[]
                        {
                            "Úi giời ơi, chiếu tướng rồi à? Chạy đằng nào bây giờ...",
                            "Căng quá căng quá, bác phải tìm đường thoát thân thôi!",
                            "Chết dở, lửa bén đến chân rồi, chạy mau kẻo cháy cả rơm!"
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Mất mùa rồi bà con ơi, quân mình rụng như lúa gặp bão...",
                            "Thế trận nguy ngập quá, cậu đánh ác chiến thật đấy.",
                            "Ruộng sâu nước lớn thế này biết bơi ngả nào đây..."
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Năm nay trúng mùa rồi, thế cờ sáng sủa ghê!",
                            "Cậu sơ ý rồi, tôi chuẩn bị gặt hái vụ mùa bội thu đây.",
                            "Bác nông dân này mà đánh cũng ra trò đấy chứ nhỉ!"
                        };
                    }
                    else if (IsEndgame(game))
                    {
                        lines = new[]
                        {
                            "Về cuối vụ rồi, chỉ còn vài cọng rơm trên ruộng thôi.",
                            "Tàn cuộc rồi, dắt vua đi bộ dạo quanh bờ đê nào."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Để xem nào... nước này khó như cuốc phải đá tảng vậy.",
                            "Con Mã này nhảy chữ L hay chữ gì nhỉ, nhức đầu ghê...",
                            "Tôi đang tính cẩn thận, không lại đi toi con bò cày.",
                            "Cờ quạt thế này tính trước tính sau đau cả đầu."
                        };
                    }
                    break;

                case GameState.Moved:
                    if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                    {
                        lines = new[]
                        {
                            "Bác tốt này cày bừa vất vả, nay được thăng làm Quan Hậu rồi!",
                            "Từ bác nông dân thành bà Hoàng Hậu, mừng rớt nước mắt!",
                            "Vượt khó thành tài, con tốt thành quan rồi làng nước ơi!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.IsCastle)
                    {
                        lines = new[]
                        {
                            "Nhập thành cho an toàn, như cất thóc vào bồ vậy!",
                            "Vua chui vào hầm tránh bão rồi nhé cậu!"
                        };
                    }
                    else if (balance >= 4)
                    {
                        lines = new[]
                        {
                            "Thế cờ tốt quá, giống như lúa trổ đòng đòng!",
                            "Tôi xin phép ép sân một chút nhé cậu ơi."
                        };
                    }
                    else if (balance <= -4)
                    {
                        lines = new[]
                        {
                            "Khó khăn trăm bề nhưng người nông dân không bỏ cuộc!",
                            "Cố gắng gỡ gạc lại chút ít hoa màu xem sao."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Tôi đi bừa một nước, hi vọng không bước hụt xuống mương.",
                            "Nước này tôi tính theo mùa vụ đấy, chắc ăn!",
                            "Thôi kệ, hạt thóc gieo xuống rồi thì đợi nảy mầm thôi.",
                            "Đi nước này cho chắc xóm chắc làng, không vội được đâu."
                        };
                    }
                    break;

                case GameState.BotDeliveredCheck:
                    lines = new[]
                    {
                        "Ái chà, tôi chiếu tướng cậu đấy! Cậu đỡ giúp tôi xem nào.",
                        "Chiếu tướng rồi! Coi chừng mất cờ nha cậu!",
                        "Úi, hình như tôi vừa chiếu trúng cậu rồi!",
                        "Chiếu một phát như sét đánh ngang tai giữa đồng ruộng!"
                    };
                    break;

                case GameState.BotCaptured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "Ối dồi ôi, tôi bắt được con Hậu to đùng của cậu rồi!",
                            "Tiếc cho cậu quá, con Hậu quý giá thế mà lọt vào tay lão nông!",
                            "Mẻ cá này to quá, bắt được cả bà Hậu rồi bà con ơi!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Tôi xin phép dắt cái Xe này về kéo lúa nhé!",
                            "Xe to thế này chở thóc về làng thì tốt biết mấy."
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Tôi xin con ngựa quý này về chuồng nhé, cậu đừng giận.",
                            "Gặt thêm được một nhánh lúa vàng rồi."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Nhặt tạm một hạt thóc con con rơi vãi.",
                            "Tôi xin con tốt này nhé cậu, tích tiểu thành đại!"
                        };
                    }
                    break;

                case GameState.Checked:
                    lines = new[]
                    {
                        "Ấy chết! Chiếu tướng à, tôi chạy vội kẻo vỡ đầu!",
                        "Khoan khoan, bác chạy sang ô này chắc thoát nạn rồi.",
                        "Cậu tấn công bất ngờ quá, tí nữa thì toi đời!"
                    };
                    break;

                case GameState.Captured:
                    if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Queen)
                    {
                        lines = new[]
                        {
                            "Ối giời ôi! Mất bà Hậu của tôi rồi, toi công cày cuốc cả vụ!",
                            "Cậu bắt con Hậu nhanh như cắt lúa thế, đau lòng quá!",
                            "Phen này về làng chắc bị các cụ cười cho thối mũi!"
                        };
                    }
                    else if (lastMove.HasValue && lastMove.Value.CapturedPiece == PieceType.Rook)
                    {
                        lines = new[]
                        {
                            "Úi chà, mất toi cái xe chở thóc của tôi rồi!",
                            "Tiếc đứt ruột con Xe, cậu ra tay nặng quá."
                        };
                    }
                    else if (lastMove.HasValue && (lastMove.Value.CapturedPiece == PieceType.Knight || lastMove.Value.CapturedPiece == PieceType.Bishop))
                    {
                        lines = new[]
                        {
                            "Ui da! Mất con ngựa quý rồi, lấy gì đi tuần ruộng đây?",
                            "Sơ sểnh một tí là cậu hốt mất quân của tôi ngay."
                        };
                    }
                    else
                    {
                        lines = new[]
                        {
                            "Thôi chết, tôi sơ hở quá để cậu ăn mất một chú tốt.",
                            "Mất một quân tốt rồi, sót ruột quá đi mất."
                        };
                    }
                    break;

                case GameState.Castled:
                    lines = new[]
                    {
                        "Cậu làm cái nhà kiên cố thế, bao giờ tôi mới vào được đây?",
                        "Nhập thành cẩn thận ghê, đúng là người chơi bài bản!"
                    };
                    break;

                case GameState.Promotion:
                    lines = new[]
                    {
                        "Ối dào, cậu cũng hóa phép con tốt thành Hậu rồi à?",
                        "Cậu có Hậu mới rồi, phen này bác nông dân lại vất vả rồi."
                    };
                    break;

                case GameState.DrawRefused:
                    lines = new[]
                    {
                        "Thôi, lúa chưa gặt xong thì chưa về, đánh tiếp cho trọn vẹn cậu ơi!",
                        "Tôi thấy ván này còn đang dang dở, ta chơi tiếp tí nữa nhé!",
                        "Cứ bình tĩnh chơi tiếp đi cậu, vội vàng làm gì đâu!"
                    };
                    break;

                case GameState.PlayerResigned:
                    lines = new[]
                    {
                        "Ủa cậu nghỉ sớm thế à? Thôi mệt thì nghỉ, bữa sau lại sang chơi với tôi nhé!",
                        "Cậu nhường tôi ván này rồi à? Cảm ơn cậu nhiều nhé!"
                    };
                    break;

                case GameState.Won:
                    lines = new[]
                    {
                        "Ối giời, tôi thắng thật này! May hơn khôn thôi, cậu nhường tôi đúng không?",
                        "Vui quá làng nước ơi, lão nông thắng được ván cờ rồi!",
                        "Cảm ơn cậu nhé, chắc hôm nay thần lúa độ tôi rồi!"
                    };
                    break;

                case GameState.Lost:
                    lines = new[]
                    {
                        "Cậu đánh đỉnh thật đấy, tôi xin thua tâm phục khẩu phục!",
                        "Thua lấm lưng trắng bụng rồi, tôi phải về cày ruộng tiếp thôi!",
                        "Cậu cừ lắm, khi nào rảnh lại dạy tôi vài nước nữa nhé!"
                    };
                    break;

                case GameState.Draw:
                    lines = new[]
                    {
                        "Hòa là đẹp cả đôi đường rồi! Mời cậu chén nước chè xanh!",
                        "Bất phân thắng bại, làng xóm hòa thuận vui vẻ cả làng!",
                        "Hòa nhau là vui vẻ nhất, không ai buồn ai giận!"
                    };
                    break;

                case GameState.TimeoutWon:
                    lines = new[]
                    {
                        "Ấy chết, đồng hồ cậu kêu reng reng hết giờ rồi kìa!",
                        "Cậu mải suy nghĩ quá quên cả thời gian mất rồi!"
                    };
                    break;

                case GameState.TimeoutLost:
                    lines = new[]
                    {
                        "Mắt mũi kèm nhem nhìn không rõ đồng hồ, hết giờ lúc nào không hay!"
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
