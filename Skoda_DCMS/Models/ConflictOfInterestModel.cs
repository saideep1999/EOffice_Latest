﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class ConflictOfInterestModel
    {
        [JsonProperty("d")]
        public ConflictOfInterestResults List { get; set; }
    }

    public partial class ConflictOfInterestResults
    {
        [JsonProperty("results")]
        public List<ConflictOfInterestData> COIList { get; set; }
    }

    public class ConflictOfInterestData
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        [JsonProperty("ExternalOrganizationName")]
        public string ExternalOrganizationName { get; set; }
        [JsonProperty("ExternalOtherOrganizationName")]
        public string ExternalOtherOrganizationName { get; set; }
        [JsonProperty("EmployeeCode")]
        public long EmployeeCode { get; set; }
        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; }
        [JsonProperty("EmployeeDepartment")]
        public string EmployeeDepartment { get; set; }
        [JsonProperty("EmployeeType")]
        public string EmployeeType { get; set; }
        [JsonProperty("EmployeeCCCode")]
        public int EmployeeCCCode { get; set; }

        [JsonProperty("EmployeeUserId")]
        public string EmployeeUserId { get; set; }
        [JsonProperty("EmployeeDesignation")]
        public string EmployeeDesignation { get; set; }//string
        [JsonProperty("EmployeeLocation")]
        public string EmployeeLocation { get; set; }//string
        [JsonProperty("EmployeeContactNo")]
        public string EmployeeContactNo { get; set; }
        [JsonProperty("EmployeeEmailId")]
        public string EmployeeEmailId { get; set; }

        //Other Employee Fields
        [JsonProperty("OnBehalfOption")]
        public string OnBehalfOption { get; set; }
        [JsonProperty("OtherEmployeeType")]
        public string OtherEmployeeType { get; set; }
        [JsonProperty("OtherExternalOrganizationName")]
        public string OtherExternalOrganizationName { get; set; }
        [JsonProperty("OtherExternalOtherOrgName")]
        public string OtherExternalOtherOrganizationName { get; set; }
        [JsonProperty("OtherEmployeeCode")]
        public long OtherEmployeeCode { get; set; }

        [JsonProperty("OtherEmployeeCCCode")]
        public long OtherEmployeeCCCode { get; set; }

        [JsonProperty("OtherEmployeeUserId")]
        public string OtherEmployeeUserId { get; set; }
        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }
        [JsonProperty("OtherEmployeeDepartment")]
        public string OtherEmployeeDepartment { get; set; }
        [JsonProperty("OtherEmployeeDesignation")]
        public string OtherEmployeeDesignation { get; set; }//string
        [JsonProperty("OtherEmployeeLocation")]
        public string OtherEmployeeLocation { get; set; }//string
        [JsonProperty("OtherEmployeeContactNo")]
        public string OtherEmployeeContactNo { get; set; }
        [JsonProperty("OtherEmployeeEmailId")]
        public string OtherEmployeeEmailId { get; set; }
        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }

        [JsonProperty("IsQ1Yes")]
        public string IsQ1Yes { get; set; }
        [JsonProperty("IsQ2Yes")]
        public string IsQ2Yes { get; set; }
        [JsonProperty("IsQ3Yes")]
        public string IsQ3Yes { get; set; }
        [JsonProperty("Elaboration1")]
        public string Elaboration1 { get; set; }
        [JsonProperty("Elaboration2")]
        public string Elaboration2 { get; set; }
        [JsonProperty("Elaboration3")]
        public string Elaboration3 { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        //Add Changes
        //A Question
        [JsonProperty("Chkaq1")]
        public string Chkaq1 { get; set; }

        [JsonProperty("soChk")]
        public string soChk { get; set; }

        [JsonProperty("shareChk")]
        public string shareChk { get; set; }

        [JsonProperty("viaChk")]
        public string viaChk { get; set; }

        [JsonProperty("partnershpChk")]
        public string partnershpChk { get; set; }

        [JsonProperty("ChkOther")]
        public string ChkOther { get; set; }

        [JsonProperty("txtOther")]
        public string txtOther { get; set; }

        [JsonProperty("txtQustionA_1_data")]
        public string txtQustionA_1_data { get; set; }

        [JsonProperty("Chkaq2")]
        public string Chkaq2 { get; set; }

        [JsonProperty("txtQustionA_2_data")]
        public string txtQustionA_2_data { get; set; }

        //B Question

        [JsonProperty("Chkbq1")]
        public string Chkbq1 { get; set; }

        [JsonProperty("txtQustionB_1_data")]
        public string txtQustionB_1_data { get; set; }

        //C Question

        [JsonProperty("Chkcq1")]
        public string Chkcq1 { get; set; }

        [JsonProperty("txtQustionC_1_data")]
        public string txtQustionC_1_data { get; set; }
    }
}