public class Character
{
  public int Id { get; set; }
  public int AccountId { get; set; }
  public string Name { get; set; } = string.Empty;
  public int Level { get; set; } = 1;
  public int HP { get; set; } = 100;
  public int MaxHP { get; set; } = 100;
  public int MP { get; set; } = 50;
  public int MaxMP { get; set; } = 50;
  public int XP { get; set; } = 0;
  public int MaxXP { get; set; } = 1000;
  public string? Race { get; set; }
  public double PosX { get; set; } = 0;
  public double PosY { get; set; } = 0;
  public int Strength { get; set; } = 10;
  public int Armor { get; set; } = 0;
  public int Defense { get; set; } = 0;
  public int Attack { get; set; } = 10;
}
