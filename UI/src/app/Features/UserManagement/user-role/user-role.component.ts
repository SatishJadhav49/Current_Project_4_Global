import { Component, HostListener, ElementRef, inject } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

import { UserManagementService } from '../shared/user-management.service';
import { SearchPipe } from '../../../Shared/pipes/search.pipe';
import { ConfirmationService, ToastService } from '../../../services';
import { UserRole } from '../../../Shared/models/userrole.model';
import { MenuService } from '../../../auth';

export class Employee {
  Employee_ID?: number;
  Employee_Name?: string;
}

export class Role {
  Role_ID?: number;
  Role_Name?: string;
}
@Component({
  selector: 'app-user-role',
  standalone: true,
  templateUrl: './user-role.component.html',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SearchPipe
  ],
})
export class UserRoleComponent {
  editingKey: number | undefined;
  constructor(private eRef: ElementRef) { }

  EmployeeSearch = '';

  // Dropdown open states
  employeeOpen = false;
  roleOpen = false;
  rightsOpen = false;

  // Search input for filtering table data
  dataSearch = '';

  // Master data
  Employee: Employee[] = [];
  Role: Role[] = [];
  RightsList = ['Create', 'Update', 'Delete'];

  EmployeeList: UserRole[] = [];

  // Index of the row being edited
  Editing_User_Role_Key: number | null = null;

  // Selected Values
  selectedEmployee: Employee | null = null;
  SelectedRoles: Role[] = [];
  SelectedRights: string[] = [];
  Employee_Name = '';

  // Search filter
  employeeSearch = '';

  // Rights
  isEdit: boolean = false;
  isDelete: boolean = false;
  isCreate: boolean = false;
  //*************************************************** Dependency Start *************************************//
  // Injecting services using Angular's inject() function
  readonly toastService = inject(ToastService);
  readonly confirmationService = inject(ConfirmationService);
  readonly userManagementService = inject(UserManagementService);
  readonly menuService = inject(MenuService);

  //*************************************************** Dependency End ****************************************//
  ngOnInit() {
    console.log(window.location.href);
    const Menu_ID = this.menuService.getMenuIDByActionName(window.location.href);
    if (Menu_ID) {
      this.isEdit = this.menuService.canEdit(Menu_ID);
      this.isDelete = this.menuService.canDelete(Menu_ID);
      this.isCreate = this.menuService.canCreate(Menu_ID);
    }
    this.getAllEmployee();
    this.getRoleList();
    this.loadUserRoles();
  }

  getAllEmployee() {
    this.userManagementService.getAllEmployees().subscribe({
      next: (EmployeeData: Employee[]) => {
        this.Employee = EmployeeData;
      },
      error: (err: any) => {
        this.toastService.showError('Error', 'Error fetching Employee')
      }
    })
  }

  getRoleList() {
    this.userManagementService.getRoleList().subscribe({
      next: (RoleData: Role[]) => {
        this.Role = RoleData;
      },
      error: (err: any) => {
        this.toastService.showError('Error', 'Error fetching Employee')
      }
    })
  }

  loadUserRoles() {
    this.userManagementService.getAllUserRole().subscribe({
      next: (data: UserRole[]) => {
        this.EmployeeList = data;


      },
      error: (err) => {
        this.toastService.showError('Error', 'Failed to load roles');
      }
    });
  }


  // Select single employee
  selectEmployee(emp: Employee) {
    this.selectedEmployee = emp;
    this.employeeOpen = false;
  }


  // Filter employee list


  // Toggle multi-select items (roles & rights)
  toggleSelection(item: Role) {
    const index = this.SelectedRoles?.findIndex(r => r.Role_ID === item.Role_ID);
    if (index === -1) {
      this.SelectedRoles?.push(item);
    } else {
      this.SelectedRoles.splice(index, 1);
    }
  }

  getSelectedRoleNames(): string {
    return this.SelectedRoles.length
      ? this.SelectedRoles.map(r => r.Role_Name).join(', ')
      : 'Select Roles';
  }

  toggleRight(right: string): void {
    const index = this.SelectedRights.indexOf(right);
    if (index === -1) {
      this.SelectedRights.push(right);
    } else {
      this.SelectedRights.splice(index, 1);
    }
  }

  // Save employee-role-rights mapping
  saveUserRole() {
    if (this.selectedEmployee && this.SelectedRoles && this.RightsList !== null && this.Editing_User_Role_Key === null) {
      const newRoles = this.SelectedRoles.filter(role =>
        !this.EmployeeList.some(
          existing =>
            existing.Employee_ID === this.selectedEmployee?.Employee_ID &&
            existing.Role_ID === role.Role_ID
        )
      );

      if (newRoles.length === 0) {
        this.toastService.showWarning('No new roles to assign', 'All selected roles are already assigned.');
        return;
      }

      const payload: UserRole[] = newRoles.map(role => ({
        Employee_ID: this.selectedEmployee?.Employee_ID,
        Role_ID: role.Role_ID,
        Role_Name: role.Role_Name,
        Is_Create: this.SelectedRights.includes('Create'),
        Is_Edit: this.SelectedRights.includes('Update'),
        Is_Delete: this.SelectedRights.includes('Delete'),
        Plant_ID: Number(localStorage.getItem('Plant_ID')),
        Audit_Type_Id: Number(localStorage.getItem('Audit_Type_Id')),
        Employee_Name: this.selectedEmployee?.Employee_Name ?? '',
        Inserted_User_ID: Number(localStorage.getItem('Employee_ID')),
        Inserted_Host: localStorage.getItem('Hostname') ?? ''
      }));


      this.userManagementService.createUserRole(payload).subscribe({
        next: (res) => {
          this.toastService.showSuccess('Saved', 'User role created successfully');
          this.refreshData();
          this.loadUserRoles();
        },
        error: (err) => {
          this.toastService.showError('Error', 'Error saving user role');

        }
      });

    }
  }

  // Reset / Refresh all fields
  refreshData() {
    this.selectedEmployee = null;
    this.SelectedRoles = [];
    this.SelectedRights = [];
    this.employeeSearch = '';


    this.employeeOpen = false;
    this.roleOpen = false;
    this.rightsOpen = false;
  }

  // Edit existing row
  editData(data: any) {


    // Find the full Employee object by ID
    const employee = this.Employee.find(emp => emp.Employee_ID == data.Employee_ID);
    if (!employee) {
      console.warn('Employee not found for ID:', data.Employee_ID);
      return;
    }

    this.selectedEmployee = employee;

    // Find the full Role object by ID
    const role = this.Role.find(r => r.Role_ID == data.Role_ID);
    if (role) {
      this.SelectedRoles = [role]; // assuming single-role editing
    }

    // Set rights based on boolean flags
    this.SelectedRights = [];
    if (data?.Is_Create) this.SelectedRights.push('Create');
    if (data?.Is_Edit) this.SelectedRights.push('Update');
    if (data?.Is_Delete) this.SelectedRights.push('Delete');

    this.Editing_User_Role_Key = data.User_Role_Key;

  }

  // Delete row
  deleteUserRole(userRole: any) {
    if (!userRole.User_Role_Key) {
      this.toastService.showWarning("userRole ID not found" + userRole.User_Role_Key, "userRole ID not found")
      return;
    }

    this.confirmationService
      .confirmDelete(
        'Delete userRole',
        'Are you sure you want to delete this userRole? This action cannot be undone.',
        `userRole: ${userRole.Employee_Name} (${userRole.User_Role_Key})`,
        'Delete userRole',
        'Cancel'
      )
      .then((confirmed) => {
        if (confirmed) {
          this.userManagementService.deleteUserRole(userRole.User_Role_Key!).subscribe({
            next: (response) => {
              this.toastService.showSuccess(
                response.messageTitle || 'Deleted',
                response.messageDetail || 'userRole deleted successfully.'
              );

              // Remove from table
              this.EmployeeList = this.EmployeeList.filter(s => s.User_Role_Key !== userRole.User_Role_Key);
            },
            error: (error) => {
              this.toastService.showError(
                'Delete Failed',
                error?.error?.message || 'An error occurred while deleting the userRole.'
              );
            }
          });
        }
      });
  }



  updateUserRole() {
    if (this.selectedEmployee && this.SelectedRoles && this.RightsList !== null && this.Editing_User_Role_Key !== null) {

      const userRoleKey = this.Editing_User_Role_Key;

      if (!userRoleKey) {
        this.toastService.showWarning('UserRole ID not found', 'Cannot update.');
        return;
      }

      const updatedData: UserRole = {
        User_Role_Key: userRoleKey,
        Employee_ID: this.selectedEmployee?.Employee_ID,
        Role_ID: this.SelectedRoles[0]?.Role_ID,
        Is_Create: this.SelectedRights.includes('Create'),
        Is_Edit: this.SelectedRights.includes('Update'),
        Is_Delete: this.SelectedRights.includes('Delete'),
        Plant_ID: Number(localStorage.getItem('Plant_ID')),
        Audit_Type_Id: Number(localStorage.getItem('Audit_Type_Id')),
        Updated_User_ID: Number(localStorage.getItem('Employee_ID')),
        Updated_Host: localStorage.getItem('Hostname') ?? "",
        Inserted_User_ID: 0
      };

      this.userManagementService.updateUserRole(userRoleKey, updatedData).subscribe({
        next: (res) => {
          if (res.isSuccessMessage) {

            this.toastService.showSuccess('Updated', 'User role updated successfully');
            this.refreshData();
            this.loadUserRoles();
            this.Editing_User_Role_Key = null;
          }
          else {
            this.toastService.showError("Error", res.messageTitle);
          }
        },
        error: (err) => {
          this.toastService.showError('Error', 'Error updating user role');

        }
      });
    }
  }


  //  Close dropdowns when clicking outside
  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event) {
    const target = event.target as HTMLElement;

    const clickedInsideEmployee = this.eRef.nativeElement.querySelector('.employee-dropdown')?.contains(target);
    const clickedInsideRole = this.eRef.nativeElement.querySelector('.role-dropdown')?.contains(target);
    const clickedInsideRights = this.eRef.nativeElement.querySelector('.rights-dropdown')?.contains(target);

    if (!clickedInsideEmployee) this.employeeOpen = false;
    if (!clickedInsideRole) this.roleOpen = false;
    if (!clickedInsideRights) this.rightsOpen = false;
  }
}
