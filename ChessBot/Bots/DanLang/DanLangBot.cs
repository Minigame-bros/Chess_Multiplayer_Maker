    using System;

namespace ChessBot.Bots.DanLang
{
    public class DanLangBot : BaseBot
    {
        public override string Name => "Dân Làng";
        public override string AvatarPath => "pack://application:,,,/ChessBot;component/Assets/Img/Dan_Lang.png";
        public override string Description => "Một nông dân thật thà, biết chút ít về cờ vua. Hay nhầm lẫn nhưng rất cố gắng.";
        public override int Elo => 1000;

        public override string GetSpeech(GameState state)
        {
            return state switch
            {
                GameState.Start => "Chào cậu, tôi mới gặt xong, làm ván cờ nhé?",
                GameState.Thinking => "Trời ơi, nước này khó nghĩ quá...",
                GameState.Moved => "Tôi đi bừa đấy, hi vọng không chết.",
                GameState.Checked => "Ấy chết, tướng tôi bị kẹt rồi à?",
                GameState.Captured => "Ui da! Mất con ngựa quý rồi.",
                GameState.Won => "Haha, tôi may mắn thôi, cậu nhường tôi phải không?",
                GameState.Lost => "Cậu đánh hay quá, tôi xin thua.",
                GameState.Draw => "Hòa là vui rồi, lát tôi mời cậu chén nước chè.",
                _ => "..."
            };
        }
    }
}
