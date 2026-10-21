/* =====================================================================
   Correlation Module - Images
   08_Images.sql - image registry + per-application image SPs
   Database    : PQ_Dashboard_Master (reads [1D_Module], [PQ_EST_GD],
                 [PQ_Torque_Audit] with 3-part names)
   Re-runnable : yes. Run AFTER 01-07. Execute the whole file (F5).

   Images are NOT stored in PQ_Dashboard_Master - they are read live from
   each application's MM_Image_Master.FileContent.

   IMAGE SP CONTRACT (every application must follow it) - two modes:
     1. Image mode  : @Leaf_ID = the leaf (Location_ID / Parameter_ID)
                      -> one row, column FileContent (bytes or base64 string);
                         no row = no image
     2. List mode   : @Leaf_ID NULL, @Leaves = leaves to check
                      -> column Leaf_ID for every leaf that HAS an image
                         (used to show the image icon only where one exists)
   To add a new application later: create its image SP and set
   MM_CR_Application.Image_SP_Name - no API / UI change.
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. Registry column ---------------------------------------------------- */
IF COL_LENGTH(N'dbo.MM_CR_Application', N'Image_SP_Name') IS NULL
    ALTER TABLE dbo.MM_CR_Application ADD Image_SP_Name SYSNAME NULL;
GO

UPDATE dbo.MM_CR_Application SET Image_SP_Name = N'SP_CR_Get_Image_1D'     WHERE App_Code IN ('1D_BIW', '1D_TCF');
UPDATE dbo.MM_CR_Application SET Image_SP_Name = N'SP_CR_Get_Image_Torque' WHERE App_Code = 'TORQUE';
UPDATE dbo.MM_CR_Application SET Image_SP_Name = N'SP_CR_Get_Image_EST_GD' WHERE App_Code IN ('EST', 'GD');
GO

/* 2. 1D TCF / BIW - image is kept per Part: Location -> Checkpoint -> Part.
      When a part has several images, the one of the same audit type
      (TCF / BIW) and model as the location is preferred.                  */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Image_1D
    @Leaf_ID DECIMAL(18,0) = NULL,
    @Leaves  dbo.MM_CR_LeafList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF @Leaf_ID IS NULL
    BEGIN
        -- List mode: leaves that have an image
        SELECT DISTINCT l.Source_Leaf_ID AS Leaf_ID
        FROM @Leaves l
        JOIN [1D_Module].dbo.MM_LocationMaster loc   ON loc.Location_ID = l.Source_Leaf_ID
        JOIN [1D_Module].dbo.MM_CheckpointMaster che ON che.Checkpoint_ID = loc.Checkpoint_ID
        WHERE EXISTS (SELECT 1 FROM [1D_Module].dbo.MM_Image_Master img
                      WHERE img.Part_ID = che.Part_ID AND img.FileContent IS NOT NULL);
        RETURN;
    END

    SELECT TOP 1 img.FileContent
    FROM [1D_Module].dbo.MM_LocationMaster loc
    JOIN [1D_Module].dbo.MM_CheckpointMaster che ON che.Checkpoint_ID = loc.Checkpoint_ID
    JOIN [1D_Module].dbo.MM_Image_Master img     ON img.Part_ID = che.Part_ID
    WHERE loc.Location_ID = @Leaf_ID
      AND img.FileContent IS NOT NULL
    ORDER BY CASE WHEN img.Audit_Type_Id = loc.Audit_Type_Id THEN 0 ELSE 1 END,
             CASE WHEN img.Model_ID = loc.Model_ID THEN 0 ELSE 1 END;
END
GO

/* 3. EST / GD - image per Location (audit type of the location preferred) */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Image_EST_GD
    @Leaf_ID DECIMAL(18,0) = NULL,
    @Leaves  dbo.MM_CR_LeafList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF @Leaf_ID IS NULL
    BEGIN
        -- List mode: leaves that have an image
        SELECT DISTINCT l.Source_Leaf_ID AS Leaf_ID
        FROM @Leaves l
        WHERE EXISTS (SELECT 1 FROM [PQ_EST_GD].dbo.MM_Image_Master img
                      WHERE img.Location_ID = l.Source_Leaf_ID AND img.FileContent IS NOT NULL);
        RETURN;
    END

    SELECT TOP 1 img.FileContent
    FROM [PQ_EST_GD].dbo.MM_Image_Master img
    LEFT JOIN [PQ_EST_GD].dbo.MM_LocationMaster loc ON loc.Location_ID = img.Location_ID
    WHERE img.Location_ID = @Leaf_ID
      AND img.FileContent IS NOT NULL
    ORDER BY CASE WHEN img.Audit_Type_Id = loc.Audit_Type_Id THEN 0 ELSE 1 END;
END
GO

/* 4. Torque - image per Parameter ---------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Image_Torque
    @Leaf_ID DECIMAL(18,0) = NULL,
    @Leaves  dbo.MM_CR_LeafList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF @Leaf_ID IS NULL
    BEGIN
        -- List mode: leaves that have an image
        SELECT DISTINCT l.Source_Leaf_ID AS Leaf_ID
        FROM @Leaves l
        WHERE EXISTS (SELECT 1 FROM [PQ_Torque_Audit].dbo.MM_Image_Master img
                      WHERE img.Parameter_ID = l.Source_Leaf_ID AND img.FileContent IS NOT NULL);
        RETURN;
    END

    SELECT TOP 1 img.FileContent
    FROM [PQ_Torque_Audit].dbo.MM_Image_Master img
    WHERE img.Parameter_ID = @Leaf_ID
      AND img.FileContent IS NOT NULL;
END
GO

/* 5. Image SP of an active application (API validates and executes it) --- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Application_Image_SP
    @App_ID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.Image_SP_Name
    FROM dbo.MM_CR_Application a
    WHERE a.App_ID = @App_ID
      AND a.Is_Active = 1
      AND OBJECT_ID(N'dbo.' + QUOTENAME(a.Image_SP_Name), N'P') IS NOT NULL;
END
GO

/* 6. Verification - every row must show OK ------------------------------ */
SELECT a.App_Name,
       a.Image_SP_Name,
       CASE WHEN OBJECT_ID(N'dbo.' + QUOTENAME(a.Image_SP_Name), N'P') IS NOT NULL THEN 'OK'
            ELSE 'MISSING - check errors above' END AS Status
FROM dbo.MM_CR_Application a
WHERE a.Is_Active = 1
ORDER BY a.Sort_Order;
GO
