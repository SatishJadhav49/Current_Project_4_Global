/* =====================================================================
   Correlation Module - New application: Master Checker
   10_App_Master_Checker.sql - registry row, grid columns, master SP, reading SP
   Database    : PQ_Dashboard_Master (reads [PQ_Master_Checker] with 3-part names)
   Re-runnable : yes. Run AFTER 01-09. Execute the whole file (F5).

   No API / UI change is needed - the Master Checker tab, its grid and its
   report cards come from MM_CR_Application / MM_CR_Application_Column.

   Mapping item = Location + Side + Gap/Flush:
     Leaf_ID   = MM_Location_Master.Location_ID
     Leaf_Type = "<Side> / <Measure>", e.g. "LH / Gap", "Common / Flush"
                 (only the side, e.g. "RH", when the location has no Gap/Flush flag;
                  only the measure, e.g. "Gap", when the part has no side flag)
   Master grid: a part with Is_LH and Is_RH gives separate LH and RH rows, and a
   location with Is_Gap and Is_Flush gives separate Gap and Flush rows.
   Readings   : side  -> MM_Audit_Sheet_Details.Side (text: LH / RH / Common)
                measure -> MM_Specification_Master.Is_Gap / Is_Flush
   Images     : stored in an application server folder - not added yet
                (Image_SP_Name stays NULL, no image icons).
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. Master SP - one row per Location x Side x Gap/Flush ----------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_Master_Checker
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT loc.Location_ID    AS Leaf_ID,
           lt.Leaf_Type,
           loc.Location_Name  AS Leaf_Name,
           part.Model_ID      AS Source_Model_ID,
           part.Part_Name,
           sd.Side,
           loc.Location_Name,
           ms.Measure_Type,
           CONCAT(part.Part_Name, ' > ', loc.Location_Name,
                  CASE WHEN lt.Leaf_Type IS NOT NULL THEN CONCAT(' (', lt.Leaf_Type, ')') ELSE '' END) AS Display_Text
    FROM [PQ_Master_Checker].dbo.MM_Part_Master part
    JOIN [PQ_Master_Checker].dbo.MM_Location_Master loc ON loc.Part_ID = part.Part_ID
    -- one row per side flag (LH and RH both set -> two rows)
    CROSS APPLY (
        SELECT CAST(N'LH' AS NVARCHAR(10)) AS Side, 1 AS Side_Order WHERE part.Is_LH = 1
        UNION ALL SELECT N'RH', 2 WHERE part.Is_RH = 1
        UNION ALL SELECT N'Common', 3 WHERE part.Is_Common = 1
        UNION ALL SELECT NULL, 4
                  WHERE ISNULL(part.Is_LH, 0) = 0 AND ISNULL(part.Is_RH, 0) = 0 AND ISNULL(part.Is_Common, 0) = 0
    ) sd
    -- one row per measure flag (Gap and Flush both set -> two rows)
    CROSS APPLY (
        SELECT CAST(N'Gap' AS NVARCHAR(10)) AS Measure_Type, 1 AS Measure_Order WHERE loc.Is_Gap = 1
        UNION ALL SELECT N'Flush', 2 WHERE loc.Is_Flush = 1
        UNION ALL SELECT NULL, 3 WHERE ISNULL(loc.Is_Gap, 0) = 0 AND ISNULL(loc.Is_Flush, 0) = 0
    ) ms
    CROSS APPLY (
        SELECT CAST(CASE WHEN sd.Side IS NOT NULL AND ms.Measure_Type IS NOT NULL
                         THEN sd.Side + N' / ' + ms.Measure_Type
                         ELSE COALESCE(sd.Side, ms.Measure_Type) END AS NVARCHAR(20)) AS Leaf_Type
    ) lt
    WHERE part.Is_Active = 1
      AND loc.Is_Active = 1
      AND part.Model_ID IN (
            SELECT mo.Model_ID FROM [PQ_Master_Checker].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY part.Part_Name, loc.Location_Name, sd.Side_Order, ms.Measure_Order;
END
GO

/* 2. Reading SP - same contract as 07_Report.sql -------------------------
      The leaf type is split back into side and measure:
        "LH / Gap" -> side LH, measure Gap;  "RH" -> side only;  "Flush" -> measure only */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Readings_Master_Checker
    @Leaves    dbo.MM_CR_LeafList READONLY,
    @From_Date DATE = NULL,
    @To_Date   DATE = NULL,
    @Last_N    INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @To_Exclusive DATE = DATEADD(DAY, 1, @To_Date);

    WITH Leaves AS
    (
        SELECT DISTINCT l.Source_Leaf_ID, l.Source_Leaf_Type,
               CASE WHEN CHARINDEX(N' / ', l.Source_Leaf_Type) > 0
                    THEN LEFT(l.Source_Leaf_Type, CHARINDEX(N' / ', l.Source_Leaf_Type) - 1)
                    WHEN l.Source_Leaf_Type IN (N'Gap', N'Flush') THEN NULL
                    ELSE l.Source_Leaf_Type END AS Side,
               CASE WHEN CHARINDEX(N' / ', l.Source_Leaf_Type) > 0
                    THEN SUBSTRING(l.Source_Leaf_Type, CHARINDEX(N' / ', l.Source_Leaf_Type) + 3, 20)
                    WHEN l.Source_Leaf_Type IN (N'Gap', N'Flush') THEN l.Source_Leaf_Type
                    ELSE NULL END AS Measure
        FROM @Leaves l
    ),
    Base AS
    (
        SELECT l.Source_Leaf_ID   AS Leaf_ID,
               l.Source_Leaf_Type AS Leaf_Type,
               audit.Audit_ID,
               audit.Audit_Date,
               1                                         AS Reading_No,
               TRY_CONVERT(DECIMAL(18,4), sheet.Reading) AS Reading,
               CAST(NULL AS DECIMAL(18,4))               AS Reading1,
               CAST(NULL AS DECIMAL(18,4))               AS Reading2,
               CAST(NULL AS DECIMAL(18,4))               AS Reading3,
               TRY_CONVERT(DECIMAL(18,4), spec.LSL)      AS LSL,
               TRY_CONVERT(DECIMAL(18,4), spec.USL)      AS USL
        FROM Leaves l
        JOIN [PQ_Master_Checker].dbo.MM_Specification_Master spec
             ON spec.Location_ID = l.Source_Leaf_ID
            AND (    l.Measure IS NULL
                 OR (l.Measure = N'Gap'   AND spec.Is_Gap = 1)
                 OR (l.Measure = N'Flush' AND spec.Is_Flush = 1))
        JOIN [PQ_Master_Checker].dbo.MM_Audit_Sheet_Details sheet
             ON sheet.Spec_ID = spec.Spec_ID
            AND (l.Side IS NULL OR LTRIM(RTRIM(sheet.Side)) COLLATE DATABASE_DEFAULT = l.Side)
        JOIN [PQ_Master_Checker].dbo.MM_Audit_Master audit ON audit.Audit_ID = sheet.Audit_ID
        WHERE ISNULL(sheet.Is_NA, 0) <> 1
          AND TRY_CONVERT(DECIMAL(18,4), sheet.Reading) IS NOT NULL
          AND (@Last_N IS NOT NULL
               OR (audit.Audit_Date >= @From_Date AND audit.Audit_Date < @To_Exclusive))
    ),
    Ranked AS
    (
        -- 1 = latest audit of the leaf; one rank per audit
        SELECT *, DENSE_RANK() OVER (PARTITION BY Leaf_ID, Leaf_Type
                                     ORDER BY Audit_Date DESC, Audit_ID DESC) AS Audit_Rank
        FROM Base
    )
    SELECT Leaf_ID, Leaf_Type, Audit_Date, Reading_No, Reading, Reading1, Reading2, Reading3, LSL, USL
    FROM Ranked
    WHERE @Last_N IS NULL OR Audit_Rank <= @Last_N
    ORDER BY Leaf_ID, Leaf_Type, Audit_Date, Audit_ID
    OPTION (RECOMPILE);
END
GO

/* 3. Registry row + grid columns ----------------------------------------- */
-- New tab goes after the existing ones (change Sort_Order to move it)
IF NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Application WHERE App_Code = 'MASTER_CHECKER')
    INSERT INTO dbo.MM_CR_Application (App_Code, App_Name, Leaf_Level_Name, Master_SP_Name, Sort_Order, Is_Active, Inserted_Date)
    SELECT 'MASTER_CHECKER', N'Master Checker', N'Location', N'SP_CR_Get_Master_Master_Checker', ISNULL(MAX(Sort_Order), 0) + 1, 1, GETDATE()
    FROM dbo.MM_CR_Application;
GO

UPDATE dbo.MM_CR_Application
SET App_Name        = N'Master Checker',
    Leaf_Level_Name = N'Location',
    Master_SP_Name  = N'SP_CR_Get_Master_Master_Checker',
    Reading_SP_Name = N'SP_CR_Get_Readings_Master_Checker',
    Image_SP_Name   = NULL,          -- images live in a server folder - added later
    Is_Active       = 1,
    Updated_Date    = GETDATE()
WHERE App_Code = 'MASTER_CHECKER';
GO

DECLARE @Columns TABLE (Column_Key VARCHAR(100), Column_Header NVARCHAR(100), Sort_Order INT);
INSERT INTO @Columns VALUES
    ('Part_Name',     N'Part',        1),
    ('Side',          N'Side',        2),
    ('Location_Name', N'Location',    3),
    ('Measure_Type',  N'Gap / Flush', 4);

INSERT INTO dbo.MM_CR_Application_Column (App_ID, Column_Key, Column_Header, Sort_Order, Is_Filterable, Is_Active)
SELECT app.App_ID, c.Column_Key, c.Column_Header, c.Sort_Order, 1, 1
FROM @Columns c
CROSS JOIN dbo.MM_CR_Application app
WHERE app.App_Code = 'MASTER_CHECKER'
  AND NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Application_Column x
                  WHERE x.App_ID = app.App_ID AND x.Column_Key = c.Column_Key);
GO

/* 4. Verification - Master_SP / Reading_SP = OK, Grid_Columns = 4 -------- */
SELECT a.App_ID, a.App_Code, a.App_Name, a.Sort_Order,
       CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Master_SP_Name), N'P') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Master_SP,
       CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Reading_SP_Name), N'P') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Reading_SP,
       (SELECT COUNT(1) FROM dbo.MM_CR_Application_Column c WHERE c.App_ID = a.App_ID AND c.Is_Active = 1) AS Grid_Columns
FROM dbo.MM_CR_Application a
WHERE a.App_Code = 'MASTER_CHECKER';
GO
