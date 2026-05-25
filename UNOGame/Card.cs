namespace UNOGame
{
    internal enum UnoColor
    {
        Red,
        Yellow,
        Green,
        Blue,
        Wild
    }

    internal enum CardKind
    {
        Number,
        Skip,
        Reverse,
        DrawTwo,
        Wild,
        WildDrawFour
    }

    internal sealed class Card
    {
        public Card(UnoColor color, CardKind kind, int number, string imageFile)
        {
            Color = color;
            Kind = kind;
            Number = number;
            ImageFile = imageFile;
        }

        public UnoColor Color { get; }
        public CardKind Kind { get; }
        public int Number { get; }
        public string ImageFile { get; }

        public string DisplayName
        {
            get
            {
                if (Kind == CardKind.Number)
                {
                    return ColorName + " " + Number;
                }

                if (Kind == CardKind.Wild)
                {
                    return "萬用牌";
                }

                if (Kind == CardKind.WildDrawFour)
                {
                    return "萬用 +4";
                }

                return ColorName + " " + KindName;
            }
        }

        public string ColorName
        {
            get
            {
                switch (Color)
                {
                    case UnoColor.Red: return "紅色";
                    case UnoColor.Yellow: return "黃色";
                    case UnoColor.Green: return "綠色";
                    case UnoColor.Blue: return "藍色";
                    default: return "萬用";
                }
            }
        }

        public string KindName
        {
            get
            {
                switch (Kind)
                {
                    case CardKind.Skip: return "禁止";
                    case CardKind.Reverse: return "迴轉";
                    case CardKind.DrawTwo: return "+2";
                    case CardKind.Wild: return "萬用";
                    case CardKind.WildDrawFour: return "+4";
                    default: return Number.ToString();
                }
            }
        }
    }
}
