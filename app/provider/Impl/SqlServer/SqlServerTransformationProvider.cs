#region License

//The contents of this file are subject to the Mozilla Public License
//Version 1.1 (the "License"); you may not use this file except in
//compliance with the License. You may obtain a copy of the License at
//http://www.mozilla.org/MPL/
//Software distributed under the License is distributed on an "AS IS"
//basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//License for the specific language governing rights and limitations
//under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Migrator.Framework;

namespace Migrator.Providers.SqlServer
{
    /// <summary>
    /// Migration transformations provider for Microsoft SQL Server.
    /// </summary>
    public class SqlServerTransformationProvider : TransformationProvider
    {
        public SqlServerTransformationProvider(string connectionString) : base(connectionString)
        {
            dialect = new SqlServerDialect();
            
            _connection = new SqlConnection();
            _connection.ConnectionString = _connectionString;
            _connection.Open();
        }

        public override bool ConstraintExists(string table, string name)
        {
            using (IDataReader reader =
                ExecuteQuery(string.Format("SELECT TOP 1 * FROM sysobjects WHERE id = object_id('{0}')", name)))
            {
                return reader.Read();
            }
        }

        public override void AddColumn(string table, string sqlColumn)
        {
            ExecuteNonQuery(string.Format("ALTER TABLE {0} ADD {1}", table, sqlColumn));
        }

        public override bool ColumnExists(string table, string column)
        {
            if (!TableExists(table))
                return false;

            using (IDataReader reader =
                ExecuteQuery(String.Format("SELECT TOP 1 * FROM syscolumns WHERE id=object_id('{0}') and name='{1}'",
                                           table, column)))
            {
                return reader.Read();
            }
        }

        public override bool TableExists(string table)
        {
            using (IDataReader reader =
                ExecuteQuery(String.Format("SELECT TOP 1 * FROM syscolumns WHERE id=object_id('{0}')",table)))
            {
                return reader.Read();
            }
        }

        public override Column[] GetColumns(string table)
        {
            List<Column> columns = new List<Column>();
            using (
                IDataReader reader =
                    ExecuteQuery(
                        String.Format("select COLUMN_NAME from information_schema.columns where table_name = '{0}';", table)))
            {
                while (reader.Read())
                {
                    columns.Add(new Column(reader[0].ToString(), DbType.String));
                }
            }

            return columns.ToArray();
        }

        public override void RemoveColumn(string table, string column)
        {
            DeleteColumnConstraints(table, column);
            base.RemoveColumn(table, column);
        }
        
        public override void RenameColumn(string tableName, string oldColumnName, string newColumnName)
        {
            if (ColumnExists(tableName, newColumnName))
                throw new MigrationException(String.Format("Table '{0}' has column named '{0}' already", tableName, newColumnName));
                
            if (ColumnExists(tableName, oldColumnName)) 
                ExecuteNonQuery(String.Format("EXEC sp_rename '{0}.{1}', '{2}', 'COLUMN'", tableName, oldColumnName, newColumnName));
        }

        public override void RenameTable(string oldName, string newName)
        {
            if (TableExists(newName))
                throw new MigrationException(String.Format("Table with name '{0}' already exists", newName));

            if (TableExists(oldName))
                ExecuteNonQuery(String.Format("EXEC sp_rename {0}, {1}", oldName, newName));
        }

        // Deletes all constraints linked to a column. Sql Server
        // doesn't seems to do this.
        private void DeleteColumnConstraints(string table, string column)
        {
            string sqlContrainte =
                String.Format("SELECT cont.name FROM SYSOBJECTS cont, SYSCOLUMNS col, SYSCONSTRAINTS cnt  "
                              + "WHERE cont.parent_obj = col.id AND cnt.constid = cont.id AND cnt.colid=col.colid "
                              + "AND col.name = '{1}' AND col.id = object_id('{0}')",
                              table, column);
            List<string> constraints = new List<string>();
            using (IDataReader reader = ExecuteQuery(sqlContrainte))
            {
                while (reader.Read())
                {
                    constraints.Add(reader.GetString(0));
                }
            }
            // Can't share the connection so two phase modif
            foreach (string constraint in constraints)
            {
                RemoveForeignKey(table, constraint);
            }
        }
    }
}