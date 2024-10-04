function highlightActiveTab() {
    //
    //highlight active menu item 
    $("ul.nav").find(".active").removeClass("active");
    var url = window.location;
    $('ul.nav a').filter(function () {
        return this.href == url;
    }).parent().addClass('active');

}

function showHideControls() {
    var extdesignation = "Team Member";

    $('input[type=radio][name=TypeOfEmployee]').change(function () {
        if (this.value == 'Internal') {
            $('#ddEmpOrganization').hide();
            $('#lblExternalOrg').hide();
            $('#divOtherExtOrgName').hide();
            $("#ddEmpDesignation option[value='" + "Select" + "']").prop("selected", true);
            $('#ddEmpDesignation').prop("disabled", false);
        }
        else {
            $('#ddEmpOrganization').show();
            $('#lblExternalOrg').show();
            $('#divOtherExtOrgName').show();
            $("#ddEmpDesignation option[value='" + extdesignation + "']").prop("selected", true);
            $('#ddEmpDesignation').prop("disabled", true);
        }
    });

    //Submitter External Org
    $("#ddEmpOrganization").change(function () {
        var selectedExtOrg = $('option:selected', this).text();
        if (selectedExtOrg == "Other") {
            $('#divOtherExtOrgName').show();
        }
        else {
            $('#divOtherExtOrgName').hide();
        }
    });

    //Onbehalf other existing emp Org
    $("#ddOtherEmpExternalOrgName").change(function () {
        var selectedExtOrg = $('option:selected', this).text();
        if (selectedExtOrg == "Other") {
            $('#divOtherEmpExtOrgName').show();
        }
        else {
            $('#divOtherEmpExtOrgName').hide();
        }
    });

    //Onbehalf New employee Org
    $("#ddOtherNewEmpExternalOrgName").change(function () {
        var selectedExtOrg = $('option:selected', this).text();
        if (selectedExtOrg == "Other") {
            $('#divOtherNewEmpExtOrgName').show();
        }
        else {
            $('#divOtherNewEmpExtOrgName').hide();
        }
    });

    $('input[type=radio][name=TypeOfOtherEmployee]').change(function () {
        if (this.value == 'Internal') {
            $('#ddOtherEmpExternalOrgName').hide();
            $('#lblOtherEmpExternalOrg').hide();
            $('#divOtherEmpExtOrgName').hide();
            $("#ddOtherExistingEmpLevel option[value='" + "Select" + "']").prop("selected", true);
            $('#ddOtherExistingEmpLevel').prop("disabled", false);
        }
        else {
            $('#ddOtherEmpExternalOrgName').show();
            $('#lblOtherEmpExternalOrg').show();
            $('#divOtherEmpExtOrgName').show();
            $("#ddOtherExistingEmpLevel option[value='" + extdesignation + "']").prop("selected", true);
            $('#ddOtherExistingEmpLevel').prop("disabled", true);
        }
    });
    $('input[type=radio][name=TypeOfOtherNewEmployee]').change(function () {
        if (this.value == 'Internal') {
            $('#ddOtherNewEmpExternalOrgName').hide();
            $('#lblOtherNewEmpExternalOrg').hide();
            $('#divOtherNewEmpExtOrgName').hide();
            $("#ddOtherNewEmpLevel option[value='" + "Select" + "']").prop("selected", true);
            $('#ddOtherNewEmpLevel').prop("disabled", false);
        }
        else {
            $('#ddOtherNewEmpExternalOrgName').show();
            $('#lblOtherNewEmpExternalOrg').show();
            $('#divOtherNewEmpExtOrgName').show();
            $("#ddOtherNewEmpLevel option[value='" + extdesignation + "']").prop("selected", true);
            $('#ddOtherNewEmpLevel').prop("disabled", true);
        }
    });

    $('input[type=radio][name=RequestSubmissionFor]').change(function () {
        //
        if (this.value == 'Self') {
            $('input[name="otherEmployeeTypeNewExt"]').prop('checked', false);
            $('#empTypeNewExt').hide();
            $('#otherExistingEmp').hide();
            $('#otherNewEmp').hide();
        }
        else {
            $('#empTypeNewExt').show();
        }
    });
    $('input[type=radio][name=otherEmployeeTypeNewExt]').change(function () {
        //
        if (this.value == 'Existing') {
            $('#otherExistingEmp').show();
            $('#otherNewEmp').hide();

            getDesignations('existingEmp');
            getOrganization('existingEmp');
            getServiceDeskLocations("existingEmp");
        }
        else {
            $('#otherExistingEmp').hide();
            $('#otherNewEmp').show();

            getDesignations('newEmp');
            getOrganization('newEmp');
            getServiceDeskLocations("newEmp");
        }
    });
}



function enableDisableSubmitterEmpFields() {
    $('input[name="TypeOfEmployee"]').prop("disabled", true);
    $("#ddEmpOrganization").prop("disabled", true);
    $("#txtOtherExtOrgName").prop("disabled", true);
    $('#ddEmpDesignation').prop("disabled", true);
    $('#ddEmpLocation').prop("disabled", true);
    $('#txtContactNo').prop("disabled", true);
    $('input[name="RequestSubmissionFor"]').prop("disabled", true);
    $('input[name="requestType"]').prop("disabled", true);
}

function enableDisableOnBehalfEmpFields(response) {
    if (response.IsOnBehalf == true) {
        $('input[name="RequestSubmissionFor"]').prop("disabled", true);
        $('input[name="otherEmployeeTypeNewExt"]').prop("disabled", true);
        if (response.OtherEmployeeType == "Existing") {
            $('input[name="TypeOfOtherEmployee"]').prop("disabled", true);
            $('#txtOtherEmpname').prop("disabled", true);
            $('#txtOtherEmpCCCode').prop("disabled", true);
            $('#txtOtherUserId').prop("disabled", true);
            $('#txtOtherEmpDept').prop("disabled", true);
            $('#txtOtherEmpCode').prop("disabled", true);
            $('#txtOtherEmpEmailId').prop("disabled", true);
            $('#ddOtherExistingEmpLevel').prop("disabled", true);
            $('#ddOtherExistingEmpLocation').prop("disabled", true);
            $('#txtOtherEmpContactNo').prop("disabled", true);
            $('#ddOtherEmpExternalOrgName').prop("disabled", true);
            $('#txtOtherEmpExtOrgName').prop("disabled", true);
        }
        else {
            $('input[name="TypeOfOtherNewEmployee"]').prop("disabled", true);
            $('#txtOtherNewEmpCCCode').prop("disabled", true);
            $('#txtOtherNewEmpname').prop("disabled", true);
            $('#txtOtherNewEmpCode').prop("disabled", true);
            $('#txtOtherNewUserId').prop("disabled", true);
            $('#txtOtherNewEmpDept').prop("disabled", true);
            $('#txtOtherNewEmailId').prop("disabled", true);
            $('#ddOtherNewEmpLevel').prop("disabled", true);
            $('#ddOtherNewEmpLocation').prop("disabled", true);
            $('#txtOtherNewEmpContactNo').prop("disabled", true);
            $('#ddOtherNewEmpExternalOrgName').prop("disabled", true);
            $('#txtNewEmpOtherExtOrgName').prop("disabled", true);
        }
    }
}

function getSubmitterUIData(requestDto) {
    requestDto.EmployeeType = $("input[name='TypeOfEmployee']:checked").val();
    requestDto.EmployeeCode = $('#txtEmpCode').val();

    if ($('#ddEmpOrganization :selected').text() == "Other") {
        requestDto.ExternalOtherOrgName = $('#txtOtherExtOrgName').val();
        requestDto.EmployeeOrg = $('#ddEmpOrganization :selected').val();
    }
    else {
        //requestDto.ExternalOtherOrgName = "";
        requestDto.EmployeeOrg = $('#ddEmpOrganization :selected').val();
    }

    requestDto.EmployeeDesignation = $('#ddEmpDesignation :selected').val();
    requestDto.EmployeeLocation = $('#ddEmpLocation :selected').val();
    requestDto.EmployeeCCCode = $('#txtCCCode').val();
    requestDto.EmployeeUserId = $('#txtUserId').val();
    requestDto.EmployeeName = $('#txtEmpname').val();
    requestDto.EmployeeDept = $('#txtDept').val();
    requestDto.EmployeeContactNo = $('#txtContactNo').val();

    return requestDto;
}

function getOnBehalfEmpUIData(requestDto) {
    requestDto.RequestSubmissionFor = $("input[name='RequestSubmissionFor']:checked").val();
    if ($("input[name='RequestSubmissionFor']:checked").val() == 'Onbehalf') {
        requestDto.IsOnBehalf = true;
        if ($("#empTypeNewExt").is(":visible")) {
            requestDto.OtherEmployeeType = $("input[name='otherEmployeeTypeNewExt']:checked").val();
            if ($("#otherExistingEmp").is(":visible") && requestDto.OtherEmployeeType == "Existing") {
                requestDto.OtherEmployeeName = $('#txtOtherEmpname').val();
                requestDto.OtherEmployeeCC = $('#txtOtherEmpCCCode').val();
                requestDto.OtherEmployeeUserId = $('#txtOtherUserId').val();
                requestDto.OtherEmployeeCode = $('#txtOtherEmpCode').val();
                requestDto.OtherEmployeeDept = $('#txtOtherEmpDept').val();
                requestDto.OtherEmployeePhone = $('#txtOtherEmpContactNo').val();
                requestDto.OtherEmployeeDesignation = $('#ddOtherExistingEmpLevel :selected').val();
                requestDto.OtherEmployeeLocation = $('#ddOtherExistingEmpLocation :selected').val();
                requestDto.OtherEmployeeEmail = $('#txtOtherEmpEmailId').val();
                requestDto.OtherEmpIntExt = $("input[name='TypeOfOtherEmployee']:checked").val() == "Internal" ? true : false;
                requestDto.OtherEmployeeTypeIntExt = $("input[name='TypeOfOtherEmployee']:checked").val();
                if (requestDto.OtherEmpIntExt == false) {
                    if ($('#ddOtherEmpExternalOrgName :selected').text() == "Other") {
                        requestDto.OtherExtEmpOtherOrgName = $('#txtOtherEmpExtOrgName').val();
                        requestDto.OtherEmpExtOrgName = $('#ddOtherEmpExternalOrgName :selected').val();
                    }
                    else {
                        //requestDto.OtherExtEmpOtherOrgName = $('#txtOtherEmpExtOrgName').val();
                        requestDto.OtherEmpExtOrgName = $('#ddOtherEmpExternalOrgName :selected').val();
                    }
                }
            }
            else {
                requestDto.OtherEmployeeName = $('#txtOtherNewEmpname').val();
                requestDto.OtherEmployeeCC = $('#txtOtherNewEmpCCCode').val();
                requestDto.OtherEmployeeUserId = $('#txtOtherNewUserId').val();
                requestDto.OtherEmployeeCode = $('#txtOtherNewEmpCode').val();
                requestDto.OtherEmployeeDept = $('#txtOtherNewEmpDept').val();
                requestDto.OtherEmployeePhone = $('#txtOtherNewEmpContactNo').val();
                requestDto.OtherEmployeeDesignation = $('#ddOtherNewEmpLevel :selected').val();
                requestDto.OtherEmployeeLocation = $('#ddOtherNewEmpLocation :selected').val();
                requestDto.OtherEmployeeEmail = $('#txtOtherNewEmailId').val();
                requestDto.OtherEmpIntExt = $("input[name='TypeOfOtherNewEmployee']:checked").val() == "Internal" ? true : false;
                requestDto.OtherEmployeeTypeIntExt = $("input[name='TypeOfOtherNewEmployee']:checked").val();
                if (requestDto.OtherEmpIntExt == false) {
                    if ($('#ddOtherNewEmpExternalOrgName :selected').text() == "Other") {
                        requestDto.OtherExtEmpOtherOrgName = $('#txtNewEmpOtherExtOrgName').val();
                        requestDto.OtherEmpExtOrgName = $('#ddOtherNewEmpExternalOrgName :selected').val();
                    }
                    else {
                        //requestDto.OtherExtEmpOtherOrgName = $('#txtNewEmpOtherExtOrgName').val();
                        requestDto.OtherEmpExtOrgName = $('#ddOtherNewEmpExternalOrgName :selected').val();
                    }
                }
            }
        }
    }
    else {
        requestDto.IsOnBehalf = false;
    }

    return requestDto;
}

function checkCurrentUserAction(vwMasterRequestId) {
    var action = "";
    $.ajax({
        type: "GET",
        url: "../MyWork/CheckCurrentUserAction",
        data: { requestId: vwMasterRequestId },
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            action = response;
        },
        error: function (response) {
            action = "";
            alert(response.responseText);
        }
    });
    return action;
}

function redirectUser() {
    window.location = '/Dashboard';
    //window.location.href = "@Url.Action('UserRequest', 'Request')";
}

function showErrorSummary(response) {
    var parsedResponse = JSON.parse(response.errors);
    $("#errorSummary").show();
    $('#errorSummaryItems').empty();
    $.each(parsedResponse, function (index, element) {
        if (element.Errors != null && element.Errors.length > 0) {
            $('<li>' + element.Errors[0].ErrorMessage + '</li>').appendTo('#errorSummaryItems');
        }
    });
    window.scrollTo(0, 0);
}

function ToApplicationDateFormat(value) {
    var pattern = /Date\(([^)]+)\)/;
    var results = pattern.exec(value);
    var dt = new Date(parseFloat(results[1]));
    return (dt.getMonth() + 1) + "/" + dt.getDate() + "/" + dt.getFullYear();
}


/*function getDesignations(empTypeDesig)*/ {
    $.ajax({
        type: "GET",
        url: "/List/GetDesignations",
        contentType: "application/json; charset=utf-8",
        datatype: 'json',
        cache: false,
        async: false,
        success: function (response) {
            if (empTypeDesig == 'existingEmp') {
                $("#ddOtherExistingEmpLevel option").remove();
                $("#ddOtherExistingEmpLevel").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, designation) {
                    $("#ddOtherExistingEmpLevel").append('<option value="' + designation.Id + '">' + designation.JobTitle + '</option>');
                });
            }
            else if (empTypeDesig == 'newEmp') {
                $("#ddOtherNewEmpLevel option").remove();
                $("#ddOtherNewEmpLevel").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, designation) {
                    $("#ddOtherNewEmpLevel").append('<option value="' + designation.Id + '">' + designation.JobTitle + '</option>');
                });
            }
            else {
                $("#ddEmpDesignation option").remove();
                $("#ddEmpDesignation").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, designation) {
                    $("#ddEmpDesignation").append('<option value="' + designation.Id + '">' + designation.JobTitle + '</option>');
                });
            }
        },          ////success
        error: function () {
            //alert('Failed to retrieve modules.');
        }
    });
}//getdesignation

function getOrganization(empTypeOrg) {
    $.ajax({
        type: "GET",
        url: "/List/GetOrganization",
        contentType: "application/json; charset=utf-8",
        datatype: 'json',
        cache: false,
        async: false,
        success: function (response) {
            if (empTypeOrg == 'existingEmp') {
                $("#ddOtherEmpExternalOrgName option").remove();
                $("#ddOtherEmpExternalOrgName").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, externalOrganization) {
                    $("#ddOtherEmpExternalOrgName").append('<option value="' + externalOrganization.Id + '">' + externalOrganization.Organization + '</option>');
                });
            }
            else if (empTypeOrg == 'newEmp') {
                $("#ddOtherNewEmpExternalOrgName option").remove();
                $("#ddOtherNewEmpExternalOrgName").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, externalOrganization) {
                    $("#ddOtherNewEmpExternalOrgName").append('<option value="' + externalOrganization.Id + '">' + externalOrganization.Organization + '</option>');
                });
            }
            else {
                $("#ddEmpOrganization option").remove();
                $("#ddEmpOrganization").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, externalOrganization) {
                    $("#ddEmpOrganization").append('<option value="' + externalOrganization.Id + '">' + externalOrganization.Organization + '</option>');
                });
            }
        },
        error: function () {
            //alert('Failed to retrieve modules.');
        }
    });
}

function getServiceDeskLocations(empType) {
    console.log(getServiceDeskLocations.caller);
    $.ajax({
        type: "GET",
        url: "/List/GetServiceDeskLocations",
        contentType: "application/json; charset=utf-8",
        datatype: 'json',
        cache: false,
        async: false,
        success: function (response) {
            //console.log(empType);            
            if (empType == 'existingEmp') {
                $("#ddOtherExistingEmpLocation option").remove();
                $("#ddOtherExistingEmpLocation").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, location) {
                    $("#ddOtherExistingEmpLocation").append('<option value="' + location.Id + '">' + location.LocationName + '</option>');
                });
            }
            else if (empType == 'newEmp') {
                $("#ddOtherNewEmpLocation option").remove();
                $("#ddOtherNewEmpLocation").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, location) {
                    $("#ddOtherNewEmpLocation").append('<option value="' + location.Id + '">' + location.LocationName + '</option>');
                });
            }
            else {
                //console.log('creation');
                $("#ddEmpLocation option").remove();
                $("#ddEmpLocation").append('<option value="' + "0" + '">' + '--Select--' + '</option>');
                $.each(response, function (i, location) {
                    $("#ddEmpLocation").append('<option value="' + location.Id + '">' + location.LocationName + '</option>');
                });
            }
        },
        error: function () {
            //alert('Failed to retrieve modules.');
        }
    });
}

function checkComments(comments) {
    if (comments == "") {
        $("#errorSummary").show();
        $('#errorSummaryItems').empty();

        $('<li>' + 'Comments are mandatory.' + '</li>').appendTo('#errorSummaryItems');

        //Set the focus on top of the page.
        window.scrollTo(0, 0);

        return true;
    }
    else {
        return false;
    }
}

function createRequestDtoList(vwMaterRequestId, formRequestId, comments) {
    var requestDtoList = [];
    var requestDto = {
        'VWMasterRequestID': vwMaterRequestId,
        'LinkedFormRequestID': formRequestId,
        'FormID': '',
        'StatusID': '',
        'ApprovalID': '',
        'EmployeeUserId': '',
        'UserName': '',
        'StartDate': '',
        'FormName': '',
        'Comments': comments
    };
    requestDtoList.push(requestDto);
    return requestDtoList;
}



function redirectToMyWork() {
    window.location = '../MyWork/MyWork';
}

Date.prototype.workingDaysFrom = function (fromDate) {
    // ensure that the argument is a valid and past date
    if (!fromDate || isNaN(fromDate) || this < fromDate) {
        return -1;
    }

    // clone date to avoid messing up original date and time
    var clonedFromDate = new Date(fromDate.getTime()),
        clonedToDate = new Date(this.getTime()),
        numOfWorkingDays = 1;

    // reset time portion
    clonedFromDate.setHours(0, 0, 0, 0);
    clonedToDate.setHours(0, 0, 0, 0);

    //var holidays = getHolidays();
    //var holidayDates = [];
    //$.each(holidays, function (i, holiday) {
    //    holidayDates.push(ToApplicationDateFormat(holiday.HolidayDate));
    //});

    while (clonedFromDate < clonedToDate) {
        clonedFromDate.setDate(clonedFromDate.getDate() + 1);
        var day = clonedFromDate.getDay();
        //var isHoliday = isInArray(holidayDates, clonedFromDate);
        if (day != 0 && day != 6) {
            numOfWorkingDays++;
        }
    }
    return numOfWorkingDays;
};

function GetSelectedRequestDetails(gridObject) {
    //   
    selRowId = gridObject.jqGrid('getGridParam', 'selrow'),

        requestDto = {};

    requestDto.VWMasterRequestID = gridObject.jqGrid('getCell', selRowId, 'VWMasterRequestID');
    requestDto.LinkedFormRequestID = gridObject.jqGrid('getCell', selRowId, 'LinkedFormRequestID');
    requestDto.FormID = gridObject.jqGrid('getCell', selRowId, 'FormID');
    requestDto.StatusID = gridObject.jqGrid('getCell', selRowId, 'StatusID');
    requestDto.ApprovalID = gridObject.jqGrid('getCell', selRowId, 'ApprovalID');
    requestDto.EmployeeNumber = gridObject.jqGrid('getCell', selRowId, 'EmployeeNumber');
    requestDto.UserName = gridObject.jqGrid('getCell', selRowId, 'UserName');
    requestDto.StartDate = gridObject.jqGrid('getCell', selRowId, 'StartDate');
    requestDto.FormName = gridObject.jqGrid('getCell', selRowId, 'FormName');

    return requestDto;
}

function getApprovalWorkflowStatusData(requestDto, workflowElement) {
    if (requestDto != undefined || requestDto != null) {
        $.ajax({
            type: "GET",
            url: "../Request/GetRequestWorkflow",
            dataType: "json",
            data: { formId: requestDto.FormID, requestId: requestDto.VWMasterRequestID, employeeNumber: requestDto.EmployeeNumber },
            contentType: "application/json; charset=utf-8",
            cache: false,
            async: false,
            success: function (response) {
                //
                //Navigate user to request list page. 
                if (response == "SelectRow") {
                    $("#selectRow-message").dialog({
                        modal: true,
                        buttons: {
                            Ok: function () {
                                $(this).dialog("close");
                            }
                        }
                    });
                }
                else if (response == "NoApprovals") {
                    $("#NoApproval-message").dialog({
                        modal: true,
                        buttons: {
                            Ok: function () {
                                $(this).dialog("close");
                            }
                        }
                    });
                }
                else {
                    showWorkflowMap(response, workflowElement);
                }
            },
            error: function (response) {
                //
                alert(response.responseText);
            }
        });
    }
}

function showWorkflowMap(response, workflowElement) {
    workflowElement.empty();
    var rowAdded = false;
    $.each(response, function (key, approvalItem) {
        if (key > 2) {
            if (rowAdded == false) {
                workflowElement.append('<div class="row">');
                rowAdded = true;
            }
        }
        if (approvalItem.status == "Approved") {
            workflowElement.append('<div class="form-group col-lg-3"><button type="button" class="btn btn-success btn-wrap-text btn-block">' +
                approvalItem.ApprovalDesc + '<br/>' + "Approved By: " +
                approvalItem.ApproverName + '<br/>' + "Approval Level: " +
                approvalItem.ApprovalLevel + '<br/>' + "Approved On: " +
                ToApplicationDateFormat(approvalItem.UpdatedDate) + '<br/>' +
                '</button></div>'
            );
        }
        if (approvalItem.status == "Rejected") {
            workflowElement.append('<div class="form-group col-lg-3"><button type="button" class="btn btn-danger btn-wrap-text btn-block">' +
                approvalItem.ApprovalDesc + '<br/>' +
                "Rejected By: " + approvalItem.ApproverName + '<br/>'
                + "Approval Level: " + approvalItem.ApprovalLevel + '<br/>'
                + "Rejected On: " + ToApplicationDateFormat(approvalItem.UpdatedDate) + '<br/>' +
                '</button></div>'
            );
        }
        if (approvalItem.status == "Submitted") {
            workflowElement.append('<div class="form-group col-lg-3"><button type="button" class="btn btn-pendingAction btn-wrap-text btn-block">' +
                approvalItem.ApprovalDesc + '<br/>' +
                "Pending With: " + approvalItem.ApproverName + '<br/>' +
                "Approval Level: " + approvalItem.ApprovalLevel + '<br/>' +
                '</button></div>'
            );
        }
        if (approvalItem.status == "Cancelled") {
            workflowElement.append('<div class="form-group col-lg-3"><button type="button" class="btn btn-primary btn-wrap-text btn-block disabled">' +
                approvalItem.ApprovalDesc + '<br/>' +
                "Pending With: " + approvalItem.ApproverName + '<br/>' +
                "Approval Level: " + approvalItem.ApprovalLevel + '<br/>' +
                '</button></div>'
            );
        }
        if (approvalItem.status == "Forwarded") {
            workflowElement.append('<div class="form-group col-lg-3"><button type="button" class="btn btn-primary btn-wrap-text btn-block disabled">' +
                approvalItem.ApprovalDesc + '<br/>' +
                "Status: " + "Forwarded" + " Approval Level: " + approvalItem.ApprovalLevel + '<br/>' +
                "Forwarded By: " + approvalItem.ApproverName + '<br/>' +
                "Forwarded On: " + ToApplicationDateFormat(approvalItem.UpdatedDate) + '<br/>' +
                '</button></div>'
            );
        }

        if (key < response.length - 1) {
            workflowElement.append('&nbsp;<div class="form-group col-lg-1 centerAlign"><img src="../Content/themes/base/images/NextStepSymbol.png" alt="nextstep" class="nextStepSymbol img-responsive"/> </div>');
        }
        else {
            workflowElement.append('</div>') //new row closed key>3 above

            workflowElement.append('<br/>');
            workflowElement.append('<div class="row">')
            workflowElement.append('<div class="form-group col-lg-12"><label id="lblWorkflowNote" class="control-label">Note:- Only one approval is required from same approval level</label>' + '</div>');
            workflowElement.append('</div>')
        }
    });
}