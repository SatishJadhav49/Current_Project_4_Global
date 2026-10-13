/* =====================================================================
   Correlation Module - Phase 1
   02_Seed_Applications.sql - application registry + grid columns
   Database    : PQ_Dashboard_Master
   Re-runnable : yes (inserts only what is missing)

   To ADD a new application later (e.g. CMM):
     1. Create its master SP (see 04_SPs_Master_Data.sql for the contract)
     2. Add one row to the @Apps list below and its columns to @Columns
     3. Re-run this script - no API / UI change needed.
   To REMOVE an application: UPDATE MM_CR_Application SET Is_Active = 0
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET NOCOUNT ON;

DECLARE @Apps TABLE
(
    App_Code        VARCHAR(30),
    App_Name        NVARCHAR(100),
    Leaf_Level_Name NVARCHAR(50),
    Master_SP_Name  SYSNAME,
    Sort_Order      INT
);

-- Tab order follows the design (1D BIW, 1D TCF, Torque, EST, GD)
INSERT INTO @Apps VALUES
    ('1D_BIW', N'1D BIW', N'Location',  N'SP_CR_Get_Master_1D_BIW', 1),
    ('1D_TCF', N'1D TCF', N'Location',  N'SP_CR_Get_Master_1D_TCF', 2),
    ('TORQUE', N'Torque', N'Parameter', N'SP_CR_Get_Master_Torque', 3),
    ('EST',    N'EST',    N'Location',  N'SP_CR_Get_Master_EST',    4),
    ('GD',     N'GD',     N'Location',  N'SP_CR_Get_Master_GD',     5);

INSERT INTO dbo.MM_CR_Application (App_Code, App_Name, Leaf_Level_Name, Master_SP_Name, Sort_Order, Is_Active, Inserted_Date)
SELECT a.App_Code, a.App_Name, a.Leaf_Level_Name, a.Master_SP_Name, a.Sort_Order, 1, GETDATE()
FROM @Apps a
WHERE NOT EXISTS (SELECT 1 FROM dbo.MM_CR_Application x WHERE x.App_Code = a.App_Code);

DECLARE @Columns TABLE
(
    App_Code      VARCHAR(30),
    Column_Key    VARCHAR(100),
    Column_Header NVARCHAR(100),
    Sort_Order    INT,
    Is_Filterable BIT
);

INSERT INTO @Columns VALUES
    -- 1D BIW : Area > Part > Checkpoint > Location
    ('1D_BIW', 'Area_Name',       N'Area',        1, 1),
    ('1D_BIW', 'Part_Name',       N'Part',        2, 1),
    ('1D_BIW', 'Checkpoint_Name', N'Check Point', 3, 1),
    ('1D_BIW', 'Location_Name',   N'Location',    4, 1),
    ('1D_BIW', 'Measure_Type',    N'Gap / Flush', 5, 1),
    -- 1D TCF : Area > Part > Checkpoint > Location
    ('1D_TCF', 'Area_Name',       N'Area',        1, 1),
    ('1D_TCF', 'Part_Name',       N'Part',        2, 1),
    ('1D_TCF', 'Checkpoint_Name', N'Check Point', 3, 1),
    ('1D_TCF', 'Location_Name',   N'Location',    4, 1),
    ('1D_TCF', 'Measure_Type',    N'Gap / Flush', 5, 1),
    -- Torque : Parameter
    ('TORQUE', 'Parameter_Name',  N'Parameter',   1, 1),
    -- EST : Parameter > Location
    ('EST',    'Parameter_Name',  N'Parameter',   1, 1),
    ('EST',    'Location_Name',   N'Location',    2, 1),
    -- GD : Parameter > Location
    ('GD',     'Parameter_Name',  N'Parameter',   1, 1),
    ('GD',     'Location_Name',   N'Location',    2, 1);

INSERT INTO dbo.MM_CR_Application_Column (App_ID, Column_Key, Column_Header, Sort_Order, Is_Filterable, Is_Active)
SELECT app.App_ID, c.Column_Key, c.Column_Header, c.Sort_Order, c.Is_Filterable, 1
FROM @Columns c
JOIN dbo.MM_CR_Application app ON app.App_Code = c.App_Code
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.MM_CR_Application_Column x
    WHERE x.App_ID = app.App_ID AND x.Column_Key = c.Column_Key
);

SELECT a.App_ID, a.App_Code, a.App_Name, a.Master_SP_Name, a.Is_Active, COUNT(c.App_Column_ID) AS Column_Count
FROM dbo.MM_CR_Application a
LEFT JOIN dbo.MM_CR_Application_Column c ON c.App_ID = a.App_ID AND c.Is_Active = 1
GROUP BY a.App_ID, a.App_Code, a.App_Name, a.Master_SP_Name, a.Is_Active, a.Sort_Order
ORDER BY a.Sort_Order;
GO
