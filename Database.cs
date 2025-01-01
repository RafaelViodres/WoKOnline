using System;
using System.Data;
using System.Data.SQLite;
using BCrypt.Net;

public class Database
{
  private readonly SQLiteConnection connection;

  public Database(string dbPath)
  {
    if (string.IsNullOrWhiteSpace(dbPath))
    {
      throw new ArgumentException("O caminho do banco de dados não pode ser nulo ou vazio.", nameof(dbPath));
    }

    connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
    connection.Open();
    CreateTables();
  }

  private void CreateTables()
  {
    using var command = connection.CreateCommand();

    command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL
            )";
    command.ExecuteNonQuery();

    command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Characters (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER,
                Name TEXT NOT NULL,
                Level INTEGER DEFAULT 1,
                HP INTEGER DEFAULT 100,
                MaxHP INTEGER DEFAULT 100,
                MP INTEGER DEFAULT 50,
                MaxMP INTEGER DEFAULT 50,
                XP INTEGER DEFAULT 0,
                MaxXP INTEGER DEFAULT 1000,
                Race TEXT,
                PosX REAL DEFAULT 0,
                PosY REAL DEFAULT 0,
                Strength INTEGER DEFAULT 10,
                Armor INTEGER DEFAULT 0,
                Defense INTEGER DEFAULT 0,
                Attack INTEGER DEFAULT 10,
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
            )";
    command.ExecuteNonQuery();
  }

  // ----------------------------------------------------------------------

  public void InsertAccount(string? username, string? password)
  {
    if (string.IsNullOrWhiteSpace(username))
    {
      throw new ArgumentException("O nome de usuário não pode ser nulo ou vazio.", nameof(username));
    }

    if (string.IsNullOrWhiteSpace(password))
    {
      throw new ArgumentException("A senha não pode ser nula ou vazia.", nameof(password));
    }

    using var command = connection.CreateCommand();
    command.CommandText = "INSERT INTO Accounts (Username, Password) VALUES (@username, @password)";
    command.Parameters.AddWithValue("@username", username);
    command.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(password));

    command.ExecuteNonQuery();
  }

  public bool DoesAccountExist(string? username)
  {
    if (string.IsNullOrWhiteSpace(username))
    {
      throw new ArgumentException("O nome de usuário não pode ser nulo ou vazio.", nameof(username));
    }

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(1) FROM Accounts WHERE Username = @username";
    command.Parameters.AddWithValue("@username", username);

    long count = (long)command.ExecuteScalar()!;
    return count > 0;
  }

  public int ValidateAccount(string? username, string? password)
  {
    if (string.IsNullOrWhiteSpace(username))
    {
      throw new ArgumentException("O nome de usuário não pode ser nulo ou vazio.", nameof(username));
    }

    if (string.IsNullOrWhiteSpace(password))
    {
      throw new ArgumentException("A senha não pode ser nula ou vazia.", nameof(password));
    }

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT Password FROM Accounts WHERE Username = @username";
    command.Parameters.AddWithValue("@username", username);

    var result = command.ExecuteScalar();
    if (result is string storedPasswordHash && BCrypt.Net.BCrypt.Verify(password, storedPasswordHash))
    {
      return GetAccountIdByUsername(username);
    }

    return -1; // Credenciais inválidas
  }

  // ----------------------------------------------------------------------

  public bool InsertCharacter(Character character)
  {
    string sql = "INSERT INTO Characters (AccountId, Name, Level, HP, MaxHP, MP, MaxMP, XP, MaxXP, Race, PosX, PosY, Strength, Armor, Defense, Attack) " +
                 "VALUES (@AccountId, @Name, @Level, @HP, @MaxHP, @MP, @MaxMP, @XP, @MaxXP, @Race, @PosX, @PosY, @Strength, @Armor, @Defense, @Attack)";
    using (var command = connection.CreateCommand())
    {
      command.CommandText = sql;
      command.Parameters.AddWithValue("@AccountId", character.AccountId);
      command.Parameters.AddWithValue("@Name", character.Name);
      command.Parameters.AddWithValue("@Level", character.Level);
      command.Parameters.AddWithValue("@HP", character.HP);
      command.Parameters.AddWithValue("@MaxHP", character.MaxHP);
      command.Parameters.AddWithValue("@MP", character.MP);
      command.Parameters.AddWithValue("@MaxMP", character.MaxMP);
      command.Parameters.AddWithValue("@XP", character.XP);
      command.Parameters.AddWithValue("@MaxXP", character.MaxXP);
      command.Parameters.AddWithValue("@Race", character.Race);
      command.Parameters.AddWithValue("@PosX", character.PosX);
      command.Parameters.AddWithValue("@PosY", character.PosY);
      command.Parameters.AddWithValue("@Strength", character.Strength);
      command.Parameters.AddWithValue("@Armor", character.Armor);
      command.Parameters.AddWithValue("@Defense", character.Defense);
      command.Parameters.AddWithValue("@Attack", character.Attack);

      int rowsAffected = command.ExecuteNonQuery();
      return rowsAffected > 0;
    }
  }

  public int GetAccountIdByUsername(string? username)
  {
    if (string.IsNullOrWhiteSpace(username))
    {
      throw new ArgumentException("O nome de usuário não pode ser nulo ou vazio.", nameof(username));
    }

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT Id FROM Accounts WHERE Username = @username";
    command.Parameters.AddWithValue("@username", username);

    var result = command.ExecuteScalar();
    return result != null ? Convert.ToInt32(result) : -1;
  }

  public Character? GetCharactersByAccountId(int accountId)
  {
    using (var command = connection.CreateCommand())
    {
      command.CommandText = "SELECT * FROM Characters WHERE AccountId = @accountId";
      command.Parameters.AddWithValue("@accountId", accountId);

      using (var reader = command.ExecuteReader())
      {
        if (reader.Read())
        {
          return new Character
          {
            Id = reader.GetInt32(0),
            AccountId = reader.GetInt32(1),
            Name = reader.GetString(2),
            Level = reader.GetInt32(3),
            HP = reader.GetInt32(4),
            MaxHP = reader.GetInt32(5),
            MP = reader.GetInt32(6),
            MaxMP = reader.GetInt32(7),
            XP = reader.GetInt32(8),
            MaxXP = reader.GetInt32(9),
            Race = reader.GetString(10),
            PosX = reader.GetDouble(11),
            PosY = reader.GetDouble(12),
            Strength = reader.GetInt32(13),
            Armor = reader.GetInt32(14),
            Defense = reader.GetInt32(15),
            Attack = reader.GetInt32(16)
          };
        };
      }
    }

    return null;
  }
}
