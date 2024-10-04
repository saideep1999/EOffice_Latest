using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Skoda_DCMS.Helpers
{
    public class GlobalClass
    {
        public ListDAL listDal;
        public EmployeeClearanceDAL empClrDAL;
        public OCRFDAL OCRFDAL;
        public CabBookingRequestDAL CabBookingRequestDAL;
        public DrivingAuthorizationDAL DrivingAuthorizationDAL;
        public InternalJobPostingDAL InternalJobPostingDAL;
        public CourierRequestDAL CourierRequestDAL;
        public DeviationNoteDAL DeviationNoteDAL;
        public SmartPhoneDAL smartPhoneDAL;
        public SoftwareRequisitionDAL softwareRequisitionDAL;
        public InternetAccessDAL internetAccessDAL;
        public InterviewRatingSheetDAL InterviewRatingSheetDAL;
        public KSRMUserIdDAL kSRMUserIdDAL;
        public GaneshUserIdDAL ganeshUserIdDAL;
        public DataBackupRestoreDAL dataBackupRestoreDAL;
        public ResourceAccountAndDLDAL resourceAccountAndDLDAL;
        public ITAssetDAL iTAssetDAL;
        public ITClearanceRequestDAL iTClearanceRequestDAL;
        public ConflictOfInterestDAL conflictOfInterestDAL;
        public SharedFolderDAL sharedFolderDAL;
        public ServerRequisitionDAL serverRequisitionDAL;
        public PhotographyPermitDAL PhotographyPermitDAL;
        public BusTransportationDAL BusTransportationDAL;
        public BEIDAL BEIDAL;
        public SFOOrderDAL SFOOrderDAL;
        public IDCardDAL IDCardDAL;
        public ReissueIDCardDAL ReissueIDCardDAL;
        public DoorAccessRequestDAL DoorAccessRequestDAL;
        public MaterialRequestDAL MaterialRequestDAL;
        public SAPUserIdCreationDAL SAPUserCreationDAL;
        public DesktopLaptopInstallationChecklistDAL desktopLaptopInstallationChecklistDAL;
        public GiftsInvitationDAL GiftsInvitationDAL;
        public NewGlobalCodeDAL NewGlobalCodeDAL;
        public UserRequestDAL UserRequestDAL;
        public ISCLSCDAL ISCLSCDAL;
        public AnalysisPartsFormPresentationDAL AnalysisPartsFormPresentation;
        public QualityMeisterbockCubingDAL QualityMeisterbockCubingDAL;
        public IMACDAL IMACDAL;
        public FixtureRequisitionDAL QFRFDAL;
        public MMRFormDAL MMRFDAL;
        public EQSAccessDAL EQSAccessDAL;
        public IPAFFormDAL IPAFDAL;
        public SignupApproverDAL signupApproverDAL;
        public CreateApprovalsDAL createApprovalsDAL;
        public POCRFormDAL POCRFormDAL;
        public DLADDAL DLAD;

        public static List<Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>> Mapping;
        public static bool IsUserLoggedOut;

        public void CreateMapping()
        {
            OCRFDAL = new OCRFDAL();
            CabBookingRequestDAL = new CabBookingRequestDAL();
            DrivingAuthorizationDAL = new DrivingAuthorizationDAL();
            InternalJobPostingDAL = new InternalJobPostingDAL();
            CourierRequestDAL = new CourierRequestDAL();
            DeviationNoteDAL = new DeviationNoteDAL();
            smartPhoneDAL = new SmartPhoneDAL();
            softwareRequisitionDAL = new SoftwareRequisitionDAL();
            internetAccessDAL = new InternetAccessDAL();
            empClrDAL = new EmployeeClearanceDAL();
            InterviewRatingSheetDAL = new InterviewRatingSheetDAL();
            kSRMUserIdDAL = new KSRMUserIdDAL();
            ganeshUserIdDAL = new GaneshUserIdDAL();
            dataBackupRestoreDAL = new DataBackupRestoreDAL();
            resourceAccountAndDLDAL = new ResourceAccountAndDLDAL();
            iTAssetDAL = new ITAssetDAL();
            iTClearanceRequestDAL = new ITClearanceRequestDAL();
            conflictOfInterestDAL = new ConflictOfInterestDAL();
            sharedFolderDAL = new SharedFolderDAL();
            serverRequisitionDAL = new ServerRequisitionDAL();
            PhotographyPermitDAL = new PhotographyPermitDAL();
            BusTransportationDAL = new BusTransportationDAL();
            BEIDAL = new BEIDAL();
            SFOOrderDAL = new SFOOrderDAL();
            IDCardDAL = new IDCardDAL();
            ReissueIDCardDAL = new ReissueIDCardDAL();
            DoorAccessRequestDAL = new DoorAccessRequestDAL();
            MaterialRequestDAL = new MaterialRequestDAL();
            SAPUserCreationDAL = new SAPUserIdCreationDAL();
            desktopLaptopInstallationChecklistDAL = new DesktopLaptopInstallationChecklistDAL();
            GiftsInvitationDAL = new GiftsInvitationDAL();
            NewGlobalCodeDAL = new NewGlobalCodeDAL();
            UserRequestDAL = new UserRequestDAL();
            ISCLSCDAL = new ISCLSCDAL();
            AnalysisPartsFormPresentation = new AnalysisPartsFormPresentationDAL();
            QualityMeisterbockCubingDAL = new QualityMeisterbockCubingDAL();
            MaterialRequestDAL = new MaterialRequestDAL();
            IMACDAL = new IMACDAL();
            QFRFDAL = new FixtureRequisitionDAL();
            MMRFDAL = new MMRFormDAL();
            EQSAccessDAL = new EQSAccessDAL();
            IPAFDAL = new IPAFFormDAL();
            signupApproverDAL = new SignupApproverDAL();
            createApprovalsDAL = new CreateApprovalsDAL();
            POCRFormDAL = new POCRFormDAL();
            DLAD = new DLADDAL();

            listDal = new ListDAL();
            if (Mapping == null)
                Mapping = new List<Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>>();

            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("BEI", "BEIForm.cshtml", BEIDAL.GetBei, BEIDAL.ViewBeiData));//used for linking all form components.   
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SFO", "order.cshtml", null, SFOOrderDAL.ViewForm));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("PAF", "PhotographyPermitForm.cshtml", null, PhotographyPermitDAL.ViewPPFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("IA", "InternetAccess.cshtml", null, internetAccessDAL.GetInternetAccessRequestDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("IDCF", "IDCardForm.cshtml", null, IDCardDAL.ViewIDCFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("ECF", "EmployeeClearanceForm.cshtml", empClrDAL.GetECF, empClrDAL.ViewECFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("RIDCF", "ReissueIDCardForm.cshtml", null, ReissueIDCardDAL.GetReissueIDCardDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SPRF", "SmartPhoneRequisitionForm.cshtml", null, smartPhoneDAL.GetSmartPhoneRequisitionDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SRF", "SoftwareRequisitionForm.cshtml", null, softwareRequisitionDAL.GetSoftwareRequisitionDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("GUICF", "GaneshUserIdCreationForm.cshtml", null, ganeshUserIdDAL.GetGaneshUserIdCreationDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("DARF", "DoorAccessRequestForm.cshtml", null, DoorAccessRequestDAL.GetDoorAccessDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("DBRF", "DataBackupRestoreForm.cshtml", null, dataBackupRestoreDAL.GetDataBackuPRestoreDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("KSRMUICF", "KSRMUserIdCreationForm.cshtml", null, kSRMUserIdDAL.GetKSRMUserIdCreationDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("ITCF", "ITClearanceForm.cshtml", null, iTClearanceRequestDAL.GetITClearanceDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("RADLF", "ResourceAccountAndDistributionListRequisitionForm.cshtml", null, resourceAccountAndDLDAL.GetResourceAccountDLDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("BTF", "BusTransportationForm.cshtml", null, BusTransportationDAL.ViewBusTransportationFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("OCRF", "OCRFForm.cshtml", null, OCRFDAL.ViewORCFFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SFF", "SharedFolderForm.cshtml", null, sharedFolderDAL.GetSharedFolderDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("ITARF", "ITAssetRequisitionForm.cshtml", null, iTAssetDAL.GetITAssetRequisitionDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("CBRF", "CabBookingRequestForm.cshtml", null, CabBookingRequestDAL.ViewCBRFFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("DAF", "DrivingAuthorizationForm.cshtml", null, DrivingAuthorizationDAL.ViewDAFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("COIF", "ConflictOfInterestForm.cshtml", null, conflictOfInterestDAL.GetConflictOfInterestDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("IJPF", "InternalJobPostingForm.cshtml", null, InternalJobPostingDAL.ViewIJPFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("CRF", "CourierRequestForm.cshtml", null, CourierRequestDAL.ViewCourierRequestFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("DNF", "DeviationNoteForm.cshtml", null, DeviationNoteDAL.ViewDeviationNoteFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("IRSF", "InterviewRatingSheetForm.cshtml", null, InterviewRatingSheetDAL.ViewIRSFFData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SRCF", "ServerRequisitionForm.cshtml", null, serverRequisitionDAL.ViewApprovalSRCF));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("MRF", "MaterialRequestForm.cshtml", null, MaterialRequestDAL.GetMaterialRequestDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SUCF", "SAPUserIdCreationForm.cshtml", null, SAPUserCreationDAL.GetSAPUserCreationDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("DLIC", "DesktopLaptopInstallationChecklist.cshtml", null, desktopLaptopInstallationChecklistDAL.GetDLICDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("GAIF", "GiftsInvitation.cshtml", null, GiftsInvitationDAL.GetGiftsInvitationDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("NGCF", "NewGlobalCode.cshtml", null, NewGlobalCodeDAL.GetNewGlobalCodeDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("URCF", "UserRequestForm.cshtml", null, UserRequestDAL.GetURCFDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("ISLS", "ISCLSCForm.cshtml", null, ISCLSCDAL.ViewISCLSCFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("APFP", "AnalysisPartsFormPresentation.cshtml", null, AnalysisPartsFormPresentation.ViewAnalysisPartsFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("QMCR", "QualityMeisterbockCubingform.cshtml", null, QualityMeisterbockCubingDAL.ViewQualityMeisterbockCubingFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("IMAC", "IMACForm.cshtml", null, IMACDAL.GetIMACDetails));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("QFRF", "FixtureRequisitionForm.cshtml", null, QFRFDAL.ViewQFRFFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("MMRF", "MMRForm.cshtml", null, MMRFDAL.ViewMMRFFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("EQSA", "EQSAccessForm.cshtml", null, EQSAccessDAL.ViewEQSAccessFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("IPAF", "IPAFForm.cshtml", null, IPAFDAL.ViewIPAFFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SAF", "SAForm.cshtml", null, signupApproverDAL.ViewSAFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("SPMF", "SinglePageMaster.cshtml", null, IPAFDAL.ViewIPAFFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("CA", "CreateApprovals.cshtml", null, createApprovalsDAL.ViewCreateApprovalsData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("POCRF", "POCRForm.cshtml", null, POCRFormDAL.ViewPOCRFormData));
            Mapping.Add(new Tuple<string, string, Func<Task<dynamic>>, Func<int, int, Task<dynamic>>>("DLAD", "DLADForm.cshtml", null, DLAD.ViewDLADFormData));
        }

        public static Dictionary<string, string> ListNames
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "SFO", "OrderDetails"},
                    { "BEI", "BEIForm"},
                    { "PAF", "PhotographyAuthorizationForm"},
                    { "IA", "InternetAccess"},
                    { "IDCF", "IDCardForm"},
                    { "ECF", "EmployeeClearanceForm"},
                    { "RIDCF", "ReissueIDCardForm"},
                    { "SPRF", "SmartPhoneRequisition"},
                    { "SRF", "SoftwareRequisition"},
                    { "GUICF", "GaneshUserIdCreation"},
                    { "DARF", "DoorAccessRequestForm"},
                    { "DBRF", "DataBackupRestore"},
                    { "KSRMUICF", "KSRMUserIdCreation"},
                    { "ITCF", "ITClearance"},
                    { "BTF", "BusTransportationForm"},
                    { "RADLF", "ResourceAccountAndDistributionList"},
                    { "SFF", "SharedFolder"},
                    { "OCRF", "OCRFForms"},
                    { "ITARF", "ITAssetRequisition"},
                    { "CBRF", "CabBookingRequestForm"},
                    { "DAF", "DrivingAuthorizationForm"},
                    { "COIF", "ConflictOfInterest"},
                    { "IJPF", "InternalJobPosting"},
                    { "CRF", "CourierRequestForm" },
                    { "DNF", "DeviationNoteForm" },
                    { "IRSF", "InterviewRatingSheetForm" },
                    { "SRCF", "ServerRequisitionForm" },
                    { "MRF", "MaterialRequestForm" },
                    { "SUCF", "SAPUserIDForm" },
                    { "DLIC", "DesktopLaptopInstallationCheckListForm" },
                    { "GAIF", "GiftsInvitationForm" },
                    { "NGCF", "NewGlobalCodeForm" },
                    { "URCF", "UserRequestForm" },
                    { "ISLS", "ISCLSCForm" },
                    { "APFP", "AnalysisPartsFormPresentationList" },
                    { "QMCR", "QualityMeisterbockCubingList" },
                    { "IMAC", "IMACForm" },
                    { "QFRF", "FixtureRequisitionForm" },
                    { "MMRF", "MMRForm" },
                    { "EQSA", "EQSAccess" },
                    { "IPAF", "IPAFForm" },
                    { "SAF", "SAFDAL" },
                    { "CA", "CreateApprovalsDAL" },
                    { "POCRF", "POCRForm" },
                    { "DLAD", "ServerLaptopDesktopLocalAdminForm" }
                };
            }
        }

        public static string ApplicationUrl { get; set; }

        public UserData GetCurrentUser()
        {
            if (HttpContext.Current != null)
            {
                var Session = HttpContext.Current.Session;
                if (Session != null && Session["UserData"] != null)
                {
                    var currentUser = (UserData)Session["UserData"];
                    return currentUser;
                }
            }
            return new UserData();
        }

        //public static List<MappingModel> AllFormsMapping
        //{
        //    get { return allFormsMapping; }
        //}

        //public static string GetViewNameByFormname(string formName)
        //{
        //    if (allFormsMapping.Any(x => x.FormName.ToLower() == formName.ToLower()))
        //        return
        //                allFormsMapping.Where(x => x.FormName.ToLower() == formName.ToLower()).FirstOrDefault().ViewName;
        //    else
        //        return "";
        //}

    }
}