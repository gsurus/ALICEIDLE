using MySqlConnector;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace ALICEIDLE.Services
{
    public class SqlDBHandler : SqlQueries
    {

        public static MySqlConnection connection { get; set; }

        public static string connectionString = Program._config["SQLConnectionString"];
        public static async Task<object> GetRandomWaifuWithTopFavoritesAsync(string type)
        {
            string query = $"{selectFromOrderBy} `Favorites` DESC LIMIT 100;";

            var response = await GetWaifusFromQuery(query);
            return response;
            
        }
        public static async Task<Waifu> QueryWaifuByTier(int tier, string gender)
        {
            string query = selectFrom;
            Random rand = new Random();
            int max = 44696;
            int randomValue = 0;
            switch (gender.ToLower())
            {
                case "male":
                    max = 8882;
                    break;
                case "female":
                    max = 10905;
                    break;
            }
            if (gender.ToLower() != "none")
                query += $" WHERE `Gender` = \"{gender}\" ";

            switch (tier)
            {
                case 0:
                    randomValue = rand.Next(1000, max);
                    query += $"ORDER BY `Favorites` DESC LIMIT {randomValue}, 20;";
                    break;
                case 1:
                    randomValue = rand.Next(300, 999);
                    query += $"ORDER BY `Favorites` DESC LIMIT {randomValue}, 20;";
                    break;
                case 2:
                    randomValue = rand.Next(50, 299);
                    query += $"ORDER BY `Favorites` DESC LIMIT {randomValue}, 20;";
                    break;
                case 3:
                    randomValue = rand.Next(0, 49);
                    query += $"ORDER BY `Favorites` DESC LIMIT {randomValue}, 20;";
                    break;
            }
            
            List<Waifu> waifu = await GetWaifusFromQuery(query);
            if (gender.ToLower() != "none")
                return waifu.Find(c => c.Gender.ToLower() == gender.ToLower());
            else return waifu[0];
        }
        public static async Task<List<Waifu>> QueryWaifuByIds(List<int> ids, bool distinct = false)
        {
            string _ids = string.Join(',', ids);
            string query = $"{selectFromWhere} `Id` IN ({_ids});";
            if (distinct)
                query = $"{selectDistinctFromWhere} `Id` IN ({_ids});";

            List<Waifu> waifu = await GetWaifusFromQuery(query);
            return waifu;
        }
        public static async Task<Waifu> QueryWaifuById(int id)
        {
            string query = $"{selectFromWhere} `Id` = {id};";

            List<Waifu> waifu = await GetWaifusFromQuery(query);
            return waifu.FirstOrDefault();
        }
        public static async Task<Waifu> QueryWaifuByName(string name)
        {
            string query = $"{selectFromWhere} `Name_Full` = \"{name}\";";
            List<Waifu> waifu = await GetWaifusFromQuery(query);

            return waifu.FirstOrDefault();
        }
        static async Task<List<Waifu>> GetWaifusFromQuery(string query)
        {
            connection = new MySqlConnection(connectionString); 
            await connection.OpenAsync();

            MySqlCommand command = new MySqlCommand(query, connection);
            MySqlDataReader reader = command.ExecuteReader();

            List<Waifu> waifuList = new List<Waifu>();
            while (reader.Read())
            {
                Waifu waifu = await MapToWaifu(reader);
                waifuList.Add(waifu);
            }
            await connection.CloseAsync();

            return waifuList;
        }
        public static async Task<Waifu> MapToWaifu(MySqlDataReader reader)
        {
            Waifu waifu = new()
            {
                Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Gender")),
                Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Age")),
                Series = reader.IsDBNull(reader.GetOrdinal("Series")) ? null : reader.GetString(reader.GetOrdinal("Series")),
                ImageURL = reader.IsDBNull(reader.GetOrdinal("ImageURL")) ? null : reader.GetString(reader.GetOrdinal("ImageURL")),
                Rarity = reader.GetString(reader.GetOrdinal("Rarity")),
                XpValue = reader.GetInt32(reader.GetOrdinal("XpValue")),
                Favorites = reader.GetInt32(reader.GetOrdinal("Favorites")),
                SeriesId = reader.IsDBNull(reader.GetOrdinal("SeriesId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("SeriesId")),
                IsAdult = reader.IsDBNull(reader.GetOrdinal("IsAdult")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("IsAdult")),
                Name = new()
                {
                    Full = reader.IsDBNull(reader.GetOrdinal("Name_Full")) ? "" : reader.GetString(reader.GetOrdinal("Name_Full")),
                    First = reader.IsDBNull(reader.GetOrdinal("Name_First")) ? "" : reader.GetString(reader.GetOrdinal("Name_First")),
                    Middle = reader.IsDBNull(reader.GetOrdinal("Name_Middle")) ? "" : reader.GetString(reader.GetOrdinal("Name_Middle")),
                    Last = reader.IsDBNull(reader.GetOrdinal("Name_Last")) ? "" : reader.GetString(reader.GetOrdinal("Name_Last")),
                    UserPreferred = reader.IsDBNull(reader.GetOrdinal("Name_UserPreferred")) ? "" : reader.GetString(reader.GetOrdinal("Name_UserPreferred")),
                    Alternative = new(),
                    AlternativeSpoiler = new(),
                    Native = reader.IsDBNull(reader.GetOrdinal("Name_Native")) ? "" : reader.GetString(reader.GetOrdinal("Name_Native")),
                },
                DateOfBirth = new()
                {
                    month = reader.IsDBNull(reader.GetOrdinal("DateOfBirth_month")) ? (int?)-1 : reader.GetInt32(reader.GetOrdinal("DateOfBirth_month")),
                    day = reader.IsDBNull(reader.GetOrdinal("DateOfBirth_day")) ? (int?)-1 : reader.GetInt32(reader.GetOrdinal("DateOfBirth_day")),
                    year = reader.IsDBNull(reader.GetOrdinal("DateOfBirth_year")) ? (int?)-1 : reader.GetInt32(reader.GetOrdinal("DateOfBirth_year")),
                },
                Media = new()
                {
                    nodes = new()
                {
                    new()
                    {
                        Popularity = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Popularity")) ? 0 : reader.GetInt32(reader.GetOrdinal("Media_nodes_Popularity")),
                        Title = new()
                        {
                            UserPreferred = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Title_UserPreferred")) ? null : reader.GetString(reader.GetOrdinal("Media_nodes_Title_UserPreferred")),
                            English = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Title_English")) ? null : reader.GetString(reader.GetOrdinal("Media_nodes_Title_English")),
                            Romaji = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Title_Romaji")) ? null : reader.GetString(reader.GetOrdinal("Media_nodes_Title_Romaji")),
                            Native = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Title_Native")) ? null : reader.GetString(reader.GetOrdinal("Media_nodes_Title_Native")),
                        },
                Id = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Media_nodes_Id")),
                IsAdult = reader.IsDBNull(reader.GetOrdinal("Media_nodes_IsAdult")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("Media_nodes_IsAdult")),
                Type = reader.IsDBNull(reader.GetOrdinal("Media_nodes_Type")) ? null : reader.GetString(reader.GetOrdinal("Media_nodes_Type"))
                }
                }
                }
            };
            return waifu;
        }

            public static async Task InsertPlayerDataIntoTable(PlayerData data)
        {
            string tableName = "PlayerData";
            List<string> fieldNames = new List<string>
            {
                "Name", "Id", "GenderPreference", "Level", 
                "Xp", "WaifuAmount", "LastCharacterRolled", "RollsSinceLastSSR", 
                "CurrentWaifu_Id", "OwnedWaifus", "RollHistory", "TotalRolls"
            };
            List<Type> dataTypes = new List<Type>
            {
                typeof(string), typeof(int), typeof(string), typeof(int), 
                typeof(int), typeof(int), typeof(double), typeof(int), 
                typeof(int), typeof(string), typeof(List<int>), typeof(int)
            };
            string insertQuery = "INSERT INTO " + tableName + " (";
            foreach (string fieldName in fieldNames)
            {
                insertQuery += fieldName + ",";
            }
            insertQuery = insertQuery.TrimEnd(',') + ") VALUES (";
            for (int i = 0; i < fieldNames.Count; i++)
            {
                insertQuery += "@" + i.ToString() + ",";
            }
            insertQuery = insertQuery.TrimEnd(',') + ")";

            using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
            {
                PlayerData row = data;
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    object fieldValue = GetPlayerDataFieldValue(row, fieldName);
                    command.Parameters.AddWithValue("@" + i.ToString(), fieldValue ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
                command.Parameters.Clear();
            }
        }
        public static async Task UpdatePlayerData(PlayerData data)
        {
            string connectionString = Program._config["SQLConnectionString"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var properties = typeof(PlayerData).GetProperties();
            var columns = properties.Select(p => new { Name = p.Name, DataType = GetSqlDataType(p.PropertyType) }).ToList();

            string tableName = "PlayerData";
            var updateQuery = new StringBuilder();
            updateQuery.Append("UPDATE ").Append(tableName).Append(" SET ");

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                updateQuery.Append(column.Name).Append(" = @").Append(i);
                if (i < columns.Count - 1)
                {
                    updateQuery.Append(", ");
                }
            }

            updateQuery.Append(" WHERE Id = @Id");

            using (MySqlCommand command = new MySqlCommand(updateQuery.ToString(), connection))
            {
                // Set the parameter values
                for (int i = 0; i < columns.Count; i++)
                {
                    var columnName = columns[i].Name;
                    object columnValue = GetColumnValue(data, columnName);
                    command.Parameters.AddWithValue("@" + i.ToString(), columnValue ?? DBNull.Value);
                }
                // Set the Id parameter value
                command.Parameters.AddWithValue("@Id", data.Id);
                await command.ExecuteNonQueryAsync();
            }
        }
        public static async Task<PlayerData> InsertPlayerData(string username, ulong id)
        {
            PlayerData data = new PlayerData
            {
                Name = username,
                Id = id,
                GenderPreference = "None",
                OwnedWaifus = new List<Tuple<int, int>>(),
                RollHistory = new List<int>()
            };

            data.OwnedWaifus.Add(new Tuple<int, int>(-1, 0));

            string connectionString = Program._config["SQLConnectionString"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            int batchSize = 1000;
            var properties = typeof(PlayerData).GetProperties();
            var columns = properties.Select(p => new { Name = p.Name, DataType = GetSqlDataType(p.PropertyType) }).ToList();

            string tableName = "PlayerData";
            string insertQuery = "INSERT INTO " + tableName + " (";
            foreach (var column in columns)
            {
                insertQuery += column.Name + ",";
            }
            insertQuery = insertQuery.TrimEnd(',') + ") VALUES (";
            for (int i = 0; i < columns.Count; i++)
            {
                insertQuery += "@" + i.ToString() + ",";
            }
            insertQuery = insertQuery.TrimEnd(',') + ")";

            using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
            {
                // Execute the insert query for each batch of rows
                PlayerData row = data;
                for (int i = 0; i < columns.Count; i++)
                {
                    string columnName = columns[i].Name;
                    object columnValue = GetColumnValue(row, columnName);
                    command.Parameters.AddWithValue("@" + i.ToString(), columnValue ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
                command.Parameters.Clear();
            }
            return data;
        }
        public static async void InsertAllPlayerData()
        {
            string connectionString = Program._config["SQLConnectionString"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            List<PlayerData> data = JsonConvert.DeserializeObject<List<PlayerData>>(File.ReadAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\waifu_data.json"));
            data = data.OrderByDescending(p => p.Id).ToList();

            int batchSize = 1000;
            var properties = typeof(PlayerData).GetProperties();
            var columns = properties.Select(p => new { Name = p.Name, DataType = GetSqlDataType(p.PropertyType) }).ToList();

            string tableName = "PlayerData";
            string insertQuery = "INSERT INTO " + tableName + " (";

            foreach (var column in columns)
            {
                insertQuery += column.Name + ",";
            }
            insertQuery = insertQuery.TrimEnd(',') + ") VALUES (";
            for (int i = 0; i < columns.Count; i++)
            {
                insertQuery += "@" + i.ToString() + ",";
            }
            insertQuery = insertQuery.TrimEnd(',') + ")";

            using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
            {
                // Execute the insert query for each batch of rows
                for (int batchStart = 0; batchStart < data.Count; batchStart += batchSize)
                {
                    int batchEnd = Math.Min(batchStart + batchSize, data.Count);
                    for (int rowIndex = batchStart; rowIndex < batchEnd; rowIndex++)
                    {
                        PlayerData row = data[rowIndex];
                        for (int i = 0; i < columns.Count; i++)
                        {
                            string columnName = columns[i].Name;
                            object columnValue = GetColumnValue(row, columnName);
                            command.Parameters.AddWithValue("@" + i.ToString(), columnValue ?? DBNull.Value);
                        }
                        await command.ExecuteNonQueryAsync();
                        command.Parameters.Clear();
                    }
                }
            }
        }
        public static async Task<bool> PlayerExists(ulong id)
        {
            PlayerData playerData = await RetrievePlayerData(id);
            if (playerData == null)
            {
                Console.WriteLine("Returned false");
                return false;
            }
            else
                return true;
        }
public static async Task<PlayerData> RetrievePlayerData(ulong id)
{
    using MySqlConnection connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    
    string query = $"SELECT * FROM `aliceidle`.`PlayerData` WHERE `Id` = {id}";
    using MySqlCommand command = new MySqlCommand(query, connection);
    using MySqlDataReader reader = await command.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        var playerData = new PlayerData();
        var properties = playerData.GetType().GetProperties();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var property = properties.FirstOrDefault(p => p.Name == columnName);
            if (property != null)
            {
                var value = reader.GetValue(i);
                if (value is DBNull)
                {
                    value = null;
                    return null;
                }

                if (property.PropertyType == typeof(List<Tuple<int, int>>) && value is string tupleListString)
                {
                    value = JsonConvert.DeserializeObject<List<Tuple<int, int>>>(tupleListString);
                }
                else if (property.PropertyType == typeof(List<int>) && value is string intListString)
                {
                    value = JsonConvert.DeserializeObject<List<int>>(intListString);
                }

                property.SetValue(playerData, Convert.ChangeType(value, property.PropertyType));
            }
        }

        return playerData;
    }

    return null;
}
        public static object GetColumnValue(PlayerData row, string columnName)
        {
            var property = typeof(PlayerData).GetProperty(columnName);
            if (property != null)
            {
                var value = property.GetValue(row);
                if (value is List<Tuple<int, int>> tupleList)
                {
                    // Convert List<Tuple<int, int>> to JSON string
                    return JsonConvert.SerializeObject(tupleList);
                }
                else if (value is List<int> intList)
                {
                    // Convert List<int> to JSON string
                    return JsonConvert.SerializeObject(intList);
                }
                else
                {
                    return value;
                }
            }
            else
            {
                return null;
            }
        }

        public static bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        public static async Task InsertDataIntoDatabase()
        {
            string connectionString = Program._config["SQLConnectionString"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            List<Waifu> data = JsonConvert.DeserializeObject<List<Waifu>>(File.ReadAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\waifus.json"));
            data.OrderBy(p => p.Favorites).Reverse().ToList();
            // Extract the field names and data types from the C# object
            // Extract the field names and data types from the Waifu class
            List<string> fieldNames = new List<string>
            {
                "id", "Gender", "Age", "Series", "ImageURL", "Rarity", "XpValue", "Favorites", "SeriesId", "IsAdult",
                "Name_Full", "Name_First", "Name_Middle", "Name_Last", "Name_UserPreferred", "Name_Alternative",
                "Name_AlternativeSpoiler", "Name_Native", "DateOfBirth_month", "DateOfBirth_day", "DateOfBirth_year",
                "Media_nodes_Popularity", "Media_nodes_Title_UserPreferred", "Media_nodes_Title_English",
                "Media_nodes_Title_Romaji", "Media_nodes_Title_Native", "Media_nodes_Id", "Media_nodes_IsAdult",
                "Media_nodes_Type"
            };
            List<Type> dataTypes = new List<Type>
            {
                typeof(int), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(int),
                typeof(int), typeof(int), typeof(bool),
                typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(List<string>), typeof(List<string>), typeof(string), typeof(int), typeof(int), typeof(int),
                typeof(int), typeof(string), typeof(string), typeof(string), typeof(string), typeof(int),
                typeof(bool), typeof(string)
            };

            // Create a new table in the database using the extracted field names and data types
            using (connection = new MySqlConnection(connectionString))
            {
                Console.WriteLine("Starting");
                int batchSize = 1000;
                string tableName = "mytable";
                string createTableQuery = "CREATE TABLE " + tableName + " (";

                for (int i = 0; i < fieldNames.Count; i++)
                {
                    Console.WriteLine($"i: {i}");
                    string fieldName = fieldNames[i];
                    Type dataType = dataTypes[i];
                    string sqlDataType = GetSqlDataType(dataType);
                    createTableQuery += fieldName + " " + sqlDataType + ",";

                }

                createTableQuery = createTableQuery.TrimEnd(',') + ")";

                using (MySqlCommand command = new MySqlCommand(createTableQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // Insert the data into the new table using a parameterized SQL query
                string insertQuery = "INSERT INTO " + tableName + " (";

                foreach (string fieldName in fieldNames)
                {
                    insertQuery += fieldName + ",";
                }

                insertQuery = insertQuery.TrimEnd(',') + ") VALUES (";

                for (int i = 0; i < fieldNames.Count; i++)
                {
                    insertQuery += "@" + i.ToString() + ",";
                }

                insertQuery = insertQuery.TrimEnd(',') + ")";
                
                using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
                {
                    // Execute the insert query for each batch of rows
                    for (int batchStart = 0; batchStart < data.Count; batchStart += batchSize)
                    {
                        int batchEnd = Math.Min(batchStart + batchSize, data.Count);
                        for (int rowIndex = batchStart; rowIndex < batchEnd; rowIndex++)
                        {
                            Waifu row = data[rowIndex];
                            for (int i = 0; i < fieldNames.Count; i++)
                            {
                                string fieldName = fieldNames[i];
                                object fieldValue = GetFieldValue(row, fieldName);
                                command.Parameters.AddWithValue("@" + i.ToString(), fieldValue ?? DBNull.Value);
                            }
                            await command.ExecuteNonQueryAsync();
                            command.Parameters.Clear();
                        }
                    }
                }
            }
        }
        
        public static async Task RemoveFavoriteFromDatabase(ulong id, int favoriteId)
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = $"UPDATE PlayerData SET OwnedWaifus = JSON_REMOVE(OwnedWaifus, REPLACE(JSON_SEARCH(OwnedWaifus, 'one', {favoriteId}), '\"', '')) WHERE Id = {id}";
            using MySqlCommand command = new MySqlCommand(query, connection);
        }
        
        public static string GetSqlDataType(Type dataType)
        {
            if (dataType == typeof(int) || dataType == typeof(long))
            {
                return "INT";
            }
            else if (dataType == typeof(float) || dataType == typeof(double))
            {
                return "FLOAT";
            }
            else if (dataType == typeof(decimal))
            {
                return "DECIMAL(18,4)";
            }
            else if (dataType == typeof(DateTime))
            {
                return "DATETIME";
            }
            else if (dataType == typeof(bool))
            {
                return "BIT";
            }
            else
            {
                return "VARCHAR(255)";
            }
        }
        
        private static object GetPlayerDataFieldValue(PlayerData row, string fieldName)
        {
            switch (fieldName)
            {
                case "Id": return row.Id;
                case "Name": return row.Name;
                case "GenderPreference": return row.GenderPreference;
                case "Level": return row.Level;
                case "Xp": return row.Xp;
                case "WaifuAmount": return row.WaifuAmount;
                case "LastCharacterRolled": return row.LastCharacterRolled;
                case "RollsSinceLastSSR": return row.RollsSinceLastSSR;
                case "CurrentWaifu_Id": return row.CurrentWaifu;
                case "OwnedWaifus": return row.OwnedWaifus;
                case "RollHistory": return row.RollHistory;
                case "TotalRolls": return row.TotalRolls;
                
                default:
                    return null;
            }
        }
        private static object GetFieldValue(Waifu row, string fieldName)
        {
            switch (fieldName)
            {
                case "id": return row.Id;
                case "Gender": return row.Gender;
                case "Age": return row.Age;
                case "Series": return row.Series;
                case "ImageURL": return row.ImageURL;
                case "Rarity": return row.Rarity;
                case "XpValue": return row.XpValue;
                case "Favorites": return row.Favorites;
                case "SeriesId": return row.SeriesId;
                case "IsAdult": return row.IsAdult;
                case "Name_Full": return row.Name.Full;
                case "Name_First": return row.Name.First;
                case "Name_Middle": return row.Name.Middle;
                case "Name_Last": return row.Name.Last;
                case "Name_UserPreferred": return row.Name.UserPreferred;
                case "Name_Alternative": return row.Name.Alternative != null ? string.Join("|", row.Name.Alternative) : null;
                case "Name_AlternativeSpoiler": return row.Name.AlternativeSpoiler != null ? string.Join("|", row.Name.AlternativeSpoiler) : null;
                case "Name_Native": return row.Name.Native;
                case "DateOfBirth_month": return row.DateOfBirth?.month;
                case "DateOfBirth_day": return row.DateOfBirth?.day;
                case "DateOfBirth_year": return row.DateOfBirth?.year;
                case "Media_nodes_Popularity":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Popularity;
                case "Media_nodes_Title_UserPreferred":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Title?.UserPreferred;
                case "Media_nodes_Title_English":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Title?.English;
                case "Media_nodes_Title_Romaji":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Title?.Romaji;
                case "Media_nodes_Title_Native":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Title?.Native;
                case "Media_nodes_Id":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Id;
                case "Media_nodes_IsAdult":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.IsAdult;
                case "Media_nodes_Type":
                    if (row.Media?.nodes.Count() < 1)
                        return null;
                    return row.Media?.nodes[0]?.Type;
                default:
                    return null;
            }
        }
    }
}
