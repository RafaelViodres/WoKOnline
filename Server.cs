using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Server
{
  private TcpListener listener;
  private const int PORT = 8080;
  private Database database;
  private ConcurrentDictionary<int, TcpClient> activeLogins;
  private ConcurrentDictionary<TcpClient, int> clientAccounts;

  public Server()
  {
    listener = new TcpListener(IPAddress.Any, PORT);
    database = new Database("game.db");
    activeLogins = new ConcurrentDictionary<int, TcpClient>();
    clientAccounts = new ConcurrentDictionary<TcpClient, int>();
  }

  public async Task StartAsync()
  {
    listener.Start();
    Console.WriteLine("Servidor iniciado na porta " + PORT);

    while (true)
    {
      var client = await listener.AcceptTcpClientAsync();
      Console.WriteLine("Cliente conectado!");
      _ = HandleClientAsync(client);
    }
  }

  private async Task HandleClientAsync(TcpClient client)
  {
    try
    {
      using (NetworkStream stream = client.GetStream())
      {
        byte[] buffer = new byte[2048];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
          string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
          Console.WriteLine("Mensagem recebida: " + message);
          await ProcessMessageAsync(message, stream, client);
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("Erro ao processar cliente: " + ex.Message);
    }
    finally
    {
      DisconnectClient(client);
    }
  }

  // ---------------------------------------------------------------
  // Comandos recebidos do client
  private async Task ProcessMessageAsync(string message, NetworkStream stream, TcpClient client)
  {
    try
    {
      var request = JsonSerializer.Deserialize<ServerRequest>(message);

      if (request == null || string.IsNullOrWhiteSpace(request.Command))
      {
        await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Invalid request format" });
        return;
      }

      switch (request.Command.ToUpper())
      {
        // Estrutura de contas e personagems
        case "LOGIN_ACCOUNT":
          await LoginAccountAsync(request.Data, stream, client);
          break;
        case "CREATE_ACCOUNT":
          await CreateAccountAsync(request.Data, stream);
          break;
        case "CREATE_CHARACTER":
          await CreateCharacterAsync(request.Data, stream);
          break;
        case "GET_CHARACTER":
          await GetCharacterAsync(request.Data, stream);
          break;
        case "SELECT_CHARACTER":
          await SelectCharacterAsync(request.Data, stream);
          break;
        default:
          await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Unknown command" });
          break;
      }
    }
    catch (JsonException)
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Malformed JSON request" });
    }
  }

  // ---------------------------------------------------------------
  // Login/Register System
  private async Task LoginAccountAsync(JsonElement data, NetworkStream stream, TcpClient client)
  {
    string username = data.GetProperty("username").GetString();
    string password = data.GetProperty("password").GetString();

    int accountId = database.ValidateAccount(username, password);
    if (accountId != -1)
    {
      if (activeLogins.ContainsKey(accountId))
      {
        await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Account already logged in" });
        return;
      }

      activeLogins[accountId] = client;
      clientAccounts[client] = accountId;

      await SendResponseAsync(stream, new ServerResponse { Status = "SUCCESS", Message = "Login successful", Data = new { AccountId = accountId } });
    }
    else
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Invalid username or password" });
    }
  }

  private async Task CreateAccountAsync(JsonElement data, NetworkStream stream)
  {
    string username = data.GetProperty("username").GetString();
    string password = data.GetProperty("password").GetString();

    if (database.DoesAccountExist(username))
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Account already exists" });
      return;
    }

    database.InsertAccount(username, password);
    await SendResponseAsync(stream, new ServerResponse { Status = "SUCCESS", Message = "Account created successfully" });
  }

  // ---------------------------------------------------------------
  // CharacterCreation System
  private async Task CreateCharacterAsync(JsonElement data, NetworkStream stream)
  {
    int accountId = data.GetProperty("accountId").GetInt32();
    string characterName = data.GetProperty("name").GetString();
    string characterRace = data.GetProperty("race").GetString();

    Character newCharacter = new Character
    {
      AccountId = accountId,
      Name = characterName,
      Level = 1,
      HP = 100,
      MaxHP = 100,
      MP = 50,
      MaxMP = 50,
      XP = 0,
      MaxXP = 100,
      Race = characterRace,
      PosX = 0.0,
      PosY = 0.0,
      Strength = 10,
      Armor = 5,
      Defense = 5,
      Attack = 10
    };

    if (database.InsertCharacter(newCharacter))
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "SUCCESS", Message = "Character created successfully" });
    }
    else
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Failed to create character" });
    }
  }

  private async Task GetCharacterAsync(JsonElement data, NetworkStream stream)
  {
    int accountId = data.GetProperty("accountId").GetInt32();
    var characters = database.GetCharactersByAccountId(accountId);

    if (characters != null)
    {
      string characterList = string.Join(",", characters);
      await SendResponseAsync(stream, new ServerResponse
      {
        Status = "SUCCESS",
        Message = "Character retrieved successfully",
        Data = new { CharacterList = characterList }
      });
    }
    else
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "No characters found" });
    }
  }

  private async Task SelectCharacterAsync(JsonElement data, NetworkStream stream)
  {
    int accountId = data.GetProperty("accountId").GetInt32();
    string characterName = data.GetProperty("characterName").GetString();

    var character = database.GetCharactersByAccountId(accountId);
    if (character != null)
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "SUCCESS", Message = "Character selected successfully" });
    }
    else
    {
      await SendResponseAsync(stream, new ServerResponse { Status = "ERROR", Message = "Character not found" });
    }
  }

  // ---------------------------------------------------------------

  private async Task SendResponseAsync(NetworkStream stream, ServerResponse response)
  {
    string jsonResponse = JsonSerializer.Serialize(response);
    byte[] data = Encoding.UTF8.GetBytes(jsonResponse);
    await stream.WriteAsync(data, 0, data.Length);
  }

  private void DisconnectClient(TcpClient client)
  {
    if (clientAccounts.ContainsKey(client))
    {
      int accountId = clientAccounts[client];
      activeLogins.TryRemove(accountId, out _);
      clientAccounts.TryRemove(client, out _);
    }
  }

  public static void Main(string[] args)
  {
    Server server = new Server();
    server.StartAsync().GetAwaiter().GetResult();
  }
}

class ServerRequest
{
  public string? Command { get; set; }
  public JsonElement Data { get; set; }
}

class ServerResponse
{
  public string? Status { get; set; }
  public string? Message { get; set; }
  public object? Data { get; set; }
}
