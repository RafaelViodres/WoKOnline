using System.ComponentModel.DataAnnotations;

public class Account
{
  [Key]  // Define que o Id é a chave primária
  public int Id { get; set; }  // ID único da conta
  public string Username { get; set; }  // Nome de usuário
  public string Password { get; set; }  // Senha da conta

  // Construtores
  public Account(int id, string username, string password)
  {
    Id = id;
    Username = username;
    Password = password;
  }
}
