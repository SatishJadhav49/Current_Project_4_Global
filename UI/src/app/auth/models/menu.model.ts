export interface SubMenu {
  $id: string;
  ActionName: string;
  LinkName: string;
  Sort_Order: number;
}

export interface MenuItem {
  $id: string;
  Menu_ID: number;
  Sort_Order: number;
  Role_ID: number;
  Role_Name: string;
  Is_AllShops: boolean;
  Is_Create: boolean;
  Is_Edit: boolean;
  Is_Delete: boolean;
  Manager_ID: number;
  SubMenuList: SubMenu[];
}

export interface MenuResponse {
  success: boolean;
  data: MenuItem[];
  message?: string;
}

// For internal component use
export interface MenuItemForComponent {
  id: string;
  label: string;
  icon: string;
  isCreate: boolean;
  isEdit: boolean;
  isDelete: boolean;
  menuId: number;
  submenu: {
    label: string;
    route: string;
  }[];
}
