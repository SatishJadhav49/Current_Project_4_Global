import { Routes } from "@angular/router";

export const UserRoutes: Routes = [
    {
        path:'createuser',
        loadComponent:() => import('../createuser/createuser.component').then(m=>m.CreateUserComponent)
    },
    {
        path:'usertorole',
        loadComponent:() => import('../user-role/user-role.component').then(m=>m.UserRoleComponent)
    }
    
]