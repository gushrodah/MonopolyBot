using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System;

// controller
namespace MonopolyBot
{
	public class ProfileControl : DialogContainer
	{
		public ProfileControl()
			: base("fillProfile")
		{
			Dialogs.Add("fillProfile",
				new WaterfallStep[]
				{
					async(dc,args,next)=>
					{
						dc.ActiveDialog.State = new Dictionary<string,object>();
						await dc.Prompt("textPrompt", "What is your name?");
					},
					async(dc,args,next)=>
					{
						dc.ActiveDialog.State["name"] = args["Value"];
						GameInfo game = UserState<GameInfo>.Get(dc.Context);
						game.User.Name = args["Value"].ToString();
						await dc.Prompt("textPrompt", $"Hello {game.User.Name}! Be prepared to lose.");
					},
					async(dc,args,next)=>
					{
						dc.ActiveDialog.State["phone"] = args["Value"];
						await dc.End(dc.ActiveDialog.State);
					}
				});
			Dialogs.Add("textPrompt", new TextPrompt());
		}
	}

	public class PlayerMoves : DialogContainer
	{
		public PlayerMoves()
			: base("roll")
		{
			Dialogs.Add("BotRoll", new BotMoves());
			Dialogs.Add("roll",
				new WaterfallStep[]
				{
					async(dc,args,next)=>
					{
						dc.ActiveDialog.State = new Dictionary<string,object>();
						await dc.Context.SendActivity("Roll?");
					},
					async(dc,args,next)=>
					{
						Random rnd = new Random((int)DateTime.Now.Ticks);
						int num = rnd.Next(1,7);
						dc.ActiveDialog.State["rollAmount"] = num;
						
						GameInfo game = UserState<GameInfo>.Get(dc.Context);
						game.User.Position += num;
						if(game.User.Position >= Board.Properties.Length)
						{
							game.User.Position -= Board.Properties.Length;
							await dc.Prompt("textPrompt", $"You passed Go. I get ${Board.GoMoney}.");
							game.User.Money += Board.GoMoney;
						}

						int position = game.User.Position;
						Property property = Board.Properties[position];
						await dc.Prompt("textPrompt", $"You've landed on {property.Name}.");
						if(game.User.Money < property.Cost)
						{
							await dc.Prompt("textPrompt", $"You dont have enough money to buy {property.Name}.");
						}
						// no one owns this yet
						else if (game.PropertyOnwers[game.User.Position] == 0)
						{
							await dc.Prompt("textPrompt", $"Do you want to buy {property.Name} for {property.Cost}?");
						}
						// mine
						else if(property.Owner == 1)
						{
							await dc.Prompt("textPrompt", $"You moved to your land: {property.Name}.");
						}
						// bots
						else if(property.Owner == 2)
						{
							await dc.Prompt("textPrompt", $"Pay me rent! ${property.Cost}.");
						}
					},
					async(dc,args,next)=>
					{
						//dc.ActiveDialog.State["BuyProperty"] = args["Value"];
						GameInfo game = UserState<GameInfo>.Get(dc.Context);
						Property property = Board.Properties[game.User.Position];
						if(args["Value"].ToString() == "yes" && game.User.Money >= property.Cost)
						{
							dc.ActiveDialog.State["BuyProperty"] = "yes";
							// TODO : options to buy or not
							game.PropertyOnwers[game.User.Position] = 1;
							// TODO : player cant buy if dont have enough money
							game.User.Money -= property.Cost;
						}
						await dc.Prompt("textPrompt", $"You now have {game.User.Money} money in your bank.");
						dc.ActiveDialog.State["BuyProperty"] = "no";

						if(game.User.Money < 0)
							await dc.End();
						else
						{
							await dc.Begin("BotRoll");
							game.Turn = 1;
						}
					}
				});
			Dialogs.Add("textPrompt", new TextPrompt());
		}
	}

	public class BotMoves : DialogContainer
	{
		public BotMoves()
			: base("BotRoll")
		{
			Dialogs.Add("BotRoll",
				new WaterfallStep[]
				{
					async(dc,args,next)=>
					{
						dc.ActiveDialog.State = new Dictionary<string,object>();
					},
					async(dc,args,next)=>
					{
						Random rnd = new Random((int)DateTime.Now.Ticks);
						int num = rnd.Next(1,7);
						dc.ActiveDialog.State["rollAmount"] = num;

						GameInfo game = UserState<GameInfo>.Get(dc.Context);
						game.Bot.Position += num;
						if(game.Bot.Position >= Board.Properties.Length)
						{
							game.Bot.Position -= Board.Properties.Length;
							await dc.Prompt("textPrompt", $"I passed Go. I get ${Board.GoMoney}.");
							game.Bot.Money += Board.GoMoney;
						}

						int position = game.Bot.Position;
						Property property = Board.Properties[position];
						await dc.Prompt("textPrompt", $"I landed on {property.Name}.");
						if(game.Bot.Money >= property.Cost && game.PropertyOnwers[game.Bot.Position] == 0)
						{
							await dc.Prompt("textPrompt", $"I'll buy {property.Name}.");
							game.Bot.Money -= property.Cost;
							game.PropertyOnwers[game.User.Position] = 2;
						}
						// no one owns this yet
						else if (game.Bot.Money < property.Cost)
						{
							await dc.Prompt("textPrompt", $"I'll pass on {property.Name} for now.");
						}
						// mine
						else if(game.PropertyOnwers[game.User.Position] == 1)
						{
							await dc.Prompt("textPrompt", $"I guess I have to pay you ${property.Cost}.");
							game.Bot.Money -= property.Cost;
						}
						// bots
						else if(game.PropertyOnwers[game.User.Position] == 2)
						{
							await dc.Prompt("textPrompt", $"I own this.");
						}
						
						if(game.User.Money < 0)
							await dc.End();
						else{
							await dc.Prompt("textPrompt", $"Your turn.");
							game.Turn = 0;
						}
					}
				});
			Dialogs.Add("textPrompt", new TextPrompt());
		}
	}

	public class FirstRun : DialogContainer
	{
		public FirstRun()
			: base("firstRun")
		{
			Dialogs.Add("fillProfile", new ProfileControl());
			Dialogs.Add("roll", new PlayerMoves());
			Dialogs.Add("firstRun",
				new WaterfallStep[]
				{
					async (dc, args, next) =>
					{
							await dc.Context.SendActivity("Welcome! We need to ask a few questions to get started.");
							await dc.Begin("fillProfile");
					},
					async (dc, args, next) =>
					{
						//await dc.Context.SendActivity($"Thanks {args["name"]} I have your phone number as {args["phone"]}!");
						UserState<GameInfo>.Get(dc.Context).User.Name = args["name"].ToString();
						//await dc.End();
						await dc.Begin("roll");
					}
				});
			Dialogs.Add("textPrompt", new TextPrompt());
		}
	}

	public class Controller : IBot
    {
		private DialogSet _dialogs;

		public Controller()
		{
			_dialogs = new DialogSet();
			_dialogs.Add("roll", new PlayerMoves());
			_dialogs.Add("BotRoll", new BotMoves());
			_dialogs.Add("fillProfile", new ProfileControl());
			_dialogs.Add("firstRun", new FirstRun());
		}
   
        public async Task OnTurn(ITurnContext context)
        {
			try
			{
				GameInfo game = context.GetUserState<GameInfo>();
				switch (context.Activity.Type)
				{
					case ActivityTypes.ConversationUpdate:
						foreach (var newMember in context.Activity.MembersAdded)
						{
							if (newMember.Id != context.Activity.Recipient.Id)
							{
								await context.SendActivity("Hello lets play Monopoly.");
							}
						}
						break;

					case ActivityTypes.Message:
						var state = ConversationState<Dictionary<string, object>>.Get(context);
						var dc = _dialogs.CreateContext(context, state);

						await dc.Continue();
						if (!context.Responded)
						{
							await dc.Begin("firstRun");
						}
						if (game.Turn == 0)
						{
							await dc.Begin("roll");
							game.Turn = 1;
						}

						break;
				}
			}
			catch (Exception e)
			{
				await context.SendActivity($"Exception: {e.Message}");
			}
        }

		private void RollDice(Player _player)
		{
			int roll = 1;

			_player.Position += roll;
			if (_player.Position >= Board.Properties.Length)
			{
				_player.Position -= Board.Properties.Length;
			}
		}
    }    
}
