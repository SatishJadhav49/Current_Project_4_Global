export class UserRole{
User_Role_Key? : number;
Employee_ID? : number;
Role_ID? : number;
Is_Create? : boolean;
Is_Edit? : boolean;
Is_Delete? : boolean;
Plant_ID? : number;
Audit_Type_Id? : number;
Employee_Name? : string ='';
Role_Name? : string = '';
Inserted_User_ID = Number(localStorage.getItem('Employee_ID'));
Inserted_Host?:string= localStorage.getItem('Hostname') ?? "";
Updated_Host?:string = localStorage.getItem('Hostname') ?? "";
Updated_User_ID? = Number(localStorage.getItem('Employee_ID'));
}