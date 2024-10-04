using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Helpers
{
    public static class Constants
    {
        #region SAP User
        public static readonly string SAP_FORM = "SAP User ID Creation/Role Authorization Form";
        public static readonly string EMPLOYEEEXTORG = "If Employee Type is External, Employee External Organization Name is required.";
        public static readonly string EMPLOYEEDESIGNATION = "Employee Designation is required";
        public static readonly string EMPLOYEELOCATION = "Employee Location is required";
        public static readonly string SAPUSERMODULEREQUIRED = "Module Name is required";
        public static readonly string ROLEAUTHCODEREQUIRED = "Role/Authorization/Transaction Code is required";
        public static readonly string REQUESTFORREQUIRED = "Request For is required";
        public static readonly string SOFTWARENAMEREQUIRED = "Software Name/Other Software Name is required";
        public static readonly string SOFTWARETYPEREQUIRED = "Software Type is required for Other Software";
        public static readonly string KEY_USER_APPROVAL = "Key User Approval";
        public static readonly string IT_SERVICE_DESK_MANAGER_APPROVAL = "IT - Service Desk Manager";
        public static readonly string CISO_LISO_APPROVAL = "CISO/LISO";
        public static readonly string LICENSE_MANAGER_APPROVAL = "License Manager Approval";
        public static readonly string TEMP_DATE_FROM = "If Employee Request Type is Temporary, From date is required.";
        public static readonly string TEMP_DATE_TO = "If Employee Request Type is Temporary, To date is required.";
        public static readonly string SUBMITTER_OTHEREMP_SAME_MESSAGE = "Submitter should be different than Other employee.";
        public static readonly string INTERNET_ACCESS_MORE_INFO = "More information is required if it is a special request.";

        public static readonly string BACKUP_RESTORE_REQFORE_REQUIRED = "Requirement For is required";
        public static readonly string BACKUP_RESTORE_REQUESTFOR_REQUIRED = "Request For is required";
        public static readonly string BACKUP_RESTORE_FOLDERPATH_REQUIRED = "Folder Path is required";
        public static readonly string BACKUP_RESTORE_FOLDERSIZE_REQUIRED = "Folder Size is required";
        public static readonly string BACKUP_RESTORE_BACKUPTYPE_REQUIRED = "Backup Type is required";
        public static readonly string BACKUP_RESTORE_RESTOREAT_REQUIRED = "Restore At is required";
        public static readonly string BACKUP_RESTORE_ALTERNATE_PATH_REQUIRED = "Alternate Folder Path is required";
        public static readonly string BACKUP_RESTORE_RESTORE_DATE_FROM = "Restore From date is required.";
        public static readonly string BACKUP_RESTORE_RESTORE_DATE_TO = "Restore To date is required.";
        public static readonly string BACKUP_RESTORE_FOLDERPATH_VALIDATION = "Folder Path is not valid. Please provide valid network folder path.";
        public static readonly string BACKUP_RESTORE_RETENTION_PERIOD = "Retention period is required";
        public static readonly string BACKUP_DETAILS = "Backup details are required";

        public static readonly string SAP_USER_FORM_STATUS_EMAIL_SUBJECT = "SAP User ID Creation/Role Authorization Form Status";
        public static readonly string SAP_USER_SUBMITTED_EMAIL_SUBJECT = "SAP User ID Creation/Role Authorization Form is Submitted";
        public static readonly string SAP_USER_APPROVAL_EMAIL_SUBJECT = "SAP User ID Creation/Role Authorization Form is Submitted for your approval";
        public static readonly string SAP_USER_FORM_COMPLETED_EMAIL_SUBJECT = "SAP User ID Creation/Role Authorization Form request is completed";
        public static readonly string SAP_USER_FORM_REJECTED_EMAIL_SUBJECT = "SAP User ID Creation/Role Authorization Form request is rejected";

        public static readonly string INTERNET_ACCESS_FORM_STATUS_EMAIL_SUBJECT = "Internet Access Form Status";
        public static readonly string INTERNET_ACCESS_SUBMITTED_EMAIL_SUBJECT = "Internet Access Form is Submitted";
        public static readonly string INTERNET_ACCESS_APPROVAL_EMAIL_SUBJECT = "Internet Access Form is Submitted for your approval";
        public static readonly string INTERNET_ACCESS_FORM_COMPLETED_EMAIL_SUBJECT = "Internet Access Form request is completed";
        public static readonly string INTERNET_ACCESS_FORM_REJECTED_EMAIL_SUBJECT = "Internet Access Form request is rejected";

        public static readonly string SOFTWARE_REQUISITION_FORM_STATUS_EMAIL_SUBJECT = "Software Requisition Form Status";
        public static readonly string SOFTWARE_REQUISITION_SUBMITTED_EMAIL_SUBJECT = "Software Requisition Request Form is Submitted";
        public static readonly string SOFTWARE_REQUISITION_APPROVAL_EMAIL_SUBJECT = "Software Requisition Request Form is Submitted for your approval";
        public static readonly string SOFTWARE_REQUISITION_FORM_COMPLETED_EMAIL_SUBJECT = "Software Requisition Form request is completed";
        public static readonly string SOFTWARE_REQUISITION_FORM_REJECTED_EMAIL_SUBJECT = "Software Requisition Form request is rejected";

        public static readonly string SMARTPHONE_REQUISITION_FORM_STATUS_EMAIL_SUBJECT = "Smartphone Requisition Form Status";
        public static readonly string SMARTPHONE_REQUISITION_SUBMITTED_EMAIL_SUBJECT = "Smartphone Requisition Request Form is Submitted";
        public static readonly string SMARTPHONE_REQUISITION_APPROVAL_EMAIL_SUBJECT = "Smartphone Requisition Request Form is Submitted for your approval";
        public static readonly string SMARTPHONE_REQUISITION_FORM_COMPLETED_EMAIL_SUBJECT = "Smartphone Requisition Form request is completed";
        public static readonly string SMARTPHONE_REQUISITION_FORM_REJECTED_EMAIL_SUBJECT = "Smartphone Requisition Form request is rejected";

        public static readonly string BACKUP_RESTORE_FORM_STATUS_EMAIL_SUBJECT = "Data Backup Restore Form Status";
        public static readonly string BACKUP_RESTORE_SUBMITTED_EMAIL_SUBJECT = "Data Backup Restore Request Form is Submitted";
        public static readonly string BACKUP_RESTORE_APPROVAL_EMAIL_SUBJECT = "Data Backup Restore Request Form is Submitted for your approval";
        public static readonly string BACKUP_RESTORE_FORM_COMPLETED_EMAIL_SUBJECT = "Data Backup Restore Form request is completed";
        public static readonly string BACKUP_RESTORE_FORM_REJECTED_EMAIL_SUBJECT = "Data Backup Restore Form request is rejected";

        public static readonly string SHARED_FOLDER_FORM_STATUS_EMAIL_SUBJECT = "Shared Folder Creation/Change Form Status";
        public static readonly string SHARED_FOLDER_SUBMITTED_EMAIL_SUBJECT = "Shared Folder Creation/Change Form is Submitted";
        public static readonly string SHARED_FOLDER_APPROVAL_EMAIL_SUBJECT = "Shared Folder Creation/Change form is Submitted for your approval";
        public static readonly string SHARED_FOLDER_FORM_COMPLETED_EMAIL_SUBJECT = "Shared Folder Creation/Change Form request is completed";
        public static readonly string SHARED_FOLDER_FORM_REJECTED_EMAIL_SUBJECT = "Shared Folder Creation/Change Form request is rejected";
        public static readonly string SHARED_FOLDER_REQUEST_DATA = "Shared folder request data is required";

        public static readonly string FORM_STATUS_EMAIL_SUBJECT = "Status";
        public static readonly string SUBMITTED_EMAIL_SUBJECT = "is Submitted";
        public static readonly string APPROVAL_EMAIL_SUBJECT = "is Submitted for your approval";
        public static readonly string FORM_COMPLETED_EMAIL_SUBJECT = "processed successfully in e|forms.";
        public static readonly string FORM_REJECTED_EMAIL_SUBJECT = "request is rejected";

        public static readonly string FORM_FORWARDED_EMAIL_SUBJECT = "request is forwarded for your approval";

        public static readonly string IT_CLEARANCE_FORM_STATUS_EMAIL_SUBJECT = "IT Clearance Form Status";
        public static readonly string IT_CLEARANCE_SUBMITTED_EMAIL_SUBJECT = "IT Clearance Request Form is Submitted";
        public static readonly string IT_CLEARANCE_APPROVAL_EMAIL_SUBJECT = "IT Clearance Request Form is Submitted for your approval";
        public static readonly string IT_CLEARANCE_FORM_COMPLETED_EMAIL_SUBJECT = "IT Clearance Form request is completed";
        public static readonly string IT_CLEARANCE_FORM_REJECTED_EMAIL_SUBJECT = "IT Clearance Form request is rejected";

        public static readonly string OTHEREMPLOYEETYPE = "Other Employee Type (existing/new) is required";
        public static readonly string OTHEREMPLOYEENAME = "Other Employee Name is required";
        public static readonly string OTHEREMPLOYEECC = "Other Employee Cost Center is required";
        public static readonly string OTHEREMPLOYEEDEPT = "Other Employee EmployeeDept is required";
        public static readonly string OTHEREMPLOYEEUSERID = "Other Employee User Id is required";
        public static readonly string OTHEREMPLOYEECODE = "Other Employee Code/Number is required";
        public static readonly string OTHEREMPLOYEEEMAIL = "Other Employee Email is required";
        public static readonly string OTHEREMPLOYEEDESIGNATION = "Other Employee Designation is required";
        public static readonly string OTHEREMPLOYEELOCATION = "Other Employee Location is required";
        public static readonly string OTHEREMPLOYEEPHONE = "Other Employee Contact Number is required";
        public static readonly string OTHEREMPLOYEETYPEINTEXT = "Other Employee Type (internal/external) is required";
        public static readonly string OTHEREMPLOYEEORG = "Other Employee External Organisation Name is required";
        #endregion

        public static readonly string NOTEXCEPTIONAL = "Not Exceptional";
        public static readonly string EXCEPTIONAL = "Exceptional";

        //Shared Folder
        public static readonly string SHAREDFOLDER_REQUESTFOR_REQUIRED = "Shared Folder Request For is required.";
        public static readonly string SHAREDFOLDER_CREATION_REQUIRED = "Shared Folder Creation details are required.";
        public static readonly string SHAREDFOLDER_CHANGE_IN_DETAILS_REQUIRED = "Shared Folder Change In details are required as Change In option is selected.";
        public static readonly string SHAREDFOLDER_CHANGE_IN_REQUIRED = "Change In is required as Existing Folder Change option is selected";
        public static readonly string SHAREDFOLDER_ADDREMOVEUSER_REQUIRED = "Shared Folder Add/Remove User Access details are required. Please Save the details and proceed.";
        public static readonly string SHAREDFOLDER_SAMEFOLDEROWNER_MSG = "Shared Folder request is allowed only for same folder owner for all folder paths. Please put another request if folder owner is different.";
        public static readonly string SHAREDFOLDER_SAMEFILESERVER_MSG = "Shared Folder request is allowed only for same file server for all folder paths. Please put another request if file server is different.";
        public static readonly string SHAREDFOLDER_FOLDERPATH = "Shared Folder Existing Folder Change Folder Path is required. ";
        public static readonly string SHAREDFOLDER_FOLDERSIZE = "Shared Folder Existing Folder Change Folder Size is required. ";
        public static readonly string SHAREDFOLDER_OWNERNAME = "Shared Folder Existing Folder Change Folder Owner Name is required. ";

        //Ganesh User Id
        public static readonly string GANESH_USERID_FORM_STATUS_EMAIL_SUBJECT = "Ganesh User ID Creation/Role Authorization Form Status";
        public static readonly string GANESH_USERID_FORM_SUBMITTED_EMAIL_SUBJECT = "Ganesh User ID Creation/Role Authorization Form is Submitted";
        public static readonly string GANESH_USERID_FORM_APPROVAL_EMAIL_SUBJECT = "Ganesh User ID Creation/Role Authorization Form is Submitted for your approval";
        public static readonly string GANESH_USERID_FORM_COMPLETED_EMAIL_SUBJECT = "Ganesh User ID Creation/Role Authorization Form request is completed";
        public static readonly string GANESH_USERID_FORM_REJECTED_EMAIL_SUBJECT = "Ganesh User ID Creation/Role Authorization Form request is rejected";
        public static readonly string GANESH_SYSTEMMODULEROLE_REQUIRED = "System Module/Role/Reason is required";

        //Resource Account & Distribution List 
        public static readonly string RES_DL_ACTION_TYPE_CREATION = "CREATION";
        public static readonly string RES_DL_ACTION_TYPE_DELETION = "DELETION";
        public static readonly string RES_DL_ACTION_TYPE_MOD = "MODIFICATION";
        public static readonly string RESACCOUNT_DL_FORM_STATUS_EMAIL_SUBJECT = "Resource Account & Distribution List Requisition Form Status";
        public static readonly string RESACCOUNT_DL_FORM_SUBMITTED_EMAIL_SUBJECT = "Resource Account & Distribution List Requisition Form is Submitted";
        public static readonly string RESACCOUNT_DL_FORM_APPROVAL_EMAIL_SUBJECT = "Resource Account & Distribution List Requisition Form is Submitted for your approval";
        public static readonly string RESACCOUNT_DL_FORM_COMPLETED_EMAIL_SUBJECT = "Resource Account & Distribution List Requisition Form request is completed";
        public static readonly string RESACCOUNT_DL_FORM_REJECTED_EMAIL_SUBJECT = "Resource Account & Distribution List Requisition Form request is rejected";

        //Incident Reporting Request
        public static readonly string INCIDENT_REPORT_FORM_NAME = "Incident Report Form";
        public static readonly string INCIDENT_REPORT_FORM_STATUS_EMAIL_SUBJECT = "Incident Reporting Request Form Status";
        public static readonly string INCIDENT_REPORT_SUBMITTED_EMAIL_SUBJECT = "Incident Reporting Request Form is Submitted";
        public static readonly string INCIDENT_REPORT_APPROVAL_EMAIL_SUBJECT = "Incident Reporting Request Form is Submitted for your approval";
        public static readonly string INCIDENT_REPORT_FORM_COMPLETED_EMAIL_SUBJECT = "Incident Reporting Request Form is completed";
        public static readonly string INCIDENT_REPORT_FORM_REJECTED_EMAIL_SUBJECT = "Incident Reporting Request Form is rejected";

        //ID Card and Door Access
        public static readonly string ID_CARD_AND_DOOR_ACCESS_FORM_STATUS_EMAIL_SUBJECT = "ID Card and Door Access Form Status";
        public static readonly string ID_CARD_AND_DOOR_ACCESS_SUBMITTED_EMAIL_SUBJECT = "ID Card and Door Access Form is Submitted";
        public static readonly string ID_CARD_AND_DOOR_ACCESS_APPROVAL_EMAIL_SUBJECT = "ID Card and Door Access Form is Submitted for your approval";
        public static readonly string ID_CARD_AND_DOOR_ACCESS_FORM_COMPLETED_EMAIL_SUBJECT = "ID Card and Door Access Form is completed";
        public static readonly string ID_CARD_AND_DOOR_ACCESS_FORM_REJECTED_EMAIL_SUBJECT = "ID Card and Door Access Form is rejected";

        //Suggestion For Order
        public static readonly string SFO_FORM_STATUS_EMAIL_SUBJECT = "Suggestion For Order Status Form Status";
        public static readonly string SFO_SUBMITTED_EMAIL_SUBJECT = "Suggestion For Order Status Form is Submitted";
        public static readonly string SFO_APPROVAL_EMAIL_SUBJECT = "Suggestion For Order Status Form is Submitted for your approval";
        public static readonly string SFO_FORM_COMPLETED_EMAIL_SUBJECT = "Suggestion For Order Status Form is completed";
        public static readonly string SFO_FORM_REJECTED_EMAIL_SUBJECT = "Suggestion For Order Status Form is rejected";

        //Conflict of Interest
        public static readonly string COI_FORM_STATUS_EMAIL_SUBJECT = "Conflict Of Interest Form Status";
        public static readonly string COI_SUBMITTED_EMAIL_SUBJECT = "Conflict Of Interest Form is Submitted";
        //public static readonly string COI_COMMENT_BY_APPROVER = "Conflict Of Interest Form has been commmented by Approvar";
        public static readonly string COI_APPROVAL_EMAIL_SUBJECT = "Conflict Of Interest Form";
        public static readonly string COI_FORM_COMPLETED_EMAIL_SUBJECT = "Conflict Of Interest Form is completed";
        public static readonly string COI_FORM_REJECTED_EMAIL_SUBJECT = "Conflict Of Interest Form is rejected";

        //Courier Request
        public static readonly string COURIER_FORM_NAME = "Courier Request Form";
        public static readonly string COURIER_FORM_STATUS_EMAIL_SUBJECT = "Courier Request Form Status";
        public static readonly string COURIER_SUBMITTED_EMAIL_SUBJECT = "Courier Request Form is Submitted";
        public static readonly string COURIER_APPROVAL_EMAIL_SUBJECT = "Courier Request Form is Submitted for your approval";
        public static readonly string COURIER_FORM_COMPLETED_EMAIL_SUBJECT = "Courier Request Form is completed";
        public static readonly string COURIER_FORM_REJECTED_EMAIL_SUBJECT = "Courier Request Form is rejected";

        //KSRM User Id
        public static readonly string KSRM_ROLE_REQUIRED = "Role is required";
        public static readonly string KSRM_REQUESTFOR_REQUIRED = "Request For is required";

        //IT Asset Requisition        
        public static readonly string LAPTOPTRAVELX = "Laptop + InternalX + Skype Access";
        public static readonly string DESKTOPSET_ASSET = "Desktop Set";
        public static readonly string LAPTOP = "Laptop";
        public static readonly string DESKTOPSET = "DesktopSet";
        public static readonly string LAPTOPALLOWED = "LaptopAllowed";
        public static readonly string LAPTOPNOTALLOWED = "LaptopNotAllowed";
        public static readonly string OTHERASSET = "OtherAsset";
        public static readonly string LAPTOPDESKTOP = "Laptop and Desktop";
        public static readonly string ITASSET_USER_SUBMITTED_EMAIL_SUBJECT = "IT Asset Form is Submitted";
        public static readonly string ITASSET_USER_APPROVAL_EMAIL_SUBJECT = "IT Asset Form is Submitted for your approval";
        public static readonly string ITASSETS_REQUIRED = "IT Required Assets are required";

        //Status Constants
        public static readonly string STATUS_SUBMITTED = "Submitted";
        public static readonly string STATUS_INPROGRESS = "InProgress";
        public static readonly string STATUS_COMPLETED = "Completed";
        public static readonly string STATUS_CANCELLED = "Cancelled";
        public static readonly string STATUS_APPROVED = "Approved";
        public static readonly string STATUS_REJECTED = "Rejected";
        public static readonly string STATUS_FORWARDED = "Forwarded";

        public static readonly string EMP_TYPE_INT = "Internal";
        public static readonly string EMP_TYPE_EXT = "External";

        #region Log Message
        public static readonly string DESIGNATIONS = "Designations";
        public static readonly string RESOURCEACCOUNTLOCATION = "Resource Account Location";
        public static readonly string EMPMASTER = "Employee master.";
        public static readonly string EMPNUMBER = "Employee number.";
        public static readonly string EMPEMAILID = "Employee email id.";
        public static readonly string EMPNAME = "Employee name.";
        public static readonly string EMP_DETAILS = "Employee details.";
        public static readonly string USERDETAILS = "User details.";
        public static readonly string EXISTING_EMP_DETIALS = "Get Existing Employee Details";
        public static readonly string COST_CENTER_DETAILS = "Cost Centers Details.";
        public static readonly string COST_CENTER_HOD_DETAILS = "Cost Centers HOD Details.";
        public static readonly string COST_CENTER_APPROVER_DETAILS = "Cost Centers Approver Details.";

        public static readonly string INTERNET_ACCESS_REQUEST = "Internet access request.";
        public static readonly string CANCEL_INTERNET_ACCESS_REQUEST = "Cancelling internet access request.";
        public static readonly string CREATE_INTERNET_ACCESS_REQUEST = "Creating internet access request.";
        public static readonly string INTERNET_ACCESS_REQUEST_DETAILS = "Internet access request details.";
        public static readonly string UPDATE_INTERNET_ACCESS_REQUEST = "Updating internet access request.";
        public static readonly string INTERNET_ACCESS_MASTER_REQUEST = "Creating Internet Access Master Request.";
        public static readonly string UPDATE_INTERNET_ACCESS_MASTER_REQUEST = "Updating Internet Access Master Request.";

        public static readonly string FIRST_LEVEL_APPROVAL = "First level approval";
        public static readonly string FIRST_LEVEL_APPROVAL_EMP_NUMBER = "First level approval emp number";

        public static readonly string CREATE_SAP_USER_REQUEST = "Creating SAP user request.";
        public static readonly string CREATE_SAP_USER_MASTER_REQUEST = "Creating SAP user master request.";
        public static readonly string UPDATE_SAP_USER_MASTER_REQUEST = "Updating SAP user master request.";
        public static readonly string UPDATE_SAP_USER_REQUEST = "Updating SAP user request.";
        public static readonly string SAP_MODULE_NAME_TEXT = "ModuleName";
        public static readonly string SAP_MODULE_NAME = "Returing SAP module name.";
        public static readonly string SAP_USER_REQUEST_DETAILS = "SAP user request details.";
        public static readonly string CREATE_SAP_USER = "Creating SAP user Request";
        public static readonly string SAP_SYSTEM_DETAILS = "SAP Systems details.";
        public static readonly string SAP_CLIENT_DETAILS = "SAP Client details.";

        public static readonly string SOFTWARE_REQUISITION_REQUEST_DETAILS = "Software Requisition Request Details.";
        public static readonly string SOFTWARE_REQUISITION_REQUEST_DTO = "Software Requisition Request Dto";
        public static readonly string CREATE_SOFTWARE_REQUISITION_REQUEST_DTO = "Creating Software Requisition Request Dto";

        public static readonly string INSERTING_SOFTWARES = "Inserting selected softwares.";
        public static readonly string UPDATE_WORKFLOW_APPROVAL_MAP = "Upadting workflow approval map";

        public static readonly string SAP_USER_MODULE_NAME = "Returing SAP user module name.";
        public static readonly string EDIT_SAP_USER_MODULE = "Editing SAP user module name.";
        public static readonly string SAP_USER_ROLE_AUTHCODE = "Creating SAP User Role AuthCode.";
        public static readonly string UPDATE_SAP_USER_ROLE_AUTHCODE = "Upating SAP User Role AuthCode.";
        public static readonly string UPDATE_SAP_USER_MODULE_NAME = "Update SAP user module name.";
        public static readonly string VALIDATE_MODEL_DATA = "Validate Model Data.";
        public static readonly string GET_SAP_USER_DATA = "Get SAP User data.";

        public static readonly string CREATE_VW_MASTER_REQUEST = "Creating VW master request.";
        public static readonly string REQUEST_DETAILS = "All request details.";
        public static readonly string PENDING_REQUEST_DETAILS = "Pending request details.";
        public static readonly string LINKED_FORM_REQUEST_ID = "Linked form request id";
        public static readonly string APPROVED_REQUEST = "Approved request.";
        public static readonly string SEND_EMAIL = "Sending Email";
        public static readonly string REJECT_REQUEST = "Rejecting request.";
        public static readonly string CANCEL_REQUEST = "Cancelling request.";
        public static readonly string REQUEST_STATUS = "Get request status.";
        public static readonly string APPROVED_REQUEST_STATUS = "Get approved request status.";

        public static readonly string MAP_REQUEST_DATA = "Mapping Form request data.";
        public static readonly string REQUEST_WORKFLOW = "Request workflow.";
        public static readonly string APPROVAL_MAP_DTO = "ApprovalMapDto.";
        public static readonly string CONDITIONAL_WORKFLOW = "Conditional workflow map.";

        #endregion

        public static readonly string APPLICATIONERROR = "Error occurred. Please contact system administrator.";


        public static readonly string KSRM_USER_SUBMITTED_EMAIL_SUBJECT = "K-SRM User ID Creation Form is Submitted";
        public static readonly string KSRM_USER_APPROVAL_EMAIL_SUBJECT = "K-SRM User ID Creation Form is Submitted for your approval";

        //Resource Account & DL
        public static readonly string RESOURCE_ACCOUNT = "Resource Account";
        public static readonly string DISTRIBUTION_LIST = "Distribution List";
        public static readonly string DL_RESOURCE_ACCOUNT_NAME = "Distribustion List(DL)/Resource Account Name is required";
        public static readonly string ADD_REMOVE_USERID = "Add/Remove User ID list is required";
        public static readonly string ACCOUNT_OWNER = "Account Owner is required";
        public static readonly string ACTION_TYPE = "Action Type is required";
        public static readonly string REQUEST_FOR = "Request For is required";
        public static readonly string RESOURCE_ACCOUNT_LOCATION = "Resource Account Location is required";
        public static readonly string RESOURCE_ACCOUNT_NAME = "Resource Account Name is required";
        public static readonly string DL_NAME = "Distribution List(DL) Name is required";
        public static readonly string DL_Domain = "Domain is required for Distribution List(DL)";

        //Incident Reporting
        public static readonly string CHOOSE_INCIDENT_DEVICE = "Please choose the appropriate device";
        public static readonly string SEVERITY_LEVEL = "Please select Severity Level/Impact Assessment value";

        //External keyword
        public static readonly string EXTERN_KEYWORD = "extern";

        //ICard & Door Access
        public static readonly string ID_CARD_NUMBER = "Please Provide Identity Card No";

        //Suggestion For Order
        public static readonly string EMPLOYEE_CONTACT_NO = "Please provide Employee Contact Number";
        public static readonly string SECTION_FACILITY = "Please provide Section or Facility";
        //public static readonly string BUDGET = "Please provide Budget Amount";
        public static readonly string SUPPLIER_NAME = "Please provide at least one supplier name";
        public static readonly string ACCEPTANCE = "Please provide at least one supplier acceptance";
        //public static readonly string OFFER_PRICE = "Please provide at least one offer price";

        //Conflict Of Interest
        public static readonly string COI_FORM_NAME = "Conflict Of Interest";
        public static readonly string COI_COMMENTS = "Please provide comments for Conflict Of Interest.";
        public static readonly string HOD_COI_COMMENTS = "Please provide comments for Conflict Of Interest.";
    }
}