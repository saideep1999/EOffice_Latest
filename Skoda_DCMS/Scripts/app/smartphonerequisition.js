$(function () {
    //debugger;
    highlightActiveTab();

    getDesignations("submitter");
    getOrganization("submitter");
    getServiceDeskLocations("submitter");

    $(document).tooltip();

    GetSmartPhoneRequisitionRequestDetails();

    showHideControls();

    //Other Existing Employee Name textbox Autocomplete
    $("#txtOtherEmpname").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "../List/GetEmployeeDetails",
                dataType: "json",
                cache: false,
                async: false,
                data: {
                    searchText: $("#txtOtherEmpname").val()
                },
                success: function (data) {
                    //debugger;
                    response($.map(data, function (item) {
                        return { label: item.DisplayName, value: item.DisplayName, raw: item };
                    }));
                }
            });
        },
        search: function (event, ui) {
            var value = $("#txtOtherEmpname").val();
            // If less than three chars, cancel the search event
            if (value.length < 3) {
                event.preventDefault();
            }
        },
        min_length: 3,
        delay: 0,
        select: function (event, ui) {
            //debugger;
            var empOtherDetails = ui.item['raw'];

            $('#txtOtherUserId').val(empOtherDetails.VWUserID);
            $('#txtOtherEmpDept').val(empOtherDetails.EmployeeDept);
            $('#txtOtherEmpEmailId').val(empOtherDetails.Email);

            //Get the other details like Cost Center Code, Employee Number from DB.
            $.ajax({
                url: "../List/GetExistingEmployeeDetails",
                dataType: "json",
                cache: false,
                async: false,
                data: {
                    otherEmpUserId: empOtherDetails.VWUserID,
                    otherEmpEmail: empOtherDetails.Email
                },
                success: function (data) {
                    $('#txtOtherEmpCCCode').val(data.EmployeeCCCode);
                    $('#txtOtherEmpCode').val(data.EmployeeNumber);
                    $('#txtOtherEmpDept').val(data.EmployeeDept);
                }

            });

        }

    });

    //Other New Employee Cost Center Number textbox Autocomplete
    $("#txtOtherNewEmpCCCode").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "../List/GetCostCenterDetails",
                dataType: "json",
                cache: false,
                async: false,
                data: {
                    searchText: $("#txtOtherNewEmpCCCode").val()
                },
                success: function (data) {
                    response($.map(data, function (item) {
                        return { label: item.CostCenter, value: item.CostCenter };
                    }))
                }
            });
        },
        search: function (event, ui) {
            //$(this).addClass('loader');
            var value = $("#txtOtherNewEmpCCCode").val();
            // If less than three chars, cancel the search event
            if (value.length < 3) {
                event.preventDefault();
            }
        },
        response: function (e, u) {
            //$(this).removeClass('loader');
        },
        min_length: 3,
        delay: 0
    });

    $("#txtTempFrom").datepicker({
        minDate: 0,
        readonly: true,
        onSelect: function (selected) {
            var dt = new Date(selected);
            dt.setDate(dt.getDate() + 1);
            $("#txtTempTo").datepicker("option", "minDate", dt);
        }
    });
    $("#txtTempTo").datepicker({
        readonly: true,
        onSelect: function (selected) {
            var dt = new Date(selected);
            dt.setDate(dt.getDate() - 1);
            $("#txtTempFrom").datepicker("option", "maxDate", dt);
        }
    });
});

function GetSmartPhoneRequisitionRequestDetails() {
    //debugger;
    $.ajax({
        type: "GET",
        url: "../SmartPhoneRequisitionForm/GetSmartPhoneDetails",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            //debugger;
            if (response != null) {
                //alert("Success");
                loadFormData(response);

                //While showing the existing submitted requests, disable some of the required fields.
                enableDisableFields(response);

                $('#lblSmartPhoneRequestId').val(response.SmartphoneReqID);
                $('#lblVWMasterRequestId').val(response.VWMasterRequestID);
            }
        },
        failure: function (response) {
            //debugger;
            alert(response.responseText);
        },
        error: function (response) {
            //debugger;
            alert(response.responseText);
        }
    });
}

function SearchEmployee() {
    //alert("search!!!");
    $.ajax({
        type: "GET",
        url: "/List/GetEmployeeDetails",
        data: '{searchText: ' + JSON.stringify(smartPhoneRequisitionDto) + '}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            //debugger;
            $("#dialog-message").dialog({
                modal: true,
                buttons: {
                    Ok: function () {
                        $(this).dialog("close");
                    }
                }
            });
        },

        failure: function (response) {
            //debugger;
            alert(response.responseText);
        },

        error: function (response) {
            //debugger;
            alert(response.responseText);
        }
    });
}
function loadSubmitterDetails(response) {
    processSubmitterEmployeeData(response);

    $('#txtBusinessFunction').val(response.EmployeeBusinessFunction);

    var requestFors = response.SAPUserRequestForDto;
    $.each(requestFors, function (i, RequestFor) {
        $('input[name="RequestFor"][value="' + RequestFor.RequestFor + '"]').prop('checked', true);
    });

    $('input[name="RequestSubmissionFor"][value="' + response.RequestSubmissionFor + '"]').prop('checked', true);
    //Other exixting employee information
    if (response.IsOnBehalf == true) {
        processOnBehalfEmployeeData(response);
    }
}
//function getDesignations(empType) {
//    $.ajax({
//        type: "GET",
//        url: "../List/GetDesignations",
//        contentType: "application/json; charset=utf-8",
//        datatype: 'json',
//        cache: false,
//        async: false,
//        success: function (response) {
//            if (empType == 'existingEmp') {
//                $("#ddOtherExistingEmpLevel option").remove();
//                $("#ddOtherExistingEmpLevel").append('<option value="' + "Select" + '">' + '--Select--' + '</option>');
//                $.each(response, function (i, designation) {
//                    $("#ddOtherExistingEmpLevel").append('<option value="' + designation.JobTitle + '">' + designation.JobTitle + '</option>');
//                });
//            }
//            else if (empType == 'newEmp') {
//                $("#ddOtherNewEmpLevel option").remove();
//                $("#ddOtherNewEmpLevel").append('<option value="' + "Select" + '">' + '--Select--' + '</option>');
//                $.each(response, function (i, designation) {
//                    $("#ddOtherNewEmpLevel").append('<option value="' + designation.JobTitle + '">' + designation.JobTitle + '</option>');
//                });
//            }
//            else {

//                $("#ddEmpDesignation option").remove();
//                $("#ddEmpDesignation").append('<option value="' + "Select" + '">' + '--Select--' + '</option>');
//                $.each(response, function (i, designation) {
//                    $("#ddEmpDesignation").append('<option value="' + designation.JobTitle + '">' + designation.JobTitle + '</option>');
//                });
//            }
//        },          ////success
//        error: function () {
//            //alert('Failed to retrieve modules.');
//        }
//    });
//}

function cancelSmartPhoneRequisitionForm() {
    window.location.href = '../FormLinks/FormLinks/';
}

function loadFormData(response) {
    processSubmitterEmployeeData(response);

    $('input[name="RequestSubmissionFor"][value="' + response.RequestSubmissionFor + '"]').prop('checked', true);
    //Other existing employee information
    if (response.IsOnBehalf == true) {
        processOnBehalfEmployeeData(response);
    }
    $('#txtBusinessFunction').val(response.BusinessNeed);
}

function enableDisableFields(response) {
    if (response.SmartphoneReqID != 0) {
        enableDisableSubmitterEmpFields();

        $('input[name="RequestFor"]').prop("disabled", true);
        $('#txtBusinessNeed').prop("disabled", true);

        enableDisableOnBehalfEmpFields(response);
    }
    //Disable submit button as request is already submitted and Approvals are in progress.
    //User should not be able to change it further.
    if (response.EmployeeRequestStatus == "InProgress" || response.EmployeeRequestStatus == "Submitted"
        || response.EmployeeRequestStatus == "Rejected" || response.EmployeeRequestStatus == "Completed"
        || response.EmployeeRequestStatus == "Cancelled") {
        $('#btnSubmit').hide();

        var currentUserAction = checkCurrentUserAction(response.VWMasterRequestID);

        if (response.IsItFromMyWorkPage && currentUserAction == "Submitted") {
            $("#btnApprove").show();
            $("#btnReject").show();
            $("#divRejectionComments").show();
        }
    }
}

function submitSmartPhoneRequisitionRequestDetails() {
    //debugger;
    $("#dialog-confirm").dialog({
        resizable: false,
        height: "auto",
        width: 400,
        modal: true,
        buttons: {
            "Yes": function () {
                $(this).dialog("close");

                //Collect all form details and save it to DB using MVC controller action
                saveAndSubmitSmartPhoneRequisitionRequestDetails();
            },

            Cancel: function () {
                $(this).dialog("close");
            }
        }
    });
}

function saveAndSubmitSmartPhoneRequisitionRequestDetails() {
    //debugger;
    //Gather all the input data and send it to controller.
    var smartPhoneRequisitionDto = {};
    smartPhoneRequisitionDto = getSubmitterUIData(smartPhoneRequisitionDto);

    smartPhoneRequisitionDto.BusinessNeed = $('#txtBusinessFunction').val();

    //Other Employee Information
    smartPhoneRequisitionDto = getOnBehalfEmpUIData(smartPhoneRequisitionDto);

    $.ajax({
        type: "POST",
        url: "../List/CreateSmartPhoneRequisitionRequest",
        data: '{smartPhoneRequisitionDto: ' + JSON.stringify(smartPhoneRequisitionDto) + '}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            //debugger;
            if (response.success != false) {
                $('#btnSubmit').hide();
                $('#errorSummaryItems').empty();
                $("#errorSummary").hide();

                $("#dialog-message").dialog({
                    modal: true,
                    buttons: {
                        Ok: function () {
                            $(this).dialog("close");

                            redirectUser();
                        }
                    }
                });
            }
            else {
                showErrorSummary(response);
            }
        },

        failure: function (response) {
            //debugger;
            alert(response.responseText);
        },

        error: function (response) {
            showErrorSummary(response);
        }
    });
}

function approveRequest() {
    //debugger;
    var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
        $('#lblSmartPhoneRequestId').val(),
        $('#txtRejectComments').val());

    approveSelectedRequest(requestDtoList);
}

function rejectRequest() {
    //debugger;
    var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
        $('#lblSmartPhoneRequestId').val(),
        $('#txtRejectComments').val());
    //Check if comments are missing.   
    var commentsMissing = checkComments(requestDtoList[0].Comments);
    if (commentsMissing == true) {
        return;
    }
    else {
        $('#errorSummaryItems').empty();
        $("#errorSummary").hide();
    }

    rejectSelectedRequest(requestDtoList);;
}

