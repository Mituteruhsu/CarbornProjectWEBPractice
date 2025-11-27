using CarbonProject.Models.EFModels.RBAC;
using System.Collections.Generic;

public class AssignRoleViewModel
{
    public int MemberId { get; set; }
    public string Username { get; set; } = "";
    // 現有角色 id 列表
    public List<int> CurrentRoleIds { get; set; } = new();
    // 表單選取的角色 id 列表
    public List<int> SelectedRoleIds { get; set; } = new();
    // 系統所有角色
    public List<Role> Roles { get; set; } = new();
}
