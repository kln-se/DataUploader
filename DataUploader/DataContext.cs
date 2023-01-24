using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataUploader
{
    internal class DataContext
    {
        private static string ConnectionString
        {
            get
            {
                string[] connectionSettings = AppConfig.GetSettings();

                string host = connectionSettings[0];
                string port = connectionSettings[1];
                string databaseName = connectionSettings[2];
                string login = connectionSettings[3];
                string password = connectionSettings[4];

                if (int.TryParse(port, out int integerPort))
                {
                    NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder
                    {
                        Host = host,
                        Port = integerPort,
                        Database = databaseName,
                        IntegratedSecurity = false,
                        Username = login,
                        Password = password
                    };

                    return csb.ToString();

                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Проверка возможности подключения к базе данных при заданных настройках подключения
        /// </summary>
        internal static async Task<string> TestConnectionAsync()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        return string.Format("Подключено ({0})", connection.State.ToString());
                    }
                    else
                    {
                        return string.Format("Подключение не удалось ({0})", connection.State.ToString());
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return "Ошибка подключения";
                }
            }
        }

        /// <summary>
        /// Запрос в БД какие есть подстанции и трансформаторы или категории и типы файлов
        /// </summary>
        /// <param name="queryType">"substations_and_transformers" или "categories_and_files"</param>
        /// <returns></returns>
        internal static async Task<List<DataModel.Node>> GetParentsAndChilrenAsync(string queryType)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    List<DataModel.Node> parentsList = new List<DataModel.Node>();
                    var parentsMap = new Dictionary<int, DataModel.Node>();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {

                        string sqlQuery = null;
                        switch (queryType)
                        {
                            case "substations_and_transformers":

                                sqlQuery = "SELECT t.trans_id, t.trans_name, s.sbst_id, s.sbst_name " +
                                           "FROM public.transformers AS t RIGHT JOIN public.substations AS s ON t.sbst_id = s.sbst_id " +
                                           "ORDER BY t.trans_id";
                                break;

                            case "categories_and_files":

                                sqlQuery = "SELECT f.f_id, f.f_name, c.ctg_id, c.ctg_name " +
                                           "FROM public.files AS f RIGHT JOIN public.categories AS c ON f.ctg_id = c.ctg_id " +
                                           "ORDER BY f.f_id";
                                break;
                        }

                        using (var cmd = new NpgsqlCommand(sqlQuery,
                                                           connection))
                        {
                            await cmd.PrepareAsync();
                            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                int? childId = (reader[0] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[0]);
                                string childName = (reader[1] == DBNull.Value) ? null : Convert.ToString(reader[1]);
                                int parentId = Convert.ToInt32(reader[2]);
                                string parentName = (reader[3] == DBNull.Value) ? null : Convert.ToString(reader[3]);

                                if (!parentsMap.ContainsKey(parentId))
                                {
                                    var tempParent = new DataModel.Node()
                                    {
                                        Name = parentName,
                                        Id = parentId,
                                        Parent = null
                                    };

                                    parentsMap[parentId] = tempParent;
                                    parentsList.Add(tempParent);
                                }

                                var tempChild = new DataModel.Node()
                                {
                                    Name = childName,
                                    Id = childId,
                                    Parent = parentsMap[parentId]
                                };

                                if (childId != null)
                                {
                                    parentsMap[parentId].Childs.Add(tempChild);
                                }
                            }
                            return parentsList;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {

                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Вставка новых подстанций и трансформаторов в БД
        /// </summary>
        ///<param name="nodeName">Имя добавляемого в БД узла</param>
        ///<param name="parentId">Необязательный параметр.
        ///Если указывается, то добавляемый узел считается дочерним, с указанным идентификатором родителя</param>
        internal static async Task<int?> InsertNodeAsync(string queryType, string nodeName, int? parentId = null)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        string sqlQuery = null;

                        // parentId = null означает, что выполняется добавление parent элемента
                        if (parentId == null)
                        {
                            switch (queryType)
                            {
                                case "substations_and_transformers":

                                    sqlQuery = "INSERT INTO public.substations (sbst_name) " +
                                               "VALUES (@node_name) " +
                                               "RETURNING sbst_id";
                                    break;

                                case "categories_and_files":

                                    sqlQuery = "INSERT INTO public.categories (ctg_name) " +
                                               "VALUES (@node_name) " +
                                               "RETURNING ctg_id";
                                    break;
                            }
                            using (var cmd = new NpgsqlCommand(sqlQuery,
                                                               connection))
                            {
                                cmd.Parameters.AddWithValue("@node_name", nodeName);

                                var nodeId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                                return nodeId;
                            }
                        }
                        // Означает, что выполняется добавление child элемента
                        else
                        {
                            switch (queryType)
                            {
                                case "substations_and_transformers":

                                    sqlQuery = "INSERT INTO public.transformers (trans_name, sbst_id) " +
                                               "VALUES (@node_name, @parent_id) " +
                                               "RETURNING trans_id";
                                    break;

                                case "categories_and_files":

                                    sqlQuery = "INSERT INTO public.files (f_name, ctg_id) " +
                                               "VALUES (@node_name, @parent_id) " +
                                               "RETURNING f_id";
                                    break;
                            }
                            using (var cmd = new NpgsqlCommand(sqlQuery,
                                                               connection))
                            {
                                cmd.Parameters.AddWithValue("@node_name", nodeName);
                                cmd.Parameters.AddWithValue("@parent_id", parentId);

                                var nodeId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                                return nodeId;
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {

                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Вставка новых подстанций и трансформаторов в БД
        /// </summary>
        internal static async Task<int?> DeleteNodeAsync(string queryType, DataModel.Node node)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        string sqlQuery = null;
                        // Удаление parent элемента
                        if (node.Parent == null)
                        {
                            switch (queryType)
                            {
                                case "substations_and_transformers":

                                    sqlQuery = "DELETE FROM public.substations " +
                                               "WHERE sbst_id=@node_id";
                                    break;

                                case "categories_and_files":

                                    sqlQuery = "DELETE FROM public.categories " +
                                               "WHERE ctg_id=@node_id";
                                    break;
                            }
                            using (var cmd = new NpgsqlCommand(sqlQuery,
                                                               connection))
                            {
                                cmd.Parameters.AddWithValue("@node_id", node.Id);
                                int updatedRows = await cmd.ExecuteNonQueryAsync();
                                return updatedRows;
                            };
                        }
                        // Удаление child элемента
                        else
                        {
                            switch (queryType)
                            {
                                case "substations_and_transformers":

                                    sqlQuery = "DELETE FROM public.transformers " +
                                               "WHERE trans_id=@node_id";
                                    break;

                                case "categories_and_files":

                                    sqlQuery = "DELETE FROM public.files " +
                                               "WHERE f_id=@node_id";
                                    break;
                            }
                            using (var cmd = new NpgsqlCommand(sqlQuery,
                                                               connection))
                            {
                                cmd.Parameters.AddWithValue("@node_id", node.Id);
                                int updatedRows = await cmd.ExecuteNonQueryAsync();
                                return updatedRows;
                            };
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {

                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Запрос в БД какие есть поля (напряжение, частота и др.) измерений
        /// </summary>
        internal static Dictionary<string, int> GetFields(int? fileId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var fieldsMap = new Dictionary<string, int>();

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    using (var cmd = new NpgsqlCommand("SELECT f.field_id, f.field_name " +
                                                        "FROM public.fields AS f " +
                                                        "WHERE f.f_id=@f_id " +
                                                        "ORDER BY field_id",
                                                        connection))
                    {
                        cmd.Parameters.AddWithValue("@f_id", fileId);
                        cmd.Prepare();
                        NpgsqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            int fieldId = Convert.ToInt32(reader[0]);
                            string fieldName = (reader[1] == DBNull.Value) ? null : Convert.ToString(reader[1]);

                            fieldsMap[fieldName] = fieldId;
                        }
                        return fieldsMap;
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Вставка новых полей в БД
        /// </summary>
        internal static Dictionary<string, int> InsertFields(List<string> fieldNames, Dictionary<string, int> fieldsMap, int? fileId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {

                        using (var cmd = new NpgsqlCommand("INSERT INTO public.fields (field_name, f_id) " +
                                                           "VALUES (@fieldName, @fileId) " +
                                                           "RETURNING field_id",
                                                           connection))
                        {
                            cmd.Parameters.Add("@fieldName", NpgsqlDbType.Varchar);
                            cmd.Parameters.Add("@fileId", NpgsqlDbType.Integer);

                            foreach (string fieldName in fieldNames)
                            {
                                cmd.Parameters["@fieldName"].Value = fieldName;
                                cmd.Parameters["@fileId"].Value = fileId;

                                int? fieldId = Convert.ToInt32(cmd.ExecuteScalar());
                                if (fieldId != null)
                                {
                                    fieldsMap[fieldName] = (int)fieldId;
                                }
                            }

                            return fieldsMap;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Вставка новых записей в БД (bulk insert)
        /// </summary>
        internal static ulong? InsertBinary(List<DataModel.Measurement> measurementsList)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    using (var writer = connection.BeginBinaryImport("COPY temp_measurements (datetime, value, field_id, trans_id, phase) " +
                                                                        "FROM STDIN (FORMAT BINARY)"))
                    {
                        for (int i = 0; i < measurementsList.Count; i++)
                        {
                            writer.StartRow();
                            writer.Write(measurementsList[i].datetime, NpgsqlDbType.Timestamp);
                            writer.Write(measurementsList[i].value, NpgsqlDbType.Real);
                            writer.Write(measurementsList[i].field_id, NpgsqlDbType.Integer);
                            writer.Write(measurementsList[i].trans_id, NpgsqlDbType.Integer);
                            writer.Write(measurementsList[i].phase, NpgsqlDbType.Varchar);

                        }
                        ulong updatedRows = writer.Complete();
                        return updatedRows;
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Проверить есть ли записи с одинаковыми ключами (между таблицами measurements
        /// и temp_measurements). Чтобы потом решить перезаписать их или пропустить.
        /// </summary>
        internal static int? СheckIntersections()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    using (var cmd = new NpgsqlCommand("SELECT count(*) " +
                                                       "FROM public.temp_measurements AS tm INNER JOIN public.measurements AS m " +
                                                       "ON tm.datetime = m.datetime AND tm.field_id = m.field_id AND tm.trans_id = m.trans_id AND tm.phase = m.phase",
                                                       connection))
                    {
                        int intersectionsCount = Convert.ToInt32(cmd.ExecuteScalar());
                        return intersectionsCount;
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Выполняет перемещение данных из таблицы temp_measurements в measurements с
        /// учетом пожеланий "перезаписать повторяющиеся", "пропустить повторяющиеся".
        /// </summary>
        internal static bool TransferTempData(string filePath, bool updateRows = false)
        {
            string filePathBackSlash = filePath.Replace("/", "\\");
            string fileName = Path.GetFileName(filePathBackSlash);
            NpgsqlTransaction transaction = null;
            int uploadedFileId;

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                    
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    transaction = connection.BeginTransaction();

                    // Вставить в БД имя файла, из которого берём данные 
                    using (var cmd = new NpgsqlCommand("INSERT INTO public.uploaded_files (uf_name, datetime) " +
                                                        "VALUES (@uploadedFileName, @datetimeOfUpload) " +
                                                        "RETURNING uf_id",
                                                        connection))
                    {
                        cmd.Parameters.AddWithValue("@uploadedFileName", fileName);
                        cmd.Parameters.AddWithValue("@datetimeOfUpload", DateTime.Now);

                        uploadedFileId = Convert.ToInt32(cmd.ExecuteScalar());
                    };

                    // Перезаписать найденные одинаковые записи
                    if (updateRows)
                    {
                        using (var cmd = new NpgsqlCommand("UPDATE public.measurements AS m " +
                                                            "SET (value, uf_id) = (subquery.value, @uploadedFileId) " +
                                                            "FROM (SELECT tm.datetime, tm.value, tm.field_id, tm.trans_id, tm.phase " +
                                                            "      FROM public.temp_measurements AS tm INNER JOIN public.measurements AS m " +
                                                            "      ON tm.datetime = m.datetime AND tm.field_id = m.field_id AND tm.trans_id = m.trans_id AND tm.phase = m.phase) AS subquery " +
                                                            "WHERE m.datetime = subquery.datetime AND m.field_id = subquery.field_id AND m.trans_id = subquery.trans_id AND m.phase = subquery.phase",
                                                            connection))
                        {
                            cmd.Parameters.AddWithValue("@uploadedFileId", uploadedFileId);
                            cmd.ExecuteNonQuery();
                        };
                    }
                        
                    // Переместить данные
                    using (var cmd = new NpgsqlCommand("INSERT INTO measurements(datetime, value, field_id, trans_id, phase, uf_id) " +
                                                        "    SELECT tm.datetime, tm.value, tm.field_id, tm.trans_id, tm.phase, @uploadedFileId " +
                                                        "    FROM public.temp_measurements AS tm LEFT JOIN public.measurements AS m " +
                                                        "    ON tm.datetime = m.datetime AND tm.field_id = m.field_id AND tm.trans_id = m.trans_id AND tm.phase = m.phase " +
                                                        "    WHERE m.datetime IS null",
                                                        connection))
                    {
                        cmd.Parameters.AddWithValue("@uploadedFileId", uploadedFileId);

                        cmd.ExecuteNonQuery();
                    };

                    // Очистить временную таблицу temp_measurements
                    using (var cmd = new NpgsqlCommand("TRUNCATE public.temp_measurements",
                                                        connection))
                    {
                        cmd.ExecuteNonQuery();
                    };
                        
                    transaction.Commit();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Запрос в БД какие файлы загружены на данный момент
        /// </summary>
        internal static async Task<List<DataModel.UploadedFile>> GetUploadedFilesAsync()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    List<DataModel.UploadedFile> uploadedFilesList = new List<DataModel.UploadedFile>();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {

                        using (var cmd = new NpgsqlCommand("SELECT" +
                                                           "    DISTINCT uf.uf_id, uf.datetime, uf.uf_name, s.sbst_name, t.trans_name, m.phase, c.ctg_name, f.f_name, COALESCE(table_cnt.m_cnt,0) as m_cnt " +
                                                           "FROM public.uploaded_files uf " +
                                                           "LEFT JOIN " +
                                                           "(" +
                                                           "select uf_id, count(*) as m_cnt from public.measurements group by uf_id" +
                                                           ") as table_cnt on table_cnt.uf_id = uf.uf_id " +
                                                           "LEFT JOIN public.measurements m on uf.uf_id  = m.uf_id " +
                                                           "LEFT JOIN public.transformers t on m.trans_id = t.trans_id " +
                                                           "LEFT JOIN public.substations s on t.trans_id = s.sbst_id " +
                                                           "LEFT JOIN public.fields fd on m.field_id  = fd.field_id " +
                                                           "LEFT JOIN public.files f on fd.f_id  = f.f_id " +
                                                           "LEFT JOIN public.categories c on f.ctg_id  = c.ctg_id " +
                                                           "ORDER BY uf.datetime DESC",
                                                           connection))
                        {
                            await cmd.PrepareAsync();
                            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                int? uploadedFileId = (reader[0] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[0]);
                                string uploadingDatetime = (reader[1] == DBNull.Value) ? null : Convert.ToString(reader[1]);
                                string uploadedFileName = (reader[2] == DBNull.Value) ? null : Convert.ToString(reader[2]);
                                string substation = (reader[3] == DBNull.Value) ? null : Convert.ToString(reader[3]);
                                string transformer = (reader[4] == DBNull.Value) ? null : Convert.ToString(reader[4]);
                                string phase = (reader[5] == DBNull.Value) ? null : Convert.ToString(reader[5]);
                                string category = (reader[6] == DBNull.Value) ? null : Convert.ToString(reader[6]);
                                string fileType = (reader[7] == DBNull.Value) ? null : Convert.ToString(reader[7]);
                                int? recordsNum = (reader[8] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[8]);

                                var tempUploadedFile = new DataModel.UploadedFile()
                                {
                                    Id = uploadedFileId,
                                    DateTime = uploadingDatetime,
                                    FileName = uploadedFileName,
                                    Substation = substation,
                                    Transformer = transformer,
                                    Phase = phase,
                                    Category = category,
                                    FileType = fileType,
                                    RecordsNum = recordsNum
                                };

                                uploadedFilesList.Add(tempUploadedFile);
                            }
                            return uploadedFilesList;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {

                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Вставка новых подстанций и трансформаторов в БД
        /// </summary>
        internal static async Task<int?> DeleteUploadedFileAsync(DataModel.UploadedFile uf)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = new NpgsqlCommand("DELETE FROM public.uploaded_files " +
                                                           "WHERE uf_id=@uploaded_file_id",
                                                            connection))
                        {
                            cmd.Parameters.AddWithValue("@uploaded_file_id", uf.Id);
                            int updatedRows = await cmd.ExecuteNonQueryAsync();
                            return updatedRows;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {

                    string messageBoxText = ex.Message;
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                    return null;
                }
            }
        }
    }
}