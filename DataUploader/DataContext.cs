using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                        Password = password,
                        Pooling = true
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
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

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
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

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
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Удаление подстанций и трансформаторов из БД
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
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

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
        }

        /// <summary>
        /// Вставка новых записей в БД (bulk insert)
        /// </summary>
        internal static ulong? InsertBinary(List<DataModel.ImportedMeasurement> measurementsList, int selectedAveragingRange, bool averagingIsEnabled)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    using (var writer = connection.BeginBinaryImport("COPY temp_measurements (datetime, value, value_min, value_max, field_id, trans_id, phase, avg_range) " +
                                                                     "FROM STDIN (FORMAT BINARY)"))
                    {
                        for (int i = 0; i < measurementsList.Count; i++)
                        {
                            writer.StartRow();
                            writer.Write(measurementsList[i].datetime, NpgsqlDbType.Timestamp);
                            writer.Write(measurementsList[i].value, NpgsqlDbType.Real);

                            if (measurementsList[i].value_min == null) { writer.Write(measurementsList[i].value_min, NpgsqlDbType.Real); }
                            else { writer.Write((float)measurementsList[i].value_min, NpgsqlDbType.Real); }

                            if (measurementsList[i].value_max == null) { writer.Write(measurementsList[i].value_max, NpgsqlDbType.Real); }
                            else { writer.Write((float)measurementsList[i].value_max, NpgsqlDbType.Real); }

                            writer.Write(measurementsList[i].field_id, NpgsqlDbType.Integer);
                            writer.Write(measurementsList[i].trans_id, NpgsqlDbType.Integer);
                            writer.Write(measurementsList[i].phase, NpgsqlDbType.Varchar);
                            if (averagingIsEnabled)
                            // С усреднением
                            { writer.Write(selectedAveragingRange, NpgsqlDbType.Smallint); }
                            // Без усреднения
                            else { writer.Write(0, NpgsqlDbType.Smallint); }
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
                                                       "ON tm.datetime = m.datetime AND tm.field_id = m.field_id AND tm.trans_id = m.trans_id AND tm.phase = m.phase AND tm.avg_range = m.avg_range",
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
        internal static int? TransferTempData(string filePath, bool updateRows = false)
        {
            string filePathBackSlash = filePath.Replace("/", "\\");
            string fileName = Path.GetFileName(filePathBackSlash);
            NpgsqlTransaction transaction = null;
            int uploadedFileId;
            int? updatedRows;

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
                                                            "SET (value, value_min, value_max, uf_id) = (subquery.value, subquery.value_min, subquery.value_max, @uploadedFileId) " +
                                                            "FROM (SELECT tm.datetime, tm.value, tm.value_min, tm.value_max, tm.field_id, tm.trans_id, tm.phase " +
                                                            "      FROM public.temp_measurements AS tm INNER JOIN public.measurements AS m " +
                                                            "      ON tm.datetime = m.datetime AND tm.field_id = m.field_id AND tm.trans_id = m.trans_id AND tm.phase = m.phase AND tm.avg_range = m.avg_range) AS subquery " +
                                                            "WHERE m.datetime = subquery.datetime AND m.field_id = subquery.field_id AND m.trans_id = subquery.trans_id AND m.phase = subquery.phase",
                                                            connection))
                        {
                            cmd.CommandTimeout = 300;
                            cmd.Parameters.AddWithValue("@uploadedFileId", uploadedFileId);
                            updatedRows = cmd.ExecuteNonQuery();
                        };
                    }

                    // Переместить данные без перезаписи
                    using (var cmd = new NpgsqlCommand("INSERT INTO measurements(datetime, value, value_min, value_max, field_id, trans_id, phase, uf_id, avg_range) " +
                                                        "    SELECT tm.datetime, tm.value, tm.value_min, tm.value_max, tm.field_id, tm.trans_id, tm.phase, @uploadedFileId, tm.avg_range " +
                                                        "    FROM public.temp_measurements AS tm LEFT JOIN public.measurements AS m " +
                                                        "    ON tm.datetime = m.datetime AND tm.field_id = m.field_id AND tm.trans_id = m.trans_id AND tm.phase = m.phase AND tm.avg_range = m.avg_range " +
                                                        "    WHERE m.datetime IS null",
                                                        connection))
                    {
                        cmd.CommandTimeout = 300;
                        cmd.Parameters.AddWithValue("@uploadedFileId", uploadedFileId);
                        updatedRows = cmd.ExecuteNonQuery();
                    };

                    // Очистить временную таблицу temp_measurements
                    using (var cmd = new NpgsqlCommand("TRUNCATE public.temp_measurements",
                                                        connection))
                    {
                        cmd.ExecuteNonQuery();
                    };

                    transaction.Commit();
                    return updatedRows;
                }
                else
                {
                    return null;
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
                                                           "    DISTINCT uf.uf_id, uf.datetime, uf.uf_name, s.sbst_name, t.trans_name, m.phase, c.ctg_name, f.f_name, m.avg_range, COALESCE(table_cnt.m_cnt,0) as m_cnt " +
                                                           "FROM public.uploaded_files uf " +
                                                           "LEFT JOIN " +
                                                           "(" +
                                                           "SELECT uf_id, count(*) AS m_cnt FROM public.measurements GROUP BY uf_id" +
                                                           ") AS table_cnt ON table_cnt.uf_id = uf.uf_id " +
                                                           "LEFT JOIN public.measurements m ON uf.uf_id  = m.uf_id " +
                                                           "LEFT JOIN public.transformers t ON m.trans_id = t.trans_id " +
                                                           "LEFT JOIN public.substations s ON t.sbst_id = s.sbst_id " +
                                                           "LEFT JOIN public.fields fd ON m.field_id  = fd.field_id " +
                                                           "LEFT JOIN public.files f ON fd.f_id  = f.f_id " +
                                                           "LEFT JOIN public.categories c ON f.ctg_id  = c.ctg_id " +
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
                                int? averagingRange = (reader[8] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[8]);
                                int? recordsNum = (reader[9] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[9]);

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
                                    AveragingRange = averagingRange,
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
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

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
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Очистить временную таблицу temp_measurements в БД
        /// </summary>
        internal static async void TruncateTempTableAsync()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    // Очистить временную таблицу temp_measurements
                    using (var cmd = new NpgsqlCommand("TRUNCATE public.temp_measurements",
                                                        connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    };
                }
            }
        }

        /// <summary>
        /// Запрос в БД какие есть подстанции, трансформаторы и фазы в таблице measurements в БД
        /// по которым можно сделать выгрузку данных
        /// </summary>
        internal static async Task<List<DataModel.Node>> GetSubstTransPhaseAsync(DateTime startDate, DateTime endDate)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    List<DataModel.Node> substationsList = new List<DataModel.Node>();
                    var substationsMap = new Dictionary<int, DataModel.Node>();
                    var transformersMap = new Dictionary<int, DataModel.Node>();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = new NpgsqlCommand("SELECT DISTINCT s.sbst_id, s.sbst_name, m.trans_id, t.trans_name, m.phase, m.avg_range " +
                                                           "FROM public.measurements m " +
                                                           "LEFT JOIN public.transformers t ON m.trans_id = t.trans_id " +
                                                           "LEFT JOIN public.substations s ON t.sbst_id = s.sbst_id " +
                                                           "WHERE m.datetime >= @start_date AND m.datetime <= @end_date " +
                                                           "ORDER BY s.sbst_id",
                                                           connection))
                        {
                            cmd.Parameters.AddWithValue("@start_date", startDate);
                            cmd.Parameters.AddWithValue("@end_date", endDate);
                            await cmd.PrepareAsync();
                            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                int substationId = Convert.ToInt32(reader[0]);
                                string substationName = Convert.ToString(reader[1]);
                                int transformerId = Convert.ToInt32(reader[2]);
                                string transformertName = Convert.ToString(reader[3]);
                                string phaseName = Convert.ToString(reader[4]);
                                int avgRange = Convert.ToInt32(reader[5]);

                                if (!substationsMap.ContainsKey(substationId))
                                {
                                    var tempSubstation = new DataModel.Node()
                                    {
                                        Name = substationName,
                                        Id = substationId,
                                        Parent = null
                                    };

                                    substationsMap[substationId] = tempSubstation;
                                    substationsList.Add(tempSubstation);
                                }

                                if (!transformersMap.ContainsKey(transformerId))
                                {
                                    var tempTransformer = new DataModel.Node()
                                    {
                                        Name = transformertName,
                                        Id = transformerId,
                                        Parent = substationsMap[substationId]
                                    };

                                    transformersMap[transformerId] = tempTransformer;
                                    substationsMap[substationId].Childs.Add(transformersMap[transformerId]);
                                }

                                if (transformersMap[transformerId].Childs.Count == 0)
                                {
                                    var tempPhase = new DataModel.Node()
                                    {
                                        Name = phaseName,
                                        Id = null,
                                        Parent = transformersMap[transformerId]
                                    };

                                    transformersMap[transformerId].Childs.Add(tempPhase);

                                    var tempAvgRange = new DataModel.Node()
                                    {
                                        //Name = string.Format("Интервал усреднения: {0} мин.", avgRange),
                                        Id = avgRange,
                                        Parent = tempPhase
                                    };

                                    if (avgRange != 0) { tempAvgRange.Name = string.Format("Интервал усреднения: {0} мин.", avgRange); }
                                    else { tempAvgRange.Name = string.Format("Интервал усреднения: {0} мин. (без усреднения)", avgRange); }

                                    tempPhase.Childs.Add(tempAvgRange);
                                }
                                else
                                {
                                    bool parentPhaseIsFound = false;
                                    foreach (DataModel.Node child in transformersMap[transformerId].Childs)
                                    {
                                        if (child.Name == phaseName)
                                        {
                                            var tempAvgRange = new DataModel.Node()
                                            {
                                                //Name = string.Format("Интервал усреднения: {0} мин.", avgRange),
                                                Id = avgRange,
                                                Parent = child
                                            };

                                            if (avgRange != 0) { tempAvgRange.Name = string.Format("Интервал усреднения: {0} мин.", avgRange); }
                                            else { tempAvgRange.Name = string.Format("Интервал усреднения: {0} мин. (без усреднения)", avgRange); }

                                            child.Childs.Add(tempAvgRange);
                                            parentPhaseIsFound = true;
                                        }
                                    }
                                    if (!parentPhaseIsFound)
                                    {
                                        var tempPhase = new DataModel.Node()
                                        {
                                            Name = phaseName,
                                            Id = null,
                                            Parent = transformersMap[transformerId]
                                        };

                                        transformersMap[transformerId].Childs.Add(tempPhase);

                                        var tempAvgRange = new DataModel.Node()
                                        {
                                            //Name = string.Format("Интервал усреднения: {0} мин.", avgRange),
                                            Id = avgRange,
                                            Parent = tempPhase
                                        };

                                        if (avgRange != 0) { tempAvgRange.Name = string.Format("Интервал усреднения: {0} мин.", avgRange); }
                                        else { tempAvgRange.Name = string.Format("Интервал усреднения: {0} мин. (без усреднения)", avgRange); }

                                        tempPhase.Childs.Add(tempAvgRange);
                                    }
                                }
                            }
                            return substationsList;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Запрос в БД какие есть доступные поля в таблице measurements в БД,
        /// по которым можно сделать выгрузку данных при выбранных подстанции, трансформатора
        /// и фазы
        /// </summary>
        internal static async Task<List<DataModel.AvailibleFieldsInfo>> GetAvailibleFieldsInfoAsync(int substationId, int transformerId, string phase, int avgRange)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    List<DataModel.AvailibleFieldsInfo> availibleFieldsInfoList = new List<DataModel.AvailibleFieldsInfo>();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = new NpgsqlCommand("SELECT DISTINCT m.field_id, fd.field_name, fl.f_name, c.ctg_name " +
                                                           "FROM public.measurements m " +
                                                           "LEFT JOIN public.fields fd ON m.field_id = fd.field_id " +
                                                           "LEFT JOIN public.transformers t ON m.trans_id = t.trans_id " +
                                                           "LEFT JOIN public.substations s ON t.sbst_id  = s.sbst_id " +
                                                           "LEFT JOIN public.files fl ON fd.f_id = fl.f_id " +
                                                           "LEFT JOIN public.categories c ON fl.ctg_id = c.ctg_id " +
                                                           "WHERE s.sbst_id = @sbst_id AND m.trans_id = @trans_id AND m.phase = @phase AND m.avg_range = @avg_range " +
                                                           "ORDER BY m.field_id",
                                                           connection))
                        {
                            cmd.Parameters.AddWithValue("@sbst_id", substationId);
                            cmd.Parameters.AddWithValue("@trans_id", transformerId);
                            cmd.Parameters.AddWithValue("@phase", phase);
                            cmd.Parameters.AddWithValue("@avg_range", avgRange);
                            await cmd.PrepareAsync();
                            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                int fieldId = Convert.ToInt32(reader[0]);
                                string fieldName = Convert.ToString(reader[1]);
                                string fileName = Convert.ToString(reader[2]);
                                string categoryName = Convert.ToString(reader[3]);

                                var tempFieldInfo = new DataModel.AvailibleFieldsInfo()
                                {
                                    FieldId = fieldId,
                                    FieldName = fieldName,
                                    FileName = fileName,
                                    CategoryName = categoryName,
                                };

                                availibleFieldsInfoList.Add(tempFieldInfo);
                            }
                            return availibleFieldsInfoList;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Запрос в БД, чтобы получить min и max значения поля datetime в таблице measurements в БД
        /// </summary>
        internal static async Task<List<DateTime>> GetMinMaxDates()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    List<DateTime> minMaxDatesList = new List<DateTime>();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = new NpgsqlCommand("(SELECT datetime FROM public.measurements ORDER BY datetime ASC LIMIT 1) " +
                                                           "UNION ALL " +
                                                           "(SELECT datetime FROM public.measurements ORDER BY datetime DESC LIMIT 1)",
                                                           connection))
                        {
                            await cmd.PrepareAsync();
                            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                DateTime tempDate = Convert.ToDateTime(reader[0]);
                                minMaxDatesList.Add(tempDate);
                            }
                            return minMaxDatesList;
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

                    return null;
                }
            }
        }

        /// <summary>
        /// Запрос в БД, чтобы получить выбранные пользователм на вкладке "Экспорт данных" данные.
        /// </summary>
        internal static List<DataModel.ExportedMeasurement> GetDataForExport(DateTime startDate, DateTime endDate, List<DataModel.AvailibleFieldsInfo> availibleFieldsInfoList, int averagingRange)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                if (connection.State == System.Data.ConnectionState.Open)
                {

                    var exportedMeasurementsList = new List<DataModel.ExportedMeasurement>();
                    var sqlCmd = new StringBuilder();
                    var fieldsExpression = new StringBuilder();

                    foreach (DataModel.AvailibleFieldsInfo afi in availibleFieldsInfoList)
                    {
                        if (afi.ValueIsChecked || afi.ValueMinIsChecked || afi.ValueMaxIsChecked)
                        {
                            fieldsExpression.AppendFormat("m.field_id = {0} OR ", afi.FieldId);
                        }
                    }
                    sqlCmd.AppendFormat("SELECT m.datetime, field_id, m.value, m.value_min, m.value_max " +
                                        "FROM public.measurements m " +
                                        "WHERE (m.datetime >= @start_date AND m.datetime <= @end_date) AND (m.avg_range = @avg_range) AND ({0}) " +
                                        "ORDER BY m.datetime, m.field_id ASC", fieldsExpression.ToString().Substring(0, fieldsExpression.ToString().Length - 4));

                    using (var cmd = new NpgsqlCommand(sqlCmd.ToString(),
                                                       connection))
                    {
                        cmd.Parameters.AddWithValue("@start_date", startDate);
                        cmd.Parameters.AddWithValue("@end_date", endDate);
                        cmd.Parameters.AddWithValue("@avg_range", averagingRange);
                        cmd.Prepare();
                        NpgsqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            DateTime dateTime = Convert.ToDateTime(reader[0]);
                            int fieldId = Convert.ToInt32(reader[1]);
                            float value = (float)reader[2];
                            float? valueMin = (reader[3] == DBNull.Value) ? null : (float?)reader[3];
                            float? valueMax = (reader[4] == DBNull.Value) ? null : (float?)reader[4];

                            var tempExportedMeasurement = new DataModel.ExportedMeasurement()
                            {
                                datetime = dateTime,
                                field_id = fieldId,
                                value = value,
                                value_min = valueMin,
                                value_max = valueMax
                            };

                            exportedMeasurementsList.Add(tempExportedMeasurement);
                        }
                        return exportedMeasurementsList;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Сохранение в БД информации о выбранных для экспорта полях.
        /// </summary>
        internal static int? SaveSelectedFieldsPreset(List<DataModel.AvailibleFieldsInfo> availibleFieldsInfoList, string presetName)
        {
            NpgsqlTransaction transaction = null;
            int savedPresetId;

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    transaction = connection.BeginTransaction();

                    // Вставить в БД имя пресета полей, выбранных ждя экспорта 
                    using (var cmd = new NpgsqlCommand("INSERT INTO public.preset_names (preset_name, datetime) " +
                                                       "VALUES (@presetName, @saveDatetime) " +
                                                       "RETURNING preset_id",
                                                       connection))
                    {
                        cmd.Parameters.AddWithValue("@presetName", presetName);
                        cmd.Parameters.AddWithValue("@saveDatetime", DateTime.Now);

                        savedPresetId = Convert.ToInt32(cmd.ExecuteScalar());
                    };

                    using (var cmd = new NpgsqlCommand("INSERT INTO public.fields_presets (preset_id, field_id, checked_value, checked_value_min, checked_value_max, value_order, value_min_order, value_max_order) " +
                                                       "VALUES (@presetId, @fieldId, @checkedValue, @checkedValueMin, @checkedValueMax, @valueOrder, @valueMinOrder, @valueMaxOrder)",
                                                       connection))
                    {
                        cmd.Parameters.Add("@presetId", NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@fieldId", NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@checkedValue", NpgsqlDbType.Boolean);
                        cmd.Parameters.Add("@checkedValueMin", NpgsqlDbType.Boolean);
                        cmd.Parameters.Add("@checkedValueMax", NpgsqlDbType.Boolean);
                        cmd.Parameters.Add("@valueOrder", NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@valueMinOrder", NpgsqlDbType.Integer);
                        cmd.Parameters.Add("@valueMaxOrder", NpgsqlDbType.Integer);

                        cmd.Prepare();

                        int updatedRows = 0;
                        foreach (DataModel.AvailibleFieldsInfo afi in availibleFieldsInfoList)
                        {
                            if (afi.ValueIsChecked || afi.ValueMinIsChecked || afi.ValueMaxIsChecked)
                            {
                                cmd.Parameters["@presetId"].Value = savedPresetId;
                                cmd.Parameters["@fieldId"].Value = afi.FieldId;
                                cmd.Parameters["@checkedValue"].Value = afi.ValueIsChecked;
                                cmd.Parameters["@checkedValueMin"].Value = afi.ValueMinIsChecked;
                                cmd.Parameters["@checkedValueMax"].Value = afi.ValueMaxIsChecked;

                                if (afi.ValueIsCheckedOrder == null) { cmd.Parameters["@valueOrder"].Value = DBNull.Value; }
                                else { cmd.Parameters["@valueOrder"].Value = afi.ValueIsCheckedOrder; }

                                if (afi.ValueMinIsCheckedOrder == null) { cmd.Parameters["@valueMinOrder"].Value = DBNull.Value; }
                                else { cmd.Parameters["@valueMinOrder"].Value = afi.ValueMinIsCheckedOrder; }

                                if (afi.ValueMaxIsCheckedOrder == null) { cmd.Parameters["@valueMaxOrder"].Value = DBNull.Value; }
                                else { cmd.Parameters["@valueMaxOrder"].Value = afi.ValueMaxIsCheckedOrder; }

                                updatedRows += Convert.ToInt32(cmd.ExecuteNonQuery());
                            }
                        }
                        transaction.Commit();
                        return updatedRows;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Запрос в БД какие есть названия сохранённых пресетов.
        /// </summary>
        internal static async Task<ObservableCollection<DataModel.PresetInfo>> GetFieldsPresetNamesAsync()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    var presetNames = new ObservableCollection<DataModel.PresetInfo>();

                    using (var cmd = new NpgsqlCommand("SELECT * FROM public.preset_names " +
                                                       "ORDER BY datetime DESC",
                                                       connection))
                    {
                        NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            var tempPresetInfo = new DataModel.PresetInfo()
                            {
                                Name = Convert.ToString(reader[1]),
                                Id = Convert.ToInt32(reader[0]),
                            };
                            presetNames.Add(tempPresetInfo);
                        }
                        return presetNames;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        internal static List<DataModel.AvailibleFieldsInfo> GetFieldsPresetContent(int presetId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    var retrievedFieldsInfo = new List<DataModel.AvailibleFieldsInfo>();

                    using (var cmd = new NpgsqlCommand("SELECT field_id, checked_value, checked_value_min, checked_value_max, value_order, value_min_order, value_max_order " +
                                                       "FROM public.fields_presets " +
                                                       "WHERE preset_id = @preset_id",
                                                       connection))
                    {
                        cmd.Parameters.AddWithValue("@preset_id", presetId);
                        NpgsqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            var tempFieldInfo = new DataModel.AvailibleFieldsInfo()
                            {
                                FieldId = Convert.ToInt32(reader[0]),
                                ValueIsChecked = Convert.ToBoolean(reader[1]),
                                ValueMinIsChecked = Convert.ToBoolean(reader[2]),
                                ValueMaxIsChecked = Convert.ToBoolean(reader[3]),
                                ValueIsCheckedOrder = (reader[4] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[4]),
                                ValueMinIsCheckedOrder = (reader[5] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[5]),
                                ValueMaxIsCheckedOrder = (reader[6] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader[6]),
                            };
                            retrievedFieldsInfo.Add(tempFieldInfo);
                        }
                        return retrievedFieldsInfo;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Удаление сохранённого пресета из БД
        /// </summary>
        internal static async Task<int?> DeleteSavedPresetAsync(DataModel.PresetInfo PresetInfo)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    var connectionTask = connection.OpenAsync();
                    await connectionTask;

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = new NpgsqlCommand("DELETE FROM public.preset_names " +
                                                           "WHERE preset_id=@preset_id",
                                                            connection))
                        {
                            cmd.Parameters.AddWithValue("@preset_id", PresetInfo.Id);
                            int updatedRows = await cmd.ExecuteNonQueryAsync();
                            return updatedRows;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = string.Format("{0}\n{1}", ex.Message, ex.InnerException);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);

                    return null;
                }
            }
        }
    }
}