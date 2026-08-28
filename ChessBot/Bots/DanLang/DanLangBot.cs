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

            public override string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null)
            {
                string[] lines;
                switch (state)
                {
                    case GameState.Start:
                        lines = new[] { "Chào cậu, tôi mới gặt xong, làm ván cờ nhé?", "Cờ quạt tí cho vui cửa vui nhà nhỉ.", "Tôi đánh kém lắm, cậu nhường tôi nhé." };
                        break;
                    case GameState.Thinking:
                        lines = new[] { "Trời ơi, nước này khó nghĩ quá...", "Để xem nào, con mã đi thế nào nhỉ?", "Tôi đang tính cẩn thận không lại đi nhầm." };
                        break;
                    case GameState.Moved:
                        if (lastMove.HasValue && lastMove.Value.Promotion.HasValue)
                        {
                            lines = new[] { "Ủa, con tốt của tôi sao lại thành con hậu thế này?", "Lên được làm quan rồi, mừng quá!" };
                        }
                        else
                        {
                            lines = new[] { "Tôi đi bừa đấy, hi vọng không chết.", "Đi nước này chắc ổn chứ?", "Thôi kệ, tới đâu thì tới." };
                        }
                        break;
                    case GameState.Checked:
                        lines = new[] { "Ấy chết, tướng tôi bị kẹt rồi à?", "Ái chà, chiếu tướng à? Tôi phải chạy đi đâu đây?", "Khoan khoan, cho tôi xin lại một nước được không?" };
                        break;
                    case GameState.Captured:
                        lines = new[] { "Ui da! Mất con ngựa quý rồi.", "Thôi chết, tôi sơ hở quá để cậu ăn mất.", "Tiếc đứt ruột, quân đó quan trọng lắm." };
                        break;
                    case GameState.Won:
                        lines = new[] { "Haha, tôi may mắn thôi, cậu nhường tôi phải không?", "Ôi chao, thắng rồi! Chắc ăn hên thôi.", "Thật không ngờ tôi lại thắng được cậu." };
                        break;
                    case GameState.Lost:
                        lines = new[] { "Cậu đánh hay quá, tôi xin thua.", "Đúng là tôi không có khiếu đánh cờ, thua lấm lưng rồi.", "Thua tâm phục khẩu phục, cậu cừ lắm!" };
                        break;
                    case GameState.Draw:
                        lines = new[] { "Hòa là vui rồi, lát tôi mời cậu chén nước chè.", "Hòa nhau nhé, ván cờ cũng lâu rồi.", "Bất phân thắng bại, vui vẻ cả làng!" };
                        break;
                    default:
                        lines = new[] { "..." };
                        break;
                }
                return lines[Rnd.Next(lines.Length)];
            }
        }
    }
