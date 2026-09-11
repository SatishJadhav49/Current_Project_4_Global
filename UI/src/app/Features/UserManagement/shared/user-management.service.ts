import { Injectable } from '@angular/core';
import { ApiRequestService } from '../../../services/api-request.service';
import { Observable } from 'rxjs';
import { UserRole } from '../../../Shared/models/userrole.model';

@Injectable({
  providedIn: 'root',
})
export class UserManagementService {
  constructor(private apiService: ApiRequestService) {}

  saveUser(userData: any): Observable<any> {
    return this.apiService.post('MM_EmployeeMaster/CreateEmployee', userData);
  }

  updateUser(userData: any): Observable<any> {
    return this.apiService.post('MM_EmployeeMaster/UpdateEmployee/'+ userData.Employee_ID, userData.saveDataList);
  }

  getUserList(): Observable<any> {
    return this.apiService.get('MM_EmployeeMaster/GetAllEmployees');
  }

  getUserDetails(userId: number): Observable<any> {
    return this.apiService.get(`MM_EmployeeMaster/GetEmployeeById/${userId}`);
  }

  deleteUser(userId: number): Observable<any> {
    return this.apiService.post(`MM_EmployeeMaster/DeleteEmployee/${userId}`, userId);
  }


  //************************** User to Role ******************************* */

  getAllUserRole(): Observable<UserRole[]> {
    return this.apiService.get('MM_User_Roles/GetAllUserRole');
  }

  createUserRole(userData: UserRole[]): Observable<any> {
    return this.apiService.post('MM_User_Roles/CreateUserRole', userData);
  }

  updateUserRole(userRoleKey: number, userData: UserRole): Observable<any> {
    return this.apiService.post('MM_User_Roles/UpdateUserRole/'+ userData.User_Role_Key, userData);
  }

  deleteUserRole(userRoleKey: number): Observable<any> {
    return this.apiService.post(`MM_User_Roles/DeleteUserRole`,userRoleKey);
  }

  getRoleList(): Observable<UserRole[]> {
    return this.apiService.get('MM_User_Roles/GetRoleList');
  }

  getAllEmployees(): Observable<UserRole[]> {
    return this.apiService.get('MM_EmployeeMaster/GetAllEmployees');
  }

}
