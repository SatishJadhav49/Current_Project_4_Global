/* =====================================================================
   Correlation Module - Phase 1
   03_SPs_Mapping.sql - parameter / mapping stored procedures
   Database    : PQ_Dashboard_Master
   Re-runnable : yes (CREATE OR ALTER - SQL Server 2016 SP1+)

   Result_Code convention for save / delete SPs:
     1 = Success, 0 = Validation error, 2 = Duplicate, 3 = Not found
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------
   Models for the dropdown (from PQ_Dashboard_Master.dbo.MM_Model)
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Models
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Model_ID, Model_Name
    FROM dbo.MM_Model
    ORDER BY Model_Name;
END
GO

/* ---------------------------------------------------------------------
   Active applications (RS1) + their grid columns (RS2)
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Applications
AS
BEGIN
    SET NOCOUNT ON;

    SELECT App_ID, App_Code, App_Name, Leaf_Level_Name, Sort_Order
    FROM dbo.MM_CR_Application
    WHERE Is_Active = 1
    ORDER BY Sort_Order, App_Name;

    SELECT c.App_ID, c.Column_Key, c.Column_Header, c.Sort_Order, c.Is_Filterable
    FROM dbo.MM_CR_Application_Column c
    JOIN dbo.MM_CR_Application a ON a.App_ID = c.App_ID
    WHERE a.Is_Active = 1 AND c.Is_Active = 1
    ORDER BY c.App_ID, c.Sort_Order;
END
GO

/* ---------------------------------------------------------------------
   Parameters of a model (RS1) + mapped count per application (RS2)
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Parameters
    @Model_ID DECIMAL(18,0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.CR_Parameter_ID,
           p.Model_ID,
           p.Parameter_Name,
           p.Inserted_Date,
           p.Updated_Date
    FROM dbo.MM_CR_Parameter p
    WHERE p.Model_ID = @Model_ID AND p.Is_Active = 1
    ORDER BY p.Parameter_Name;

    SELECT m.CR_Parameter_ID,
           a.App_ID,
           a.App_Name,
           COUNT(1) AS Mapped_Count
    FROM dbo.MM_CR_Parameter_Mapping m
    JOIN dbo.MM_CR_Parameter p   ON p.CR_Parameter_ID = m.CR_Parameter_ID
    JOIN dbo.MM_CR_Application a ON a.App_ID = m.App_ID
    WHERE p.Model_ID = @Model_ID
      AND p.Is_Active = 1
      AND m.Is_Active = 1
      AND a.Is_Active = 1
    GROUP BY m.CR_Parameter_ID, a.App_ID, a.App_Name, a.Sort_Order
    ORDER BY m.CR_Parameter_ID, a.Sort_Order;
END
GO

/* ---------------------------------------------------------------------
   Create (@CR_Parameter_ID = 0 / NULL) or rename a parameter
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Save_Parameter
    @CR_Parameter_ID INT = NULL,
    @Model_ID        DECIMAL(18,0),
    @Parameter_Name  NVARCHAR(250),
    @User_ID         DECIMAL(18,0) = NULL,
    @Host            NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Parameter_Name  = LTRIM(RTRIM(ISNULL(@Parameter_Name, N'')));
    SET @CR_Parameter_ID = ISNULL(@CR_Parameter_ID, 0);

    IF @Parameter_Name = N''
    BEGIN
        SELECT 0 AS Result_Code, N'Parameter name is required.' AS Message, @CR_Parameter_ID AS ID;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MM_Model WHERE Model_ID = @Model_ID)
    BEGIN
        SELECT 0 AS Result_Code, N'Selected model was not found.' AS Message, @CR_Parameter_ID AS ID;
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.MM_CR_Parameter
        WHERE Model_ID = @Model_ID
          AND Parameter_Name = @Parameter_Name
          AND Is_Active = 1
          AND CR_Parameter_ID <> @CR_Parameter_ID
    )
    BEGIN
        SELECT 2 AS Result_Code, N'Parameter "' + @Parameter_Name + N'" already exists for this model.' AS Message, @CR_Parameter_ID AS ID;
        RETURN;
    END

    BEGIN TRY
        IF @CR_Parameter_ID = 0
        BEGIN
            INSERT INTO dbo.MM_CR_Parameter (Model_ID, Parameter_Name, Is_Active, Inserted_User_ID, Inserted_Host, Inserted_Date)
            VALUES (@Model_ID, @Parameter_Name, 1, @User_ID, @Host, GETDATE());

            SELECT 1 AS Result_Code, N'Parameter created successfully.' AS Message, CAST(SCOPE_IDENTITY() AS INT) AS ID;
        END
        ELSE
        BEGIN
            -- Model of an existing parameter is never changed, only the name
            UPDATE dbo.MM_CR_Parameter
            SET Parameter_Name  = @Parameter_Name,
                Updated_User_ID = @User_ID,
                Updated_Host    = @Host,
                Updated_Date    = GETDATE()
            WHERE CR_Parameter_ID = @CR_Parameter_ID
              AND Model_ID = @Model_ID
              AND Is_Active = 1;

            IF @@ROWCOUNT = 0
                SELECT 3 AS Result_Code, N'Parameter not found.' AS Message, @CR_Parameter_ID AS ID;
            ELSE
                SELECT 1 AS Result_Code, N'Parameter updated successfully.' AS Message, @CR_Parameter_ID AS ID;
        END
    END TRY
    BEGIN CATCH
        -- Unique index hit by a concurrent save
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SELECT 2 AS Result_Code, N'Parameter "' + @Parameter_Name + N'" already exists for this model.' AS Message, @CR_Parameter_ID AS ID;
            RETURN;
        END;
        THROW;
    END CATCH
END
GO

/* ---------------------------------------------------------------------
   Soft delete a parameter together with its active mappings
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Delete_Parameter
    @CR_Parameter_ID INT,
    @User_ID         DECIMAL(18,0) = NULL,
    @Host            NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Parameter WHERE CR_Parameter_ID = @CR_Parameter_ID AND Is_Active = 1)
    BEGIN
        SELECT 3 AS Result_Code, N'Parameter not found.' AS Message, @CR_Parameter_ID AS ID;
        RETURN;
    END

    BEGIN TRANSACTION;

    UPDATE dbo.MM_CR_Parameter_Mapping
    SET Is_Active = 0, Updated_User_ID = @User_ID, Updated_Host = @Host, Updated_Date = GETDATE()
    WHERE CR_Parameter_ID = @CR_Parameter_ID AND Is_Active = 1;

    UPDATE dbo.MM_CR_Parameter
    SET Is_Active = 0, Updated_User_ID = @User_ID, Updated_Host = @Host, Updated_Date = GETDATE()
    WHERE CR_Parameter_ID = @CR_Parameter_ID;

    COMMIT TRANSACTION;

    SELECT 1 AS Result_Code, N'Parameter deleted successfully.' AS Message, @CR_Parameter_ID AS ID;
END
GO

/* ---------------------------------------------------------------------
   Parameter header (RS1) + everything mapped to it (RS2)
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   Master SP name of an active application (API validates and executes it)
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Application_Master_SP
    @App_ID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.Master_SP_Name
    FROM dbo.MM_CR_Application a
    WHERE a.App_ID = @App_ID
      AND a.Is_Active = 1
      AND OBJECT_ID(N'dbo.' + QUOTENAME(a.Master_SP_Name), N'P') IS NOT NULL;
END
GO

/* ---------------------------------------------------------------------
   Active mappings of one application within a model - used to flag rows
   in the master-data grid (mapped here / mapped to other parameters)
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   Bulk map leaves of one application to a parameter (one transaction).
   Leaves already mapped to this parameter are skipped, not duplicated.
   A leaf is identified by Source_Leaf_ID + Source_Leaf_Type (e.g. Gap / Flush).
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   Unmap (soft) - one or many mapping rows
   --------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Unmap
    @Mapping_IDs dbo.MM_CR_IdList READONLY,
    @User_ID     DECIMAL(18,0) = NULL,
    @Host        NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE m
    SET Is_Active = 0, Updated_User_ID = @User_ID, Updated_Host = @Host, Updated_Date = GETDATE()
    FROM dbo.MM_CR_Parameter_Mapping m
    JOIN @Mapping_IDs i ON i.ID = m.Mapping_ID
    WHERE m.Is_Active = 1;

    DECLARE @Updated INT = @@ROWCOUNT;

    SELECT CASE WHEN @Updated > 0 THEN 1 ELSE 3 END AS Result_Code,
           CASE WHEN @Updated > 0 THEN CAST(@Updated AS NVARCHAR(10)) + N' item(s) unmapped successfully.'
                ELSE N'Nothing to unmap - item(s) already removed.' END AS Message,
           @Updated AS ID;
END
GO
