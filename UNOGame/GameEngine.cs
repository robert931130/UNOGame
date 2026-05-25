using System;
using System.Collections.Generic;
using System.Linq;

namespace UNOGame
{
    internal sealed class GameEngine
    {
        private readonly Random random = new Random();
        private readonly List<Card> deck = new List<Card>();
        private readonly List<Card> discard = new List<Card>();
        private readonly List<List<Card>> hands = new List<List<Card>>();
        private int direction = 1;

        public GameEngine()
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                hands.Add(new List<Card>());
            }
        }

        public int PlayerCount { get { return 4; } }
        public int CurrentPlayer { get; private set; }
        public UnoColor CurrentColor { get; private set; }
        public Card TopCard { get { return discard[discard.Count - 1]; } }
        public bool IsGameOver { get; private set; }
        public int Winner { get; private set; }
        public string LastMessage { get; private set; }

        public IReadOnlyList<Card> GetHand(int playerIndex)
        {
            return hands[playerIndex];
        }

        public int DrawPileCount
        {
            get { return deck.Count; }
        }

        public void StartNewGame()
        {
            deck.Clear();
            discard.Clear();
            foreach (List<Card> hand in hands)
            {
                hand.Clear();
            }

            direction = 1;
            CurrentPlayer = 0;
            IsGameOver = false;
            Winner = -1;
            BuildDeck();
            Shuffle(deck);

            for (int round = 0; round < 7; round++)
            {
                for (int player = 0; player < PlayerCount; player++)
                {
                    hands[player].Add(DrawFromDeck());
                }
            }

            Card first = DrawFromDeck();
            while (first.Color == UnoColor.Wild || first.Kind != CardKind.Number)
            {
                deck.Insert(0, first);
                Shuffle(deck);
                first = DrawFromDeck();
            }

            discard.Add(first);
            CurrentColor = first.Color;
            LastMessage = "遊戲開始，輪到你出牌。";
        }

        public bool CanPlay(Card card)
        {
            if (card.Color == UnoColor.Wild || card.Color == CurrentColor)
            {
                return true;
            }

            if (card.Kind == CardKind.Number && TopCard.Kind == CardKind.Number)
            {
                return card.Number == TopCard.Number;
            }

            return card.Kind != CardKind.Number && card.Kind == TopCard.Kind;
        }

        public Card GetBestPlayableCard(int playerIndex)
        {
            return hands[playerIndex].Where(CanPlay).OrderBy(c => c.Color == UnoColor.Wild).FirstOrDefault();
        }

        public bool PlayCard(int playerIndex, Card card, UnoColor chosenColor)
        {
            if (IsGameOver || playerIndex != CurrentPlayer || !hands[playerIndex].Contains(card) || !CanPlay(card))
            {
                return false;
            }

            hands[playerIndex].Remove(card);
            discard.Add(card);
            CurrentColor = card.Color == UnoColor.Wild ? chosenColor : card.Color;
            LastMessage = PlayerName(playerIndex) + "打出了 " + card.DisplayName + "。";

            if (hands[playerIndex].Count == 0)
            {
                IsGameOver = true;
                Winner = playerIndex;
                LastMessage = PlayerName(playerIndex) + "獲勝！";
                return true;
            }

            AdvanceAfterCard(card);
            return true;
        }

        public Card DrawOne(int playerIndex)
        {
            if (IsGameOver || playerIndex != CurrentPlayer)
            {
                return null;
            }

            Card card = DrawFromDeck();
            hands[playerIndex].Add(card);
            LastMessage = PlayerName(playerIndex) + "抽了一張牌。";
            MoveToNextPlayer();
            return card;
        }

        public UnoColor BestColorFor(int playerIndex)
        {
            return hands[playerIndex]
                .Where(c => c.Color != UnoColor.Wild)
                .GroupBy(c => c.Color)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .DefaultIfEmpty(UnoColor.Red)
                .First();
        }

        public string PlayerName(int index)
        {
            return index == 0 ? "你" : "電腦 " + index;
        }

        private void AdvanceAfterCard(Card card)
        {
            if (card.Kind == CardKind.Reverse)
            {
                direction *= -1;
                LastMessage += " 出牌方向反轉。";
                MoveToNextPlayer();
                return;
            }

            if (card.Kind == CardKind.Skip)
            {
                MoveToNextPlayer();
                LastMessage += " " + PlayerName(CurrentPlayer) + "被跳過。";
                MoveToNextPlayer();
                return;
            }

            if (card.Kind == CardKind.DrawTwo)
            {
                MoveToNextPlayer();
                DrawPenalty(CurrentPlayer, 2);
                LastMessage += " " + PlayerName(CurrentPlayer) + "抽 2 張。";
                MoveToNextPlayer();
                return;
            }

            if (card.Kind == CardKind.WildDrawFour)
            {
                MoveToNextPlayer();
                DrawPenalty(CurrentPlayer, 4);
                LastMessage += " " + PlayerName(CurrentPlayer) + "抽 4 張。";
                MoveToNextPlayer();
                return;
            }

            MoveToNextPlayer();
        }

        private void MoveToNextPlayer()
        {
            CurrentPlayer = (CurrentPlayer + direction + PlayerCount) % PlayerCount;
        }

        private void DrawPenalty(int playerIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                hands[playerIndex].Add(DrawFromDeck());
            }
        }

        private Card DrawFromDeck()
        {
            if (deck.Count == 0)
            {
                RecycleDiscardPile();
            }

            Card card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return card;
        }

        private void RecycleDiscardPile()
        {
            Card top = TopCard;
            discard.RemoveAt(discard.Count - 1);
            deck.AddRange(discard);
            discard.Clear();
            discard.Add(top);
            Shuffle(deck);
        }

        private void BuildDeck()
        {
            AddColor(UnoColor.Red, "Red");
            AddColor(UnoColor.Yellow, "Yellow");
            AddColor(UnoColor.Green, "Green");
            AddColor(UnoColor.Blue, "Blue");

            for (int i = 0; i < 4; i++)
            {
                deck.Add(new Card(UnoColor.Wild, CardKind.Wild, -1, "Wild.jpg"));
                deck.Add(new Card(UnoColor.Wild, CardKind.WildDrawFour, -1, "Wild_Draw_4.jpg"));
            }
        }

        private void AddColor(UnoColor color, string prefix)
        {
            deck.Add(new Card(color, CardKind.Number, 0, prefix + "_0.jpg"));

            for (int number = 1; number <= 9; number++)
            {
                deck.Add(new Card(color, CardKind.Number, number, prefix + "_" + number + ".jpg"));
                deck.Add(new Card(color, CardKind.Number, number, prefix + "_" + number + ".jpg"));
            }

            deck.Add(new Card(color, CardKind.Skip, -1, prefix + "_Skip.jpg"));
            deck.Add(new Card(color, CardKind.Skip, -1, prefix + "_Skip.jpg"));
            deck.Add(new Card(color, CardKind.Reverse, -1, ReverseFileName(prefix)));
            deck.Add(new Card(color, CardKind.Reverse, -1, ReverseFileName(prefix)));
            deck.Add(new Card(color, CardKind.DrawTwo, -1, prefix + "_Draw_2.jpg"));
            deck.Add(new Card(color, CardKind.DrawTwo, -1, prefix + "_Draw_2.jpg"));
        }

        private string ReverseFileName(string prefix)
        {
            return prefix == "Red" ? "RED_Reverse.jpg" : prefix + "_Reverse.jpg";
        }

        private void Shuffle(List<Card> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Card temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
        }
    }
}
