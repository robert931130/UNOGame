using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UNOGame
{
    internal sealed class MainForm : Form
    {
        private readonly GameEngine game = new GameEngine();
        private readonly Dictionary<string, Image> imageCache = new Dictionary<string, Image>();
        private readonly Timer computerTimer = new Timer();
        private readonly Timer animationTimer = new Timer();
        private readonly PlayerHandView playerHandView = new PlayerHandView();
        private readonly ComputerHandView[] computerViews = new ComputerHandView[3];
        private readonly PictureBox discardPicture = new PictureBox();
        private readonly PictureBox drawPicture = new PictureBox();
        private readonly PictureBox animationCard = new PictureBox();
        private readonly Label statusLabel = new Label();
        private readonly Label currentColorLabel = new Label();
        private readonly Label drawCountLabel = new Label();
        private readonly Button drawButton = new Button();
        private readonly Button newGameButton = new Button();
        private readonly Button helpButton = new Button();
        private int animationStep;
        private Point animationStart;
        private Point animationEnd;
        private Card pendingComputerCard;
        private int pendingComputerPlayer;
        private bool pendingComputerDraw;

        public MainForm()
        {
            Text = "UNO 遊戲 - Windows Forms";
            ClientSize = new Size(1120, 790);
            MinimumSize = new Size(980, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(25, 92, 61);
            Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            BuildLayout();
            computerTimer.Interval = 900;
            computerTimer.Tick += ComputerTimer_Tick;
            animationTimer.Interval = 24;
            animationTimer.Tick += AnimationTimer_Tick;

            game.StartNewGame();
            AudioPlayer.Play("遊戲開始.mp3");
            RefreshBoard();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            foreach (Image image in imageCache.Values)
            {
                image.Dispose();
            }

            base.OnFormClosed(e);
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 194F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            Controls.Add(root);

            TableLayoutPanel opponents = new TableLayoutPanel();
            opponents.Dock = DockStyle.Fill;
            opponents.ColumnCount = 3;
            opponents.Padding = new Padding(16, 12, 16, 8);
            opponents.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            opponents.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            opponents.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            root.Controls.Add(opponents, 0, 0);

            Image backImage = LoadCardImage("UNO_Back.jpeg");
            for (int i = 0; i < computerViews.Length; i++)
            {
                computerViews[i] = new ComputerHandView();
                computerViews[i].Dock = DockStyle.Fill;
                computerViews[i].Margin = new Padding(8);
                computerViews[i].BackImage = backImage;
                opponents.Controls.Add(computerViews[i], i, 0);
            }

            TableLayoutPanel center = new TableLayoutPanel();
            center.Dock = DockStyle.Fill;
            center.ColumnCount = 5;
            center.RowCount = 1;
            center.Padding = new Padding(24, 8, 24, 8);
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            root.Controls.Add(center, 0, 1);

            drawPicture.Dock = DockStyle.Fill;
            drawPicture.SizeMode = PictureBoxSizeMode.Zoom;
            drawPicture.Cursor = Cursors.Hand;
            drawPicture.Click += delegate { DrawForHuman(); };
            center.Controls.Add(CreateCardFrame("抽牌堆", drawPicture), 1, 0);

            FlowLayoutPanel actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.FlowDirection = FlowDirection.TopDown;
            actionPanel.WrapContents = false;
            actionPanel.Padding = new Padding(8, 44, 8, 8);

            currentColorLabel.AutoSize = false;
            currentColorLabel.Width = 180;
            currentColorLabel.Height = 38;
            currentColorLabel.TextAlign = ContentAlignment.MiddleCenter;
            currentColorLabel.Font = new Font(Font, FontStyle.Bold);
            currentColorLabel.ForeColor = Color.White;

            drawCountLabel.AutoSize = false;
            drawCountLabel.Width = 180;
            drawCountLabel.Height = 28;
            drawCountLabel.ForeColor = Color.White;
            drawCountLabel.TextAlign = ContentAlignment.MiddleCenter;

            drawButton.Text = "抽牌";
            drawButton.Width = 180;
            drawButton.Height = 38;
            StyleCenterButton(drawButton);
            drawButton.Click += delegate { DrawForHuman(); };

            newGameButton.Text = "重新開始";
            newGameButton.Width = 180;
            newGameButton.Height = 38;
            StyleCenterButton(newGameButton);
            newGameButton.Click += delegate
            {
                computerTimer.Stop();
                animationTimer.Stop();
                animationCard.Visible = false;
                game.StartNewGame();
                AudioPlayer.Play("遊戲開始.mp3");
                RefreshBoard();
            };

            helpButton.Text = "規則";
            helpButton.Width = 180;
            helpButton.Height = 34;
            StyleCenterButton(helpButton);
            helpButton.Click += delegate
            {
                MessageBox.Show(
                    "出牌需符合目前顏色、相同數字、相同功能，或打出萬用牌。\n\n功能牌：Skip 跳過下一位、Reverse 反轉方向、Draw 2 讓下一位抽 2 張、Wild Draw 4 讓下一位抽 4 張並指定顏色。",
                    "UNO 規則",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            actionPanel.Controls.Add(currentColorLabel);
            actionPanel.Controls.Add(drawCountLabel);
            actionPanel.Controls.Add(drawButton);
            actionPanel.Controls.Add(newGameButton);
            actionPanel.Controls.Add(helpButton);
            center.Controls.Add(actionPanel, 2, 0);

            discardPicture.Dock = DockStyle.Fill;
            discardPicture.SizeMode = PictureBoxSizeMode.Zoom;
            center.Controls.Add(CreateCardFrame("棄牌堆", discardPicture), 3, 0);

            playerHandView.Dock = DockStyle.Fill;
            playerHandView.BackColor = Color.FromArgb(36, 112, 76);
            playerHandView.LoadCardImage = LoadCardImage;
            playerHandView.CanPlayCard = c => game.CanPlay(c) && game.CurrentPlayer == 0 && !animationTimer.Enabled;
            playerHandView.CardClicked += PlayerHandView_CardClicked;
            root.Controls.Add(playerHandView, 0, 2);

            statusLabel.Dock = DockStyle.Fill;
            statusLabel.ForeColor = Color.White;
            statusLabel.Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Padding = new Padding(16, 0, 16, 0);
            root.Controls.Add(statusLabel, 0, 3);

            animationCard.Visible = false;
            animationCard.Size = new Size(84, 120);
            animationCard.SizeMode = PictureBoxSizeMode.Zoom;
            animationCard.BackColor = Color.Transparent;
            Controls.Add(animationCard);
            animationCard.BringToFront();
        }

        private static void StyleCenterButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.White;
            button.FlatAppearance.BorderSize = 2;
            button.BackColor = Color.FromArgb(25, 92, 61);
            button.ForeColor = Color.White;
            button.UseVisualStyleBackColor = false;
        }

        private Control CreateCardFrame(string title, PictureBox picture)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 2;
            panel.ColumnCount = 1;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Padding = new Padding(8);

            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Text = title;
            label.ForeColor = Color.White;
            label.Font = new Font(Font, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleCenter;

            panel.Controls.Add(label, 0, 0);
            panel.Controls.Add(picture, 0, 1);
            return panel;
        }

        private void RefreshBoard()
        {
            discardPicture.Image = LoadCardImage(game.TopCard.ImageFile);
            drawPicture.Image = LoadCardImage("UNO_Back.jpeg");

            for (int i = 0; i < computerViews.Length; i++)
            {
                int player = i + 1;
                computerViews[i].Title = game.PlayerName(player);
                computerViews[i].CardCount = game.GetHand(player).Count;
                computerViews[i].IsCurrentTurn = game.CurrentPlayer == player;
                computerViews[i].Invalidate();
            }

            currentColorLabel.Text = "目前顏色：" + ColorName(game.CurrentColor);
            currentColorLabel.BackColor = ToDrawingColor(game.CurrentColor);
            currentColorLabel.ForeColor = Color.White;
            drawCountLabel.Text = "抽牌堆：" + game.DrawPileCount + " 張";
            statusLabel.Text = game.LastMessage;
            drawButton.Enabled = !game.IsGameOver && game.CurrentPlayer == 0 && !animationTimer.Enabled;

            RenderPlayerHand();

            if (game.IsGameOver)
            {
                computerTimer.Stop();
                PlayWinnerVoice();
                MessageBox.Show(game.LastMessage, "遊戲結束", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (game.CurrentPlayer != 0 && !animationTimer.Enabled)
            {
                statusLabel.Text = game.PlayerName(game.CurrentPlayer) + "思考中...";
                computerTimer.Start();
            }
            else
            {
                computerTimer.Stop();
            }
        }

        private void RenderPlayerHand()
        {
            playerHandView.SetCards(game.GetHand(0).OrderBy(c => c.Color).ThenBy(c => c.Kind).ThenBy(c => c.Number));
        }

        private void PlayerHandView_CardClicked(object sender, CardEventArgs e)
        {
            PlayHumanCard(e.Card);
        }

        private void PlayHumanCard(Card card)
        {
            if (game.CurrentPlayer != 0 || game.IsGameOver || animationTimer.Enabled || card == null || !game.CanPlay(card))
            {
                return;
            }

            UnoColor chosenColor = card.Color;
            if (card.Color == UnoColor.Wild)
            {
                using (ColorChoiceForm chooser = new ColorChoiceForm())
                {
                    if (chooser.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    chosenColor = chooser.SelectedColor;
                }
            }

            game.PlayCard(0, card, chosenColor);
            AudioPlayer.Play("put card_soundEffect.mp3");
            PlayColorVoiceIfNeeded(card, chosenColor);
            if (game.GetHand(0).Count == 1)
            {
                AudioPlayer.Play("UNO.mp3");
            }
            RefreshBoard();
        }

        private void DrawForHuman()
        {
            if (game.CurrentPlayer != 0 || game.IsGameOver || animationTimer.Enabled)
            {
                return;
            }

            game.DrawOne(0);
            AudioPlayer.Play("drawing-card_soundEffect.mp3");
            RefreshBoard();
        }

        private void ComputerTimer_Tick(object sender, EventArgs e)
        {
            computerTimer.Stop();
            if (game.IsGameOver || game.CurrentPlayer == 0)
            {
                return;
            }

            pendingComputerPlayer = game.CurrentPlayer;
            pendingComputerCard = game.GetBestPlayableCard(pendingComputerPlayer);
            pendingComputerDraw = pendingComputerCard == null;
            StartComputerAnimation();
        }

        private void StartComputerAnimation()
        {
            Control computer = computerViews[pendingComputerPlayer - 1];
            if (pendingComputerDraw)
            {
                animationStart = CenterPointOf(drawPicture);
                animationEnd = CenterPointOf(computer);
                animationCard.Image = LoadCardImage("UNO_Back.jpeg");
            }
            else
            {
                animationStart = CenterPointOf(computer);
                animationEnd = CenterPointOf(discardPicture);
                animationCard.Image = LoadCardImage(pendingComputerCard.ImageFile);
            }

            animationCard.Location = animationStart;
            animationCard.Visible = true;
            animationCard.BringToFront();
            animationStep = 0;
            statusLabel.Text = pendingComputerDraw
                ? game.PlayerName(pendingComputerPlayer) + "沒有可出的牌，正在抽牌..."
                : game.PlayerName(pendingComputerPlayer) + "準備出牌...";
            animationTimer.Start();
        }

        private Point CenterPointOf(Control control)
        {
            return PointToClient(control.Parent.PointToScreen(new Point(control.Left + control.Width / 2 - animationCard.Width / 2, control.Top + control.Height / 2 - animationCard.Height / 2)));
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animationStep++;
            double t = Math.Min(1.0, animationStep / 24.0);
            double ease = 1 - Math.Pow(1 - t, 3);
            int x = (int)(animationStart.X + (animationEnd.X - animationStart.X) * ease);
            int y = (int)(animationStart.Y + (animationEnd.Y - animationStart.Y) * ease - Math.Sin(t * Math.PI) * 50);
            animationCard.Location = new Point(x, y);

            if (t < 1.0)
            {
                return;
            }

            animationTimer.Stop();
            animationCard.Visible = false;

            if (pendingComputerDraw)
            {
                game.DrawOne(pendingComputerPlayer);
                AudioPlayer.Play("drawing-card_soundEffect.mp3");
            }
            else
            {
                UnoColor chosenColor = pendingComputerCard.Color == UnoColor.Wild ? game.BestColorFor(pendingComputerPlayer) : pendingComputerCard.Color;
                Card playedCard = pendingComputerCard;
                game.PlayCard(pendingComputerPlayer, pendingComputerCard, chosenColor);
                AudioPlayer.Play("put card_soundEffect.mp3");
                PlayColorVoiceIfNeeded(playedCard, chosenColor);
                if (!game.IsGameOver && game.GetHand(pendingComputerPlayer).Count == 1)
                {
                    AudioPlayer.Play("UNO.mp3");
                }
            }

            pendingComputerCard = null;
            RefreshBoard();
        }

        private void PlayColorVoiceIfNeeded(Card card, UnoColor chosenColor)
        {
            if (card.Color == UnoColor.Wild)
            {
                AudioPlayer.Play(ColorName(chosenColor) + ".mp3");
            }
        }

        private void PlayWinnerVoice()
        {
            if (game.Winner == 0)
            {
                AudioPlayer.Play("你贏了.mp3");
            }
            else if (game.Winner >= 1 && game.Winner <= 3)
            {
                AudioPlayer.Play("電腦" + game.Winner + "獲勝.mp3");
            }
        }

        private Image LoadCardImage(string fileName)
        {
            if (imageCache.ContainsKey(fileName))
            {
                return imageCache[fileName];
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);
            Image image = Image.FromFile(path);
            imageCache[fileName] = image;
            return image;
        }

        private static string ColorName(UnoColor color)
        {
            switch (color)
            {
                case UnoColor.Red: return "紅色";
                case UnoColor.Yellow: return "黃色";
                case UnoColor.Green: return "綠色";
                case UnoColor.Blue: return "藍色";
                default: return "萬用";
            }
        }

        private static Color ToDrawingColor(UnoColor color)
        {
            switch (color)
            {
                case UnoColor.Red: return Color.FromArgb(206, 48, 45);
                case UnoColor.Yellow: return Color.FromArgb(214, 174, 36);
                case UnoColor.Green: return Color.FromArgb(42, 142, 84);
                case UnoColor.Blue: return Color.FromArgb(46, 105, 204);
                default: return Color.FromArgb(60, 60, 60);
            }
        }
    }

    internal sealed class CardEventArgs : EventArgs
    {
        public CardEventArgs(Card card)
        {
            Card = card;
        }

        public Card Card { get; private set; }
    }

    internal sealed class PlayerHandView : Control
    {
        private readonly List<Card> cards = new List<Card>();
        private readonly Dictionary<Card, Rectangle> cardBounds = new Dictionary<Card, Rectangle>();
        private int scrollOffset;
        private bool dragging;
        private int dragStartX;
        private int dragStartOffset;
        private bool movedDuringDrag;

        public PlayerHandView()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        public Func<string, Image> LoadCardImage { get; set; }
        public Func<Card, bool> CanPlayCard { get; set; }
        public event EventHandler<CardEventArgs> CardClicked;

        public void SetCards(IEnumerable<Card> newCards)
        {
            cards.Clear();
            cards.AddRange(newCards);
            ClampScroll();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampScroll();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            scrollOffset -= e.Delta / 3;
            ClampScroll();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            dragging = true;
            movedDuringDrag = false;
            dragStartX = e.X;
            dragStartOffset = scrollOffset;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging)
            {
                return;
            }

            int delta = e.X - dragStartX;
            if (Math.Abs(delta) > 4)
            {
                movedDuringDrag = true;
            }

            scrollOffset = dragStartOffset - delta;
            ClampScroll();
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
            Capture = false;

            if (movedDuringDrag)
            {
                return;
            }

            foreach (KeyValuePair<Card, Rectangle> pair in cardBounds)
            {
                if (pair.Value.Contains(e.Location) && CanPlay(pair.Key))
                {
                    EventHandler<CardEventArgs> handler = CardClicked;
                    if (handler != null)
                    {
                        handler(this, new CardEventArgs(pair.Key));
                    }
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            cardBounds.Clear();

            const int margin = 12;
            const int cardW = 96;
            const int cardH = 137;
            const int captionH = 24;
            int x = margin - scrollOffset;
            int y = 18;

            foreach (Card card in cards)
            {
                Rectangle outer = new Rectangle(x - 5, y - 5, cardW + 10, cardH + captionH + 10);
                bool playable = CanPlay(card);
                using (Brush brush = new SolidBrush(playable ? Color.Gold : Color.Transparent))
                {
                    e.Graphics.FillRectangle(brush, outer);
                }

                Rectangle imageRect = new Rectangle(x, y, cardW, cardH);
                if (LoadCardImage != null)
                {
                    e.Graphics.DrawImage(LoadCardImage(card.ImageFile), imageRect);
                }

                Rectangle caption = new Rectangle(x, y + cardH, cardW, captionH);
                using (Brush captionBrush = new SolidBrush(Color.FromArgb(225, ToDrawingColor(card.Color))))
                using (Brush textBrush = new SolidBrush(Color.White))
                using (Font font = new Font("Microsoft JhengHei UI", 8F, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(captionBrush, caption);
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString(ShortName(card), font, textBrush, caption, format);
                }

                Rectangle hit = new Rectangle(x - 5, y - 5, cardW + 10, cardH + captionH + 10);
                cardBounds[card] = hit;
                x += cardW + margin + 12;
            }

            DrawDragBar(e.Graphics);
        }

        private void DrawDragBar(Graphics graphics)
        {
            int max = MaxScroll;
            if (max <= 0)
            {
                return;
            }

            Rectangle track = new Rectangle(24, Height - 14, Width - 48, 5);
            using (Brush trackBrush = new SolidBrush(Color.FromArgb(110, 255, 255, 255)))
            using (Brush thumbBrush = new SolidBrush(Color.White))
            {
                graphics.FillRectangle(trackBrush, track);
                int thumbW = Math.Max(64, track.Width * Width / Math.Max(Width, ContentWidth));
                int thumbX = track.X + (track.Width - thumbW) * scrollOffset / max;
                graphics.FillRectangle(thumbBrush, new Rectangle(thumbX, track.Y - 2, thumbW, 9));
            }
        }

        private bool CanPlay(Card card)
        {
            return CanPlayCard != null && CanPlayCard(card);
        }

        private int ContentWidth
        {
            get { return cards.Count == 0 ? Width : 24 + cards.Count * 120; }
        }

        private int MaxScroll
        {
            get { return Math.Max(0, ContentWidth - Width); }
        }

        private void ClampScroll()
        {
            scrollOffset = Math.Max(0, Math.Min(scrollOffset, MaxScroll));
        }

        private static string ShortName(Card card)
        {
            if (card.Kind == CardKind.Number)
            {
                return ColorName(card.Color) + " " + card.Number;
            }

            if (card.Kind == CardKind.Wild)
            {
                return "萬用";
            }

            if (card.Kind == CardKind.WildDrawFour)
            {
                return "萬用 +4";
            }

            return ColorName(card.Color) + " " + card.KindName;
        }

        private static string ColorName(UnoColor color)
        {
            switch (color)
            {
                case UnoColor.Red: return "紅色";
                case UnoColor.Yellow: return "黃色";
                case UnoColor.Green: return "綠色";
                case UnoColor.Blue: return "藍色";
                default: return "萬用";
            }
        }

        private static Color ToDrawingColor(UnoColor color)
        {
            switch (color)
            {
                case UnoColor.Red: return Color.FromArgb(206, 48, 45);
                case UnoColor.Yellow: return Color.FromArgb(214, 174, 36);
                case UnoColor.Green: return Color.FromArgb(42, 142, 84);
                case UnoColor.Blue: return Color.FromArgb(46, 105, 204);
                default: return Color.FromArgb(45, 45, 45);
            }
        }
    }

    internal sealed class ComputerHandView : Panel
    {
        public string Title { get; set; }
        public int CardCount { get; set; }
        public bool IsCurrentTurn { get; set; }
        public Image BackImage { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(IsCurrentTurn ? Color.FromArgb(255, 235, 143) : Color.FromArgb(244, 239, 217));

            using (Font titleFont = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold))
            using (Font countFont = new Font("Microsoft JhengHei UI", 9F, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(34, 34, 34)))
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                e.Graphics.DrawString((Title ?? "電腦") + (IsCurrentTurn ? "  思考中" : ""), titleFont, textBrush, new RectangleF(0, 8, Width, 24), format);
                e.Graphics.DrawString(CardCount + " 張牌", countFont, textBrush, new RectangleF(0, 34, Width, 22), format);
            }

            if (BackImage == null || CardCount <= 0)
            {
                return;
            }

            int visible = Math.Min(CardCount, 10);
            float cardW = 50F;
            float cardH = 76F;
            float spacing = visible <= 1 ? 0 : Math.Min(24F, (Width - 100F) / (visible - 1));
            float totalW = spacing * (visible - 1) + cardW;
            float startX = (Width - totalW) / 2F;
            float baseY = Math.Max(64F, Height - cardH - 22F);

            for (int i = 0; i < visible; i++)
            {
                float ratio = visible == 1 ? 0 : (i - (visible - 1) / 2F) / ((visible - 1) / 2F);
                float angle = ratio * 15F;
                float x = startX + i * spacing;
                float y = baseY + Math.Abs(ratio) * 7F;

                GraphicsState state = e.Graphics.Save();
                e.Graphics.TranslateTransform(x + cardW / 2F, y + cardH / 2F);
                e.Graphics.RotateTransform(angle);
                e.Graphics.DrawImage(BackImage, -cardW / 2F, -cardH / 2F, cardW, cardH);
                e.Graphics.Restore(state);
            }
        }
    }

    internal sealed class ColorChoiceForm : Form
    {
        public ColorChoiceForm()
        {
            Text = "選擇顏色";
            ClientSize = new Size(360, 120);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(16);
            panel.WrapContents = false;
            Controls.Add(panel);

            AddButton(panel, UnoColor.Red, Color.FromArgb(206, 48, 45), "紅色");
            AddButton(panel, UnoColor.Yellow, Color.FromArgb(240, 197, 52), "黃色");
            AddButton(panel, UnoColor.Green, Color.FromArgb(42, 142, 84), "綠色");
            AddButton(panel, UnoColor.Blue, Color.FromArgb(46, 105, 204), "藍色");
        }

        public UnoColor SelectedColor { get; private set; }

        private void AddButton(FlowLayoutPanel panel, UnoColor color, Color backColor, string text)
        {
            Button button = new Button();
            button.Width = 72;
            button.Height = 72;
            button.Margin = new Padding(5);
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.Text = text;
            button.Tag = color;
            button.Click += delegate
            {
                SelectedColor = (UnoColor)button.Tag;
                DialogResult = DialogResult.OK;
                Close();
            };
            panel.Controls.Add(button);
        }
    }
}
