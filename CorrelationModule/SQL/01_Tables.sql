/* =====================================================================
   Correlation Module - Phase 1 : Parameter Mapping Master
   01_Tables.sql  - tables, table types and indexes
   Database       : PQ_Dashboard_Master
   Re-runnable    : yes (IF NOT EXISTS guards)

   PRE-REQUISITE  : dbo.MM_Model (Model_ID, Model_Name) must exist in
                    PQ_Dashboard_Master - it drives the Model dropdown and
                    the Model_Name lookup into every source application.
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------
   1. Application registry - one row per source application.
      Adding / removing an application = insert / deactivate a row here.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MM_CR_Application', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_CR_Application
    (
        App_ID            INT IDENTITY(1,1) NOT NULL,
        App_Code          VARCHAR(30)       NOT NULL,
        App_Name          NVARCHAR(100)     NOT NULL,
        Leaf_Level_Name   NVARCHAR(50)      NOT NULL,   -- Location / Parameter
        Master_SP_Name    SYSNAME           NOT NULL,   -- SP returning master data for a model
        Sort_Order        INT               NOT NULL CONSTRAINT DF_MM_CR_Application_Sort DEFAULT (0),
        Is_Active         BIT               NOT NULL CONSTRAINT DF_MM_CR_Application_Active DEFAULT (1),
        Inserted_User_ID  DECIMAL(18,0)     NULL,
        Inserted_Host     NVARCHAR(100)     NULL,
        Inserted_Date     DATETIME          NOT NULL CONSTRAINT DF_MM_CR_Application_InsDate DEFAULT (GETDATE()),
        Updated_User_ID   DECIMAL(18,0)     NULL,
        Updated_Host      NVARCHAR(100)     NULL,
        Updated_Date      DATETIME          NULL,
        CONSTRAINT PK_MM_CR_Application PRIMARY KEY CLUSTERED (App_ID),
        CONSTRAINT UQ_MM_CR_Application_Code UNIQUE (App_Code)
    );
END
GO

/* ---------------------------------------------------------------------
   2. Grid columns per application (drives the UI table dynamically).
      Column_Key must match a column returned by the app's Master SP.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MM_CR_Application_Column', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_CR_Application_Column
    (
        App_Column_ID     INT IDENTITY(1,1) NOT NULL,
        App_ID            INT               NOT NULL,
        Column_Key        VARCHAR(100)      NOT NULL,
        Column_Header     NVARCHAR(100)     NOT NULL,
        Sort_Order        INT               NOT NULL CONSTRAINT DF_MM_CR_AppColumn_Sort DEFAULT (0),
        Is_Filterable     BIT               NOT NULL CONSTRAINT DF_MM_CR_AppColumn_Filter DEFAULT (1),
        Is_Active         BIT               NOT NULL CONSTRAINT DF_MM_CR_AppColumn_Active DEFAULT (1),
        CONSTRAINT PK_MM_CR_Application_Column PRIMARY KEY CLUSTERED (App_Column_ID),
        CONSTRAINT FK_MM_CR_AppColumn_App FOREIGN KEY (App_ID) REFERENCES dbo.MM_CR_Application (App_ID),
        CONSTRAINT UQ_MM_CR_AppColumn_Key UNIQUE (App_ID, Column_Key)
    );
END
GO

/* ---------------------------------------------------------------------
   3. Global (correlation) parameter - unique name per Model.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MM_CR_Parameter', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_CR_Parameter
    (
        CR_Parameter_ID   INT IDENTITY(1,1) NOT NULL,
        Model_ID          DECIMAL(18,0)     NOT NULL,   -- PQ_Dashboard_Master.dbo.MM_Model
        Parameter_Name    NVARCHAR(250)     NOT NULL,
        Is_Active         BIT               NOT NULL CONSTRAINT DF_MM_CR_Parameter_Active DEFAULT (1),
        Inserted_User_ID  DECIMAL(18,0)     NULL,
        Inserted_Host     NVARCHAR(100)     NULL,
        Inserted_Date     DATETIME          NOT NULL CONSTRAINT DF_MM_CR_Parameter_InsDate DEFAULT (GETDATE()),
        Updated_User_ID   DECIMAL(18,0)     NULL,
        Updated_Host      NVARCHAR(100)     NULL,
        Updated_Date      DATETIME          NULL,
        CONSTRAINT PK_MM_CR_Parameter PRIMARY KEY CLUSTERED (CR_Parameter_ID)
    );
END
GO

-- Same name allowed in different models, never twice in one model (active rows only)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_MM_CR_Parameter_Model_Name' AND object_id = OBJECT_ID(N'dbo.MM_CR_Parameter'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_MM_CR_Parameter_Model_Name
        ON dbo.MM_CR_Parameter (Model_ID, Parameter_Name)
        WHERE Is_Active = 1;
GO

/* ---------------------------------------------------------------------
   4. Mapping - global parameter <-> leaf item of a source application.
      Unmap = Is_Active 0 (history kept for tracking).
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_CR_Parameter_Mapping
    (
        Mapping_ID          INT IDENTITY(1,1) NOT NULL,
        CR_Parameter_ID     INT               NOT NULL,
        App_ID              INT               NOT NULL,
        Source_Model_ID     DECIMAL(18,0)     NULL,       -- Model_ID inside the source application DB
        Source_Leaf_ID      DECIMAL(18,0)     NOT NULL,   -- Location_ID / Parameter_ID in source DB
        Source_Display_Text NVARCHAR(1000)    NULL,       -- snapshot e.g. Area > Part > Checkpoint > Location
        Is_Active           BIT               NOT NULL CONSTRAINT DF_MM_CR_Mapping_Active DEFAULT (1),
        Inserted_User_ID    DECIMAL(18,0)     NULL,
        Inserted_Host       NVARCHAR(100)     NULL,
        Inserted_Date       DATETIME          NOT NULL CONSTRAINT DF_MM_CR_Mapping_InsDate DEFAULT (GETDATE()),
        Updated_User_ID     DECIMAL(18,0)     NULL,
        Updated_Host        NVARCHAR(100)     NULL,
        Updated_Date        DATETIME          NULL,
        CONSTRAINT PK_MM_CR_Parameter_Mapping PRIMARY KEY CLUSTERED (Mapping_ID),
        CONSTRAINT FK_MM_CR_Mapping_Parameter FOREIGN KEY (CR_Parameter_ID) REFERENCES dbo.MM_CR_Parameter (CR_Parameter_ID),
        CONSTRAINT FK_MM_CR_Mapping_App FOREIGN KEY (App_ID) REFERENCES dbo.MM_CR_Application (App_ID)
    );
END
GO

-- No duplicate active mapping of the same leaf to the same parameter.
-- (One leaf CAN be mapped to several different parameters.)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_MM_CR_Mapping_Active' AND object_id = OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_MM_CR_Mapping_Active
        ON dbo.MM_CR_Parameter_Mapping (CR_Parameter_ID, App_ID, Source_Leaf_ID)
        WHERE Is_Active = 1;
GO

-- Reverse lookup (leaf -> parameters), used for flags now and for readings in Phase 2
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MM_CR_Mapping_App_Leaf' AND object_id = OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping'))
    CREATE NONCLUSTERED INDEX IX_MM_CR_Mapping_App_Leaf
        ON dbo.MM_CR_Parameter_Mapping (App_ID, Source_Leaf_ID)
        INCLUDE (CR_Parameter_ID)
        WHERE Is_Active = 1;
GO

/* ---------------------------------------------------------------------
   5. Table types used for bulk map / unmap
   --------------------------------------------------------------------- */
IF TYPE_ID(N'dbo.MM_CR_LeafList') IS NULL
    CREATE TYPE dbo.MM_CR_LeafList AS TABLE
    (
        Source_Leaf_ID      DECIMAL(18,0)  NOT NULL,
        Source_Model_ID     DECIMAL(18,0)  NULL,
        Source_Display_Text NVARCHAR(1000) NULL
    );
GO

IF TYPE_ID(N'dbo.MM_CR_IdList') IS NULL
    CREATE TYPE dbo.MM_CR_IdList AS TABLE
    (
        ID INT NOT NULL
    );
GO
