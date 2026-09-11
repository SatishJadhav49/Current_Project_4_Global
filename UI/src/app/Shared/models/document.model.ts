export class DocumentModel {
    Document_ID : number =0;
    Employee_ID? : number=0;
    Employee_Name ?:string = localStorage.getItem('Employee_Name') ?? "";
    Document_Title?:string = localStorage.getItem('Document_Title') ?? "";
    Document_Path?:string = localStorage.getItem('Document_Path') ?? "";
    Inserted_Host?:string = localStorage.getItem('Hostname') ?? "";
    Inserted_Date? : Date;
    Inserted_User_ID = Number(localStorage.getItem('Employee_ID'));
    Plant_Code?:string = localStorage.getItem('Plant_Code') ?? "";
}