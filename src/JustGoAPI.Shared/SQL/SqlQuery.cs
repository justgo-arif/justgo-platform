using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JustGoAPI.Shared.SQL
{
    public class SqlQuery
    {
        public string AttendanceStatusTable()
        {
            return @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AttendanceStatus')
                BEGIN
                    -- Create the table if it does not exist
                    CREATE TABLE AttendanceStatus (
                        Id INT PRIMARY KEY IDENTITY(1,1),  -- Auto-incrementing primary key
                        Name NVARCHAR(100) NOT NULL        -- Name column with a maximum length of 100 characters
                    );
    
                    -- Insert data into the newly created table
                    INSERT INTO AttendanceStatus (Name) VALUES
                    ('Checked In'), ('Away'), ('Injured'), ('Not Showed'),('Pending');
                END
                ELSE
                BEGIN
                    -- Alter the table to add columns if they don't exist
                    -- Add the Id column if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Id' AND object_id = OBJECT_ID('AttendanceStatus'))
                    BEGIN
                        ALTER TABLE AttendanceStatus ADD Id INT PRIMARY KEY IDENTITY(1,1);
                    END

                    -- Add the Name column if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Name' AND object_id = OBJECT_ID('AttendanceStatus'))
                    BEGIN
                        ALTER TABLE AttendanceStatus ADD Name NVARCHAR(100) NOT NULL;
                    END

                    -- Insert data into the existing table
                    IF NOT EXISTS (SELECT * FROM AttendanceStatus WHERE Name IN ('Checked In', 'Away', 'Injured', 'Not Showed','Pending'))
                    BEGIN
                        INSERT INTO AttendanceStatus (Name) VALUES
                        ('Checked In'), ('Away'), ('Injured'), ('Not Showed'),('Pending');
                    END
                END";
        }

        public string EventDateTable() {

            return @"if not exists(select * from sys.tables where [name] = 'EventRecurringScheduleOccurrenceDate')
            begin
            CREATE TABLE [dbo].[EventRecurringScheduleOccurrenceDate](
                [RowId] [int] IDENTITY(1,1) NOT NULL,
                [ScheduleId] [int] NULL,
                [EventDocId] [int] NULL,
                [EntityTypeId] [int] NULL,
                [OwnerId] [int] NULL,
                [DayOfWeek] [varchar](50) NULL,
                [OccurrenceDate] [datetime] NULL,
                [ScheduleDateWithDay] [varchar](50) NULL,
            PRIMARY KEY CLUSTERED 
            (
                [RowId] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
            ) ON [PRIMARY]
            end";
        
        }

        public string EventAttendancesTableAlter()
        {
            return @"IF EXISTS (SELECT 1 
           FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'EventAttendances' 
             AND COLUMN_NAME = 'CheckedInAt' 
             AND TABLE_SCHEMA = 'dbo') 
            BEGIN
                PRINT 'Column exists. Skipping ALTER TABLE.'
            END
            ELSE
            BEGIN
                ALTER TABLE EventAttendances
            ADD CheckedInAt Datetime DEFAULT null;
            END";
        }

        public string JustGoAppGlobalSettings()
        {
            return @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GlobalSettings]') AND type in (N'U'))
                DROP TABLE [dbo].[GlobalSettings]
                GO

                /****** Object:  Table [dbo].[GlobalSettings]    Script Date: 13/03/2025 13:58:08 ******/
                SET ANSI_NULLS ON
                GO

                SET QUOTED_IDENTIFIER ON
                GO

                CREATE TABLE [dbo].[GlobalSettings](
	                [Id] [int] IDENTITY(1,1) NOT NULL,
	                [ItemKey] [nvarchar](50) NOT NULL,
	                [Value] [nvarchar](max) NULL,
	                [IsEncrypted] [bit] NOT NULL,
                 CONSTRAINT [PK_GlobalSettings] PRIMARY KEY CLUSTERED 
                (
	                [Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
                ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
                GO";
        }
        

    }
}
