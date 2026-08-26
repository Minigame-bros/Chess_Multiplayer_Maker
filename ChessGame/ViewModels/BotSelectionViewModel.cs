using System.Collections.ObjectModel;
using System.Windows.Input;
using ChessBot;
using ChessBot.Bots.Noob;
using ChessBot.Bots.DanLang;
using ChessBot.Bots.CaHeo;
using ChessBot.Bots.ConLon;
using ChessBot.Bots.QuanVua;

namespace ChessGame.ViewModels
{
    public class BotSelectionViewModel
    {
        public ObservableCollection<IChessBot> Bots { get; set; }
        public IChessBot SelectedBot { get; set; }

        public ICommand StartGameCommand { get; }
        public ICommand BackCommand { get; }

        public BotSelectionViewModel()
        {
            Bots = new ObservableCollection<IChessBot>
            {
                new NoobBot(),
                new DanLangBot(),
                new CaHeoBot(),
                new ConLonBot(),
                new QuanVuaBot()
            };

            SelectedBot = Bots[0];

            StartGameCommand = new RelayCommand(_ =>
            {
                if (SelectedBot != null)
                {
                    MainViewModel.Instance.NavigateTo(new GameRoomViewModel(SelectedBot));
                }
            });

            BackCommand = new RelayCommand(_ =>
            {
                MainViewModel.Instance.NavigateTo(new MainMenuViewModel());
            });
        }
    }
}
