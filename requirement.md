IN this project develop an major functionality that allows users to create one parameter and map other applications parameters/location to this parameter and track/see trends of X chart and other analysis one single page. The functionality should include the following features:
1. Parameter Creation: Users should be able to create a new parameter by providing a name. The system should validate the input to ensure that the parameter name is unique.
2. Parameter Mapping: Users should be able to map existing parameters from other applications to the newly created parameter. The system should provide a user-friendly interface for selecting and linking these parameters. Simple give top bar of module name and show data in the table as per that module seleted. User will select that parameter row , and each appplication have one last key point like Location or parameter or may be other. based on applcations we can save its last identity ID in our sql database for tracking purposes. and mappping purpose. User should be able to see what he have apped againt that parameter and also unmap if required. The system should ensure that the mapping is consistent and does not allow duplicate mappings.
3. Understand this propely and design backend in such a way that it can handle multiple applications and their parameters efficiently. The backend should be able to store the parameter mappings in a relational database, ensuring data integrity and easy retrieval for analysis.
4. In the future we might add/remove applications, so the system should be designed to accommodate changes in the list of applications without requiring significant modifications to the existing functionality.
5. First discuss this with me and then we can start the development. We need to ensure that the design is scalable and maintainable, keeping in mind potential future enhancements.
6. In this we wiill just map parameters and track them, we will not be doing any data analysis or charting in this phase. The focus will be on creating a robust parameter mapping system that can serve as a foundation for future analytical features.
7. in ythe future we will get readijgs and other data from respective applications and we will use this mapping to show the data in a single page for analysis. But for now we will just focus on parameter mapping and tracking.
8. Reffer query.sql file for the sample and this requirement.md file for the requirement. also reffer corelation module ppt for deisgn or other.



<!-- Reports Requirements -->

1. Now as per above mapped parameters, we need to create a report generation functionality that allows users to generate reports based on the mapped parameters. Add this report with name of Corellation. The report should include the following features:
   - Report Generation: Users should be able to select model and a parameter which user have added against which he added mapped parameters from different applications and generate a report that includes the trend chart for the all mapped parameters on the single page just application name, parameter name and its trend chart. The system should provide options for customizing the report, such as selecting a date range. on the that page in best way so that user can co-relate the data and see the trend of all mapped parameters in a single page. The report should be exportable in common formats such as PDF or Excel for further analysis.
   - Data Retrival: The system should efficiently retrieve the necessary data from the database based on the selected parameters and date range. and eveny application have different table names and columns. also we need to show its specifications for that appication specifciation tables also different for each application. So we need to design the backend in such a way that it can handle different table names and columns for each application and retrieve the data accordingly. The system should ensure that the data retrieval process is optimized for performance, especially when dealing with large datasets. Below are the table names and columns for each application that we need to consider for data retrieval:
= 1D TCF/BIW: 
select top 100 audit.Audit_Date, sheet.Reading, spec.MinVal as LSL, spec.MaxVal as USL , spec.Is_Gap,spec.Is_Flushness from 
MM_Vehicle_Audit audit 
join MM_Track_Sheet sheet on audit.Audit_ID = sheet.Audit_ID
join MM_LocationMaster loc on sheet.Location_ID = loc.Location_ID
join MM_SpecificationMaster spec on sheet.Specification_ID = spec.Specification_ID
where sheet.Is_NA=0


= Torque Audit:
select sheet.Reading1,sheet.Reading2,sheet.Reading3, sheet.Average, audit.Audit_Date, para.LSL, para.USL
from MM_Audit_Sheet_Details sheet
join MM_Audit_Master audit on audit.Audit_ID= sheet.Audit_ID
join MM_ParameterMaster para on sheet.Parameter_ID = para.Parameter_ID
where sheet.Is_NA=0


= EST / GD
select audit.Audit_Date, sheet.Reading1,sheet.Reading2,sheet.Reading3, sheet.Average, spec.LSL, spec.USL
from MM_Audit_Sheet_Datails sheet
join MM_Audit_Master audit on sheet.Audit_ID = audit.Audit_ID
join MM_LocationMaster loc on sheet.Location_ID = loc.Location_ID
join MM_SpecificationDetails spec on sheet.Specification_ID = spec.Specification_ID
where sheet.Is_NA =0


2. We might add multiple graphs for the comparizon in the future, so add tabl on the top to navigate between different graphs and show the data in a single page. The system should be designed to accommodate future enhancements, allowing for the addition of new graph types and customization options without requiring significant modifications to the existing functionality.
3. also add zoom option for the graph and based on that graph zoom click just zoom that graph in the pop up modal.
4. Use card type design, of each graph with application name and specifications name on top.
5. on the top after filters show that parameter name which we have added.
6. out of speicfication points should be highlight in red icon on the graph.


<!-- Image Requirements -->
1. We have images for each application and we need to show that image on the top of the graph with application name and specification name. The system should be designed to handle different image formats and sizes, ensuring that the images are displayed correctly across various devices and screen resolutions.
2. Dont store images in our database, insted directly fetch it from the respective application database table.
3. We have image in the DB table as filecontent, you can directly show it. Here is application wise logic where we have images.
4. 1D TCF/BIW: select FileContent, Area_ID,Part_ID,Model_ID,Audit_Type_Id from MM_Image_Master where Part_ID=@Part_ID 
5. EST/GD: select FileContent,Parameter_ID,Location_ID,Audit_Type_Id  from MM_Image_Master where Location_ID=@Location_ID
6. Torque Audit: select FileContent, Parameter_ID from MM_Image_Master where Parameter_ID = @Parameter_ID
7. Show this image view option in the master where user mapping points with parameter and also in the report page on the top of the graph with application name and specification name. The system should ensure that the images are displayed in a visually appealing manner, enhancing the overall user experience.
8. On click zoom image to the full screen as per screen.
9. The system should be designed to handle different image sizes, ensuring that the images are displayed correctly across various devices and screen resolutions.

<!-- Addition of new applications -->
1. CMM :
select top 100 
para.side, para.Parameter_Name, para.Point_Number, area.Area_Name, spec.Co_ordinate, spec.LSL, spec.USL, spec.IS_CTQ
from MM_ParameterMaster para
join MM_AreaMaster area on para.Area_ID = area.Area_ID
join MM_Parameter_SpecDetails_Master spec on para.Parameter_ID = spec.Parameter_ID
where para.Model_ID = @Model_ID


- For reading take deviation 
select top 100 audit.Audit_Date, sheet.Parameter_ID,sheet.Coordinate, Deviation, sheet.Specification_ID from MM_Audit_Sheet_Details sheet 
join MM_ParameterMaster para on sheet.Parameter_ID = para.Parameter_ID
join MM_Audit_Master audit on sheet.Audit_ID = audit.Audit_ID

- No images,
-  in this parameter will be the main along with co-ordinate. In the specification table there might be multiple values for the same paremeter and co-ordinate but fo the reading we need to take the reaidngs only form specification id join to sheet table. and for the mapping selection keep mentioned unique parameter and co-ordinate combination for the mapping purpose. also it like side, area, parameter name, point no, coordinate.


2. Master Checker :
select part.Part_Name, part.Is_LH, part.Is_RH, part.Is_Common, loc.Location_Name, loc.Is_Gap, loc.Is_Flush
from 
MM_Part_Master part 
join MM_Location_Master  loc on part.Part_ID = loc.Part_ID
where part.Model_ID = @Model_ID and part.Is_Active=1 and loc.Is_Active=1

- For reading
select audit.Audit_Date, sheet.Reading, spec.LSL, spec.USL, loc.Location_ID, spec.Spec_ID
from MM_Audit_Sheet_Details sheet
join MM_Audit_Master audit on sheet.Audit_ID= audit.Audit_ID
join MM_Specification_Master spec on spec.Spec_ID = sheet.Spec_ID
join MM_Location_Master loc on loc.Location_ID = spec.Location_ID
where sheet.Is_NA <> 1

- For images we have images stored in the application server folder and we have image name in the table MM_Image_Master for now ignore this later on we will at it.
- database name - PQ_Master_Checker
- for mapping , in the part master we have lh, rh & common flags . some times is_lh and is_rh both will be true so for  that in the side show both separate rows , so there will be this columns - part name , side, location name, gap/flush . and other coluns same as other

