/* =====================================================================
   Correlation Module - New application: CMM
   09_App_CMM.sql - registry row, grid columns, master SP, reading SP
   Database    : PQ_Dashboard_Master (reads [PQ_CMM] with 3-part names)
   Re-runnable : yes. Run AFTER 01-08. Execute the whole file (F5).

   No API / UI change is needed - the CMM tab, its grid and its report
   cards come from MM_CR_Application / MM_CR_Application_Column.

   CMM mapping item = Parameter + Co-ordinate:
     Leaf_ID   = MM_ParameterMaster.Parameter_ID
     Leaf_Type = co-ordinate (stored in MM_CR_Parameter_Mapping.Source_Leaf_Type,
                 the same column 1D uses for Gap / Flush)
   The spec table can hold several rows for one parameter + co-ordinate;
   the master grid shows each combination once. Readings take LSL / USL
   from the spec row joined through MM_Audit_Sheet_Details.Specification_ID.
   CMM has no Is_Active columns (parameter / area) and no Is_NA (sheet).
   No images for CMM (Image_SP_Name stays NULL - no image icons).
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. Master SP - one row per Parameter + Co-ordinate --------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_CMM
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT para.Parameter_ID    AS Leaf_ID,
           c.Coordinate         AS Leaf_Type,
           para.Parameter_Name  AS Leaf_Name,
           para.Model_ID        AS Source_Model_ID,
           para.Side,
           area.Area_Name,
           para.Parameter_Name,
           para.Point_Number,
           c.Coordinate,
           CONCAT(para.Side, ' > ', area.Area_Name, ' > ', para.Parameter_Name,
                  ' (Pt ', para.Point_Number, ') > ', c.Coordinate) AS Display_Text
    FROM [PQ_CMM].dbo.MM_ParameterMaster para
    JOIN [PQ_CMM].dbo.MM_AreaMaster area ON area.Area_ID = para.Area_ID
    JOIN (
        -- unique parameter + co-ordinate (the spec table may repeat them)
        SELECT DISTINCT spec.Parameter_ID,
               CAST(NULLIF(LTRIM(RTRIM(CAST(spec.Co_ordinate AS NVARCHAR(50)))), N'') AS NVARCHAR(20)) AS Coordinate
        FROM [PQ_CMM].dbo.MM_Parameter_SpecDetails_Master spec
        WHERE spec.Co_ordinate IS NOT NULL
    ) c ON c.Parameter_ID = para.Parameter_ID
    WHERE c.Coordinate IS NOT NULL
      AND para.Model_ID IN (
            SELECT mo.Model_ID FROM [PQ_CMM].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY para.Side, area.Area_Name, para.Parameter_Name, para.Point_Number, c.Coordinate;
END
GO

/* 2. Reading SP - Deviation per audit (same contract as 07_Report.sql) --- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Readings_CMM
    @Leaves    dbo.MM_CR_LeafList READONLY,
    @From_Date DATE = NULL,
    @To_Date   DATE = NULL,
    @Last_N    INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @To_Exclusive DATE = DATEADD(DAY, 1, @To_Date);

    WITH Base AS
    (
        SELECT l.Source_Leaf_ID   AS Leaf_ID,
               l.Source_Leaf_Type AS Leaf_Type,
               audit.Audit_ID,
               audit.Audit_Date,
               1                                           AS Reading_No,
               TRY_CONVERT(DECIMAL(18,4), sheet.Deviation) AS Reading,
               CAST(NULL AS DECIMAL(18,4))                 AS Reading1,
               CAST(NULL AS DECIMAL(18,4))                 AS Reading2,
               CAST(NULL AS DECIMAL(18,4))                 AS Reading3,
               TRY_CONVERT(DECIMAL(18,4), spec.LSL)        AS LSL,
               TRY_CONVERT(DECIMAL(18,4), spec.USL)        AS USL
        FROM (SELECT DISTINCT Source_Leaf_ID, Source_Leaf_Type FROM @Leaves) l
        JOIN [PQ_CMM].dbo.MM_Audit_Sheet_Details sheet
             ON sheet.Parameter_ID = l.Source_Leaf_ID
            AND (l.Source_Leaf_Type IS NULL
                 OR LTRIM(RTRIM(CAST(sheet.Coordinate AS NVARCHAR(50)))) COLLATE DATABASE_DEFAULT = l.Source_Leaf_Type)
        JOIN [PQ_CMM].dbo.MM_Audit_Master audit ON audit.Audit_ID = sheet.Audit_ID
        -- spec of THIS reading (the spec table can have several rows per parameter + co-ordinate)
        LEFT JOIN [PQ_CMM].dbo.MM_Parameter_SpecDetails_Master spec ON spec.Specification_ID = sheet.Specification_ID
        WHERE TRY_CONVERT(DECIMAL(18,4), sheet.Deviation) IS NOT NULL
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
IF NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Application WHERE App_Code = 'CMM')
    INSERT INTO dbo.MM_CR_Application (App_Code, App_Name, Leaf_Level_Name, Master_SP_Name, Sort_Order, Is_Active, Inserted_Date)
    SELECT 'CMM', N'CMM', N'Parameter', N'SP_CR_Get_Master_CMM', ISNULL(MAX(Sort_Order), 0) + 1, 1, GETDATE()
    FROM dbo.MM_CR_Application;
GO

UPDATE dbo.MM_CR_Application
SET App_Name        = N'CMM',
    Leaf_Level_Name = N'Parameter',
    Master_SP_Name  = N'SP_CR_Get_Master_CMM',
    Reading_SP_Name = N'SP_CR_Get_Readings_CMM',
    Image_SP_Name   = NULL,          -- no images for CMM
    Is_Active       = 1,
    Updated_Date    = GETDATE()
WHERE App_Code = 'CMM';
GO

DECLARE @Columns TABLE (Column_Key VARCHAR(100), Column_Header NVARCHAR(100), Sort_Order INT);
INSERT INTO @Columns VALUES
    ('Side',           N'Side',        1),
    ('Area_Name',      N'Area',        2),
    ('Parameter_Name', N'Parameter',   3),
    ('Point_Number',   N'Point No',    4),
    ('Coordinate',     N'Co-ordinate', 5);

INSERT INTO dbo.MM_CR_Application_Column (App_ID, Column_Key, Column_Header, Sort_Order, Is_Filterable, Is_Active)
SELECT app.App_ID, c.Column_Key, c.Column_Header, c.Sort_Order, 1, 1
FROM @Columns c
CROSS JOIN dbo.MM_CR_Application app
WHERE app.App_Code = 'CMM'
  AND NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Application_Column x
                  WHERE x.App_ID = app.App_ID AND x.Column_Key = c.Column_Key);
GO

/* 4. Verification - every row must show OK ------------------------------ */
SELECT a.App_ID, a.App_Code, a.App_Name, a.Sort_Order,
       CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Master_SP_Name), N'P') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Master_SP,
       CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Reading_SP_Name), N'P') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Reading_SP,
       (SELECT COUNT(1) FROM dbo.MM_CR_Application_Column c WHERE c.App_ID = a.App_ID AND c.Is_Active = 1) AS Grid_Columns
FROM dbo.MM_CR_Application a
WHERE a.App_Code = 'CMM';
GO
