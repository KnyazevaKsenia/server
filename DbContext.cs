using System.Text;
using Npgsql;
using System.Data;
using Microsoft.EntityFrameworkCore;


namespace OmahaPokerServer
{
    public class PokerDbContext : DbContext
    {
        static NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5555,
            Username = "postgres",
            Password = "20242424",
            Database = "OmahaPoker"
        };
        static string connectionString = builder.ConnectionString;
        NpgsqlConnection _dbConnection = new NpgsqlConnection(connectionString);
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }

        /*public async Task GetUser(string login, CancellationToken cancellationToken = default)
        {
            await _dbConnection.OpenAsync(cancellationToken);
            try
            {
                const string sqlQuery = "SELECT * FROM users WHERE login =@login";
                var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
                cmd.Parameters.AddWithValue("login", login);
                var reader = await cmd.ExecuteReaderAsync(cancellationToken);//это метод для выполнения SQL-запроса, который возвращает полный набор результатов
                if (reader.HasRows && await reader.ReadAsync(cancellationToken))
                {
                    return new User
                    {
                        Id = reader.GetInt64("id"),
                        Login = reader.GetString("login"),
                        Password = reader.GetString("password"),
                        Role = reader.GetString("role"),
                    };
                }
            }
            finally
            {
                await _dbConnection.CloseAsync();
            }
            return null;
        }
*/
        public async Task<(int playerId, int wallet)> GetPlayerByNickname(string nickname, CancellationToken cancellationToken)
        {
            await _dbConnection.OpenAsync(cancellationToken);
            try
            {
                if (nickname != null)
                {
                    const string sqlQuery = "SELECT player_id, wallet FROM players WHERE nickname = @nickname";
                    var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
                    cmd.Parameters.AddWithValue("nickname", nickname);
                    var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                    if (reader.HasRows && await reader.ReadAsync(cancellationToken))
                    {
                        int playerId = reader.GetInt32(0);   // Получаем player_id
                        int wallet = reader.GetInt32(1);     // Получаем wallet

                        return (playerId, wallet);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await _dbConnection.CloseAsync();
            }

            // Возвращаем значения по умолчанию, если запись не найдена
            return (0, 0);
        }

        public async Task<(int playerId, int wallet)> SavePlayer(string nickname, string password, CancellationToken cancellationToken)
        {
            await _dbConnection.OpenAsync(cancellationToken);
            try
            {
                if (nickname != null && password != null)
                {
                    const string sqlQuery = "INSERT INTO players (nickname, password) VALUES (@nickname, @password) returning player_id, wallet";
                    var cmd = new NpgsqlCommand(sqlQuery, _dbConnection);
                    cmd.Parameters.AddWithValue("nickname", nickname);
                    cmd.Parameters.AddWithValue("password", password);
                    var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                    if (reader.HasRows && await reader.ReadAsync(cancellationToken))
                    {
                        int playerId = reader.GetInt32(0);
                        int wallet = reader.GetInt32(1);
                        return (playerId, wallet);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await _dbConnection.CloseAsync();
            }
            return (0, 0);
        }


    }
}