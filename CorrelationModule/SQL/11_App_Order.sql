/* =====================================================================
   Correlation Module - custom application order per parameter
   11_App_Order.sql - order table + SPs; mapping tabs and report cards follow it
   Database    : PQ_Dashboard_Master
   Re-runnable : yes. Run AFTER 01-10. Execute the whole file (F5).

   A parameter without a saved order uses the default application order
   (MM_CR_Application.Sort_Order). Saving an empty list resets to default.
   SP_CR_Get_Applications / SP_CR_Get_Report_Mappings are the same as in
   03_SPs_Mapping.sql / 07_Report.sql (kept identical there).
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 1. Order table --------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MM_CR_Parameter_App_Order', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MM_CR_Parameter_App_Order
    (
        CR_Parameter_ID   INT            NOT NULL,
        App_ID            INT            NOT NULL,
        Sort_Order        INT            NOT NULL,
        Inserted_User_ID  DECIMAL(18,0)  NULL,
        Inserted_Host     NVARCHAR(100)  NULL,
        Inserted_Date     DATETIME       NOT NULL CONSTRAINT DF_MM_CR_AppOrder_InsDate DEFAULT (GETDATE()),
        CONSTRAINT PK_MM_CR_Parameter_App_Order PRIMARY KEY CLUSTERED (CR_Parameter_ID, App_ID),
        CONSTRAINT FK_MM_CR_AppOrder_Parameter FOREIGN KEY (CR_Parameter_ID) REFERENCES dbo.MM_CR_Parameter (CR_Parameter_ID),
        CONSTRAINT FK_MM_CR_AppOrder_App FOREIGN KEY (App_ID) REFERENCES dbo.MM_CR_Application (App_ID)
    );
END
GO

/* 2. Ordered id list (table type) --------------------------------------- */
IF TYPE_ID(N'dbo.MM_CR_OrderList') IS NULL
    CREATE TYPE dbo.MM_CR_OrderList AS TABLE
    (
        ID         INT NOT NULL,
        Sort_Order INT NOT NULL
    );
GO

/* 3. Applications (tabs) in the parameter's order ------------------------ */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Applications
    @CR_Parameter_ID INT = NULL   -- when given, apps follow that parameter's custom order
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.App_ID, a.App_Code, a.App_Name, a.Leaf_Level_Name, a.Sort_Order
    FROM dbo.MM_CR_Application a
    LEFT JOIN dbo.MM_CR_Parameter_App_Order o
           ON o.CR_Parameter_ID = @CR_Parameter_ID AND o.App_ID = a.App_ID
    WHERE a.Is_Active = 1
    -- custom order first; apps not in it keep their default order after them
    ORDER BY CASE WHEN o.Sort_Order IS NULL THEN 1 ELSE 0 END, o.Sort_Order, a.Sort_Order, a.App_Name;

    SELECT c.App_ID, c.Column_Key, c.Column_Header, c.Sort_Order, c.Is_Filterable
    FROM dbo.MM_CR_Application_Column c
    JOIN dbo.MM_CR_Application a ON a.App_ID = c.App_ID
    WHERE a.Is_Active = 1 AND c.Is_Active = 1
    ORDER BY c.App_ID, c.Sort_Order;
END
GO

/* 4. Report mappings in the parameter's order ---------------------------- */
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
    LEFT JOIN dbo.MM_CR_Parameter_App_Order o
           ON o.CR_Parameter_ID = m.CR_Parameter_ID AND o.App_ID = m.App_ID
    WHERE m.CR_Parameter_ID = @CR_Parameter_ID
      AND m.Is_Active = 1
      AND a.Is_Active = 1
    -- the parameter's custom application order (report cards follow it), else default
    ORDER BY CASE WHEN o.Sort_Order IS NULL THEN 1 ELSE 0 END, o.Sort_Order, a.Sort_Order, m.Source_Display_Text;
END
GO

/* 5. Order editor: active apps in effective order + mapped count --------- */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Get_Parameter_App_Order
    @CR_Parameter_ID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.App_ID,
           a.App_Name,
           a.Sort_Order AS Default_Sort_Order,
           o.Sort_Order AS Custom_Sort_Order,
           (SELECT COUNT(1) FROM dbo.MM_CR_Parameter_Mapping m
            WHERE m.CR_Parameter_ID = @CR_Parameter_ID AND m.App_ID = a.App_ID AND m.Is_Active = 1) AS Mapped_Count
    FROM dbo.MM_CR_Application a
    LEFT JOIN dbo.MM_CR_Parameter_App_Order o
           ON o.CR_Parameter_ID = @CR_Parameter_ID AND o.App_ID = a.App_ID
    WHERE a.Is_Active = 1
    ORDER BY CASE WHEN o.Sort_Order IS NULL THEN 1 ELSE 0 END, o.Sort_Order, a.Sort_Order, a.App_Name;
END
GO

/* 6. Save the order (replaces the old one); empty list = back to default - */
CREATE OR ALTER PROCEDURE dbo.SP_CR_Save_Parameter_App_Order
    @CR_Parameter_ID INT,
    @Apps            dbo.MM_CR_OrderList READONLY,
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

    DELETE FROM dbo.MM_CR_Parameter_App_Order WHERE CR_Parameter_ID = @CR_Parameter_ID;

    INSERT INTO dbo.MM_CR_Parameter_App_Order (CR_Parameter_ID, App_ID, Sort_Order, Inserted_User_ID, Inserted_Host, Inserted_Date)
    SELECT @CR_Parameter_ID, x.ID, ROW_NUMBER() OVER (ORDER BY x.Sort_Order, x.ID), @User_ID, @Host, GETDATE()
    FROM (SELECT ID, MIN(Sort_Order) AS Sort_Order FROM @Apps GROUP BY ID) x
    JOIN dbo.MM_CR_Application a ON a.App_ID = x.ID AND a.Is_Active = 1;

    DECLARE @Saved INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT 1 AS Result_Code,
           CASE WHEN @Saved = 0 THEN N'Application order reset to default.'
                ELSE N'Application order saved.' END AS Message,
           @CR_Parameter_ID AS ID;
END
GO
