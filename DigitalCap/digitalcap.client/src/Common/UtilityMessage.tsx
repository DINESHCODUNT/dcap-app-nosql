export const PageName = {
    CreateGrading: 'CreateGrading',
    EditGrading: 'EditGrading',
    DeleteGrading: 'DeleteGrading',
    DeletePart: 'DeletePart',
    SavePart: 'SavePart',
    UpdatePart: 'UpdatePart',
    CreateSection: 'CreateSection',
    UpdateSection: 'UpdateSection',
    DeleteSection: 'DeleteSection',
    CreateDescription: 'CreateDescription',
    EditDescription: 'EditDescription',
    CreateTank: 'CreateTank',
    EditTank: 'EditTank',
    DeleteTank: 'DeleteTank',
    CreateClientProfile: 'CreateClientProfile',
    EditClientProfile: 'EditClientProfile',
    DeleteClientProfile: 'DeleteClientProfile',
    CreateUser: 'CreateUser',
    UpdateUser: 'UpdateUser',
    DeleteUser: 'DeleteUser',
    CreateProject: 'CreateProject',
    UpdateProject: 'UpdateProject',
    UploadFile: 'UploadFile',
    DeleteFile: 'DeleteFile',
    SaveTaskDetailsSuccess: 'SaveTaskDetailsSuccess',
    CreateCommentSuccess: 'CreateCommentSuccess',
    UpdateCommentSuccess: 'UpdateCommentSuccess',
    ExportSuccess: 'ExportSuccess',
    DeleteComment: 'DeleteComment',
} as const;

export type PageNameType = (typeof PageName)[keyof typeof PageName];

export const Buttons = {
    ok: "Ok",
    cancel: "Cancel",
    save: "Save",
    update: "Update",
    submit: "Submit",
    Continue: "Continue",
} as const;

export type ButtonType = (typeof Buttons)[keyof typeof Buttons];

export function GetTitle(pageNames: PageNameType | string | undefined): string | undefined {
    if (!pageNames) return undefined;

    switch (pageNames) {
        case PageName.CreateGrading:
        case "CreateGrading":
            return 'Create Grading';

        case PageName.EditGrading:
        case "EditGrading":
        case "UpdateGrading":
            return 'Edit Grading';

        case PageName.DeletePart:
        case "DeletePart":
            return 'Delete Part';

        case PageName.SavePart:
        case "SavePart":
            return 'Save Part';

        case PageName.UpdatePart:
        case "UpdatePart":
            return 'Update Part';

        case PageName.CreateSection:
        case "CreateSection":
            return 'Create Section';

        case PageName.UpdateSection:
        case "UpdateSection":
            return 'Update Section';

        case PageName.DeleteSection:
        case "DeleteSection":
            return 'Delete Section';

        case PageName.DeleteGrading:
        case "DeleteGrading":
            return 'Delete Grading';

        case PageName.CreateDescription:
        case "CreateDescription":
            return 'Create Description';

        case PageName.EditDescription:
        case "EditDescription":
        case "UpdateDescription":
            return 'Edit Description';

        case PageName.CreateTank:
        case "CreateTank":
            return 'Create Tank';

        case PageName.EditTank:
        case "EditTank":
        case "UpdateTank":
            return 'Edit Tank';

        case PageName.DeleteTank:
        case "DeleteTank":
            return 'Delete Tank';

        case "DeleteTankImage":
            return 'Delete Image';

        case "UpdateTankStatus":
            return 'Update Tank Status';

        case "SaveExportSettings":
            return 'Save Export Settings';

        case PageName.CreateClientProfile:
        case "CreateClientProfile":
            return 'Create Client Profile';

        case PageName.EditClientProfile:
        case "EditClientProfile":
            return 'Edit Client Profile';

        case PageName.DeleteClientProfile:
        case "DeleteClientProfile":
            return 'Delete Client Profile';

        case PageName.DeleteComment:
        case "DeleteComment":
            return 'Delete Comment';

        case "ExportSuccess":
            return 'Export PDF';

        case "CreateUser":
            return 'Create User';

        case "UpdateUser":
            return 'Update User';

        case "DeleteUser":
            return 'Delete User';

        case "CreateProject":
            return 'Create Project';

        case "UpdateProject":
            return 'Update Project';

        case "UploadFile":
            return "File Upload";

        case PageName.DeleteFile:
        case "DeleteFile":
            return "Delete File";

        case "SaveTaskDetailsSuccess":
            return "Task Details";

        case "CreateCommentSuccess":
            return "Comment";

        case "UpdateCommentSuccess":
            return "Comment";
            
        default:
            return undefined;
    }
}

export function GetConformationMessage(
    pageNames: PageNameType
): string | undefined {
    switch (pageNames) {
        default:
            return undefined;
    }
}

export function resultMessage(
    name: string,
    action: 'Created' | 'Updated' | 'Deleted' | 'Saved' | 'Uploaded'
): string {
    return `${name} - has been ${action} Successfully.`;
}

export function GetPopupMessage(pageName?: string) {
    switch (pageName) {
        case "CreateGrading":
            return "Grading saved successfully";

        case "UpdateGrading":
            return "Grading updated successfully";

        case "DeletePart":
            return "Part deleted successfully";

        case "SavePart":
            return "Part saved successfully";

        case "UpdatePart":
            return "Part updated successfully";

        case "CreateSection":
            return "Section saved successfully";

        case "UpdateSection":
            return "Section updated successfully";

        case "DeleteSection":
            return "Section deleted successfully";

        case "DeleteGrading":
            return "Grading deleted successfully";

        case "CreateDescription":
            return "Description saved successfully";

        case "EditDescription":
            return "Description updated successfully";

        case "CreateTank":
            return "Tank saved successfully";

        case "UpdateTank":
            return "Tank updated successfully";

        case "DeleteTank":
            return "Tank deleted successfully";

        case "DeleteTankImage":
            return "Image deleted successfully";

        case "UpdateTankStatus":
            return "Tank status updated successfully";

        case "SaveExportSettings":
            return "Settings saved successfully";

        case "CreateClientProfile":
            return "Client Profile saved successfully";

        case "EditClientProfile":
            return "Client Profile updated successfully";

        case "DeleteClientProfile":
            return "Client Profile deleted successfully";
        case "CreateUser":
            return "User created successfully";

        case "UpdateUser":
            return "User updated successfully";

        case "DeleteUser":
            return "User deleted successfully";

        case "CreateProject":
            return "Project created successfully";

        case "UpdateProject":
            return "Project updated successfully";

        case "UploadFile":
            return "File uploaded successfully";

        case "DeleteFile":
            return "File deleted successfully";

        case "SaveTaskDetailsSuccess":
            return "Task details saved successfully.";

        case "CreateCommentSuccess":
            return "Comment saved successfully.";

        case "UpdateCommentSuccess":
            return "Comment updated successfully.";

        case "ExportSuccess":
            return "PDF exported successfully";

        case "DeleteComment":
            return "Comment deleted successfully";

        default:
            return "";
    }
}

