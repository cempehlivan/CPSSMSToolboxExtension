using CPSSMSToolboxExtension.Helpers;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Data;

namespace CPSSMSToolboxExtension
{
    public static class Creator
    {
        public static string CSharpClass(INodeInformation table)
        {
            string table_name = table.InvariantName;

            string Sql = @"declare @TableName sysname = '" + table_name + @"'
declare @Result varchar(max) = 'public class ' + @TableName + '
{
'

select @Result = @Result + '    public ' + ColumnType + NullableSign + ' ' + ColumnName + ' { get; set; }
'
from
(
    select
        replace(col.name, ' ', '_') ColumnName,
        column_id ColumnId,
        case typ.name
            when 'bigint' then 'long'
            when 'binary' then 'byte[]'
            when 'bit' then 'bool'
            when 'char' then 'string'
            when 'date' then 'DateTime'
            when 'datetime' then 'DateTime'
            when 'datetime2' then 'DateTime'
            when 'datetimeoffset' then 'DateTimeOffset'
            when 'decimal' then 'decimal'
            when 'float' then 'float'
            when 'image' then 'byte[]'
            when 'int' then 'int'
            when 'money' then 'decimal'
            when 'nchar' then 'string'
            when 'ntext' then 'string'
            when 'numeric' then 'decimal'
            when 'nvarchar' then 'string'
            when 'real' then 'double'
            when 'smalldatetime' then 'DateTime'
            when 'smallint' then 'short'
            when 'smallmoney' then 'decimal'
            when 'text' then 'string'
            when 'time' then 'TimeSpan'
            when 'timestamp' then 'DateTime'
            when 'tinyint' then 'byte'
            when 'uniqueidentifier' then 'Guid'
            when 'varbinary' then 'byte[]'
            when 'varchar' then 'string'
            else 'UNKNOWN_' + typ.name
        end ColumnType,
        case 
            when col.is_nullable = 1 and typ.name in ('bigint', 'bit', 'date', 'datetime', 'datetime2', 'datetimeoffset', 'decimal', 'float', 'int', 'money', 'numeric', 'real', 'smalldatetime', 'smallint', 'smallmoney', 'time', 'tinyint', 'uniqueidentifier')
            then '?' 
            else '' 
        end NullableSign
    from sys.columns col
        join sys.types typ on
            col.system_type_id = typ.system_type_id AND col.user_type_id = typ.user_type_id
    where object_id = object_id(@TableName)
) t
order by ColumnId

set @Result = @Result + '}'

print @Result;
SELECT @Result
";

            string _class = Executor.GetSqlScalar(table, Sql).ToString();


            //if (!_class.cpIsNullOrEmpty())
            //{
            //    _class = _class.Substring(0, _class.Length - 1);

            //    string method = ClassSaveMethod(table);

            //    _class +=
            //          Environment.NewLine + "    public long saveChanges()"
            //        + Environment.NewLine + "    {"
            //        + Environment.NewLine + method
            //        + Environment.NewLine + "        return rs;"
            //        + Environment.NewLine + "    }"
            //        + Environment.NewLine + "}";
            //}


            return _class;
        }

        public static string CreateSelectSP(INodeInformation table)
        {
            string[] _splt = table.InvariantName.Split('.');
            string scheme_name = _splt[0];
            string table_name = _splt[1];

            DataTable dt = Executor.GetDataTable(table, $@"SELECT
                                    c.is_identity
                                   ,Col.*
                                   ,Tab.CONSTRAINT_NAME
                                FROM INFORMATION_SCHEMA.COLUMNS Col WITH(NOLOCK)
                                    LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Tab WITH(NOLOCK) ON Col.COLUMN_NAME = Tab.COLUMN_NAME AND Col.TABLE_NAME = Tab.TABLE_NAME AND Tab.TABLE_SCHEMA = Col.TABLE_SCHEMA
                                    LEFT JOIN sys.columns c WITH(NOLOCK) ON c.[name] = Col.COLUMN_NAME AND c.[object_id] = OBJECT_ID('{table.InvariantName}')
                                WHERE Col.TABLE_SCHEMA = '{scheme_name}' AND Col.TABLE_NAME = '{table_name}'");

            string str = "CREATE PROCEDURE [" + scheme_name + "].[SP_Select_" + table_name + "] ( ";

            for (int num = 0; num < dt.Rows.Count; num++)
            {
                object obj2;
                obj2 = str;
                str = string.Concat(new object[] { obj2, Environment.NewLine, "\t", (num != 0) ? ", " : "", "@", dt.Rows[num]["COLUMN_NAME"].ToString(), " ", dt.Rows[num]["DATA_TYPE"], (dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() == "-1" ? "(MAX)" : dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() != "" ? "(" + dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() + ")" : ""), " = ", "NULL" });

            }

            str +=
                 Environment.NewLine + ")"
                + Environment.NewLine + "AS"
                + Environment.NewLine + "BEGIN"
                + Environment.NewLine;

            str += Environment.NewLine
                + "SELECT * FROM [" + scheme_name + "].[" + table_name + "] WHERE "
                + Environment.NewLine;


            for (int num = 0; num < dt.Rows.Count; num++)
            {

                str += dt.Rows[num]["COLUMN_NAME"].ToString() + " = isnull(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + ", " + dt.Rows[num]["COLUMN_NAME"].ToString() + ")";

                if (num != (dt.Rows.Count - 1))
                {
                    str += Environment.NewLine + "AND ";
                }
            }


            str += Environment.NewLine +
                    Environment.NewLine + "END" + Environment.NewLine + "GO";


            return str;
        }

        public static string CreateInsertUpdateSP(INodeInformation table)
        {
            string scheme_name = table.InvariantName.Split('.')[0];
            string table_name = table.InvariantName.Split('.')[1];

            int num;
            DataTable dt = Executor.GetDataTable(table, $@"SELECT
                                    c.is_identity
                                   ,Col.*
                                   ,Tab.CONSTRAINT_NAME
                                FROM INFORMATION_SCHEMA.COLUMNS Col WITH(NOLOCK)
                                    LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Tab WITH(NOLOCK) ON Col.COLUMN_NAME = Tab.COLUMN_NAME AND Col.TABLE_NAME = Tab.TABLE_NAME AND Tab.TABLE_SCHEMA = Col.TABLE_SCHEMA
                                    LEFT JOIN sys.columns c WITH(NOLOCK) ON c.[name] = Col.COLUMN_NAME AND c.[object_id] = OBJECT_ID('{table.InvariantName}')
                                WHERE Col.TABLE_SCHEMA = '{scheme_name}' AND Col.TABLE_NAME = '{table_name}'");

            string str = "CREATE PROCEDURE [" + scheme_name + "].[SP_Save_" + table_name + "] (";
            for (num = 0; num < dt.Rows.Count; num++)
            {
                object obj2;

                obj2 = str;
                str = string.Concat(new object[] { obj2, Environment.NewLine, "\t", (num != 0) ? ", " : "  ", "@", dt.Rows[num]["COLUMN_NAME"].ToString(), " ", dt.Rows[num]["DATA_TYPE"].ToString().ToUpperInvariant(), (dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() == "-1" ? "(MAX)" : dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() != "" ? "(" + dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() + ")" : ""), " = ", "NULL" });

            }
            string str2 = str;
            str2 = (str2 + Environment.NewLine + ") " + Environment.NewLine + "AS" + Environment.NewLine + "BEGIN" + Environment.NewLine + Environment.NewLine + "\tSET NOCOUNT ON;" + Environment.NewLine + "\tSET XACT_ABORT ON;") + Environment.NewLine + "\tDECLARE @rec BIGINT;";

            str2 = str2 + Environment.NewLine + Environment.NewLine + "\tIF EXISTS(SELECT 1 FROM [" + scheme_name + "].[" + table_name + "] WITH(NOLOCK) WHERE ";

            bool flag2 = false;
            for (num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    if (flag2)
                    {
                        str2 += " AND " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                    }
                    else
                    {
                        str2 += " " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                        flag2 = true;
                    }
                }
            }


            str2 += ") " + Environment.NewLine + "\tBEGIN" + Environment.NewLine + "\t";
            str = ((str2 + Environment.NewLine + "\t\tUPDATE [" + scheme_name + "].[" + table_name + "]") + Environment.NewLine + "\t\tSET" + Environment.NewLine);
            for (num = 0; num < dt.Rows.Count; num++)
            {
                if (((!dt.Rows[num]["COLUMN_DEFAULT"].cpToBool()) && dt.Rows[num]["CONSTRAINT_NAME"].ToString() == ""))
                {
                    str2 = str;
                    str = str2 + "\t\t\t[" + dt.Rows[num]["COLUMN_NAME"].ToString() + "] = ISNULL(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + ",[" + dt.Rows[num]["COLUMN_NAME"].ToString() + "])"
                        + Environment.NewLine + ",";
                }
            }
            str = str.Trim(',');
            str = str + Environment.NewLine + "\t\tWHERE";
            bool flag = false;
            for (num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    if (flag)
                    {
                        str2 = str;
                        str = str2 + Environment.NewLine + "\t\t\tAND " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                    }
                    else
                    {
                        str2 = str;
                        str = str2 + Environment.NewLine + "\t\t\t" + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                        flag = true;
                    }
                }
            }
            str2 = (((str + Environment.NewLine + Environment.NewLine + "\t\tSELECT @rec = @@ROWCOUNT;") + Environment.NewLine + "") + Environment.NewLine + "\tEND" + Environment.NewLine + "\tELSE" + Environment.NewLine + "\tBEGIN") + Environment.NewLine;
            str2 = str2 + Environment.NewLine + "\t\tINSERT INTO [" + scheme_name + "].[" + table_name + "]";
            str = str2 + Environment.NewLine + "\t\t(" + Environment.NewLine + "\t\t\t [" + dt.Rows[0]["COLUMN_NAME"].ToString() + "]";
            for (num = 1; num < dt.Rows.Count; num++)
            {
                str2 = str;
                str = str2 + Environment.NewLine + "\t\t\t,[" + dt.Rows[num]["COLUMN_NAME"].ToString() + "]";
            }
            str = ((str + Environment.NewLine + "\t\t)") + Environment.NewLine + "\t\tVALUES") + Environment.NewLine + "\t\t(" + Environment.NewLine + "\t\t\t @" + dt.Rows[0]["COLUMN_NAME"].ToString();
            for (num = 1; num < dt.Rows.Count; num++)
            {
                if ((dt.Rows[num]["COLUMN_DEFAULT"].ToString()) == "")
                {
                    str = str + Environment.NewLine + "\t\t\t,@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                }
                else
                {
                    str2 = str;
                    str = str2 + Environment.NewLine + "\t\t\t,ISNULL(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + "," + dt.Rows[num]["COLUMN_DEFAULT"].ToString().RepairDefaultValue() + ")";
                }
            }
            str = (((((((str + Environment.NewLine + "\t\t)") + Environment.NewLine + Environment.NewLine + "\t\tSELECT @rec = @@ROWCOUNT;") + Environment.NewLine + "\t\t--SELECT @rec = SCOPE_IDENTITY();" + Environment.NewLine + "\tEND;") + Environment.NewLine + Environment.NewLine + "\tIF (@rec>0)") + Environment.NewLine + "\t\tSELECT @rec") + Environment.NewLine + "\tELSE") + Environment.NewLine + "\t\tSELECT NULL;") + Environment.NewLine + "END" + Environment.NewLine + "GO";
            return str;
        }

        public static string CreateSQLtableLog(INodeInformation table)
        {
            string scheme_name = table.InvariantName.Split('.')[0];
            string table_name = table.InvariantName.Split('.')[1];
            string log_table = "log_" + table_name;


            string sql = $@"
IF EXISTS(
	SELECT 1 FROM sys.tables t WITH(NOLOCK) 
	INNER JOIN sys.schemas s WITH(NOLOCK) ON s.schema_id = t.schema_id
	WHERE s.[name] = '{scheme_name}' and t.[name] = '{log_table}'
)
BEGIN
	DROP TABLE {scheme_name}.{log_table};
END

SELECT TOP(1) * INTO {scheme_name}.{log_table} FROM {scheme_name}.{table_name} ORDER BY 1;
GO
TRUNCATE TABLE {scheme_name}.{log_table};
GO
ALTER TABLE {scheme_name}.{log_table}
ADD
    log_type VARCHAR(2)
   ,log_hostname VARCHAR(20)
   ,log_program_name VARCHAR(100)
   ,log_runtime DATETIME NOT NULL DEFAULT (GETDATE ())
   ,log_login_name VARCHAR(100)
   ,log_sql NVARCHAR(MAX)
   ,log_id BIGINT NOT NULL IDENTITY(1, 1)
GO


CREATE TRIGGER {log_table}_insert
ON {scheme_name}.{table_name}
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

	DECLARE @EventSource TABLE (EventType NVARCHAR(30) NULL, [Parameters] INT NULL, EventInfo NVARCHAR(MAX) NULL);
	INSERT INTO @EventSource
	EXEC ('dbcc inputbuffer (' + @@spid + ') with no_infomsgs');


	INSERT INTO {scheme_name}.{log_table}
	SELECT
		i.*
	   ,'I'            AS log_type
	   ,p.hostname     AS log_host_name
	   ,p.program_name AS log_program_name
	   ,GETDATE ()     AS log_runtime
	   ,p.loginame     AS log_login_name
	   ,d.EventInfo    AS log_sql
	FROM inserted i
		,sys.sysprocesses p
		,@EventSource d
	WHERE p.spid = @@spid;

END;

GO

CREATE TRIGGER {log_table}_delete
ON {scheme_name}.{table_name}
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

	DECLARE @EventSource TABLE (EventType NVARCHAR(30) NULL, [Parameters] INT NULL, EventInfo NVARCHAR(MAX) NULL);
	INSERT INTO @EventSource
	EXEC ('dbcc inputbuffer (' + @@spid + ') with no_infomsgs');

    INSERT INTO {scheme_name}.{log_table}
    SELECT
        i.*
       ,'D'            AS log_type
       ,p.hostname     AS log_host_name
       ,p.program_name AS log_program_name
       ,GETDATE ()     AS log_runtime
	   ,p.loginame     AS log_login_name
	   ,d.EventInfo    AS log_sql
    FROM deleted i
        ,sys.sysprocesses p
		,@EventSource d
    WHERE p.spid = @@spid;

END;


GO

CREATE TRIGGER {log_table}_update
ON {scheme_name}.{table_name}
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

	DECLARE @EventSource TABLE (EventType NVARCHAR(30) NULL, [Parameters] INT NULL, EventInfo NVARCHAR(MAX) NULL);
	INSERT INTO @EventSource
	EXEC ('dbcc inputbuffer (' + @@spid + ') with no_infomsgs');

    INSERT INTO {scheme_name}.{log_table}
    SELECT
        i.*
       ,'UD'           AS log_type
       ,p.hostname     AS log_host_name
       ,p.program_name AS log_program_name
       ,GETDATE ()     AS log_runtime
       ,p.loginame     AS log_login_name
	   ,d.EventInfo    AS log_sql
    FROM deleted i
        ,sys.sysprocesses p
		,@EventSource d
    WHERE p.spid = @@spid;


    INSERT INTO {scheme_name}.{log_table}
    SELECT
        i.*
       ,'UI'           AS log_type
       ,p.hostname     AS log_host_name
       ,p.program_name AS log_program_name
       ,GETDATE ()     AS log_runtime
	   ,p.loginame     AS log_login_name
	   ,d.EventInfo    AS log_sql
    FROM inserted i
        ,sys.sysprocesses p
		,@EventSource d
    WHERE p.spid = @@spid;

END
GO
            ";

            return sql;
        }

        public static string CreateWhoIsActiveSP(INodeInformation table)
        {
            string sql = "";

            System.IO.FileInfo a = new System.IO.FileInfo(GetAssemblyLocalPathFrom(typeof(CPSSMSToolboxExtensionCommand)));

            if (a.Exists)
            {
                string filepath = a.DirectoryName + "\\Resources\\who_is_active.sql";

                if (System.IO.File.Exists(filepath))
                    sql = System.IO.File.ReadAllText(filepath);
            }


            return sql;
        }



        public static string CreateTableSP_SET(INodeInformation table)
        {
            string scheme_name = table.InvariantName.Split('.')[0];
            string table_name = table.InvariantName.Split('.')[1];

            string key = "";

            int num;
            DataTable dt = Executor.GetDataTable(table, $@"SELECT
                                    c.is_identity
                                   ,Col.*
                                   ,Tab.CONSTRAINT_NAME
                                FROM INFORMATION_SCHEMA.COLUMNS Col WITH(NOLOCK)
                                    LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Tab WITH(NOLOCK) ON Col.COLUMN_NAME = Tab.COLUMN_NAME AND Col.TABLE_NAME = Tab.TABLE_NAME AND Tab.TABLE_SCHEMA = Col.TABLE_SCHEMA
                                    LEFT JOIN sys.columns c WITH(NOLOCK) ON c.[name] = Col.COLUMN_NAME AND c.[object_id] = OBJECT_ID('{table.InvariantName}')
                                WHERE Col.TABLE_SCHEMA = '{scheme_name}' AND Col.TABLE_NAME = '{table_name}'");

            string str = "CREATE PROCEDURE [" + scheme_name + "].[sp" + table_name + "SET] (";
            for (num = 0; num < dt.Rows.Count; num++)
            {
                object obj2;

                obj2 = str;
                str = string.Concat(new object[] { obj2, Environment.NewLine, "\t", (num != 0) ? ", " : "  ", "@", dt.Rows[num]["COLUMN_NAME"].ToString(), " ", dt.Rows[num]["DATA_TYPE"].ToString().ToUpperInvariant(), (dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() == "-1" ? "(MAX)" : dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() != "" ? "(" + dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() + ")" : ""), " = ", "NULL" });

            }
            string str2 = str;
            str2 = (str2 + Environment.NewLine + ") " + Environment.NewLine + "AS" + Environment.NewLine + "BEGIN" + Environment.NewLine + Environment.NewLine + "\tSET NOCOUNT ON;" + Environment.NewLine + "\tSET XACT_ABORT ON;") + Environment.NewLine + Environment.NewLine + "\tDECLARE" + Environment.NewLine + "\t\t @recordResult INT=0" + Environment.NewLine + "\t\t,@recordID BIGINT=0;" + Environment.NewLine + Environment.NewLine;

            str2 += "\tBEGIN TRY";

            str2 = str2 + Environment.NewLine + Environment.NewLine + "\t\tIF EXISTS(SELECT 1 FROM [" + scheme_name + "].[" + table_name + "] WITH(NOLOCK) WHERE ";

            bool flag2 = false;
            for (num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    if (flag2)
                    {
                        str2 += " AND " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                        key = "";
                    }
                    else
                    {
                        str2 += " " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                        flag2 = true;

                        key = dt.Rows[num]["COLUMN_NAME"].ToString();
                    }
                }
            }


            str2 += ") " + Environment.NewLine + "\t\tBEGIN" + Environment.NewLine + "\t";
            str = ((str2 + Environment.NewLine + "\t\t\tUPDATE [" + scheme_name + "].[" + table_name + "] SET ") + Environment.NewLine);
            for (num = 0; num < dt.Rows.Count; num++)
            {
                if (((!dt.Rows[num]["COLUMN_DEFAULT"].cpToBool()) && dt.Rows[num]["CONSTRAINT_NAME"].ToString() == ""))
                {
                    str2 = str;
                    str = str2 + "\t\t\t\t[" + dt.Rows[num]["COLUMN_NAME"].ToString() + "] = ISNULL(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + ",[" + dt.Rows[num]["COLUMN_NAME"].ToString() + "])"
                        + Environment.NewLine + ",";
                }
            }
            str = str.Trim(',');
            str = str + "\t\t\tWHERE";
            bool flag = false;
            for (num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    if (flag)
                    {
                        str2 = str;
                        str = str2 + Environment.NewLine + "\t\t\t\tAND " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                    }
                    else
                    {
                        str2 = str;
                        str = str2 + Environment.NewLine + "\t\t\t\t" + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                        flag = true;
                    }
                }
            }
            str2 = str
                + Environment.NewLine
                + Environment.NewLine + "\t\t\tSELECT"
                + Environment.NewLine + "\t\t\t\t @recordResult=@@ROWCOUNT"
                + Environment.NewLine + "\t\t\t\t,@recordID=" + (key.cpIsNullOrEmpty() ? "KEY_GELECEK;" : "@" + key + ";")
                + Environment.NewLine
                + Environment.NewLine + "\t\tEND"
                + Environment.NewLine + "\t\tELSE"
                + Environment.NewLine + "\t\tBEGIN"
                + Environment.NewLine
                + Environment.NewLine + "\t\t\tINSERT INTO [" + scheme_name + "].[" + table_name + "]";


            str = str2 + Environment.NewLine + "\t\t\t\t(" + Environment.NewLine + "\t\t\t\t [" + dt.Rows[0]["COLUMN_NAME"].ToString() + "]";
            for (num = 1; num < dt.Rows.Count; num++)
            {
                str2 = str;
                str = str2 + Environment.NewLine + "\t\t\t\t,[" + dt.Rows[num]["COLUMN_NAME"].ToString() + "]";
            }
            str = ((str + Environment.NewLine + "\t\t\t\t)") + Environment.NewLine + "\t\t\tVALUES") + Environment.NewLine + "\t\t\t\t(" + Environment.NewLine + "\t\t\t\t @" + dt.Rows[0]["COLUMN_NAME"].ToString();
            for (num = 1; num < dt.Rows.Count; num++)
            {
                if ((dt.Rows[num]["COLUMN_DEFAULT"].ToString()) == "")
                {
                    str = str + Environment.NewLine + "\t\t\t\t,@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                }
                else
                {
                    str2 = str;
                    str = str2 + Environment.NewLine + "\t\t\t\t,ISNULL(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + "," + dt.Rows[num]["COLUMN_DEFAULT"].ToString().RepairDefaultValue() + ")";
                }
            }
            str = str
                + Environment.NewLine + "\t\t\t\t)"
                + Environment.NewLine
                + Environment.NewLine + "\t\t\tSELECT"
                + Environment.NewLine + "\t\t\t @recordResult=@@ROWCOUNT"
                + Environment.NewLine + "\t\t\t,@recordID=SCOPE_IDENTITY();"
                + Environment.NewLine + "\t\tEND"
                + Environment.NewLine
                + Environment.NewLine + "\t\tIF @recordResult>0"
                + Environment.NewLine + "\t\tBEGIN"
                + Environment.NewLine + "\t\t\tSELECT"
                + Environment.NewLine + "\t\t\t\t t.*"
                + Environment.NewLine + "\t\t\t\t,@recordResult AS recordResult"
                + Environment.NewLine + "\t\t\t\t,'Kayıt işlemi başarılı.' AS recordMessage"
                + Environment.NewLine + "\t\t\tFROM [" + scheme_name + "].[" + table_name + "] t WITH(NOLOCK)"
                + Environment.NewLine + "\t\t\tWHERE"
                + Environment.NewLine;

            if (!key.cpIsNullOrEmpty())
                str += "\t\t\t\t" + key + "= @recordID";
            else
            {
                flag = false;
                for (num = 0; num < dt.Rows.Count; num++)
                {
                    if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                    {
                        if (flag)
                            str += Environment.NewLine + "\t\t\t\tAND " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                        else
                        {
                            str += Environment.NewLine + "\t\t\t\t\t " + dt.Rows[num]["COLUMN_NAME"].ToString() + "=@" + dt.Rows[num]["COLUMN_NAME"].ToString();
                            flag = true;
                        }
                    }
                }
            }

            str = str
                + Environment.NewLine
                + Environment.NewLine + "\t\tEND"
                + Environment.NewLine + "\t\tELSE"
                + Environment.NewLine + "\t\tBEGIN"
                + Environment.NewLine + "\t\t\tSELECT";

            for (num = 0; num < dt.Rows.Count; num++)
            {
                str += Environment.NewLine + "\t\t\t\t" + (num == 0 ? " " : ",") + "@" + dt.Rows[num]["COLUMN_NAME"].ToString() + " AS " + dt.Rows[num]["COLUMN_NAME"].ToString();
            }

            str = str
                + Environment.NewLine + "\t\t\t\t,@recordResult AS recordResult"
                + Environment.NewLine + "\t\t\t\t,'Kayıt işlemi başarısız.' AS recordMessage"
                + Environment.NewLine + "\t\tEND"
                + Environment.NewLine
                + Environment.NewLine + "\tEND TRY"
                + Environment.NewLine + "\tBEGIN CATCH"
                + Environment.NewLine + "\t\tSELECT";

            for (num = 0; num < dt.Rows.Count; num++)
            {
                str += Environment.NewLine + "\t\t\t" + (num == 0 ? " " : ",") + "@" + dt.Rows[num]["COLUMN_NAME"].ToString() + " AS " + dt.Rows[num]["COLUMN_NAME"].ToString();
            }

            str = str
                + Environment.NewLine + "\t\t\t,-1 AS recordResult"
                + Environment.NewLine + "\t\t\t,ERROR_MESSAGE() AS recordMessage"
                + Environment.NewLine + "\tEND CATCH"
                + Environment.NewLine + "END"
                + Environment.NewLine + "GO";
            return str;
        }

        public static string CreateTableSP_GET(INodeInformation table)
        {
            string[] _splt = table.InvariantName.Split('.');
            string scheme_name = _splt[0];
            string table_name = _splt[1];

            DataTable dt = Executor.GetDataTable(table, $@"SELECT
                                    c.is_identity
                                   ,Col.*
                                   ,Tab.CONSTRAINT_NAME
                                FROM INFORMATION_SCHEMA.COLUMNS Col WITH(NOLOCK)
                                    LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Tab WITH(NOLOCK) ON Col.COLUMN_NAME = Tab.COLUMN_NAME AND Col.TABLE_NAME = Tab.TABLE_NAME AND Tab.TABLE_SCHEMA = Col.TABLE_SCHEMA
                                    LEFT JOIN sys.columns c WITH(NOLOCK) ON c.[name] = Col.COLUMN_NAME AND c.[object_id] = OBJECT_ID('{table.InvariantName}')
                                WHERE Col.TABLE_SCHEMA = '{scheme_name}' AND Col.TABLE_NAME = '{table_name}'");

            string str = "CREATE PROCEDURE [" + scheme_name + "].[sp" + table_name + "GET] ( ";

            for (int num = 0; num < dt.Rows.Count; num++)
            {
                object obj2;
                obj2 = str;
                str = string.Concat(new object[] { obj2, Environment.NewLine, "\t", (num != 0) ? ", " : "  ", "@", dt.Rows[num]["COLUMN_NAME"].ToString(), " ", dt.Rows[num]["DATA_TYPE"].ToString().ToUpperInvariant(), (dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() == "-1" ? "(MAX)" : dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() != "" ? "(" + dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() + ")" : ""), " = ", "NULL" });
                //str = string.Concat(new object[] { obj2, Environment.NewLine, "\t", (num != 0) ? ", " : "", "@", dt.Rows[num]["COLUMN_NAME"].ToString(), " ", dt.Rows[num]["DATA_TYPE"], (dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() == "-1" ? "(MAX)" : dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() != "" ? "(" + dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() + ")" : ""), " = ", "NULL" });

            }

            str +=
                 Environment.NewLine + ")"
                + Environment.NewLine + "AS"
                + Environment.NewLine + "BEGIN"
                + Environment.NewLine
                + Environment.NewLine + "\tSELECT"
                + Environment.NewLine + "\t\t*"
                + Environment.NewLine + "\tFROM [" + scheme_name + "].[" + table_name + "] WITH(NOLOCK)"
                + Environment.NewLine + "\tWHERE"
                + Environment.NewLine;


            for (int num = 0; num < dt.Rows.Count; num++)
            {
                string isnull = dt.Rows[num]["COLUMN_NAME"].ToString() + " = ISNULL(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + ", " + dt.Rows[num]["COLUMN_NAME"].ToString() + ")";

                if (num == 0)
                    str += "\t\t ";
                else
                    str += "\tAND ";

                if (dt.Rows[num]["DATA_TYPE"].cpToString().ToLower() == "varchar")
                    str += "(" + isnull + " OR 1<=CHARINDEX(@" + dt.Rows[num]["COLUMN_NAME"].ToString() + "," + dt.Rows[num]["COLUMN_NAME"].ToString() + "))";
                else
                    str += isnull;

                str += Environment.NewLine;
            }


            str += Environment.NewLine
                   + Environment.NewLine + "END"
                   + Environment.NewLine + "GO";


            return str;
        }

        public static string CreateTableSP_DEL(INodeInformation table)
        {
            string[] _splt = table.InvariantName.Split('.');
            string scheme_name = _splt[0];
            string table_name = _splt[1];

            DataTable dt = Executor.GetDataTable(table, $@"SELECT
                                    c.is_identity
                                   ,Col.*
                                   ,Tab.CONSTRAINT_NAME
                                FROM INFORMATION_SCHEMA.COLUMNS Col WITH(NOLOCK)
                                    LEFT JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Tab WITH(NOLOCK) ON Col.COLUMN_NAME = Tab.COLUMN_NAME AND Col.TABLE_NAME = Tab.TABLE_NAME AND Tab.TABLE_SCHEMA = Col.TABLE_SCHEMA
                                    LEFT JOIN sys.columns c WITH(NOLOCK) ON c.[name] = Col.COLUMN_NAME AND c.[object_id] = OBJECT_ID('{table.InvariantName}')
                                WHERE Col.TABLE_SCHEMA = '{scheme_name}' AND Col.TABLE_NAME = '{table_name}'");

            string str = "CREATE PROCEDURE [" + scheme_name + "].[sp" + table_name + "DEL] ( ";

            int index_count = 0;
            for (int num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    object obj2;
                    obj2 = str;
                    str = string.Concat(new object[] { obj2, Environment.NewLine, "\t", (index_count != 0) ? ", " : "  ", "@", dt.Rows[num]["COLUMN_NAME"].ToString(), " ", dt.Rows[num]["DATA_TYPE"].ToString().ToUpperInvariant(), (dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() == "-1" ? "(MAX)" : dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() != "" ? "(" + dt.Rows[num]["CHARACTER_MAXIMUM_LENGTH"].ToString() + ")" : ""), " = ", "NULL" });

                    index_count++;
                }
            }

            str +=
                 Environment.NewLine + ")"
                + Environment.NewLine + "AS"
                + Environment.NewLine + "BEGIN"
                + Environment.NewLine
                + Environment.NewLine + "\tSET NOCOUNT ON;"
                + Environment.NewLine + "\tSET XACT_ABORT ON;"
                + Environment.NewLine
                + Environment.NewLine + "\tDECLARE @recordResult INT=0;"
                + Environment.NewLine
                + Environment.NewLine + "\tBEGIN TRY"
                + Environment.NewLine
                + Environment.NewLine + "\t\tDELETE FROM [" + scheme_name + "].[" + table_name + "]"
                + Environment.NewLine + "\t\tWHERE"
                ;

            index_count = 0;
            for (int num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    if (index_count == 0)
                        str += Environment.NewLine + "\t\t\t ";
                    else
                        str += Environment.NewLine + "\t\tAND ";

                    str += dt.Rows[num]["COLUMN_NAME"].ToString() + " = @" + dt.Rows[num]["COLUMN_NAME"].ToString();

                    index_count++;
                }
            }

            str +=
                  Environment.NewLine
                + Environment.NewLine + "\t\tSELECT @recordResult=@@ROWCOUNT;"
                + Environment.NewLine
                + Environment.NewLine + "\t\tIF @recordResult>0"
                + Environment.NewLine + "\t\tBEGIN"
                + Environment.NewLine + "\t\t\tSELECT";

            index_count = 0;
            for (int num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    str += Environment.NewLine + "\t\t\t\t" + (index_count == 0 ? " " : ",") + "@" + dt.Rows[num]["COLUMN_NAME"].ToString() + " AS " + dt.Rows[num]["COLUMN_NAME"].ToString();
                    index_count++;
                }
            }


            str +=
                  Environment.NewLine + "\t\t\t\t,@recordResult AS recordResult"
                + Environment.NewLine + "\t\t\t\t,'Silme işlemi başarılı.' AS recordMessage"
                + Environment.NewLine + "\t\tEND"
                + Environment.NewLine + "\t\tELSE"
                + Environment.NewLine + "\t\tBEGIN"
                + Environment.NewLine + "\t\t\tSELECT";


            index_count = 0;
            for (int num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    str += Environment.NewLine + "\t\t\t\t" + (index_count == 0 ? " " : ",") + "@" + dt.Rows[num]["COLUMN_NAME"].ToString() + " AS " + dt.Rows[num]["COLUMN_NAME"].ToString();
                    index_count++;
                }
            }

            str +=
                  Environment.NewLine + "\t\t\t\t,@recordResult AS recordResult"
                + Environment.NewLine + "\t\t\t\t,'Silme işlemi başarısız. Kayıt bulunamadı.' AS recordMessage"
                + Environment.NewLine + "\t\tEND"
                + Environment.NewLine
                + Environment.NewLine + "\tEND TRY"
                + Environment.NewLine + "\tBEGIN CATCH"
                + Environment.NewLine
                + Environment.NewLine + "\t\tSELECT";

            index_count = 0;
            for (int num = 0; num < dt.Rows.Count; num++)
            {
                if (dt.Rows[num]["CONSTRAINT_NAME"].ToString() != "")
                {
                    str += Environment.NewLine + "\t\t\t" + (index_count == 0 ? " " : ",") + "@" + dt.Rows[num]["COLUMN_NAME"].ToString() + " AS " + dt.Rows[num]["COLUMN_NAME"].ToString();
                    index_count++;
                }
            }

            str +=
                  Environment.NewLine + "\t\t\t,-1 AS recordResult"
                + Environment.NewLine + "\t\t\t,ERROR_MESSAGE() AS recordMessage"
                + Environment.NewLine
                + Environment.NewLine + "\tEND CATCH"
                + Environment.NewLine
                + Environment.NewLine + "END"
                + Environment.NewLine + "GO";


            return str;
        }

        private static string GetAssemblyLocalPathFrom(Type type)
        {
            string codebase = type.Assembly.CodeBase;
            var uri = new Uri(codebase, UriKind.Absolute);
            return uri.LocalPath;
        }

        private static string RepairDefaultValue(this string ColumnDefault)
        {
            if (String.IsNullOrWhiteSpace(ColumnDefault))
                return ColumnDefault;

            string val = ColumnDefault;

            if (val.ToLower(System.Globalization.CultureInfo.GetCultureInfo("en")) == "(getdate())")
                return "GETDATE()";
            else if (val.ToLower(System.Globalization.CultureInfo.GetCultureInfo("en")) == "(newid())")
                return "NEWID()";

            return val.Replace("((", "").Replace("))", "").Replace("('", "'").Replace("')", "'");
        }
    }

}
