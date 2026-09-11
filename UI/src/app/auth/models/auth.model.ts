export interface TokenResponse {
  token?: string;
  success: boolean;
  message?: string;
}

export interface UserInfo {
  Employee_ID: string;
  Employee_Name: string;
  Department_ID: string;
  Is_AllShops: boolean;
  Audit_Type_Id: string;
  Plant_ID: string;
  Plant_Code: string;
  Manager_ID: string;
  Email_Address: string;
  Shop_ID: string;
}

export interface UserSearchResponse {
  success: boolean;
  data: UserInfo[];
  message?: string;
}

export interface AuthUser {
  token: string;
  userType: string;
  isAllShops: string;
  Audit_Type_Id: string;
  Plant_ID: string;
  plantCode: string;
  userId: string;
  managerId: string;
  email: string;
  name: string;
  shopId: string;
}
