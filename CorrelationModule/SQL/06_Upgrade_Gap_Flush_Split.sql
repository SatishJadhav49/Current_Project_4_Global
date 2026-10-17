/* =====================================================================
   Correlation Module - Phase 1
   06_Upgrade_Gap_Flush_Split.sql
   Database    : PQ_Dashboard_Master
   Re-runnable : yes

   WHY : In 1D BIW / 1D TCF a location can be measured for BOTH Gap and
         Flush. These are now separate rows that are mapped separately, so a
         mapping is identified by Source_Leaf_ID + Source_Leaf_Type.

   RUN ON A DATABASE THAT ALREADY HAS 01-05 INSTALLED, IN THIS ORDER:
     1. this script (06)
     2. 03_SPs_Mapping.sql       (re-run - updated SPs)
     3. 04_SPs_Master_Data.sql   (re-run - 1D SPs now return Gap / Flush rows)
   (A fresh install only needs 01-05; they already contain this change.)

   Existing 1D mappings are converted automatically:
     - location with Gap only     -> type Gap
     - location with Flush only   -> type Flush
     - location with Gap + Flush  -> type Gap + an extra Flush mapping row
       (the location was mapped as a whole, so both measures stay mapped)
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. New column -------------------------------------------------------- */
IF COL_LENGTH(N'dbo.MM_CR_Parameter_Mapping', N'Source_Leaf_Type') IS NULL
    ALTER TABLE dbo.MM_CR_Parameter_Mapping ADD Source_Leaf_Type NVARCHAR(20) NULL;
GO

/* 2. Rebuild indexes to include the type ------------------------------- */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping')
      AND i.name = N'UX_MM_CR_Mapping_Active'
      AND c.name = N'Source_Leaf_Type'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_MM_CR_Mapping_Active' AND object_id = OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping'))
        DROP INDEX UX_MM_CR_Mapping_Active ON dbo.MM_CR_Parameter_Mapping;

    CREATE UNIQUE NONCLUSTERED INDEX UX_MM_CR_Mapping_Active
        ON dbo.MM_CR_Parameter_Mapping (CR_Parameter_ID, App_ID, Source_Leaf_ID, Source_Leaf_Type)
        WHERE Is_Active = 1;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping')
      AND i.name = N'IX_MM_CR_Mapping_App_Leaf'
      AND c.name = N'Source_Leaf_Type'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MM_CR_Mapping_App_Leaf' AND object_id = OBJECT_ID(N'dbo.MM_CR_Parameter_Mapping'))
        DROP INDEX IX_MM_CR_Mapping_App_Leaf ON dbo.MM_CR_Parameter_Mapping;

    CREATE NONCLUSTERED INDEX IX_MM_CR_Mapping_App_Leaf
        ON dbo.MM_CR_Parameter_Mapping (App_ID, Source_Leaf_ID, Source_Leaf_Type)
        INCLUDE (CR_Parameter_ID)
        WHERE Is_Active = 1;
END
GO

/* 3. Convert existing 1D mappings (type still NULL) -------------------- */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @OneD TABLE (App_ID INT PRIMARY KEY);
INSERT INTO @OneD (App_ID)
SELECT App_ID FROM dbo.MM_CR_Application WHERE App_Code IN ('1D_BIW', '1D_TCF');

-- 3a. Gap + Flush locations: add the Flush mapping row
INSERT INTO dbo.MM_CR_Parameter_Mapping
    (CR_Parameter_ID, App_ID, Source_Model_ID, Source_Leaf_ID, Source_Leaf_Type, Source_Display_Text,
     Is_Active, Inserted_User_ID, Inserted_Host, Inserted_Date)
SELECT m.CR_Parameter_ID, m.App_ID, m.Source_Model_ID, m.Source_Leaf_ID, N'Flush',
       LEFT(CONCAT(m.Source_Display_Text, N' (Flush)'), 1000),
       1, m.Inserted_User_ID, m.Inserted_Host, m.Inserted_Date
FROM dbo.MM_CR_Parameter_Mapping m
JOIN @OneD a ON a.App_ID = m.App_ID
JOIN [1D_Module].dbo.MM_LocationMaster loc ON loc.Location_ID = m.Source_Leaf_ID
WHERE m.Is_Active = 1
  AND m.Source_Leaf_Type IS NULL
  AND loc.Is_Gap = 1
  AND loc.Is_Flushness = 1
  AND NOT EXISTS (
        SELECT 1 FROM dbo.MM_CR_Parameter_Mapping x
        WHERE x.CR_Parameter_ID = m.CR_Parameter_ID
          AND x.App_ID = m.App_ID
          AND x.Source_Leaf_ID = m.Source_Leaf_ID
          AND x.Source_Leaf_Type = N'Flush'
          AND x.Is_Active = 1);

-- 3b. Set the type on the original row (Gap first, else Flush)
UPDATE m
SET Source_Leaf_Type    = t.Leaf_Type,
    Source_Display_Text = LEFT(CONCAT(m.Source_Display_Text, N' (', t.Leaf_Type, N')'), 1000)
FROM dbo.MM_CR_Parameter_Mapping m
JOIN @OneD a ON a.App_ID = m.App_ID
JOIN [1D_Module].dbo.MM_LocationMaster loc ON loc.Location_ID = m.Source_Leaf_ID
CROSS APPLY (
    SELECT CASE WHEN loc.Is_Gap = 1 THEN N'Gap'
                WHEN loc.Is_Flushness = 1 THEN N'Flush' END AS Leaf_Type
) t
WHERE m.Is_Active = 1
  AND m.Source_Leaf_Type IS NULL
  AND t.Leaf_Type IS NOT NULL
  AND NOT EXISTS (
        SELECT 1 FROM dbo.MM_CR_Parameter_Mapping x
        WHERE x.CR_Parameter_ID = m.CR_Parameter_ID
          AND x.App_ID = m.App_ID
          AND x.Source_Leaf_ID = m.Source_Leaf_ID
          AND x.Source_Leaf_Type = t.Leaf_Type
          AND x.Is_Active = 1);

COMMIT TRANSACTION;
GO

/* 4. Recreate the table type with Source_Leaf_Type --------------------- */
-- A table type cannot be altered: drop the SP that uses it, then the type.
-- SP_CR_Save_Mappings is recreated by re-running 03_SPs_Mapping.sql.
IF TYPE_ID(N'dbo.MM_CR_LeafList') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.table_types tt
        JOIN sys.columns c ON c.object_id = tt.type_table_object_id
        WHERE tt.name = N'MM_CR_LeafList' AND c.name = N'Source_Leaf_Type')
BEGIN
    IF OBJECT_ID(N'dbo.SP_CR_Save_Mappings', N'P') IS NOT NULL
        DROP PROCEDURE dbo.SP_CR_Save_Mappings;

    DROP TYPE dbo.MM_CR_LeafList;
END
GO

IF TYPE_ID(N'dbo.MM_CR_LeafList') IS NULL
    CREATE TYPE dbo.MM_CR_LeafList AS TABLE
    (
        Source_Leaf_ID      DECIMAL(18,0)  NOT NULL,
        Source_Leaf_Type    NVARCHAR(20)   NULL,
        Source_Model_ID     DECIMAL(18,0)  NULL,
        Source_Display_Text NVARCHAR(1000) NULL
    );
GO

PRINT 'Upgrade done. Now re-run 03_SPs_Mapping.sql and 04_SPs_Master_Data.sql.';
GO
