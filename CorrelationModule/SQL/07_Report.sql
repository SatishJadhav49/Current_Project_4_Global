/* =====================================================================
   Correlation Module - Phase 2 : Correlation Report
   07_Report.sql - readings registry, per-application reading SPs,
                   report lookup SP
   Database    : PQ_Dashboard_Master (reads [1D_Module], [PQ_EST_GD],
                 [PQ_Torque_Audit] with 3-part names)
   Re-runnable : yes. Run AFTER 01-06. Execute the whole file (F5).
                 (Re-run it after an update - all SPs are CREATE OR ALTER.)

   READING SP CONTRACT (every application must follow it):
     Input  : @Leaves    dbo.MM_CR_LeafList READONLY  (Source_Leaf_ID + Source_Leaf_Type)
              @From_Date DATE, @To_Date DATE          (both inclusive) - date mode
              @Last_N    INT                          - "last N audits" mode: when set,
                         the dates are ignored and the last N audits of EACH leaf
                         (with all their readings) are returned
     Output : Leaf_ID, Leaf_Type   - must echo the values passed in @Leaves
              Audit_Date           - x axis
              Reading_No           - 1 for single-reading apps, 1..3 for Reading1..3
              Reading              - plotted value (NULL readings are not returned)
              LSL, USL             - specification valid for that reading
   To add a new application later: create its reading SP and set
   MM_CR_Application.Reading_SP_Name - no API / UI change.
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. Registry column ---------------------------------------------------- */
IF COL_LENGTH(N'dbo.MM_CR_Application', N'Reading_SP_Name') IS NULL
    ALTER TABLE dbo.MM_CR_Application ADD Reading_SP_Name SYSNAME NULL;
GO

UPDATE dbo.MM_CR_Application SET Reading_SP_Name = N'SP_CR_Get_Readings_1D'     WHERE App_Code IN ('1D_BIW', '1D_TCF');
UPDATE dbo.MM_CR_Application SET Reading_SP_Name = N'SP_CR_Get_Readings_Torque' WHERE App_Code = 'TORQUE';
UPDATE dbo.MM_CR_Application SET Reading_SP_Name = N'SP_CR_Get_Readings_EST_GD' WHERE App_Code IN ('EST', 'GD');
GO

/* 2. 1D TCF / BIW readings ---------------------------------------------
      One reading per track sheet row. Gap / Flush leaves only get readings
      whose specification is of the same type.                             */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Readings_1D
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
               1                                         AS Reading_No,
               TRY_CONVERT(DECIMAL(18,4), sheet.Reading) AS Reading,
               TRY_CONVERT(DECIMAL(18,4), spec.MinVal)   AS LSL,
               TRY_CONVERT(DECIMAL(18,4), spec.MaxVal)   AS USL
        FROM (SELECT DISTINCT Source_Leaf_ID, Source_Leaf_Type FROM @Leaves) l
        JOIN [1D_Module].dbo.MM_Track_Sheet sheet        ON sheet.Location_ID = l.Source_Leaf_ID
        JOIN [1D_Module].dbo.MM_Vehicle_Audit audit      ON audit.Audit_ID = sheet.Audit_ID
        JOIN [1D_Module].dbo.MM_SpecificationMaster spec ON spec.Specification_ID = sheet.Specification_ID
        WHERE ISNULL(sheet.Is_NA, 0) = 0
          AND TRY_CONVERT(DECIMAL(18,4), sheet.Reading) IS NOT NULL
          AND (    l.Source_Leaf_Type IS NULL
               OR (l.Source_Leaf_Type = N'Gap'   AND spec.Is_Gap = 1)
               OR (l.Source_Leaf_Type = N'Flush' AND spec.Is_Flushness = 1))
          AND (@Last_N IS NOT NULL
               OR (audit.Audit_Date >= @From_Date AND audit.Audit_Date < @To_Exclusive))
    ),
    Ranked AS
    (
        -- 1 = latest audit of the leaf; one rank per audit (all its readings share it)
        SELECT *, DENSE_RANK() OVER (PARTITION BY Leaf_ID, Leaf_Type
                                     ORDER BY Audit_Date DESC, Audit_ID DESC) AS Audit_Rank
        FROM Base
    )
    SELECT Leaf_ID, Leaf_Type, Audit_Date, Reading_No, Reading, LSL, USL
    FROM Ranked
    WHERE @Last_N IS NULL OR Audit_Rank <= @Last_N
    ORDER BY Leaf_ID, Leaf_Type, Audit_Date, Audit_ID, Reading_No
    OPTION (RECOMPILE);
END
GO

/* 3. Torque readings - Reading1..3 returned as separate points ---------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Readings_Torque
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
               r.Reading_No,
               r.Reading,
               TRY_CONVERT(DECIMAL(18,4), para.LSL) AS LSL,
               TRY_CONVERT(DECIMAL(18,4), para.USL) AS USL
        FROM (SELECT DISTINCT Source_Leaf_ID, Source_Leaf_Type FROM @Leaves) l
        JOIN [PQ_Torque_Audit].dbo.MM_Audit_Sheet_Details sheet ON sheet.Parameter_ID = l.Source_Leaf_ID
        JOIN [PQ_Torque_Audit].dbo.MM_Audit_Master audit        ON audit.Audit_ID = sheet.Audit_ID
        JOIN [PQ_Torque_Audit].dbo.MM_ParameterMaster para      ON para.Parameter_ID = sheet.Parameter_ID
        CROSS APPLY (VALUES
            (1, TRY_CONVERT(DECIMAL(18,4), sheet.Reading1)),
            (2, TRY_CONVERT(DECIMAL(18,4), sheet.Reading2)),
            (3, TRY_CONVERT(DECIMAL(18,4), sheet.Reading3))
        ) r (Reading_No, Reading)
        WHERE ISNULL(sheet.Is_NA, 0) = 0
          AND r.Reading IS NOT NULL
          AND (@Last_N IS NOT NULL
               OR (audit.Audit_Date >= @From_Date AND audit.Audit_Date < @To_Exclusive))
    ),
    Ranked AS
    (
        -- 1 = latest audit of the leaf; one rank per audit (all its readings share it)
        SELECT *, DENSE_RANK() OVER (PARTITION BY Leaf_ID, Leaf_Type
                                     ORDER BY Audit_Date DESC, Audit_ID DESC) AS Audit_Rank
        FROM Base
    )
    SELECT Leaf_ID, Leaf_Type, Audit_Date, Reading_No, Reading, LSL, USL
    FROM Ranked
    WHERE @Last_N IS NULL OR Audit_Rank <= @Last_N
    ORDER BY Leaf_ID, Leaf_Type, Audit_Date, Audit_ID, Reading_No
    OPTION (RECOMPILE);
END
GO

/* 4. EST / GD readings - Reading1..3 returned as separate points --------
      NOTE: table name MM_Audit_Sheet_Datails is spelled as in the database. */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Readings_EST_GD
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
               r.Reading_No,
               r.Reading,
               TRY_CONVERT(DECIMAL(18,4), spec.LSL) AS LSL,
               TRY_CONVERT(DECIMAL(18,4), spec.USL) AS USL
        FROM (SELECT DISTINCT Source_Leaf_ID, Source_Leaf_Type FROM @Leaves) l
        JOIN [PQ_EST_GD].dbo.MM_Audit_Sheet_Datails sheet  ON sheet.Location_ID = l.Source_Leaf_ID
        JOIN [PQ_EST_GD].dbo.MM_Audit_Master audit         ON audit.Audit_ID = sheet.Audit_ID
        JOIN [PQ_EST_GD].dbo.MM_SpecificationDetails spec  ON spec.Specification_ID = sheet.Specification_ID
        CROSS APPLY (VALUES
            (1, TRY_CONVERT(DECIMAL(18,4), sheet.Reading1)),
            (2, TRY_CONVERT(DECIMAL(18,4), sheet.Reading2)),
            (3, TRY_CONVERT(DECIMAL(18,4), sheet.Reading3))
        ) r (Reading_No, Reading)
        WHERE ISNULL(sheet.Is_NA, 0) = 0
          AND r.Reading IS NOT NULL
          AND (@Last_N IS NOT NULL
               OR (audit.Audit_Date >= @From_Date AND audit.Audit_Date < @To_Exclusive))
    ),
    Ranked AS
    (
        -- 1 = latest audit of the leaf; one rank per audit (all its readings share it)
        SELECT *, DENSE_RANK() OVER (PARTITION BY Leaf_ID, Leaf_Type
                                     ORDER BY Audit_Date DESC, Audit_ID DESC) AS Audit_Rank
        FROM Base
    )
    SELECT Leaf_ID, Leaf_Type, Audit_Date, Reading_No, Reading, LSL, USL
    FROM Ranked
    WHERE @Last_N IS NULL OR Audit_Rank <= @Last_N
    ORDER BY Leaf_ID, Leaf_Type, Audit_Date, Audit_ID, Reading_No
    OPTION (RECOMPILE);
END
GO

/* 5. Report lookup - parameter header (RS1) + active mappings with the
      reading SP of their application (RS2). Reading_SP_Name is NULL when the
      app has none configured or the SP does not exist.                     */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Report_Mappings
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
           m.App_ID,
           a.App_Name,
           a.Leaf_Level_Name,
           a.Sort_Order,
           CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Reading_SP_Name), N'P') IS NOT NULL
                THEN a.Reading_SP_Name END AS Reading_SP_Name,
           m.Source_Leaf_ID,
           m.Source_Leaf_Type,
           m.Source_Display_Text
    FROM dbo.MM_CR_Parameter_Mapping m
    JOIN dbo.MM_CR_Application a ON a.App_ID = m.App_ID
    WHERE m.CR_Parameter_ID = @CR_Parameter_ID
      AND m.Is_Active = 1
      AND a.Is_Active = 1
    ORDER BY a.Sort_Order, m.Source_Display_Text;
END
GO

/* 6. Verification - every row must show OK ------------------------------ */
SELECT a.App_Name,
       a.Reading_SP_Name,
       CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Reading_SP_Name), N'P') IS NOT NULL THEN 'OK'
            ELSE 'MISSING - check errors above' END AS Status
FROM dbo.MM_CR_Application a
WHERE a.Is_Active = 1
ORDER BY a.Sort_Order;
GO
