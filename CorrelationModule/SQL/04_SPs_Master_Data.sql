/* =====================================================================
   Correlation Module - Phase 1
   04_SPs_Master_Data.sql - one master-data SP per source application
   Database    : PQ_Dashboard_Master (reads other DBs on the same server
                 with 3-part names: [1D_Module], [PQ_EST_GD], [PQ_Torque_Audit])
   Re-runnable : yes (CREATE OR ALTER)

   CONTRACT every master SP must follow (the API relies on it):
     Input  : @Model_Name NVARCHAR(200)  - Model_Name from PQ_Dashboard_Master.MM_Model
              (Model_IDs differ per application, so each SP resolves its own
               Model_ID(s) from its own MM_Model by name)
     Output : Leaf_ID          - ID stored in MM_CR_Parameter_Mapping.Source_Leaf_ID
              Leaf_Type        - OPTIONAL sub-type stored in Source_Leaf_Type when one
                                 leaf produces several rows (1D: Gap / Flush).
                                 A leaf is identified by Leaf_ID + Leaf_Type.
              Leaf_Name        - leaf display name
              Source_Model_ID  - Model_ID inside the source application
              Display_Text     - full path shown in the "Mapped" list
              + any columns listed in MM_CR_Application_Column for the app

   ASSUMPTION : each source DB has dbo.MM_Model (Model_ID, Model_Name).
                Change the Model lookup line if the table differs.
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ------------------------------ 1D BIW ------------------------------ */
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

/* ------------------------------ 1D TCF ------------------------------ */
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

/* ------------------------------ Torque ------------------------------ */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_Torque
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT para.Parameter_ID   AS Leaf_ID,
           para.Parameter_Name AS Leaf_Name,
           para.Model_ID       AS Source_Model_ID,
           para.Parameter_Name,
           para.Parameter_Name AS Display_Text
    FROM [PQ_Torque_Audit].dbo.MM_ParameterMaster para
    WHERE para.Is_Active = 1
      AND para.Model_ID IN (
            SELECT mo.Model_ID FROM [PQ_Torque_Audit].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY para.SORTORDER;
END
GO

/* ------------------------------- EST -------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_EST
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT loc.Location_ID    AS Leaf_ID,
           loc.Location_Name  AS Leaf_Name,
           loc.Model_ID       AS Source_Model_ID,
           para.Parameter_Name,
           loc.Location_Name,
           CONCAT(para.Parameter_Name, ' > ', loc.Location_Name) AS Display_Text
    FROM [PQ_EST_GD].dbo.MM_LocationMaster loc
    JOIN [PQ_EST_GD].dbo.MM_ParameterMaster para ON loc.Parameter_ID = para.Parameter_ID
    WHERE loc.Audit_Type_Id = 1
      AND loc.Is_Active = 1 AND para.Is_Active = 1
      AND loc.Model_ID IN (
            SELECT mo.Model_ID FROM [PQ_EST_GD].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY para.Parameter_Name, loc.Location_Name;
END
GO

/* -------------------------------- GD -------------------------------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Master_GD
    @Model_Name NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT loc.Location_ID    AS Leaf_ID,
           loc.Location_Name  AS Leaf_Name,
           loc.Model_ID       AS Source_Model_ID,
           para.Parameter_Name,
           loc.Location_Name,
           CONCAT(para.Parameter_Name, ' > ', loc.Location_Name) AS Display_Text
    FROM [PQ_EST_GD].dbo.MM_LocationMaster loc
    JOIN [PQ_EST_GD].dbo.MM_ParameterMaster para ON loc.Parameter_ID = para.Parameter_ID
    WHERE loc.Audit_Type_Id = 2
      AND loc.Is_Active = 1 AND para.Is_Active = 1
      AND loc.Model_ID IN (
            SELECT mo.Model_ID FROM [PQ_EST_GD].dbo.MM_Model mo
            WHERE LTRIM(RTRIM(mo.Model_Name)) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(@Model_Name)))
    ORDER BY para.Parameter_Name, loc.Location_Name;
END
GO
