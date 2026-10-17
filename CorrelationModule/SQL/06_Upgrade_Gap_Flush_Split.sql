/* =====================================================================
   Correlation Module - Phase 1
   06_Upgrade_Gap_Flush_Split.sql
   Database    : PQ_Dashboard_Master
   Re-runnable : yes

   WHY : In 1D BIW / 1D TCF a location can be measured for BOTH Gap and
         Flush. These are now separate rows that are mapped separately, so a
         mapping is identified by Source_Leaf_ID + Source_Leaf_Type.

   RUN ON A DATABASE THAT ALREADY HAS 01-05 INSTALLED - this one script
   is self-contained (column, indexes, data conversion, table type AND all
   affected stored procedures). Execute the WHOLE file (F5, nothing selected).
   A verification result is shown at the end - every row must say OK.
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
-- SP_CR_Save_Mappings is recreated in step 5 below.
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

/* 5. Recreate the stored procedures affected by the change ------------
      (identical to the versions in 03_SPs_Mapping.sql / 04_SPs_Master_Data.sql) */

CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Parameter_Mappings
    @CR_Parameter_ID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.CR_Parameter_ID,
           p.Model_ID,
           mo.Model_Name,
           p.Parameter_Name
    FROM dbo.MM_CR_Parameter p
    LEFT JOIN dbo.MM_Model mo ON mo.Model_ID = p.Model_ID
    WHERE p.CR_Parameter_ID = @CR_Parameter_ID AND p.Is_Active = 1;

    SELECT m.Mapping_ID,
           m.CR_Parameter_ID,
           m.App_ID,
           a.App_Name,
           a.Leaf_Level_Name,
           m.Source_Model_ID,
           m.Source_Leaf_ID,
           m.Source_Leaf_Type,
           m.Source_Display_Text,
           m.Inserted_Date,
           e.Employee_Name AS Mapped_By
    FROM dbo.MM_CR_Parameter_Mapping m
    JOIN dbo.MM_CR_Application a ON a.App_ID = m.App_ID
    LEFT JOIN dbo.MM_Employee e  ON e.Employee_ID = m.Inserted_User_ID
    WHERE m.CR_Parameter_ID = @CR_Parameter_ID
      AND m.Is_Active = 1
      AND a.Is_Active = 1
    ORDER BY a.Sort_Order, m.Source_Display_Text;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Leaf_Mappings
    @App_ID   INT,
    @Model_ID DECIMAL(18,0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT m.Source_Leaf_ID,
           m.Source_Leaf_Type,
           m.CR_Parameter_ID,
           p.Parameter_Name
    FROM dbo.MM_CR_Parameter_Mapping m
    JOIN dbo.MM_CR_Parameter p ON p.CR_Parameter_ID = m.CR_Parameter_ID
    WHERE m.App_ID = @App_ID
      AND p.Model_ID = @Model_ID
      AND m.Is_Active = 1
      AND p.Is_Active = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_CR_Save_Mappings
    @CR_Parameter_ID INT,
    @App_ID          INT,
    @Leaves          dbo.MM_CR_LeafList READONLY,
    @User_ID         DECIMAL(18,0) = NULL,
    @Host            NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Parameter WHERE CR_Parameter_ID = @CR_Parameter_ID AND Is_Active = 1)
    BEGIN
        SELECT 3 AS Result_Code, N'Parameter not found.' AS Message, 0 AS Inserted_Count, 0 AS Skipped_Count;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Application WHERE App_ID = @App_ID AND Is_Active = 1)
    BEGIN
        SELECT 3 AS Result_Code, N'Application not found or inactive.' AS Message, 0 AS Inserted_Count, 0 AS Skipped_Count;
        RETURN;
    END

    DECLARE @Requested INT = (
        SELECT COUNT(1) FROM (SELECT DISTINCT Source_Leaf_ID, NULLIF(LTRIM(RTRIM(Source_Leaf_Type)), N'') AS T FROM @Leaves) d
    );

    IF @Requested = 0
    BEGIN
        SELECT 0 AS Result_Code, N'Select at least one row to map.' AS Message, 0 AS Inserted_Count, 0 AS Skipped_Count;
        RETURN;
    END

    BEGIN TRANSACTION;

    INSERT INTO dbo.MM_CR_Parameter_Mapping
        (CR_Parameter_ID, App_ID, Source_Model_ID, Source_Leaf_ID, Source_Leaf_Type, Source_Display_Text, Is_Active, Inserted_User_ID, Inserted_Host, Inserted_Date)
    SELECT @CR_Parameter_ID, @App_ID, l.Source_Model_ID, l.Source_Leaf_ID, l.Source_Leaf_Type, l.Source_Display_Text, 1, @User_ID, @Host, GETDATE()
    FROM (
        SELECT Source_Leaf_ID,
               NULLIF(LTRIM(RTRIM(Source_Leaf_Type)), N'') AS Source_Leaf_Type,
               MAX(Source_Model_ID)     AS Source_Model_ID,
               MAX(Source_Display_Text) AS Source_Display_Text
        FROM @Leaves
        GROUP BY Source_Leaf_ID, NULLIF(LTRIM(RTRIM(Source_Leaf_Type)), N'')
    ) l
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.MM_CR_Parameter_Mapping x WITH (UPDLOCK, HOLDLOCK)
        WHERE x.CR_Parameter_ID = @CR_Parameter_ID
          AND x.App_ID = @App_ID
          AND x.Source_Leaf_ID = l.Source_Leaf_ID
          AND ISNULL(x.Source_Leaf_Type, N'') = ISNULL(l.Source_Leaf_Type, N'')
          AND x.Is_Active = 1
    );

    DECLARE @Inserted INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT 1 AS Result_Code,
           CASE WHEN @Inserted = @Requested
                THEN CAST(@Inserted AS NVARCHAR(10)) + N' item(s) mapped successfully.'
                ELSE CAST(@Inserted AS NVARCHAR(10)) + N' item(s) mapped, '
                     + CAST(@Requested - @Inserted AS NVARCHAR(10)) + N' already mapped and skipped.'
           END AS Message,
           @Inserted AS Inserted_Count,
           @Requested - @Inserted AS Skipped_Count;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_1D_BIW
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT loc.Location_ID   AS Leaf_ID,
           mt.Measure_Type   AS Leaf_Type,
           loc.Location_Name AS Leaf_Name,
           loc.Model_ID      AS Source_Model_ID,
           area.Area_Name    AS Area_Name,
           part.Part_Name    AS Part_Name,
           che.Checkpoint_Name,
           loc.Location_Name,
           mt.Measure_Type,
           CONCAT(area.Area_Name, ' > ', part.Part_Name, ' > ', che.Checkpoint_Name, ' > ', loc.Location_Name,
                  CASE WHEN mt.Measure_Type IS NOT NULL THEN CONCAT(' (', mt.Measure_Type, ')') ELSE '' END) AS Display_Text
    FROM [1D_Module].dbo.MM_LocationMaster loc
    JOIN [1D_Module].dbo.MM_CheckpointMaster che ON loc.Checkpoint_ID = che.Checkpoint_ID
    JOIN [1D_Module].dbo.MM_PartMaster part      ON che.Part_ID = part.Part_ID
    JOIN [1D_Module].dbo.MM_AreaMaster area      ON part.Area_ID = area.Area_ID
    -- One row per measure: a location with both Gap and Flush gives two rows
    CROSS APPLY (
        SELECT CAST(N'Gap' AS NVARCHAR(20)) AS Measure_Type, 1 AS Measure_Order WHERE loc.Is_Gap = 1
        UNION ALL
        SELECT N'Flush', 2 WHERE loc.Is_Flushness = 1
        UNION ALL
        SELECT NULL, 3 WHERE ISNULL(loc.Is_Gap, 0) = 0 AND ISNULL(loc.Is_Flushness, 0) = 0
    ) mt
    WHERE loc.Audit_Type_Id = 2
      AND loc.Is_Active = 1 AND che.Is_Active = 1 AND part.Is_Active = 1 AND area.Is_Active = 1
      AND loc.Model_ID IN (
            SELECT mo.Model_ID FROM [1D_Module].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY loc.SORTORDER, che.SORTORDER, part.SORTORDER, area.SORTORDER, mt.Measure_Order;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_1D_TCF
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT loc.Location_ID   AS Leaf_ID,
           mt.Measure_Type   AS Leaf_Type,
           loc.Location_Name AS Leaf_Name,
           loc.Model_ID      AS Source_Model_ID,
           area.Area_Name    AS Area_Name,
           part.Part_Name    AS Part_Name,
           che.Checkpoint_Name,
           loc.Location_Name,
           mt.Measure_Type,
           CONCAT(area.Area_Name, ' > ', part.Part_Name, ' > ', che.Checkpoint_Name, ' > ', loc.Location_Name,
                  CASE WHEN mt.Measure_Type IS NOT NULL THEN CONCAT(' (', mt.Measure_Type, ')') ELSE '' END) AS Display_Text
    FROM [1D_Module].dbo.MM_LocationMaster loc
    JOIN [1D_Module].dbo.MM_CheckpointMaster che ON loc.Checkpoint_ID = che.Checkpoint_ID
    JOIN [1D_Module].dbo.MM_PartMaster part      ON che.Part_ID = part.Part_ID
    JOIN [1D_Module].dbo.MM_AreaMaster area      ON part.Area_ID = area.Area_ID
    -- One row per measure: a location with both Gap and Flush gives two rows
    CROSS APPLY (
        SELECT CAST(N'Gap' AS NVARCHAR(20)) AS Measure_Type, 1 AS Measure_Order WHERE loc.Is_Gap = 1
        UNION ALL
        SELECT N'Flush', 2 WHERE loc.Is_Flushness = 1
        UNION ALL
        SELECT NULL, 3 WHERE ISNULL(loc.Is_Gap, 0) = 0 AND ISNULL(loc.Is_Flushness, 0) = 0
    ) mt
    WHERE loc.Audit_Type_Id = 1
      AND loc.Is_Active = 1 AND che.Is_Active = 1 AND part.Is_Active = 1 AND area.Is_Active = 1
      AND loc.Model_ID IN (
            SELECT mo.Model_ID FROM [1D_Module].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY loc.SORTORDER, che.SORTORDER, part.SORTORDER, area.SORTORDER, mt.Measure_Order;
END
GO

/* 6. Verification - every row must show OK ---------------------------- */
SELECT Check_Item,
       CASE WHEN Is_Ok = 1 THEN 'OK' ELSE 'MISSING / OLD - re-run this script and check errors' END AS Status
FROM (
    SELECT 'Column MM_CR_Parameter_Mapping.Source_Leaf_Type' AS Check_Item,
           CASE WHEN COL_LENGTH(N'dbo.MM_CR_Parameter_Mapping', N'Source_Leaf_Type') IS NOT NULL THEN 1 ELSE 0 END AS Is_Ok
    UNION ALL
    SELECT 'Type MM_CR_LeafList has Source_Leaf_Type',
           CASE WHEN EXISTS (SELECT 1 FROM sys.table_types tt JOIN sys.columns c ON c.object_id = tt.type_table_object_id
                             WHERE tt.name = N'MM_CR_LeafList' AND c.name = N'Source_Leaf_Type') THEN 1 ELSE 0 END
    UNION ALL
    SELECT 'Index UX_MM_CR_Mapping_Active includes type',
           CASE WHEN EXISTS (SELECT 1 FROM sys.indexes i
                             JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                             JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                             WHERE i.name = N'UX_MM_CR_Mapping_Active' AND c.name = N'Source_Leaf_Type') THEN 1 ELSE 0 END
    UNION ALL
    SELECT 'SP ' + p.Name + ' uses Leaf type',
           CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + p.Name)) LIKE '%Leaf_Type%' THEN 1 ELSE 0 END
    FROM (VALUES (N'SP_CR_Get_Parameter_Mappings'), (N'SP_CR_Get_Leaf_Mappings'), (N'SP_CR_Save_Mappings'),
                 (N'SP_CR_Get_Master_1D_BIW'), (N'SP_CR_Get_Master_1D_TCF')) p(Name)
    UNION ALL
    SELECT 'Master SP exists: ' + a.Master_SP_Name,
           CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Master_SP_Name), N'P') IS NOT NULL THEN 1 ELSE 0 END
    FROM dbo.MM_CR_Application a
    WHERE a.Is_Active = 1
) v;
GO
