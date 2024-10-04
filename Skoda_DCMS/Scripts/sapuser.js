$(function () {

    highlightActiveTab();

    getSapModuleNames();

    getDesignations("submitter");
    getOrganization("submitter");
    getServiceDeskLocations("submitter");
    $(document).tooltip();

    //debugger;
    getSapUserRequestDetails();


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

    $('input[type=radio][name=EmployeeRequestType]').change(function () {
        //debugger;
        if (this.value == 'Permanent') {
            $('#lblTempFrom').hide();
            $('#txtTempFrom').hide();
            $('#lblTempTo').hide();
            $('#txtTempTo').hide();
        }
        else {
            $('#lblTempFrom').show();
            $('#txtTempFrom').show();
            $('#lblTempTo').show();
            $('#txtTempTo').show();
        }
    });

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


    $('input[type=checkbox][name=RequestFor]').change(function () {

        //Role Authorization Check.
        var requestForArray = [];
        $.each($("input[name='RequestFor']:checked"), function () {
            requestForArray.push($(this).val());
        });

        var selected = requestForArray.join(',');

        // Check if there are selected checkboxes
        if (selected.length > 0 && selected.indexOf("RoleAuth") !== -1) {
            $('#divModule').show();
        }
        else {
            $('#divModule').hide();
        }
    });
});


function getSapUserRequestDetails() {
    $.ajax({
        type: "GET",
        url: "../SAPUserForm/GetSapUserDetails",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            //debugger;
            if (response != null) {
                //alert("Success");
                loadSubmitterEmployeeDetails(response);
                //Role Auth Grid Data                
                loadRoleAuthDetails(response.SAPUserRoleAuthCodeDto, response.SAPUserReqID);
                //While showing the existing submitted requests, disable some of the required fields.
                enableDisableFields(response);
                $('#lblSAPUserRequestId').val(response.SAPUserReqID);
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


function loadSubmitterEmployeeDetails(response) {

    //debugger;

    processSubmitterEmployeeData(response);

    $('input[name="EmployeeRequestType"][value="' + response.EmployeeRequestType + '"]').prop('checked', true);

    if (response.IsSapClientRequired == true) {
        $('input[name="SAPGUI"][value="' + response.IsSapClientRequired + '"]').prop('checked', true);
    }

    $('#txtBusinessFunc').val(response.EmployeeBusinessFunction);

    //Bind SAP User Request For
    //$('#ddModuleName').val(response.EmployeeCCCode);
    var requestFors = response.SAPUserRequestForDto;
    $.each(requestFors, function (i, RequestFor) {
        $('input[name="RequestFor"][value="' + RequestFor.RequestFor + '"]').prop('checked', true);
    });

    //Bind SAP User Modules
    $('#txtSubModule').val(response.EmployeeSubModuleName);
    var modules = response.SAPUserModuleDto;
    if (modules.length != 0) {
        $('#divModule').show();
        $.each(modules, function (i, ModuleName) {
            $("#ddModuleName option[value='" + ModuleName.ModuleName + "']").prop("selected", true);
        });
    }

    $('input[name="RequestSubmissionFor"][value="' + response.RequestSubmissionFor + '"]').prop('checked', true);
    //Other existing employee information
    if (response.IsOnBehalf == true) {
        processOnBehalfEmployeeData(response);
    }

    if (response.EmployeeRequestType == "Temporary") {
        $('#lblTempFrom').show();
        $('#txtTempFrom').show();
        $('#lblTempTo').show();
        $('#txtTempTo').show();

        $('#txtTempFrom').val(ToApplicationDateFormat(response.TempFrom));
        $('#txtTempTo').val(ToApplicationDateFormat(response.TempTo));
    }
}

function enableDisableFields(response) {
    if (response.SAPUserReqID != 0) {
        enableDisableSubmitterEmpFields();

        $('input[name="RequestFor"]').prop("disabled", true);
        $('#ddModuleName').prop("disabled", true);
        $('#txtSubModule').prop("disabled", true);
        $('input[name="SAPGUI"]').prop("disabled", true);
        $('#txtBusinessFunc').prop("disabled", true);

        enableDisableOnBehalfEmpFields(response);

        if (response.EmployeeRequestType == "Temporary") {
            $('#txtTempFrom').prop("disabled", true);
            $('#txtTempTo').prop("disabled", true);
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
}

function loadRoleAuthDetails(userRoleAuthCodes, sapUserRequestId) {
    //debugger;
    var grid = $("#RoleAuthGrid");
    $.extend($.jgrid.inlineEdit, {
        keys: true
    });

    grid.jqGrid
        ({
            //url: "/SAPUser/GetRoleAuthCodeDetails",
            data: userRoleAuthCodes,
            datatype: 'local',
            mtype: 'Get',
            //table header name  
            pager: "#pager2",
            colNames: ['RoleAuthCodeID', 'System', 'Client', 'Role/Autorization/Transaction Code', 'Reason'],
            //colModel takes the data from controller and binds to grid  
            colModel: [
                { name: "RoleAuthCodeID", sorttype: "int", hidden: true, editable: false, editrules: { required: false } },
                { name: "SystemCode", editable: true, editoptions: { maxlength: 10 }, editrules: { required: true }, align: 'center' },
                { name: "ClientCode", editable: true, editoptions: { maxlength: 10 }, editrules: { required: true }, align: 'center' },
                { name: "RoleAuthCode", editable: true, editoptions: { maxlength: 100 }, editrules: { required: true }, align: 'center' },
                { name: "Reason", editable: true, editoptions: { maxlength: 250 }, editrules: { required: true } }
            ],
            height: '100%',
            iconSet: "fontAwesome",
            navOptions: {
                iconsOverText: true,
                addtext: "Add",
                edittext: "Edit",
                deltext: "Delete"
            },

            ignoreCase: true,
            rownumbers: true,
            rowNum: 10,
            rowList: [10, 20, 30],
            viewrecords: true,
            caption: 'Role/Authorization/Transaction Code',
            editurl: '../SAPUserForm/EditSAPUserModulesData',
            emptyrecords: 'No records',
            autowidth: true,
        });

    if (sapUserRequestId == 0) {
        grid.jqGrid('navGrid', '#pager2', { add: false, edit: false, del: false, search: false });
        grid.jqGrid('inlineNav', '#pager2', { addParams: { position: "last" } });
    }

    $(window).on("resize", function () {
        var newWidth = $("#RoleAuthGrid").closest(".ui-jqgrid").parent().width();
        grid.jqGrid("setGridWidth", newWidth, true);
    });
}

function submitSapUserDetails() {


    $("#dialog-confirm").dialog({
        resizable: false,
        height: "auto",
        width: 400,
        modal: true,
        buttons: {
            "Yes": function () {
                $(this).dialog("close");

                //Collect all form details and save it to DB using MVC controller action
                saveAndSubmitSAPUserDetails();
            },

            Cancel: function () {
                $(this).dialog("close");
            }
        }
    });
}

function saveAndSubmitSAPUserDetails() {
    //debugger;
    //Gather all the input data and send it to controller.
    var sapUserRequestDto = {};
    sapUserRequestDto = getSubmitterUIData(sapUserRequestDto);

    sapUserRequestDto.EmployeeSubModuleName = $('#txtSubModule').val();
    sapUserRequestDto.EmployeeBusinessFunction = $('#txtBusinessFunc').val();
    sapUserRequestDto.EmployeeRequestType = $("input[name='EmployeeRequestType']:checked").val();
    sapUserRequestDto.IsSapClientRequired = $("input[name='SAPGUI']:checked").val() == "true" ? true : false;

    if ($("input[name='EmployeeRequestType']:checked").val() == 'Temporary') {
        sapUserRequestDto.TempFrom = $('#txtTempFrom').val();
        sapUserRequestDto.TempTo = $('#txtTempTo').val();
    }

    var modules = [];
    $('#ddModuleName :selected').each(function (i, selected) {
        var module = $(selected).val();
        var sapUserModule = { 'ModuleName': module };
        modules.push(sapUserModule);
    });
    sapUserRequestDto.SAPUserModuleDto = modules;

    var requestFor = [];
    $.each($("input[name='RequestFor']:checked"), function () {
        var request = $(this).val();
        var sapRequestFor = { 'RequestFor': request };
        requestFor.push(sapRequestFor);
    });
    sapUserRequestDto.SAPUserRequestForDto = requestFor;

    //get the role auth code grid data and send it to controller.
    roleAuthCodes = [];
    var roleAuthCodesGridData = $("#RoleAuthGrid").getRowData();
    sapUserRequestDto.SAPUserRoleAuthCodeDto = roleAuthCodesGridData;

    //Other Employee Information
    sapUserRequestDto = getOnBehalfEmpUIData(sapUserRequestDto);

    $.ajax({
        type: "POST",
        url: "../SAPUserForm/CreateSAPUser",
        data: '{sapUserRequestDto: ' + JSON.stringify(sapUserRequestDto) + '}',
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

function cancelSapUserDetails() {
    window.location.href = '../FormLinks/FormLinks/';
}


function buttonFormatter(cellvalue, options, rowObject) {
    return '<button onclick=\"viewDetailStatus();\">View</button>';
}



function SearchEmployee() {
    //alert("search!!!");

    $.ajax({
        type: "GET",
        url: "/Search/GetEmployeeDetails",
        data: '{searchText: ' + JSON.stringify(sapUserRequestDto) + '}',
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



function getSapModuleNames() {
    //debugger;

    $.ajax({
        type: "GET",
        url: "../SAPUserForm/GetSapModuleNames",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        async: false,
        success: function (response) {
            $.each(response, function (i, moduleName) {
                $("#ddModuleName").append('<option value="' + moduleName.ModuleShortName + '">' + moduleName.ModuleFullName + '</option>');
            });
        },
        error: function () {
            //alert('Failed to retrieve modules.');
        }
    });

}

function EnableSearchForNameField() {
    //debugger;

}

function EnableSearchForCostCenterField() {

}

function approveRequest() {
    //debugger;
    var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
        $('#lblSAPUserRequestId').val(),
        $('#txtRejectComments').val());

    approveSelectedRequest(requestDtoList);
}

function rejectRequest() {
    //debugger;
    var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
        $('#lblSAPUserRequestId').val(),
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



