/* =====================================================================
   Correlation Module - Phase 1
   05_Menu_Insert.sql - adds "Mapping Master" under the Masters menu
   Database    : PQ_Dashboard_Master
   Re-runnable : yes

   Sidebar submenus come from MM_Sub_Menus (ActionName = Angular route).
   Everyone whose role has the Masters menu will see the new entry.

   >>> CHECK @Masters_Menu_ID before running. By default it picks the
       first active menu whose LinkName contains 'Master'. Set it
       explicitly if that is not the right one.
   ===================================================================== */
USE [PQ_Dashboard_Master];
GO
SET NOCOUNT ON;

DECLARE @Masters_Menu_ID DECIMAL(18,0) = NULL;   -- <<< set explicitly if needed
DECLARE @ActionName NVARCHAR(200) = N'correlation/mapping-master';
DECLARE @LinkName   NVARCHAR(200) = N'Mapping Master';

IF @Masters_Menu_ID IS NULL
    SELECT TOP 1 @Masters_Menu_ID = Menu_ID
    FROM dbo.MM_Menus
    WHERE LinkName LIKE N'%Master%' AND ISNULL(Is_Active, 1) = 1
    ORDER BY Sort_Order;

IF @Masters_Menu_ID IS NULL
BEGIN
    RAISERROR(N'Masters menu not found in MM_Menus - set @Masters_Menu_ID manually.', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM dbo.MM_Sub_Menus WHERE ActionName = @ActionName AND Menu_ID = @Masters_Menu_ID)
BEGIN
    UPDATE dbo.MM_Sub_Menus SET Is_Active = 1 WHERE ActionName = @ActionName AND Menu_ID = @Masters_Menu_ID;
    PRINT 'Mapping Master submenu already exists - ensured active.';
    RETURN;
END

DECLARE @Sort_Order INT = (SELECT ISNULL(MAX(Sort_Order), 0) + 1 FROM dbo.MM_Sub_Menus WHERE Menu_ID = @Masters_Menu_ID);
DECLARE @Audit_Type_Id DECIMAL(18,0) = (SELECT Audit_Type_Id FROM dbo.MM_Menus WHERE Menu_ID = @Masters_Menu_ID);

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.MM_Sub_Menus'), N'Sub_Menu_ID', N'IsIdentity') = 1
BEGIN
    INSERT INTO dbo.MM_Sub_Menus (ActionName, LinkName, Menu_ID, Sort_Order, Is_Active, Audit_Type_Id, Inserted_Date)
    VALUES (@ActionName, @LinkName, @Masters_Menu_ID, @Sort_Order, 1, @Audit_Type_Id, GETDATE());
END
ELSE
BEGIN
    DECLARE @Sub_Menu_ID DECIMAL(18,0) = (SELECT ISNULL(MAX(Sub_Menu_ID), 0) + 1 FROM dbo.MM_Sub_Menus);
    INSERT INTO dbo.MM_Sub_Menus (Sub_Menu_ID, ActionName, LinkName, Menu_ID, Sort_Order, Is_Active, Audit_Type_Id, Inserted_Date)
    VALUES (@Sub_Menu_ID, @ActionName, @LinkName, @Masters_Menu_ID, @Sort_Order, 1, @Audit_Type_Id, GETDATE());
END

SELECT * FROM dbo.MM_Sub_Menus WHERE Menu_ID = @Masters_Menu_ID ORDER BY Sort_Order;
GO
