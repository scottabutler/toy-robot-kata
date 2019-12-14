<Query Kind="Program" />

void Main()
{
	var input = "REPORT|MOVE|MOVE|PLACE -2,10,NORTH|MOVE|MOVE|REPORT|MOVE|MOVE|MOVE|MOVE|RIGHT|MOVE|MOVE|REPORT|PLACE 3,3,SOUTH|RIGHT|REPORT|MOVE|MOVE|REPORT|LEFT|MOVE|MOVE|MOVE|MOVE|MOVE|MOVE|REPORT";
	var commands = input
		.Split('|')
		.Select(x => CommandFactory.CreateCommand(x))
		.ToList();

	var result = commands.Aggregate<ICommand, IState>(
		new UnplacedState(), 
		(state, command) => { 
			return command.IsValid(state) ? command.Execute(state) : state; 
		}
	);
	
	result.Dump();
}

static class CommandFactory
{
	public static ICommand CreateCommand(string input)
	{		
		switch (input.ToUpperInvariant().Split(' ')[0])
		{
			case "PLACE":
				return new PlaceCommand(input);
			case "MOVE":
				return new MoveCommand();
			case "LEFT":
				return new LeftCommand();
			case "RIGHT":
				return new RightCommand();
			case "REPORT":
				return new ReportCommand(new ConsoleOutput());
			default:
				throw new ApplicationException($"Unknown command: '{input}'");
		}
	}
}

class Table
{
	public int MinX { get; }
	public int MinY { get; }
	public int MaxX { get; }
	public int MaxY { get; }
	
	public Table(int minX, int minY, int maxX, int maxY)
	{
		MinX = minX;
		MinY = minY;
		MaxX = maxX;
		MaxY = maxY;
	}
}

/* STATES */
interface IState 
{ 
	bool IsPlaced();
}

class UnplacedState : IState
{
	public bool IsPlaced() => false;
	
	public int MaxX => 4;
	public int MaxY => 4;
}

class PlacedState : IState
{
	public bool IsPlaced() => true;
	public int X { get; }
	public int Y { get; }
	public Direction Direction { get; }
	
	public int MaxX => 4;
	public int MaxY => 4;
	
	public PlacedState(int x, int y, Direction direction)
	{
		X = x;
		Y = y;
		Direction = direction;
	}
}

enum Direction
{
	NORTH,
	SOUTH,
	EAST,
	WEST
}

/* COMMANDS */
interface ICommand 
{
	bool IsValid(IState state);
	IState Execute(IState state);
}

class PlaceCommand : ICommand
{
	private int _x;
	private int _y;
	private Direction _direction;
	private bool _inputValid;
	
	public PlaceCommand(string input)
	{
		var regex = new Regex(@"(PLACE){1}\s{1}(?<x>\d){1},(?<y>\d){1},(?<direction>((NORTH)|(EAST)|(SOUTH)|(WEST)))");
		var match = regex.Match(input);
		_inputValid = match.Success;
		
		if (match.Success)
		{
			_x = Int16.Parse(match.Groups["x"].Value);
			_y = Int16.Parse(match.Groups["y"].Value);
			_direction = (Direction)Enum.Parse(typeof(Direction), match.Groups["direction"].Value);
		}
	}

	public IState Execute(IState state)
	{
		return new PlacedState(_x, _y, _direction);
	}

	public bool IsValid(IState state)
	{
		if (state.IsPlaced())
		{
			var placedState = (PlacedState)state;
			return _inputValid 
				&& _x <= placedState.MaxX
				&& _x >= 0			 
				&& _y <= placedState.MaxY
				&& _y >= 0;
		}
		else
		{
			var unplacedState = (UnplacedState)state;
			return _inputValid 
				&& _x <= unplacedState.MaxX
				&& _x >= 0			 
				&& _y <= unplacedState.MaxY
				&& _y >= 0;
		}
	}
}

class MoveCommand : ICommand
{
	public IState Execute(IState state)
	{
		PlacedState placedState = (PlacedState)state;
		switch (placedState.Direction)
		{
			case Direction.NORTH:
				return new PlacedState(x: placedState.X, y: placedState.Y + 1, direction: placedState.Direction);
			case Direction.SOUTH:
				return new PlacedState(x: placedState.X, y: placedState.Y - 1, direction: placedState.Direction);
			case Direction.EAST:
				return new PlacedState(x: placedState.X + 1, y: placedState.Y, direction: placedState.Direction);
			case Direction.WEST:
				return new PlacedState(x: placedState.X - 1, y: placedState.Y, direction: placedState.Direction);
			default:
				throw new ApplicationException($"Cannot move in direction '{placedState.Direction.ToString()}'");
		}
	}

	public bool IsValid(IState state)
	{
		if (!state.IsPlaced()) return false;
		
		var placedState = (PlacedState)state;
		switch(placedState.Direction)
		{
			case Direction.NORTH:
				return placedState.Y < placedState.MaxY;
			case Direction.SOUTH:
				return placedState.Y > 0;
			case Direction.EAST:
				return placedState.X < placedState.MaxX;
			case Direction.WEST:
				return placedState.X > 0;
			default:
				throw new ApplicationException("Unhandled direction in MoveCommand");
		}
	}
}

class LeftCommand : ICommand
{
	public IState Execute(IState state)
	{
		PlacedState placedState = (PlacedState)state;
		switch (placedState.Direction)
		{
			case Direction.NORTH:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.WEST);
			case Direction.SOUTH:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.EAST);
			case Direction.EAST:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.NORTH);
			case Direction.WEST:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.SOUTH);
			default:
				throw new ApplicationException($"Cannot turn left from direction '{placedState.Direction.ToString()}'");
		}
	}

	public bool IsValid(IState state)
	{
		return state.IsPlaced();
	}
}

class RightCommand : ICommand
{
	public IState Execute(IState state)
	{
		PlacedState placedState = (PlacedState)state;
		switch (placedState.Direction)
		{
			case Direction.NORTH:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.EAST);
			case Direction.SOUTH:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.WEST);
			case Direction.EAST:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.SOUTH);
			case Direction.WEST:
				return new PlacedState(x: placedState.X, y: placedState.Y, direction: Direction.NORTH);
			default:
				throw new ApplicationException($"Cannot turn right from direction '{placedState.Direction.ToString()}'");
		}
	}

	public bool IsValid(IState state)
	{
		return state.IsPlaced();
	}
}

class ReportCommand : ICommand
{
	private IOutput _output;
	
	public ReportCommand(IOutput output)
	{
		_output = output;
	}

	public IState Execute(IState state)
	{
		var placedState = (PlacedState)state;
		var message = $"{placedState.X},{placedState.Y},{placedState.Direction.ToString()}";
		_output.Log(message);		
		return state;
	}

	public bool IsValid(IState state)
	{
		return state.IsPlaced();
	}
}

/* OUTPUTS */
interface IOutput
{
	void Log(string message);
}

class ConsoleOutput : IOutput
{
	public void Log(string message)
	{
		Console.WriteLine(message);
	}
}