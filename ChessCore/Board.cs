namespace ChessCore
{
    public class Board
    {
        private readonly Piece?[,] pieces = new Piece[8, 8];

        public Position? EnPassantTarget { get; set; }

        public Piece? this[int row, int col]
        {
            get => pieces[row, col];
            set => pieces[row, col] = value;
        }

        public Piece? this[Position pos]
        {
            get => pieces[pos.Row, pos.Col];
            set => pieces[pos.Row, pos.Col] = value;
        }

        public static Board Initial()
        {
            Board board = new Board();
            board.AddStartPieces();
            return board;
        }

        private void AddStartPieces()
        {
            this[0, 0] = new Rook(PlayerColor.Black);
            this[0, 1] = new Knight(PlayerColor.Black);
            this[0, 2] = new Bishop(PlayerColor.Black);
            this[0, 3] = new Queen(PlayerColor.Black);
            this[0, 4] = new King(PlayerColor.Black);
            this[0, 5] = new Bishop(PlayerColor.Black);
            this[0, 6] = new Knight(PlayerColor.Black);
            this[0, 7] = new Rook(PlayerColor.Black);

            this[7, 0] = new Rook(PlayerColor.White);
            this[7, 1] = new Knight(PlayerColor.White);
            this[7, 2] = new Bishop(PlayerColor.White);
            this[7, 3] = new Queen(PlayerColor.White);
            this[7, 4] = new King(PlayerColor.White);
            this[7, 5] = new Bishop(PlayerColor.White);
            this[7, 6] = new Knight(PlayerColor.White);
            this[7, 7] = new Rook(PlayerColor.White);

            for (int c = 0; c < 8; c++)
            {
                this[1, c] = new Pawn(PlayerColor.Black);
                this[6, c] = new Pawn(PlayerColor.White);
            }
        }

        public bool IsEmpty(Position pos)
        {
            return this[pos] == null;
        }
    }
}
