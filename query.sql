-- 1D BIW Master Data retival query
select loc.Location_ID, loc.Location_Name, loc.Is_Gap,loc.Is_Flushness, che.Checkpoint_ID,che.Checkpoint_Name,part.Part_ID , part.Part_Name, area.Area_ID, area.Area_name
from MM_LocationMaster loc
join MM_CheckpointMaster che on loc.Checkpoint_ID = che.Checkpoint_ID
join MM_PartMaster part on che.Part_ID = part.Part_ID
join MM_AreaMaster area on part.Area_ID = area.Area_ID
where loc.Audit_Type_Id=2 and loc.Is_Active=1 and che.Is_Active=1 and part.Is_Active=1 and area.Is_Active = 1 and loc.Model_ID = @Model_ID
order by loc.SORTORDER , che.SORTORDER,part.SORTORDER, area.SORTORDER

-- 1D TCF Master Data retival query
select loc.Location_ID, loc.Location_Name, loc.Is_Gap,loc.Is_Flushness, che.Checkpoint_ID,che.Checkpoint_Name,part.Part_ID , part.Part_Name, area.Area_ID, area.Area_name
from MM_LocationMaster loc
join MM_CheckpointMaster che on loc.Checkpoint_ID = che.Checkpoint_ID
join MM_PartMaster part on che.Part_ID = part.Part_ID
join MM_AreaMaster area on part.Area_ID = area.Area_ID
where loc.Audit_Type_Id=1 and loc.Is_Active=1 and che.Is_Active=1 and part.Is_Active=1 and area.Is_Active = 1 and loc.Model_ID = @Model_ID
order by loc.SORTORDER , che.SORTORDER,part.SORTORDER, area.SORTORDER


-- EST master data retrieval query
select loc.Location_ID,loc.Location_Name, para.Parameter_ID,para.Parameter_Name
from [MM_LocationMaster] loc
join MM_ParameterMaster para on loc.Parameter_ID = para.Parameter_ID
where loc.Audit_Type_Id=1 and loc.Is_Active=1 and para.Is_Active=1 and loc.Model_ID=@Model_ID

-- GD master data retrieval query
select loc.Location_ID,loc.Location_Name, para.Parameter_ID,para.Parameter_Name
from [MM_LocationMaster] loc
join MM_ParameterMaster para on loc.Parameter_ID = para.Parameter_ID
where loc.Audit_Type_Id=2 and loc.Is_Active=1 and para.Is_Active=1 and loc.Model_ID=@Model_ID


-- Toruqe master data retrieval query
select Parameter_ID,Parameter_Name from MM_ParameterMaster where Is_Active=1 and Model_ID=@Model_ID
order by SORTORDER