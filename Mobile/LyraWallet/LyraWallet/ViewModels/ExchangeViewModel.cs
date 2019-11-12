﻿using Lyra.Exchange;
using LyraWallet.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace LyraWallet.ViewModels
{
    public class ExchangeViewModel : BaseViewModel
    {
        private List<string> _tokenList;
        public List<string> TokenList
        {
            get
            {
                return _tokenList;
            }
            set
            {
                var st = SelectedToken;
                SetProperty(ref _tokenList, value);
                SelectedToken = st;
            }
        }

        public ObservableCollection<KeyValuePair<Decimal, Decimal>> SellOrders { get; } = new ObservableCollection<KeyValuePair<decimal, decimal>>();

        public string FilterKeyword { get; set; }
        public string TargetTokenBalance { get => _targetTokenBalance; set => SetProperty(ref _targetTokenBalance, value); }
        public string LeXBalance { get => _lexBalance; set => SetProperty(ref _lexBalance, value); }
        public string SelectedToken { get => _selectedToken; set {
                SetProperty(ref _selectedToken, value);
                UpdateHoldings();
            }
        }

        private string _selectedToken;
        private string _targetTokenBalance;
        private string _lexBalance;

        private string _buyPrice;
        private string _buyAmount;
        private string _sellPrice;
        private string _sellAmount;

        public ICommand BuyCommand { get; }
        public ICommand SellCommand { get; }
        public string BuyPrice { get => _buyPrice; set => SetProperty(ref _buyPrice, value); }
        public string BuyAmount { get => _buyAmount; set => SetProperty(ref _buyAmount, value); }
        public string SellPrice { get => _sellPrice; set => SetProperty(ref _sellPrice, value); }
        public string SellAmount { get => _sellAmount; set => SetProperty(ref _sellAmount, value); }

        HttpClient _client;
        HubConnection _exchangeHub;
        public ExchangeViewModel()
        {
            _exchangeHub = new HubConnectionBuilder()
                .WithUrl("http://lex.lyratokens.com:5493/ExchangeHub")
                .Build();

            _exchangeHub.On<Decimal, Decimal, bool>("OrderCreated", (price, amount, isBuy) =>
            {
                if(isBuy)
                {

                }
                else
                {
                    SellOrders.Add(new KeyValuePair<Decimal, Decimal>(price, amount));
                }
            });

            Task.Run(async () => await Connect() );

            _client = new HttpClient
            {
                //BaseAddress = new Uri("https://localhost:5001/api/")
                BaseAddress = new Uri("http://lex.lyratokens.com:5493/api/"),
#if DEBUG
                Timeout = new TimeSpan(0, 30, 0)        // for debug. but 10 sec is too short for real env
#else
                Timeout = new TimeSpan(0, 0, 30)
#endif
            };

            MessagingCenter.Subscribe<BalanceViewModel>(
                this, MessengerKeys.BalanceRefreshed, async (sender) =>
                {
                    await Touch();
                    UpdateHoldings();
                });

            BuyCommand = new Command(async () =>
            {
                await SubmitOrder(true);
            });

            SellCommand = new Command(async () =>
            {
                await SubmitOrder(false);
            });
        }

        async Task Connect()
        {
            try
            {
                await _exchangeHub.StartAsync();
            }
            catch (Exception ex)
            {
                // Something has gone wrong
            }
        }

        async Task SendMessage(string user, string message)
        {
            try
            {
                await _exchangeHub.InvokeAsync("SendMessage", user, message);
            }
            catch (Exception ex)
            {
                // send failed
            }
        }

        private async Task SubmitOrder(bool IsBuy)
        {
            try
            {
                TokenTradeOrder order = new TokenTradeOrder()
                {
                    CreatedTime = DateTime.Now,
                    AccountID = App.Container.AccountID,
                    NetworkID = App.Container.CurrentNetwork,
                    BuySellType = IsBuy ? OrderType.Buy : OrderType.Sell,
                    TokenName = SelectedToken,
                    Price = Decimal.Parse(IsBuy ? BuyPrice : SellPrice),
                    Amount = decimal.Parse(IsBuy ? BuyAmount : SellAmount)
                };
                order.Sign(App.Container.PrivateKey);
                var reqStr = JsonConvert.SerializeObject(order);
                var reqContent = new StringContent(reqStr, Encoding.UTF8, "application/json");
                var resp = await _client.PostAsync("exchange/submit", reqContent);

                var txt = resp.Content;
            }
            catch (Exception)
            {

            }
        }
        private void UpdateHoldings()
        {
            if(App.Container.Balances == null)
            {
                LeXBalance = $"Hold Lyra.LeX: 0";
                TargetTokenBalance = $"Hold {SelectedToken}: 0";
            }
            else
            {
                LeXBalance = $"Hold Lyra.LeX: {App.Container.Balances["Lyra.LeX"]}";
                if(SelectedToken == null)
                {
                    TargetTokenBalance = "";
                }
                else
                {
                    if (App.Container.Balances.ContainsKey(SelectedToken))
                    {
                        TargetTokenBalance = $"Hold {SelectedToken}: {App.Container.Balances[SelectedToken]}";
                    }
                    else
                    {
                        TargetTokenBalance = $"Hold {SelectedToken}: 0";
                    }
                }
            }
        }

        internal async Task Touch()
        {
            try
            {
                TokenList = await App.Container.GetTokens(FilterKeyword);
            }
            catch(Exception ex)
            {
                //TokenList = new List<string>() { ex.Message };
            }
        }
    }
}