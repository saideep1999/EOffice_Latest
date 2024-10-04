$(function () {
    getDesignations("submitter");
    getOrganization("submitter");
    getServiceDeskLocations("submitter");
    GetSoftwareRequisitionRequestDetails();

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
        //
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
                    //
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
            //
            var empOtherDetails = ui.item['raw'];

            $('#txtOtherUserId').val(empOtherDetails.VWUserID);
            $('#txtOtherEmpDept').val(empOtherDetails.EmployeeDept);
            $('#txtOtherEmpEmailId').val(empOtherDetails.Email);

            //Get the other details like Cost Center Code, Employee Number from DB.
            $.ajax({
                url: "../Search/GetExistingEmployeeDetails",
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

    //Add Selected softwares to Selected Software List Box
    $('#btnRight').click(function (e) {
        var selectedOpts = $('#lstAvailableSoftwares option:selected');
        if (selectedOpts.length == 0) {
            //alert("Nothing to move.");
            e.preventDefault();
        }

        $('#lstSelectedSoftwares').append($(selectedOpts).clone());
        $(selectedOpts).remove();
        e.preventDefault();
    });

    $('#btnLeft').click(function (e) {
        var selectedOpts = $('#lstSelectedSoftwares option:selected');
        if (selectedOpts.length == 0) {
            //alert("Nothing to move.");
            e.preventDefault();

        }

        $('#lstAvailableSoftwares').append($(selectedOpts).clone());
        $(selectedOpts).remove();
        e.preventDefault();
    });
});

function searchAvailableSoftware() {
    //
    var searchText = $("#txtSearchSoftware").val();
    var softwareType = $("input[name='SoftwareRequestType']:checked").val();

    if (softwareType == undefined) {
        $("#software-type-message").dialog({
            modal: true,
            buttons: {
                Ok: function () {
                    $(this).dialog("close");
                }
            }
        });
        return;
    }

    $.ajax({
        type: "GET",
        url: "../List/GetAvailableSoftwares",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        cache: false,
        async: false,
        data: {
            softwareType: softwareType,
            searchText: searchText
        },
        success: function (response) {
            if (response != null) {
                $("#lstAvailableSoftwares").empty();
                $.each(response, function (i, softwareDetails) {
                    $("#lstAvailableSoftwares").append('<option value="' + softwareDetails.Name + "$" + softwareDetails.Version + "$" + softwareType + '">' + softwareDetails.Name + '</option>');
                });
            }
        },
        error: function () {
            //alert('Failed to retrieve modules.');
        }
    });
}

function GetSoftwareRequisitionRequestDetails() {

    $.ajax({
        type: "GET",
        url: "../List/GetSoftwareRequisitionRequestDetails",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            //
            if (response != null) {
                //alert("Success");
                if (response.SoftwareReqID != 0) {
                    if (response.EmployeeRequestStatus.toUpperCase() == "INPROGRESS" || response.EmployeeRequestStatus.toUpperCase() == "SUBMITTED") {
                        if (response.IsItFromMyWorkPage) {
                            $("#btnApprove").show();
                            $("#btnReject").show();
                            $("#divRejectionComments").show();
                        }
                    }

                    $("#userSelectedSoftwareDivViewOnly").show();
                    $("#softwareTypeDiv").hide();
                    $("#selectSoftwareDiv").hide();
                    $("#searchSoftwareDiv").hide();

                    $('#lblSoftwareReqRequestId').val(response.SoftwareReqID);
                    $('#lblVWMasterRequestId').val(response.VWMasterRequestID);
                }
                else {
                    $("#userSelectedSoftwareDivViewOnly").hide();
                    $("#softwareTypeDiv").show();
                    $("#selectSoftwareDiv").show();
                    $("#searchSoftwareDiv").show();
                    $("#divOtherSoftwareNote").show();
                    $("#OtherSoftwareDiv1").show();
                    $("#OtherSoftwareDiv2").show();
                    $("#OtherSoftwareDiv3").show();
                    $("#OtherSoftwareDiv4").show();
                    //$("#OtherSoftwareDiv5").show();
                }

                processSubmitterEmployeeData(response);

                $('input[name="EmployeeRequestType"][value="' + response.EmployeeRequestType + '"]').prop('checked', true);

                if (response.EmployeeRequestType == "Temporary") {
                    $('#lblTempFrom').show();
                    $('#txtTempFrom').show();
                    $('#lblTempTo').show();
                    $('#txtTempTo').show();

                    $('#txtTempFrom').val(ToApplicationDateFormat(response.TempFrom));
                    $('#txtTempTo').val(ToApplicationDateFormat(response.TempTo));
                }

                $('input[name="RequestSubmissionFor"][value="' + response.RequestSubmissionFor + '"]').prop('checked', true);
                //Other existing employee information
                if (response.IsOnBehalf == true) {
                    processOnBehalfEmployeeData(response);
                }

                $('#txtBusinessNeed').val(response.BusinessNeed);
                $('input[name="SoftwareRequestType"][value="' + response.SoftwareType + '"]').prop('checked', true);

                //Show the selected softwares
                showSelectedSoftwares(response);

                //While showing the existing submitted requests, disable some of the required fields.
                enableDisableFields(response);
            }
        },
        error: function (response) {
            //
            alert(response.responseText);
        }
    });
}

function showSelectedSoftwares(response) {
    var requestedSoftwares = response.SelectedSoftwaresDto;
    if (requestedSoftwares != null) {
        //$.each(selectedSoftwares, function (i, softwareDetails) {
        //    if (softwareDetails.IsOtherSoftware == false) {
        //        $("#lstSelectedSoftwaresViewOnly").append('<option value="' + softwareDetails.SoftwareName + "$" + softwareDetails.SoftwareVersion + "$" + response.SoftwareType + '">' + softwareDetails.SoftwareName + '</option>');
        //        $("#lstSelectedSoftwares").append('<option value="' + softwareDetails.SoftwareName + "$" + softwareDetails.SoftwareVersion + "$" + response.SoftwareType + '">' + softwareDetails.SoftwareName + '</option>');
        //    }            
        //});

        showRequetsedSoftwares(requestedSoftwares);
    }
}

function showRequetsedSoftwares(requestedSoftwares) {

    var requestedSoftwareGrid = $("#requestedSoftwareGrid");

    requestedSoftwareGrid.jqGrid
        ({

            data: requestedSoftwares,
            datatype: 'local',
            mtype: 'GET',
            pager: "#requestedSoftwareGridPager",
            cache: false,
            colNames: ['Software Name', 'Software Version', 'Software Type'],
            //colModel takes the data from controller and binds to grid  
            colModel: [
                { name: "SoftwareName", align: 'center' },
                { name: "SoftwareVersion", align: 'center' },
                { name: "SoftwareType", align: 'center' }
            ],
            height: '100%',
            width: '100%',
            ignoreCase: true,
            rownumbers: true,
            rowNum: 10,
            rowList: [10, 20, 30, 40],
            viewrecords: true,
            emptyrecords: 'No records found.',
            autowidth: true,
            ajaxGridOptions: { cache: false }
        });

    requestedSoftwareGrid.jqGrid('navGrid', '#requestedSoftwareGridPager', { add: false, edit: false, del: false, search: false });
}

function enableDisableFields(response) {
    if (response.SoftwareReqID != 0) {
        enableDisableSubmitterEmpFields();

        $('input[name="EmployeeRequestType"]').prop("disabled", true);
        $('input[name="RequestFor"]').prop("disabled", true);

        if (!response.IsItFromMyWorkPage) {
            $('input[name="SoftwareRequestType"]').prop("disabled", true);
        }
        $('#txtMoreInformation').prop("disabled", true);
        $('#txtBusinessNeed').prop("disabled", true);

        enableDisableOnBehalfEmpFields(response);

        if (response.EmployeeRequestType == "Temporary") {
            $('#txtTempFrom').prop("disabled", true);
            $('#txtTempTo').prop("disabled", true);
        }

        //Disable submit button if request status is completed or rejected.
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

function submitSoftwareRequisitionRequest() {
    $("#dialog-confirm").dialog({
        resizable: false,
        height: "auto",
        width: 400,
        modal: true,
        buttons: {
            "Yes": function () {
                $(this).dialog("close");

                //Collect all form details and save it to DB using MVC controller action
                saveAndsubmitSoftwareRequisitionRequest();
            },

            Cancel: function () {
                $(this).dialog("close");
            }
        }
    });
}

function saveAndsubmitSoftwareRequisitionRequest() {
    //
    var softwareRequisitionRequestDto = {};
    softwareRequisitionRequestDto.SoftwareReqID = $('#lblSoftwareReqRequestId').val();
    softwareRequisitionRequestDto = getSubmitterUIData(softwareRequisitionRequestDto);

    softwareRequisitionRequestDto.EmployeeRequestType = $("input[name='EmployeeRequestType']:checked").val();

    if ($("input[name='EmployeeRequestType']:checked").val() == 'Temporary') {
        softwareRequisitionRequestDto.TempFrom = $('#txtTempFrom').val();
        softwareRequisitionRequestDto.TempTo = $('#txtTempTo').val();
    }

    softwareRequisitionRequestDto.BusinessNeed = $('#txtBusinessNeed').val();
    softwareRequisitionRequestDto.SoftwareType = $("input[name='SoftwareRequestType']:checked").val();

    var selectedSoftwares = [];

    $('#lstSelectedSoftwares option').each(function () {
        var softwareDetails = $(this).val().split("$");
        var selectedSoftwareName = {
            'SoftwareName': softwareDetails[0],
            'SoftwareVersion': softwareDetails[1],
            'SoftwareType': softwareDetails[2],
            'IsOtherSoftware': 'false'
        };
        selectedSoftwares.push(selectedSoftwareName);
    });

    //Other Software details
    var selectedSoftwareName = {
        'SoftwareName': $('#txtOtherSoftware1').val(),
        'SoftwareVersion': $('#txtOtherSoftwareVersion1').val(),
        'SoftwareType': $('#ddOtherSoftwareType1 :selected').val(),
        'IsOtherSoftware': 'true'
    };
    if ($('#txtOtherSoftware1').val() != "") {
        softwareRequisitionRequestDto.SoftwareType = $('#ddOtherSoftwareType1 :selected').val();
        selectedSoftwares.push(selectedSoftwareName);
    }

    selectedSoftwareName = {
        'SoftwareName': $('#txtOtherSoftware2').val(),
        'SoftwareVersion': $('#txtOtherSoftwareVersion2').val(),
        'SoftwareType': $('#ddOtherSoftwareType2 :selected').val(),
        'IsOtherSoftware': 'true'
    };
    if ($('#txtOtherSoftware2').val() != "") {
        softwareRequisitionRequestDto.SoftwareType = $('#ddOtherSoftwareType2 :selected').val();
        selectedSoftwares.push(selectedSoftwareName);
    }

    selectedSoftwareName = {
        'SoftwareName': $('#txtOtherSoftware3').val(),
        'SoftwareVersion': $('#txtOtherSoftwareVersion3').val(),
        'SoftwareType': $('#ddOtherSoftwareType3 :selected').val(),
        'IsOtherSoftware': 'true'
    };
    if ($('#txtOtherSoftware3').val() != "") {
        softwareRequisitionRequestDto.SoftwareType = $('#ddOtherSoftwareType3 :selected').val();
        selectedSoftwares.push(selectedSoftwareName);
    }

    selectedSoftwareName = {
        'SoftwareName': $('#txtOtherSoftware4').val(),
        'SoftwareVersion': $('#txtOtherSoftwareVersion4').val(),
        'SoftwareType': $('#ddOtherSoftwareType4 :selected').val(),
        'IsOtherSoftware': 'true'
    };
    if ($('#txtOtherSoftware4').val() != "") {
        softwareRequisitionRequestDto.SoftwareType = $('#ddOtherSoftwareType4 :selected').val();
        selectedSoftwares.push(selectedSoftwareName);
    }

    //selectedSoftwareName = {
    //    'SoftwareName': $('#txtOtherSoftware5').val(),
    //    'SoftwareVersion': $('#txtOtherSoftwareVersion5').val(),
    //    'SoftwareType': $('#ddOtherSoftwareType5 :selected').val(),
    //    'IsOtherSoftware': 'true'
    //};
    //if ($('#txtOtherSoftware5').val() != "") {
    //    selectedSoftwares.push(selectedSoftwareName);
    //}

    //split the comma separated software list.
    //if (otherSelectedSoftwares != "") {
    //    var otherSelectedSoftArray = otherSelectedSoftwares.split(',');
    //    var i;
    //    for (i = 0; i < otherSelectedSoftArray.length; ++i) {            
    //    }   
    //}
    softwareRequisitionRequestDto.SelectedSoftwaresDto = selectedSoftwares;

    //Other Employee Information
    softwareRequisitionRequestDto = getOnBehalfEmpUIData(softwareRequisitionRequestDto);

    $.ajax({
        type: "POST",
        url: "../List/CreateSoftwareRequisitionRequest",
        data: '{softwareRequisitionRequestDto: ' + JSON.stringify(softwareRequisitionRequestDto) + '}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        async: false,
        success: function (response) {
            //
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
            //
            alert(response.responseText);
        },

        error: function (response) {
            showErrorSummary(response);
        }
    });
}

function cancelSoftReq() {
    window.location.href = '../FormLinks/FormLinks/';
}

function approveRequest() {
    //
    var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
        $('#lblSoftwareReqRequestId').val(),
        $('#txtRejectComments').val());

    approveSelectedRequest(requestDtoList);
}

function rejectRequest() {
    //  

    var requestDtoList = createRequestDtoList($('#lblVWMasterRequestId').val(),
        $('#lblSoftwareReqRequestId').val(),
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



