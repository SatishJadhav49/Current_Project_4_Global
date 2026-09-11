import { Component, EventEmitter, inject, OnInit, Output } from '@angular/core';
import { UserManagementService } from '../shared/user-management.service';
import { CommonService } from '../../../services/common.service';
import { ToastService } from '../../../services/toast.service';
import { ConfirmationService } from '../../../services/confirmation.service';
import { FormsModule } from '@angular/forms';
import { UserCreateModel } from '../shared/user.model';
import { SearchPipe } from '../../../Shared/pipes/search.pipe';
import { MenuService } from '../../../auth';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-createuser',
  standalone: true,
  imports: [CommonModule, FormsModule, SearchPipe],
  templateUrl: './createuser.component.html',
  styleUrls: ['./createuser.component.css'],
})
export class CreateUserComponent implements OnInit {
  // Form Fields
  employeeName: string = '';
  employeeNo: string = '';
  emailAddress: string = '';

  // Reporting Manager
  selectedManager: number = 0;

  // Table Data
  userList: any[] = [];

  // Search functionality
  searchTerm: string = '';
  searchReportingManager: string = '';

  // Rights
  isEdit: boolean = false;
  isDelete: boolean = false;
  isCreate: boolean = false;

  dropdownOpen: boolean = false;

  get filteredUserList() {
    if (!this.searchTerm) {
      return this.userList;
    }
    return this.userList.filter(
      (user) =>
        user.Employee_Name?.toLowerCase().includes(
          this.searchTerm.toLowerCase(),
        ) ||
        user.Employee_No?.toString().includes(this.searchTerm) ||
        user.Designation_Name?.toLowerCase().includes(
          this.searchTerm.toLowerCase(),
        ),
    );
  }

  // Other
  editData: any = null;
  isEditMode: boolean = false;


  @Output() close = new EventEmitter<void>();

  // Dependancy
  readonly userManagementService = inject(UserManagementService);
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);
  readonly confirmationService = inject(ConfirmationService);
  readonly menuService = inject(MenuService);

  ngOnInit(): void {
    const Menu_ID = this.menuService.getMenuIDByActionName(
      window.location.href,
    );
    if (Menu_ID) {
      this.isEdit = this.menuService.canEdit(Menu_ID);
      this.isDelete = this.menuService.canDelete(Menu_ID);
      this.isCreate = this.menuService.canCreate(Menu_ID);
    }
    document.title = 'Create User - QCRT';
    this.getTableData();
  }

  onEmployeeNoChange(): void {
    if (this.employeeNo && this.employeeNo.trim()) {
      this.emailAddress = this.employeeNo.trim() + '@mahindra.com';
    }
  }

  // ********************************** Declaration End *******************************//
  // ********************************** Save/Form Section Start *******************************//
  onSave(): void {
    // Validation before saving
    if (!this.employeeName.trim()) {
      this.toastService.showWarning(
        'Validation Error',
        'Employee name is required.',
      );
      return;
    }

    if (!this.employeeNo.trim()) {
      this.toastService.showWarning(
        'Validation Error',
        'Employee number is required.',
      );
      return;
    }

    if (!this.emailAddress.trim()) {
      this.toastService.showWarning(
        'Validation Error',
        'Email address is required.',
      );
      return;
    }

    // Email format validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.emailAddress.trim())) {
      this.toastService.showWarning(
        'Validation Error',
        'Please enter a valid email address.',
      );
      return;
    }

    let saveDataList: UserCreateModel[] = [];
    const saveData: UserCreateModel = {
      Employee_ID: this.editData?.Employee_ID || 0,
      Employee_Name: this.employeeName,
      Employee_No: this.employeeNo,
      Email_Address: this.emailAddress,
      Reporting_Manager_ID: this.selectedManager,
      Audit_Type_Id: parseInt(localStorage.getItem('Audit_Type_Id') || '0'),
      Plant_ID: parseInt(localStorage.getItem('Plant_ID') || '0'),
      Plant_Code: localStorage.getItem('Plant_Code') || '',
      Inserted_Host: localStorage.getItem('Hostname') || '',
      Inserted_User_ID: localStorage.getItem('Employee_ID')
        ? parseInt(localStorage.getItem('Employee_ID') || '0')
        : 0,
      Updated_Host: localStorage.getItem('Hostname') || '',
      Updated_User_ID: localStorage.getItem('Employee_ID')
        ? parseInt(localStorage.getItem('Employee_ID') || '0')
        : 0,
    };
    saveDataList.push(saveData);

    // Check if it's edit mode or create mode
    if (this.isEditMode && this.editData) {
      // Update existing user
      const updateData = {
        Employee_ID: this.editData.Employee_ID,
        saveDataList,
      };

      this.userManagementService.updateUser(updateData).subscribe(
        (response) => {
          if (response.isSuccessMessage) {
            this.toastService.showSuccess(
              response.messageTitle,
              response.messageDetail,
            );
            this.onReset();
            this.getTableData();
          }
          else {
            this.toastService.showError(
              response.messageTitle,
              response.messageDetail || 'Failed to update user. Please try again.',
            );
          }
        },
        (error) => {
          console.error('Error updating user:', error);
          this.toastService.showError(
            'Update Failed',
            error?.error?.message ||
            'An error occurred while updating the user. Please try again.',
          );
        },
      );
    } else {
      // Create new user
      this.userManagementService.saveUser(saveDataList).subscribe(
        (response) => {
          console.log('User saved successfully:', response);
          this.toastService.showSuccess(
            response.messageTitle,
            response.messageDetail,
          );
          this.onReset();
          this.getTableData();
        },
        (error) => {
          console.error('Error saving user:', error);
          this.toastService.showError(
            'Save Failed',
            error?.error?.message ||
            'An error occurred while saving the user. Please try again.',
          );
        },
      );
    }
  }

  onReset(): void {
    this.employeeName = '';
    this.employeeNo = '';
    this.emailAddress = '';
    this.selectedManager = 0;
    this.isEditMode = false;
    this.editData = null;
  }

  // Edit and Delete methods
  onEdit(user: any): void {
    console.log('Edit user:', user);
    this.isEditMode = true;
    this.editData = user;

    this.userManagementService
      .getUserDetails(user.Employee_ID)
      .subscribe((res: any) => {
        console.log(res);
        this.employeeName = res.Employee_Name;
        this.employeeNo = res.Employee_No;
        this.emailAddress = res.Email_Address || '';
        this.selectedManager = res.Reporting_Manager_ID;
      });
    window.scrollTo(0, 0); // Scroll to top after edit
  }

  onDelete(user: any): void {
    // Show confirmation dialog before deleting
    this.confirmationService
      .confirmDelete(
        'Delete User',
        'Are you sure you want to delete this user? This action cannot be undone.',
        `${user.Employee_Name} (${user.Employee_No})`,
        'Delete User',
        'Cancel',
      )
      .then((confirmed) => {
        if (confirmed) {
          // User confirmed deletion, proceed with API call
          this.userManagementService.deleteUser(user.Employee_ID).subscribe(
            (response) => {
              this.toastService.showSuccess(
                response.messageTitle,
                response.messageDetail,
              );
              this.userList = this.userList.filter(
                (u) => u.Employee_ID !== user.Employee_ID,
              );
            },
            (error) => {
              this.toastService.showError(
                'Delete Failed',
                error?.error?.message ||
                'An error occurred while deleting the user. Please try again.',
              );
            },
          );
        }
        // If not confirmed, do nothing (user cancelled)
      });
  }
  // ********************************** Save/Form Section End *******************************//
  // ********************************** Table Section Start *******************************//

  getTableData() {
    this.userManagementService.getUserList().subscribe(
      (res) => {
        this.userList = res;
      },
      (error) => {
        console.error('Error loading user list:', error);
        this.toastService.showError(
          'Load Error',
          'Failed to load user list. Please refresh the page.',
        );
      },
    );
  }

  // ********************************** Table Section End *******************************//

  refreshData() {
    this.getTableData();
  }

  selectManager(item: any) {
    this.selectedManager = item.Employee_ID;
    this.dropdownOpen = false;
  }

  getUserName(Employee_ID: number) {
    return (
      this.userList.find((e) => e.Employee_ID == Employee_ID).Employee_Name ??
      ''
    );
  }
}
