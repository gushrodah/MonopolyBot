using System.Collections.Generic;

namespace MonopolyBot
{
	public class Player
	{
		public string Name { get; set; } = "";
		public int Position { get; set; } = 0;
		public int Money { get; set; } = 100;
		public bool HasRolled { get; set; }
	}

	public class Property
	{
		public string Name { get; } = "";
		public int Cost { get; } = 10;
		// unowned = -1, bot = 0, player = 1
		public int Owner { get; set; } = -1;

		public Property(string _name,int _cost)
		{
			Name = _name;
			Cost = _cost;
			Owner = -1;
		}
	}

	public static class Board
	{
		public static Property[] Properties = new Property[]
		{
			new Property("a", 1),
			new Property("b", 2),
			new Property("c", 3),
			new Property("d", 2),
			new Property("e", 3),
			new Property("f", 4),
			new Property("g", 4),
			new Property("h", 5),
			new Property("i", 6),
			new Property("j", 7)
		};

		public static int GoMoney = 10;
	}

	public class GameInfo
	{
		public Player User = new Player();
		public Player Bot = new Player();
		public int Turn = -1;
		// 0 = empty, 1 = player, 2 = bot
		public int[] PropertyOnwers = new int[10]; 
}


	public class ConversationInfo : Dictionary<string, object> { }
}
